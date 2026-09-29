using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TinyPyIDE
{
    internal static class Program
    {

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        // 互斥体唯一ID，自定义一个GUID，保证全局唯一
        private const string UniqueMutexId = @"TinyPyIDE‑{8F62B4D2‑7135‑481A‑927C‑E22AB8612345}";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern uint RegisterWindowMessage(string lpString);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);
        private const int WM_OPENFILE = 0x0500;
        private const int WM_COPYDATA = 0x004A;
        private const int SW_RESTORE = 9;
        
        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, UniqueMutexId, out createdNew))
            {
                if (!createdNew)
                {
                    // 已有实例，激活旧窗口
                    Process current = Process.GetCurrentProcess();
                    foreach (var process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                        {
                            // 如果命令行带文件路径，发送消息通知旧实例打开文件
                            if (args.Length > 0)
                            {
                                string filePath = args[0];
                                // 使用 WM_COPYDATA 在进程间安全传递字符串
                                var bytes = (filePath.Length + 1) * 2; // Unicode 字节数
                                IntPtr dataPtr = Marshal.StringToHGlobalUni(filePath);
                                try
                                {
                                    var cds = new COPYDATASTRUCT();
                                    cds.dwData = IntPtr.Zero;
                                    cds.cbData = bytes;
                                    cds.lpData = dataPtr;
                                    SendMessage(process.MainWindowHandle, WM_COPYDATA, IntPtr.Zero, ref cds);
                                }
                                finally
                                {
                                    Marshal.FreeHGlobal(dataPtr);
                                }
                            }
                            ShowWindow(process.MainWindowHandle, SW_RESTORE);
                            SetForegroundWindow(process.MainWindowHandle);
                            break;
                        }
                    }
                    return;
                }

               //如果当前工作目录不是程序所在目录，则切换到程序所在目录
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string exeDir = Path.GetDirectoryName(exePath);
                if (exeDir != null && Directory.GetCurrentDirectory() != exeDir)
                {
                    Directory.SetCurrentDirectory(exeDir);
                }
                

                ApplicationConfiguration.Initialize();
                Form1 mainForm = new Form1();
                //获取命令行参数，即用户在命令行中输入的文件名，注意，不包含程序名本身
                if (args.Length > 0)
                {
                    string filePath = args[0];
                    if (File.Exists(filePath))
                        mainForm.StartupFile = filePath;
                }
                Application.Run(mainForm);
            }
        }
    }
}