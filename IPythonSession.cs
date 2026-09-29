using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TinyPyIDE
{
    public class IPythonSession : IDisposable
    {
        #region 状态枚举
        private enum SessionState
        {
            Initializing,
            Ready
        }
        #endregion

        #region 常量
        private const string StartArguments = "--simple-prompt";
        public const string InputPrompt = "In : ";
        public const string ContinuePrompt = "... : ";
        // 初始化最大等待时长 5秒
        private const int InitTimeoutMs = 5000;
        #endregion

        #region 进程变量
        private Process _ipythonProcess;
        private StreamWriter _stdin;
        private readonly StringBuilder _globalOutputBuffer = new StringBuilder();
        private bool _disposed = false;
        private SessionState _sessionState = SessionState.Initializing;
        private readonly object _bufferLock = new object();
        #endregion

        #region UI绑定
        private readonly RichTextBox _terminalRtb;
        private int _inputStartPos = 0;
        #endregion

        #region 回调
        private Action<string> AppendOutputAction;
        public Func<string> GetCompletionPrefixFunc;
        public Action<List<string>> CompletionReady;
        #endregion

        public IPythonSession(RichTextBox terminalRichTextBox)
        {
            _terminalRtb = terminalRichTextBox;
            AppendOutputAction = text =>
            {
                if (_terminalRtb.InvokeRequired)
                {
                    _terminalRtb.Invoke(AppendOutputAction, text);
                    return;
                }
                AppendTerminalText(text);
            };
        }

        public bool Start(string ipythonExePath = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = string.IsNullOrEmpty(ipythonExePath) ? "ipython" : ipythonExePath,
                Arguments = StartArguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            psi.EnvironmentVariables["PYTHONUTF8"] = "1";

            _ipythonProcess = new Process { StartInfo = psi };
            _ipythonProcess.OutputDataReceived += OnStdOutData;
            _ipythonProcess.ErrorDataReceived += OnStdErrData;

            bool ok = _ipythonProcess.Start();
            if (!ok) return false;

            _stdin = _ipythonProcess.StandardInput;
            _stdin.AutoFlush = true;

            _ipythonProcess.BeginOutputReadLine();
            _ipythonProcess.BeginErrorReadLine();

            // 启动独立监控线程：持续扫描缓冲区寻找提示符
            new Thread(() =>
            {
                DateTime start = DateTime.Now;
                while (DateTime.Now.Subtract(start).TotalMilliseconds < InitTimeoutMs)
                {
                    lock (_bufferLock)
                    {
                        string allText = _globalOutputBuffer.ToString();
                        // 判断缓冲区是否包含 In : （交互提示符）
                        if (allText.Contains($"{Environment.NewLine}{InputPrompt}") || allText.StartsWith(InputPrompt))
                        {
                            _sessionState = SessionState.Ready;
                            return;
                        }
                    }
                    Thread.Sleep(100);
                }
                // 超时兜底强制就绪，防止卡死
                _sessionState = SessionState.Ready;
                AppendOutputAction("\r\n【警告】初始化等待超时，强制进入交互模式\r\n");
            }).Start();

            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            _stdin?.Close();
            if (_ipythonProcess != null && !_ipythonProcess.HasExited)
            {
                _ipythonProcess.Kill();
                _ipythonProcess.WaitForExit(1000);
            }
            _ipythonProcess?.Dispose();
        }

        #region 终端文本操作
        private void AppendTerminalText(string text)
        {
            _terminalRtb.SelectionStart = _terminalRtb.TextLength;
            _terminalRtb.SelectedText = text;
            _terminalRtb.ScrollToCaret();

            // 只要已经就绪，任何提示符都更新锁定位置
            if (_sessionState == SessionState.Ready)
            {
                if (text.EndsWith(InputPrompt) || text.EndsWith(ContinuePrompt))
                {
                    RefreshInputLockPosition();
                }
            }
        }

        private void RefreshInputLockPosition()
        {
            _inputStartPos = _terminalRtb.TextLength;
        }

        public bool IsSelectionInReadOnlyArea()
        {
            int selStart = _terminalRtb.SelectionStart;
            int selLength = _terminalRtb.SelectionLength;
            int selEnd = selStart + selLength;

            if (selEnd > selStart && selStart < _inputStartPos)
                return true;
            if (selLength == 0 && selStart < _inputStartPos)
                return true;

            return false;
        }

        public string GetCurrentInputCode()
        {
            if (_terminalRtb.TextLength < _inputStartPos) return "";
            return _terminalRtb.Text.Substring(_inputStartPos);
        }
        #endregion

        #region 代码提交
        public void SubmitCurrentInput()
        {
            if (_sessionState != SessionState.Ready)
            {
                AppendOutputAction("\r\n[警告] IPython尚未初始化完成，请稍等...\r\n");
                return;
            }
            string code = GetCurrentInputCode();
            SubmitCode(code);
        }

        public void PushCodeFromEditor(string code)
        {
            if (_sessionState != SessionState.Ready)
            {
                AppendOutputAction("\r\n[警告] IPython尚未初始化完成，请稍等...\r\n");
                return;
            }
            AppendOutputAction($"\r\n{InputPrompt}{code}\r\n");
            SubmitCode(code);
        }

        private void SubmitCode(string code)
        {
            if (_stdin == null || _ipythonProcess.HasExited)
            {
                AppendOutputAction("\r\n[错误] IPython内核未运行！\r\n");
                if (_sessionState == SessionState.Ready)
                    AppendOutputAction($"{InputPrompt}");
                return;
            }
            _stdin.WriteLine(code);
        }
        #endregion

        #region Tab补全
        public void TriggerCompletion()
        {
            if (_sessionState != SessionState.Ready)
                return;

            string prefix = GetCompletionPrefixFunc?.Invoke() ?? GetCurrentInputCode();
            if (string.IsNullOrWhiteSpace(prefix)) return;

            new Thread(() =>
            {
                try
                {
                    string cmd = $"%complete {prefix}";
                    string result = ExecuteSilentCommand(cmd);
                    StringReader reader = new StringReader(result);
                    List<string> candidates = new List<string>();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            candidates.Add(line.Trim());
                    }
                    CompletionReady?.Invoke(candidates);
                }
                catch
                {
                    CompletionReady?.Invoke(new List<string>());
                }
            }).Start();
        }

        private string ExecuteSilentCommand(string cmd)
        {
            _stdin.WriteLine(cmd);
            Thread.Sleep(150);
            lock (_bufferLock)
            {
                string res = _globalOutputBuffer.ToString();
                _globalOutputBuffer.Clear();
                return res;
            }
        }
        #endregion

        #region 数据流接收
        private void OnStdOutData(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            string lineText = e.Data + Environment.NewLine;
            lock (_bufferLock)
            {
                _globalOutputBuffer.Append(lineText);
            }
            AppendOutputAction(lineText);
        }

        private void OnStdErrData(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            string errText = $"[stderr] {e.Data}{Environment.NewLine}";
            lock (_bufferLock)
            {
                _globalOutputBuffer.Append(errText);
            }
            AppendOutputAction(errText);
        }
        #endregion
    }
}
