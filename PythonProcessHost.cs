using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace TinyPyIDE
{
    public class PythonProcessHost : IDisposable
    {
        #region 通信协议常量（和Python桥保持一致）
        private const char MsgStart = '\x02';
        private const char MsgEnd = '\x03';
        #endregion

        #region 【对外回调事件，UI层订阅】
        /// <summary>桥进程启动就绪</summary>
        public Action ? OnBridgeReady { get; set; }

        /// <summary>收到stdout输出</summary>
        public Action<string> ? OnStdOut { get; set; }

        /// <summary>收到stderr输出（异常、报错）</summary>
        public Action<string> ? OnStdErr { get; set; }

        /// <summary>Python调用input()，需要前端提供用户输入</summary>
        /// 参数：input提示文本
        public Action<string> ? NeedUserInput { get; set; }

        /// <summary>一段代码执行完成</summary>
        /// bool = 是否执行成功无异常
        public Action<bool> ? CodeExecuted { get; set; }

        /// <summary>进程退出通知</summary>
        public Action ? OnProcessExit { get; set; }
        #endregion

        #region 私有成员
        private Process ? _pythonProcess;
        private StreamWriter ? _stdinWriter;
        private bool _disposed;
        private readonly string ? _pythonExe;
        private readonly string ? _bridgeScriptPath;
        private TinyPyLog tplog = null;


        // 接收缓冲区，分包重组
        private readonly StringBuilder _recvBuffer = new StringBuilder();
        private readonly object _recvLock = new object();
        #endregion

        #region 【构造器】
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="pythonExePath">python.exe路径，PATH存在直接填"python"</param>
        /// <param name="bridgeScriptFullPath">backend_bridge.py完整绝对路径</param>
        public PythonProcessHost(string pythonExePath, string bridgeScriptFullPath= "backend_bridge.py")
        {
            _pythonExe = pythonExePath;
            //需要把相对路径的backend_bridge.py转换为绝对路径
            //backend_bridge.py放在exe同级目录下，但是启动exe程序的工作目录可能不是exe所在目录，所以需要转换为绝对路径
            if (!Path.IsPathRooted(bridgeScriptFullPath))
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                bridgeScriptFullPath = Path.Combine(exeDir, bridgeScriptFullPath);
            }
            _bridgeScriptPath = bridgeScriptFullPath;

            if (tplog == null)
                tplog = new TinyPyLog();
        }
        #endregion

        #region 【启动python桥】
        /// <summary>启动Python桥进程</summary>
        public bool Start()
        {
            StopAndClean();

            var psi = new ProcessStartInfo
            {
                FileName = _pythonExe,
                Arguments = $"\"{_bridgeScriptPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            psi.EnvironmentVariables["PYTHONUTF8"] = "1";

            _pythonProcess = new Process { StartInfo = psi };
            _pythonProcess.Exited += OnProcessExited;

            bool ok = _pythonProcess.Start();
            if (!ok)
                return false;

            _stdinWriter = _pythonProcess.StandardInput;
            _stdinWriter.AutoFlush = true;

            // 异步监听stdout（JSON消息流）
            _readerThread = new Thread(ReadStdOutLoop)
            {
                IsBackground = true,
                Name = "PyStdOutReader"
            };
            _readerThread.Start();

            // stderr单独监听（桥本身崩溃信息）
            _pythonProcess.BeginErrorReadLine();
            _pythonProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    OnStdErr?.Invoke($"[Bridge Crash Log] {e.Data}{Environment.NewLine}");
            };

            return true;
        }
        #endregion

        #region 【对外接口，提交代码、文件执行或者提交用户的输入,还有退出桥进程】
        /// <summary>【对外核心接口】提交Python代码执行</summary>
        public void SubmitPythonCode(string code)
        {
            SendJsonMessage(new
            {
                type = "execute_code",
                code = code
            });
            tplog.WriteLog("execute_code: " + code);
        }

        public void SubmitPythonFile(string filePath)
        {
            SendJsonMessage(new
            {
                type = "execute_file",
                path = filePath
            });
            tplog.WriteLog("execute_file: " + filePath);
        }

        /// <summary>用户在终端输入完成，回复Python input()请求</summary>
        public void SubmitUserInput(string inputText)
        {
            SendJsonMessage(new
            {
                type = "submit_input",
                text = inputText
            });
            tplog.WriteLog("submit_input: " + inputText);
        }
        
        /// <summary>发送关闭指令，退出桥进程</summary>
        public void ShutdownBridge()
        {
            SendJsonMessage(new { type = "shutdown" });
        }

        public void KillProcess()
        {
            // 尝试优雅停止并清理，避免直接 Dispose 导致后台读取线程卡死
            try
            {
                // 先关闭 stdin 写入端，通知子进程
                try { _stdinWriter?.Close(); } catch { }

                // 使用 StopAndClean 来等待子进程退出并释放进程句柄
                StopAndClean();

                // 尝试关闭 stdout 流以解除可能阻塞的读取线程
                try { _pythonProcess?.StandardOutput?.BaseStream?.Close(); } catch { }

                if (tplog != null)
                {
                    tplog.Dispose();
                    tplog = null;
                }
            }
            catch { }
            finally
            {
                // 释放进程对象但不调用 Dispose() 导致额外的全局清理（由外部 Dispose 控制生命周期）
                if (_pythonProcess != null)
                {
                    try { _pythonProcess.Dispose(); } catch { }
                    _pythonProcess = null;
                }
                _stdinWriter = null;
            }
        }
        #endregion

        #region 底层消息发送
        /// <summary>序列化对象为JSON，添加首尾标记发送</summary>
        private void SendJsonMessage(object payload)
        {
            if (_pythonProcess == null || _pythonProcess.HasExited || _stdinWriter == null)
                return;

            string json = System.Text.Json.JsonSerializer.Serialize(payload,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            string packet = $"{MsgStart}{json}{MsgEnd}";

            try
            {
                _stdinWriter.Write(packet);
                _stdinWriter.Flush();
            }
            catch
            {
                // 管道断开忽略
            }
        }
        #endregion

        #region 接收循环 & 消息分包解析
        private Thread? _readerThread;

        private void ReadStdOutLoop()
        {
            byte[] buf = new byte[1024];
            Stream stream = _pythonProcess?.StandardOutput.BaseStream;

            while(!_disposed && _pythonProcess != null && !_pythonProcess.HasExited)
            {
                try
                {
                    int len = stream?.Read(buf, 0, buf.Length) ?? 0;
                    if (len <= 0) break;

                    string chunk = Encoding.UTF8.GetString(buf, 0, len);
                    FeedReceiveChunk(chunk);
                }
                catch
                {
                    break;
                }
            }
        }

        /// <summary>推入一段收到的数据，自动切分完整数据包</summary>
        private void FeedReceiveChunk(string chunk)
        {
            lock (_recvLock)
            {
                _recvBuffer.Append(chunk);

                // 循环切出所有完整包
                while (true)
                {
                    string all = _recvBuffer.ToString();
                    int startIdx = all.IndexOf(MsgStart);
                    int endIdx = all.IndexOf(MsgEnd, startIdx + 1);

                    if (startIdx < 0 || endIdx < 0)
                        break;

                    // 提取一条JSON
                    string jsonStr = all.Substring(startIdx + 1, endIdx - startIdx - 1);
                    // 缓冲区截断到包尾部之后
                    _recvBuffer.Remove(0, endIdx + 1);

                    DispatchMessage(jsonStr);
                }
            }
        }

        /// <summary>分发收到的JSON消息到对应回调</summary>
        private void DispatchMessage(string json)
        {
            try
            {
                var msg = System.Text.Json.JsonSerializer.Deserialize<MessageRoot>(json);
                if (msg == null || string.IsNullOrEmpty(msg.type))
                    return;

                switch (msg.type)
                {
                    case "bridge_ready":
                        OnBridgeReady?.Invoke();
                        break;
                    case "stdout":
                        OnStdOut?.Invoke(msg.data);
                        break;
                    case "stderr":
                        OnStdErr?.Invoke(msg.data);
                        break;
                    case "need_input":
                        NeedUserInput?.Invoke(msg.prompt ?? "");
                        break;
                    case "exec_done":
                        CodeExecuted?.Invoke(msg.success);
                        break;
                    case "exception":
                        OnStdErr?.Invoke(msg.data);
                        break;
                    case "expr_output":
                        OnStdOut?.Invoke(msg.data);
                        break;
                    case "exit":
                        OnProcessExit?.Invoke();
                        break;
                }
            }
            catch
            {
                // JSON解析失败直接丢弃脏数据
            }
        }

        // 消息模型
        private class MessageRoot
        {
            public string ? type { get; set; }
            public string ? data { get; set; }
            public string ? prompt { get; set; }
            public bool success { get; set; }
        }
        #endregion

        #region 进程释放
        private void OnProcessExited(object sender, EventArgs e)
        {
            OnProcessExit?.Invoke();
        }

        private void StopAndClean()
        {
            _stdinWriter?.Close();
            if (_pythonProcess != null)
            {
                if (!_pythonProcess.HasExited)
                {
                    try
                    {
                        ShutdownBridge();
                        _pythonProcess.WaitForExit(800);
                        if (!_pythonProcess.HasExited)
                            _pythonProcess.Kill();
                    }
                    catch { }
                }
                // 等待读取线程退出，避免后台线程继续访问已释放的流
                try
                {
                    if (_readerThread != null && _readerThread.IsAlive)
                    {
                        if (!_readerThread.Join(500))
                        {
                            try { _readerThread.Interrupt(); } catch { }
                        }
                    }
                }
                catch { }

                try { _pythonProcess.Dispose(); } catch { }
                _pythonProcess = null;
            }
            _stdinWriter = null;
            lock (_recvLock)
                _recvBuffer.Clear();
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
            //StopAndClean();  //这个似乎被重复调用
        }
        #endregion
    }
}