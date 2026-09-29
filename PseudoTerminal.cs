using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TinyPyIDE
{
    // 可复用的 Scintilla 编辑器控件，封装原来在 Form1 的 Scintilla 配置与事件逻辑
    public class PseudoTerminal : UserControl
    {
        private readonly Scintilla scintilla;
        //private readonly FunctionSignatureConfig config;
        //private readonly List<string> defaultCompletions;
        //private static readonly object s_lexerLock = new object();
        private readonly Form1 parentForm;
        internal PythonProcessHost  pyHost = new ("python");
        private int historyEndPosition = 0;  //定义历史边界位置，初始为0
        internal const string promptStr = "In : "; //系统提示符字符串
        internal const string exeFileStr = "%Run "; //执行文件提示符字符串
        internal string codePrompt = "";           //程序运行中显示的提示符字符串，默认为空
        internal RecordCommand rcmd = new ();  //用于记录用户输入的命令，包括在模拟终端输入的命令和通过菜单执行的Python程序

        public enum CurrentStatus   //判断当前终端状态
        {
            Interactive = 0,      //交互情况下等待用户输入 
            WaitingForInput = 1,  //执行代码等待用户输入
            ExecutingCode = 2,    //正在执行代码
            InvokeHistory = 3,    //正在调用历史记录
            Idle = 4              //空闲状态
        }
        internal CurrentStatus curSts = CurrentStatus.Interactive;                              


        // public string? FilePath { get; private set; }
        public bool IsDirty { get; private set; }

        //public event EventHandler? DocumentChanged;


        // 主构造函数，designTime 用于在设计时跳过会做 I/O 的初始化逻辑
        public PseudoTerminal(FunctionSignatureConfig config, Form1 parentForm, bool designTime = false)
        {
            this.parentForm = parentForm;

            // 在设计时避免执行可能的磁盘/环境 I/O
            if (!designTime)
            {
                try { config.LoadFromAppDirectory(); } catch { }
            }
            scintilla = new Scintilla();
            InitializeScintilla();
            this.Controls.Add(scintilla);

            // 订阅焦点相关事件，以便在用户把输入焦点移到终端时通知父窗体
            // 注意：点击菜单会把焦点移走，因此我们需要在控件实际获得焦点时记录
            scintilla.GotFocus += (s, e) => parentForm.NotifyEditorGotFocus("terminal");

            this.GotFocus += (s, e) => parentForm.NotifyEditorGotFocus("terminal");

            //启动通讯进程
            StartPythonProcess();
        }

        //启动通讯进程
        public void StartPythonProcess()
        {
            if (pyHost == null) //冗余设计
            {
                pyHost = new PythonProcessHost("python");
            }
            //为了能够重启python进程，把下面所有的事件订阅全部从“+=”改为“=”
            // 桥就绪
            pyHost.OnBridgeReady = () =>
            {
                AppendScintillaText("【系统】Python运行环境已就绪\n"+promptStr);
                curSts = CurrentStatus.Interactive;
            };

            // 标准输出
            pyHost.OnStdOut = text =>
            {
                AppendScintillaText(text);
                curSts = CurrentStatus.Interactive;
            };

            // 错误输出
            pyHost.OnStdErr = text =>
            {
                AppendScintillaText(text);
                curSts = CurrentStatus.Interactive;
            };

            // Python执行input()需要用户输入
            pyHost.NeedUserInput = prompt =>
            {
                AppendScintillaText(prompt);
                curSts = CurrentStatus.WaitingForInput;
                codePrompt = prompt; //记录当前提示符
            };

            // 代码执行完毕
            pyHost.CodeExecuted = success =>
            {
                AppendScintillaText("\n" + promptStr);
                curSts = CurrentStatus.Interactive;
            };
            pyHost.Start();
        }

        // ===================== 工具函数：线程安全写入Scintilla =====================
        void AppendScintillaText(string txt)
        {
            if (scintilla.InvokeRequired)
            {
                scintilla.Invoke(new Action(() => AppendScintillaText(txt)));
                return;
            }
            scintilla.AppendText(txt);
            scintilla.SelectionStart = scintilla.TextLength;
            scintilla.ScrollCaret();          //ScrollToCaret();
            historyEndPosition = scintilla.TextLength; //更新历史边界
        }

        private void Scintilla_KeyDown(object? sender, KeyEventArgs e)
        {
            //parentForm.SetStatusText($"Cursor: {scintilla.CurrentPosition}, HistoryEnd: {historyEndPosition}");
            if (IsInHistoryRegion())
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
                {
                    e.SuppressKeyPress = true; // 阻止 Scintilla 处理该键
                    e.Handled = true;
                    return;
                }
                else if (e.Control && (e.KeyCode == Keys.V || e.KeyCode == Keys.X))
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    return;
                }
            }
            else if (e.KeyCode == Keys.Back && scintilla.CurrentPosition == historyEndPosition)
            {   // 阻止在历史区域的最后一个位置删除，防止删除历史内容
                e.SuppressKeyPress = true;
                e.Handled = true;
                return;
            }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)  //向上或者向下键，获取历史命令
            {
                CommandEntry priorCommand;  
                if (e.KeyCode == Keys.Up)
                    priorCommand = rcmd.getPriorComm();
                else
                    priorCommand = rcmd.getNextComm();

                if (priorCommand != null)
                {
                    curSts = CurrentStatus.InvokeHistory;
                    //删除当前行内容
                    int currentLineIndex = scintilla.LineFromPosition(scintilla.CurrentPosition);
                    var currentLine = scintilla.Lines[currentLineIndex];
                    scintilla.DeleteRange(currentLine.Position, currentLine.Length);
                    //插入历史命令
                    string insertText;
                    if (priorCommand.IsExeFile)
                        insertText = promptStr + exeFileStr + priorCommand.Command;
                    else
                        insertText = promptStr + priorCommand.Command;
                    scintilla.InsertText(currentLine.Position, insertText);
                    scintilla.SelectionStart = currentLine.Position + insertText.Length;
                }
                e.SuppressKeyPress = true;
                e.Handled = true;
                return;
            }
            else if(e.KeyCode == Keys.Enter)  //在用户交互行输入了回车符
            {
                int lineIdx = scintilla.LineFromPosition(scintilla.CurrentPosition);
                var line = scintilla.Lines[lineIdx];
                scintilla.SelectionStart = line.Position + line.Length;  // 光标移动到行末
            }
        }

        private void Scintilla_KeyPress(object? sender, KeyPressEventArgs e)
        {
            //parentForm.SetStatusText($"Cursor: {scintilla.CurrentPosition}, HistoryEnd: {historyEndPosition}");
            if (IsInHistoryRegion())
            {
                //只有移动光标键可以在历史区域移动光标，其他输入都被阻止
                if (e.KeyChar != (char)Keys.Left && e.KeyChar != (char)Keys.Right && 
                    e.KeyChar != (char)Keys.Up && e.KeyChar != (char)Keys.Down )
                {
                    e.Handled = true; // 阻止在历史区域输入
                }
            }
            else
                if(curSts == CurrentStatus.InvokeHistory)
                {
                    //如果当前状态是调用历史命令，则在用户输入任何字符后，切换回交互式状态
                    curSts = CurrentStatus.Interactive;
                }
        }

        private void Scintilla_KeyUp(object? sender, KeyEventArgs e)
        {
            // 此时的 CurrentPosition 已经是移动后的真实值
            parentForm.SetStatusText($"Cursor: {scintilla.CurrentPosition}, HistoryEnd: {historyEndPosition}");
        }


        private void InitializeScintilla()
        {
            
            scintilla.Dock = DockStyle.Fill;
            try
            {
                // 基本样式与 lexer
                scintilla.LexerName = "python";
                scintilla.Margins[0].Type = MarginType.Symbol;
                scintilla.Margins[0].Width = 50;  // 边框宽度，与上面显示行号的对齐，更美观
                
                SetupPythonStyling();
                ClearSomeKeys();
                scintilla.CharAdded += Scintilla_CharAdded;
                scintilla.KeyDown += Scintilla_KeyDown;   //准备在历史区域内屏蔽按键
                scintilla.KeyPress += Scintilla_KeyPress;
                scintilla.KeyUp += Scintilla_KeyUp;

                //屏蔽默认右键菜单，改为自定义菜单
                scintilla.ContextMenuStrip = new ContextMenuStrip();
            }
            catch
            {
                MessageBox.Show("警告","无法正常设置Python终端，请检查Python是否正确安装",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearSomeKeys()
        {
            try
            {
                scintilla.ClearCmdKey(Keys.Control | Keys.B);
                scintilla.ClearCmdKey(Keys.Control | Keys.D);
                scintilla.ClearCmdKey(Keys.Control | Keys.Q);
                scintilla.ClearCmdKey(Keys.Control | Keys.W);
                scintilla.ClearCmdKey(Keys.Control | Keys.E);
                scintilla.ClearCmdKey(Keys.Control | Keys.R);
                scintilla.ClearCmdKey(Keys.Control | Keys.T);
                scintilla.ClearCmdKey(Keys.Control | Keys.O);
                scintilla.ClearCmdKey(Keys.Control | Keys.P);
                scintilla.ClearCmdKey(Keys.Control | Keys.F);
                scintilla.ClearCmdKey(Keys.Control | Keys.G);
                scintilla.ClearCmdKey(Keys.Control | Keys.H);
                scintilla.ClearCmdKey(Keys.Control | Keys.J);
                scintilla.ClearCmdKey(Keys.Control | Keys.K);
                scintilla.ClearCmdKey(Keys.Control | Keys.L);
                scintilla.ClearCmdKey(Keys.Control | Keys.M);
                scintilla.ClearCmdKey(Keys.Control | Keys.S);
                scintilla.ClearCmdKey(Keys.Control | Keys.N);
                scintilla.ClearCmdKey(Keys.Control | Keys.Z);
            }
            catch { }
        }


        private void SetupPythonStyling()
        {
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = "Consolas";
            scintilla.Styles[Style.Default].Size = 14;
            scintilla.Styles[Style.Default].ForeColor = Color.Black;
            scintilla.StyleClearAll();

            scintilla.SetKeywords(0, string.Join(" ", FunctionSignatureConfig.PythonKeywordArray));

            scintilla.Styles[ScintillaNET.Style.Python.Default].ForeColor = Color.Black;
            scintilla.Styles[ScintillaNET.Style.Python.CommentLine].ForeColor = Color.FromArgb(122, 122, 122);
            scintilla.Styles[ScintillaNET.Style.Python.Number].ForeColor = Color.Olive;
            scintilla.Styles[ScintillaNET.Style.Python.String].ForeColor = Color.FromArgb(34, 139, 34);
            scintilla.Styles[ScintillaNET.Style.Python.Character].ForeColor = Color.Brown;
            scintilla.Styles[ScintillaNET.Style.Python.Word].ForeColor = Color.Blue;
            scintilla.Styles[ScintillaNET.Style.Python.Triple].ForeColor = Color.Gray;
            scintilla.Styles[ScintillaNET.Style.Python.ClassName].ForeColor = Color.Teal;
            scintilla.Styles[ScintillaNET.Style.Python.Operator].ForeColor = Color.Purple;
            scintilla.Styles[ScintillaNET.Style.Python.Identifier].ForeColor = Color.Black;
        }


        // 用户换行，要将数据发送给Python解释器，返回 true 表示处理并应结束事件处理
        // 能进入到这个函数，必定在历史区域之外，否则在前面就会被拦截
        private bool HandleNewLineIndent()
        {

            if (scintilla == null) return false;  //防御性编程，scintilla 为空时直接返回 false

            int pos = scintilla.CurrentPosition;
            int line = scintilla.LineFromPosition(pos) - 1;  //已经换到了下一行，所以要减1
            string currentLineText = scintilla.Lines[line].Text ?? string.Empty;

            if (currentLineText.Length <= 0) return true;
            
            if (currentLineText == promptStr) //如果等于提示符，则可以直接返回了
            {
                AppendScintillaText(promptStr);
                curSts = CurrentStatus.Interactive;
                return true;
            }


            if (curSts == CurrentStatus.WaitingForInput)  //程序运行中等待用户输入
            {
                //如果当前行以用户提示开头，则去掉提示再发送给Python解释器
                string userInput;
                //查找是否包含codePrompt，如果包含则去掉codePrompt，否则直接使用当前行文本
                userInput = currentLineText.StartsWith(codePrompt) ? currentLineText[codePrompt.Length..] : currentLineText;
                userInput = userInput.Trim('\r', '\n');
                pyHost.SubmitUserInput(userInput);
                return true;
            }
            else if (curSts == CurrentStatus.InvokeHistory || curSts == CurrentStatus.Interactive)  //正在调用历史命令，可能是执行程序文件，也可能是执行命令
            {
                string userInput;
                if (currentLineText.StartsWith(promptStr + exeFileStr)) //说明这一行是执行文件
                {
                    userInput = currentLineText[(promptStr.Length + exeFileStr.Length)..];    //取出文件名
                    userInput = userInput.Trim();
                    if (userInput.Length > 0)
                    {
                        pyHost.SubmitPythonFile(userInput);
                        rcmd.addCommand(userInput, true);  // 将执行文件的命令添加到记录中，并标记为执行文件
                    }
                    else
                        AppendScintillaText(promptStr);
                }
                else if (currentLineText.StartsWith(promptStr))      //这一行是执行命令
                {
                    userInput = currentLineText.Substring(promptStr.Length);
                    userInput = userInput.Trim();
                    if (userInput.Length > 0)
                    {
                        pyHost.SubmitPythonCode(userInput);
                        rcmd.addCommand(userInput, false);  // 将用户输入的命令添加到记录中，并标记为执行命令
                    }
                    else
                        AppendScintillaText(promptStr);
                }
                return true;
            }
            return false;   //无法处理输入
        }

        // 执行指定文件的代码，返回 true 表示处理完毕
        public void ExcuteFile(string FileName)
        {
            //先判断当前状态，如果不是交互式状态，则不允许执行文件
            if (curSts != CurrentStatus.Interactive)
                return;
            curSts = CurrentStatus.ExecutingCode;   //切换状态为正在执行代码
            //先将伪终端最后一行清空，并显示要运行的文件名
            int lastLineIndex = scintilla.Lines.Count - 1;
            if (lastLineIndex >= 0)
            {
                var ln = scintilla.Lines[lastLineIndex];
                scintilla.DeleteRange(ln.Position, ln.Length);
            }
            scintilla.AppendText(promptStr + exeFileStr + FileName+ "\n");
            rcmd.addCommand(FileName, true);
            pyHost.SubmitPythonFile(FileName);
        }

        // 处理配对符号自动补全
        private bool HandleAutoPair(char opener)
        {
            int pos = scintilla.CurrentPosition;
            char closer = opener switch
            {
                '(' => ')',
                '[' => ']',
                '"' => '"',
                '\'' => '\'',
                '{' => '}',
                _ => '\0'
            };

            try
            {
                    scintilla.InsertText(pos, closer.ToString());
                    scintilla.CurrentPosition = pos;
                    scintilla.SelectionStart = pos;
                    scintilla.SelectionEnd = pos;
            }
            catch { }
            return true;
        }

  


        //处理回车符和括号匹配
        private void Scintilla_CharAdded(object? sender, CharAddedEventArgs e)
        {
            char ch = (char)e.Char;
            if (ch == '\n')
            {
                if (HandleNewLineIndent()) return;
                //HandleNewLineIndent();
            }

            if (ch == '(' || ch == '[' || ch == '"' || ch == '\'' || ch == '{')
            {
                if (HandleAutoPair(ch)) return;
            }
        }

        private bool IsInHistoryRegion()
        {
            return scintilla.CurrentPosition < historyEndPosition;
        }

        public void StopAndRestart()
        {
            if (pyHost != null)
            {
                pyHost.OnProcessExit = null;
                pyHost.KillProcess();
                pyHost = null;
                curSts = CurrentStatus.Interactive;
                // 杀掉进程后重新启动
                StartPythonProcess();
            }
        }


    }

}
