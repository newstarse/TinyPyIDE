using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO; // <- 如果还没有
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

//using ConEmu.WinForms;
namespace TinyPyIDE
{
    public partial class Form1 : Form
    {
        // 启动时要延后打开的文件列表（由 Program.Main 填充），在 Form1_Load 中打开，它的访问权限应该是internal，以便 Program.Main 可以访问
        internal string StartupFile = "";
        private readonly List<string> defaultCompletions;

        private readonly Dictionary<string, string> functionSignatures;  // 从配置文件加载的函数签名映射（不区分大小写）
        private readonly Dictionary<string, Dictionary<string, string>> classSignatures; // 类 -> (成员 -> 签名)
        // 共享的配置与 lexilla 路径，用于创建每个编辑器实例
        private FunctionSignatureConfig? sigConfig;
        private string? lexillaPath;
        private int newDocCounter = 1;
        // 存储每个标签关闭按钮的位置（由 DrawItem 计算并缓存）
        private readonly Dictionary<int, Rectangle> tabCloseRects = new();
        // 保存伪终端控件的引用，以便在运行代码时访问
        private PseudoTerminal? terminal;
        // 记录最后获得输入焦点的编辑控件（用于菜单命令路由）
        private string? lastFocusedEditorControl;
        //处理配置文件
        private IniConfig? iniConfig = null;
        //处理最近打开的文件菜单
        private HandleRecentFiles? recentFilesHandler = null;
        //查找替换对话框
        private FindReplaceForm? findReplaceDialog = null;

        public void LoadIniConfig()
        {
            iniConfig = IniConfig.Instance;
            iniConfig.SetFilePath("TinyPy.ini");
            iniConfig.Load();
        }

        // PseudoTerminal 或 ScintillaEditor 在获得输入焦点时会调用此方法，
        // Form1 记录最后活跃的编辑控件以便菜单命令路由使用。
        public void NotifyEditorGotFocus(string editorControl)
        {
            lastFocusedEditorControl = editorControl;
        }

        public void SaveIniConfig()
        {
            iniConfig?.Save();
        }

        public void BindButtonClick()
        {
            runButton.Click += (s, e) => 运行程序ToolStripMenuItem_Click(s, e);
            quitButton.Click += (s, e) => 退出ToolStripMenuItem_Click(s, e);
            openButton.Click += (s, e) => 打开ToolStripMenuItem_Click(s, e);
            newFileButton.Click += (s, e) => 新建ToolStripMenuItem_Click(s, e);
            saveButton.Click += (s, e) => 保存ToolStripMenuItem_Click(s, e);
            stopButton.Click += (s, e) => 终止重启ToolStripMenuItem_Click(s, e);
        }

        public void InitRecentFiles()
        {
            recentFilesHandler = HandleRecentFiles.Instance;
            // 从配置读取时返回的是按 menuN 升序的列表（menu1, menu2, ...）
            // 由于 AddRecentFile 会把新项插入到列表开头，为了保持最终列表顺序与 menuN 一致，
            // 需要反向遍历配置中的列表（先插入 menuN 再插入 menu1）
            var recentFromIni = iniConfig.GetRecentFileNames();
            for (int i = recentFromIni.Count - 1; i >= 0; i--)
            {
                recentFilesHandler.AddRecentFile(recentFromIni[i]);
            }
        }

        public void InitRecentFilesMenu()
        {
            if (recentFilesHandler == null) return;
            最近的文件ToolStripMenuItem.DropDownItems.Clear();
            var recentFiles = recentFilesHandler.GetShowNames();
            foreach (var showName in recentFiles)
            {
                var item = new ToolStripMenuItem(showName);
                item.Click += (s, args) =>
                {
                    // 根据显示名称找到对应的完整路径
                    var fullPath = recentFilesHandler.GetFullName(showName);
                    if (fullPath != null)
                    {
                        // 打开文件
                        OpenFileInEditor(fullPath);
                    }
                };
                最近的文件ToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        // helper to get active editor
        private ScintillaEditor? GetActiveEditor()
        {
            if (tabControl1.SelectedTab == null) return null;
            return tabControl1.SelectedTab.Controls.OfType<ScintillaEditor>().FirstOrDefault();
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // 在窗体关闭时复用退出菜单的逻辑
            // 如果退出操作被取消（用户在保存提示中选择 Cancel），则阻止窗体关闭
            var editors = new List<ScintillaEditor>();
            foreach (TabPage p in tabControl1.TabPages)
            {
                var ed = p.Controls.OfType<ScintillaEditor>().FirstOrDefault();
                if (ed != null) editors.Add(ed);
            }

            foreach (var ed in editors)
            {
                if (!ed.CloseEditor())
                {
                    e.Cancel = true;
                    return;
                }
            }

            //把配置写回到ini文件中去
            SaveConfigFile();
        }

        private void SaveConfigFile()
        {
            //先把recentFileName放到iniConfig对象里面，然后再保存
            iniConfig.UpdateRecentFileNames(recentFilesHandler.GetFullNames());
            iniConfig.Save();  //
        }

        //修改状态栏的值，主要用于调试
        public void SetStatusText(string text)
        {
            toolStripStatusLabel1.Text = text;
        }

        public Form1()
        {
            InitializeComponent();
            LoadIniConfig();    // 加载配置文件
            InitRecentFiles();  // 初始化最近打开的文件列表，这两行不可交换
            // 准备 lexilla 动态库路径并创建首个编辑器标签
            string extPath = "runtimes\\win-x64\\native";
            // 将结果赋值给类字段 lexillaPath（不要用 var 声明新的局部变量）
            lexillaPath = Path.Combine(AppContext.BaseDirectory, extPath, "Lexilla.dll");

            // 初始化默认补全词表（供全局用途）
            defaultCompletions = (FunctionSignatureConfig.PythonKeywordArray.Concat(FunctionSignatureConfig.PythonBuiltins)).ToList();
            // 加载函数签名配置并保存共享实例
            sigConfig = new FunctionSignatureConfig();
            sigConfig.LoadFromAppDirectory();
            functionSignatures = sigConfig.Signatures;
            classSignatures = sigConfig.ClassSignatures;

            // 不在构造函数中立即创建编辑器，改为在窗体加载完成后创建，避免与 Designer 中的占位 tab 冲突
            this.Load += Form1_Load;
            // 窗口关闭（右上角 X）也要走退出检查逻辑
            this.FormClosing += Form1_FormClosing;
            tabControl1.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            // 使用固定大小的标签以便我们可以按需调整宽度
            tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            // 设定一个合理的初始宽度
            tabControl1.ItemSize = new Size(200, tabControl1.ItemSize.Height);
            tabControl1.DrawItem += TabControl1_DrawItem;
            tabControl1.MouseDown += TabControl1_MouseDown;
            // 初始化最近文件菜单
            InitRecentFilesMenu();
            // 绑定工具栏按钮点击事件
            BindButtonClick();
            tabControl1.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            // 启用窗口拖放打开文件功能
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;
        }

        private void Form1_DragEnter(object? sender, DragEventArgs e)
        {
            // 只接受文件拖放，且至少包含一个 .py 文件
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!
;
                if (files.Any(f => string.Equals(Path.GetExtension(f), ".py", StringComparison.OrdinalIgnoreCase)))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void Form1_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null) return;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            foreach (var f in files)
            {
                try
                {
                    if (File.Exists(f) && string.Equals(Path.GetExtension(f), ".py", StringComparison.OrdinalIgnoreCase))
                    {
                        OpenFileInEditor(f);
                        recentFilesHandler?.AddRecentFile(f);
                    }
                }
                catch { }
            }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (findReplaceDialog == null) return;
            var Editor = GetActiveEditor();
            if (Editor == null) return;
            findReplaceDialog.SetEditor(Editor.ScintillaControl);
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // 创建首个编辑器标签
            if (StartupFile=="")
            {
                var _ = CreateNewTab();
            }
            else
            {
                OpenFileInEditor(StartupFile);
                recentFilesHandler.AddRecentFile(StartupFile);
            }

            // 创建伪终端控件并放置在下方面板
            try
            {
                terminal = new PseudoTerminal(sigConfig, this);
                //terminal.Dock = DockStyle.Fill; //这行代码反而会导致终端控件被 statusStrip 遮挡，改为手动调整大小
                splitContainer1.Panel2.Controls.Add(terminal);
                HookTerminalLayoutAdjust();
             }
            catch
            {
                //获取当前工作目录
                string currentDir = Environment.CurrentDirectory;
                //获取当前程序所在目录
                string exeDir = AppContext.BaseDirectory;
                MessageBox.Show("无法创建终端控件，请检查配置文件或运行环境。当前路径：" + currentDir + "，程序路径：" + exeDir,
                                "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //另一个进程通过WM_COPYDATA发送文件路径时接收并打开（用于单实例传参），不会被代码显式调用
        protected override void WndProc(ref Message m)
        {
            const int WM_COPYDATA = 0x004A;
            if (m.Msg == WM_COPYDATA)
            {
                try
                {
                    // COPYDATASTRUCT在内存中为: dwData (IntPtr), cbData (int), lpData (IntPtr)
                    var cds = Marshal.PtrToStructure<CopyDataStruct>(m.LParam);
                    if (cds.lpData != IntPtr.Zero && cds.cbData > 0)
                    {
                        string filePath = Marshal.PtrToStringUni(cds.lpData);
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            // 在 UI 线程上打开文件并更新最近文件
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                try
                                {
                                    OpenFileInEditor(filePath);
                                    recentFilesHandler?.AddRecentFile(filePath);
                                }
                                catch 
                                {
                                    MessageBox.Show("无法打开文件: " + filePath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }));
                        }
                    }
                }
                catch
                { 
                    MessageBox.Show("接收文件路径时发生错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            base.WndProc(ref m);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        // 在 TabControl 中创建一个新标签并加入 ScintillaEditor，返回新创建的编辑器实例
        private ScintillaEditor CreateNewTab(string? title = null)
        {
            var name = title ?? $"无标题-{newDocCounter++}";
            TabPage page;
            // 如果 Designer 中存在一个占位的空 tab（例如 tabPage1），复用它以避免出现空白不可编辑的首个页签
            bool reused = false;
            if (tabControl1.TabPages.Count == 1 && tabControl1.TabPages[0].Controls.Count == 0)
            {
                page = tabControl1.TabPages[0];
                page.Text = name;
                page.Controls.Clear(); // 确保为空
                reused = true;
            }
            else
            {
                page = new TabPage(name);
            }

            if (sigConfig == null)
            {
                sigConfig = new FunctionSignatureConfig();
                sigConfig.LoadFromAppDirectory();
            }

            var editor = new ScintillaEditor(sigConfig, lexillaPath);
            editor.Dock = DockStyle.Fill;

            // 文档更改时在标签名后加 * 标识未保存
            editor.DocumentChanged += (s, e) =>
            {
                if (!page.Text.EndsWith('*')) page.Text = page.Text + "*";
            };

            page.Controls.Add(editor);
            if (!reused)
            {
                tabControl1.TabPages.Add(page);
            }
            tabControl1.SelectedTab = page;
            // 强制重绘以更新关闭按钮位置缓存
            tabControl1.Invalidate();
            editor.Focus();
            return editor;
        }

        private void TabControl1_DrawItem(object? sender, DrawItemEventArgs e)
        {
            var g = e.Graphics;
            var tab = tabControl1.TabPages[e.Index];
            var rect = tabControl1.GetTabRect(e.Index);
            rect.Inflate(-2, -2);

            // 计算关闭按钮矩形（右侧小 X）
            var closeSize = new Size(12, 12);
            var closeRect = new Rectangle(rect.Right - closeSize.Width - 6, rect.Top + (rect.Height - closeSize.Height) / 2, closeSize.Width, closeSize.Height);

            // 背景
            var isSelected = (tabControl1.SelectedIndex == e.Index);
            var backColor = isSelected ? SystemColors.ControlLightLight : SystemColors.Control;
            using (var b = new SolidBrush(backColor)) g.FillRectangle(b, rect);

            // 文本区域：确保文本不覆盖关闭按钮，使用省略号裁剪
            var textRect = new Rectangle(rect.Left + 4, rect.Top + 2, closeRect.Left - (rect.Left + 8), rect.Height - 4);
            TextRenderer.DrawText(g, tab.Text, this.Font, textRect, SystemColors.ControlText, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // 缓存用于 MouseDown 检测（使用相对于 tabControl 的坐标）
            tabCloseRects[e.Index] = closeRect;

            // 绘制关闭 X
            using (var p = new Pen(Color.DarkGray, 2))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawLine(p, closeRect.Left + 2, closeRect.Top + 2, closeRect.Right - 2, closeRect.Bottom - 2);
                g.DrawLine(p, closeRect.Right - 2, closeRect.Top + 2, closeRect.Left + 2, closeRect.Bottom - 2);
            }
        }

        private void TabControl1_MouseDown(object? sender, MouseEventArgs e)
        {
            // 直接根据当前标签的绘制计算关闭按钮区域，避免使用可能过时的缓存索引
            var closeSize = new Size(12, 12);
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                var rect = tabControl1.GetTabRect(i);
                rect.Inflate(-2, -2);
                var closeRect = new Rectangle(rect.Right - closeSize.Width - 6, rect.Top + (rect.Height - closeSize.Height) / 2, closeSize.Width, closeSize.Height);
                if (closeRect.Contains(e.Location))
                {
                    // 触发关闭逻辑：先让对应编辑器做保存确认
                    if (i < 0 || i >= tabControl1.TabPages.Count) return; // 额外保护
                    var page = tabControl1.TabPages[i];
                    var ed = page.Controls.OfType<ScintillaEditor>().FirstOrDefault();
                    bool didClose;

                    if (ed != null)
                    {
                        // ScintillaEditor.CloseEditor 会尝试移除其宿主 TabPage
                        didClose = ed.CloseEditor();
                    }
                    else
                    {
                        // 没有编辑器控件时直接移除标签
                        tabControl1.TabPages.RemoveAt(i);
                        didClose = true;
                    }

                    if (didClose)
                    {
                        // 如果 CloseEditor 没有移除 tab（极少见），确保移除它以避免残留
                        if (tabControl1.TabPages.Contains(page))
                        {
                            tabControl1.TabPages.Remove(page);
                        }

                        // 清理缓存中可能存在的旧索引
                        if (tabCloseRects.ContainsKey(i)) tabCloseRects.Remove(i);

                        // 如果没有标签则创建一个空标签保证可编辑状态
                        if (tabControl1.TabPages.Count == 0)
                        {
                            //CreateNewTab();
                        }
                    }

                    // 点击在关闭按钮上，不再进一步处理
                    return;
                }
            }
        }

        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewTab();
        }

        private void 关闭ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var editor = GetActiveEditor();
            if (editor == null) return;

            // 尝试关闭当前编辑器，若用户取消则不做任何操作
            var closed = editor.CloseEditor();
            if (!closed) return;

            // 如果关闭后没有剩余标签，则新建一个空标签以保持可编辑状态
            if (tabControl1.TabPages.Count == 0)
            {
                //CreateNewTab("Untitled");
            }
        }

        private void 退出ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // 在退出前逐个询问每个编辑器是否需要保存。
            // 收集当前所有编辑器实例的快照，避免在遍历时被移除的TabPage干扰枚举。
            var editors = new List<ScintillaEditor>();
            foreach (TabPage p in tabControl1.TabPages)
            {
                var ed = p.Controls.OfType<ScintillaEditor>().FirstOrDefault();
                if (ed != null) editors.Add(ed);
            }

            // 逐个调用CloseEditor，这将处理未保存提示并在确认后移除对应标签。
            foreach (var ed in editors)
            {
                // 如果用户取消保存，则中断退出流程
                if (!ed.CloseEditor())
                {
                    return;
                }
            }

            // 所有编辑器都已关闭或保存，安全退出应用
            this.Close();
        }

        private void 打开ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Python Files|*.py|All Files|*.*";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                OpenFileInEditor(dlg.FileName);
                recentFilesHandler.AddRecentFile(dlg.FileName);
            }
        }

        public void OpenFileInEditor(string path)
        {
            // 如果该文件已在某个编辑器中打开，则切换到对应标签
            foreach (TabPage p in tabControl1.TabPages)
            {
                var ed = p.Controls.OfType<ScintillaEditor>().FirstOrDefault();
                if (ed != null && !string.IsNullOrEmpty(ed.FilePath) && string.Equals(ed.FilePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    tabControl1.SelectedTab = p;
                    return;
                }
            }

            // 否则新建一个标签并在其中打开文件
            var newEditor = CreateNewTab(Path.GetFileName(path));
            try
            {
                newEditor.LoadFile(path);
                UpdateTabTitle(newEditor);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开文件失败: " + ex.Message, "打开", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void 保存ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var editor = GetActiveEditor();
            if (editor == null) return;
            // 仅在内容已修改（IsDirty）时才执行保存
            if (!editor.IsDirty) return;


            if (editor.Save())
            {
                UpdateTabTitle(editor);
                recentFilesHandler.AddRecentFile(editor.FilePath);
            }
            else
            {
                MessageBox.Show("保存失败", "保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void 另存为ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var editor = GetActiveEditor();
            if (editor == null) return;
            if (editor.SaveAs())
            {
                UpdateTabTitle(editor);
                recentFilesHandler.AddRecentFile(editor.FilePath);
            }
        }

        // 根据编辑器的FilePath更新所属标签页标题（去掉末尾的 *）
        private void UpdateTabTitle(ScintillaEditor editor)
        {
            var page = tabControl1.SelectedTab;
            if (page == null) return;
            // 使用文件名作为标题；如果没有文件路径则保留原有标题
            if (!string.IsNullOrEmpty(editor.FilePath))
            {
                page.Text = Path.GetFileName(editor.FilePath);
            }
            else
            {
                // 对于未命名文档，如果存在 '*' 标记则保留，否则不改变
                if (!page.Text.EndsWith('*')) page.Text = page.Text;
            }
        }

        //运行当前编辑器中的Python代码
        private void 运行程序ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 获取当前活动编辑器
            var editor = GetActiveEditor();
            bool result = false;
            if (editor == null) return;
            //如果改动后未保存，则自动保存
            if (editor.IsDirty)
            {
                result = editor.Save();
                if (result) UpdateTabTitle(editor);
            }
            else  //如果没有改动，还要判断当前是否是一个空文件
            {
                if (editor.FilePath == null)
                    return;
                else
                    result = true;
            }

            if (result)
            {
                //调用PseudoTerminal中的方法运行代码
                if (terminal != null && editor.FilePath != null)
                {
                    terminal.ExcuteFile(editor.FilePath);
                    terminal.Focus();  //运行后将焦点切换到终端控件
                }
            }
        }

        // 由于terminal控件的最后一行可能被statusStrip遮挡，所以需要在窗体大小改变、splitContainer分割条移动时重新调整terminal控件的大小
        void HookTerminalLayoutAdjust()
        {
            // 在这些事件发生时重新调整终端大小
            splitContainer1.Panel2.SizeChanged += (s, e) => AdjustTerminalBounds();
            splitContainer1.SplitterMoved += (s, e) => AdjustTerminalBounds();
            this.Resize += (s, e) => AdjustTerminalBounds();

            // 立即执行一次以初始化正确大小
            AdjustTerminalBounds();
        }

        void AdjustTerminalBounds()
        {
            if (splitContainer1 == null) return;
            var panel = splitContainer1.Panel2;

            // 计算终端应该占用的区域（保留 statusStrip 的高度）
            int sbHeight = statusStrip1?.Height ?? 0;
            int w = panel.ClientSize.Width;
            int h = Math.Max(0, panel.ClientSize.Height - sbHeight) - 5;

            if (terminal != null && !terminal.IsDisposed)
            {
                terminal.SetBounds(0, 0, w, h);
            }
        }

        //如果当前程序正在运行，则终止运行并重启后端python解释器
        private void 终止重启ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (terminal != null)
                terminal.StopAndRestart();
        }

        //启动系统终端（cmd.exe）并进入当前编辑器所在目录
        private void 在终端中运行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var editor = GetActiveEditor();
            if (editor == null) return;

            if (editor.FilePath != null)
            {
                var directory = Path.GetDirectoryName(editor.FilePath);
                //启动python解释器并执行当前文件
                System.Diagnostics.Process.Start("cmd.exe", $"/k cd /d {directory} && python {editor.FilePath}");
            }
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("TinyPyIDE\n版本 0.1\n\n作者：刘新\n邮箱: liuxin@xtu.edu.cn\n2026-9", "关于 TinyPyIDE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void 进入PythonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var editor = GetActiveEditor();
            if (editor == null) return;
            var directory = Environment.CurrentDirectory;       //获取当前工作目录
            if (editor.FilePath != null)
                directory = Path.GetDirectoryName(editor.FilePath);
            //启动python解释器并进入当前文件所在目录
            System.Diagnostics.Process.Start("cmd.exe", $"/k cd /d {directory} && python");
        }


        // 在用户展开“最近的文件”菜单时触发，确保菜单项在展开时刷新
        private void 最近的文件ToolStripMenuItem_DropDownOpening(object? sender, EventArgs e)
        {
            if (recentFilesHandler != null && recentFilesHandler.IsChanged)
            {
                InitRecentFilesMenu();
            }
        }
        //展开二级菜单项，显示最近打开的文件列表
        private void 最近的文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (recentFilesHandler != null && recentFilesHandler.IsChanged)
                InitRecentFilesMenu();
        }

        private void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //先判断输入焦点是否在编辑器中，如果是，则调用编辑器的复制方法
            var editor = GetActiveEditor();
            if (editor == null) return;
            if (lastFocusedEditorControl == "editor")
            {
                editor.Copy();
            }
        }

        private void 剪切ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //先判断输入焦点是否在编辑器中，如果是，则调用编辑器的剪切方法
            var editor = GetActiveEditor();
            if (editor == null) return;
            if (lastFocusedEditorControl == "editor")
            {
                editor.Cut();
            }
        }

        private void 粘贴ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //先判断输入焦点是否在编辑器中，如果是，则调用编辑器的粘贴方法
            var editor = GetActiveEditor();
            if (editor == null) return;
            if (lastFocusedEditorControl == "editor")
            {
                editor.Paste();
            }
        }

        private void 全选ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //先判断输入焦点是否在编辑器中，如果是，则调用编辑器的全选方法
            var editor = GetActiveEditor();
            if (editor == null) return;
            if (lastFocusedEditorControl == "editor")
            {
                editor.SelectAll();
            }
        }

        private void 撤销ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //先判断输入焦点是否在编辑器中，如果是，则调用编辑器的撤销方法
            var editor = GetActiveEditor();
            if (editor == null) return;
            if (lastFocusedEditorControl == "editor")
            {
                editor.Undo();
            }
        }

        private void 重做ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //先判断输入焦点是否在编辑器中，如果是，则调用编辑器的重做方法
            var editor = GetActiveEditor();
            if (editor == null) return;
            if (lastFocusedEditorControl == "editor")
            {
                editor.Redo();
            }
        }

        private void 查找替换ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var editor = GetActiveEditor();
            if (editor == null) return;
            if (lastFocusedEditorControl != "editor") return;

            findReplaceDialog = new FindReplaceForm();
            findReplaceDialog.FormClosed += (s, e) =>
            {
                findReplaceDialog = null;
            };
            findReplaceDialog.SetEditor(editor.ScintillaControl);

            //对话窗没有显示在主窗口的中间，需要调整位置，手动居中到主窗体
            int x = this.Left + (this.Width - findReplaceDialog.Width) / 2;
            int y = this.Top + (this.Height - findReplaceDialog.Height) / 2;
            findReplaceDialog.Location = new Point(x, y);
            findReplaceDialog.Show(this);  // 将 Form1 作为父窗体，确保对话框在主窗体前面
            findReplaceDialog.BringToFront();
            findReplaceDialog.txtFind.Focus();  // 打开后将焦点设置到查找文本框
        }

        private void 字体ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontDialog fontDlg = new FontDialog();

            // 可选配置
            fontDlg.ShowColor = false;          //不显示颜色选择
            fontDlg.AllowVerticalFonts = false; //不允许垂直字体
            fontDlg.AllowScriptChange = false;  //不允许脚本更改
            fontDlg.Font = iniConfig.GetFont(); //默认使用当前字体

            if (fontDlg.ShowDialog() == DialogResult.OK)
            {
                // 用户确认选择
                int size = (int)Math.Round(fontDlg.Font.Size); // 将字体大小四舍五入为整数
                //Font selectedFont = fontDlg.Font;
                Font selectedFont = new Font(fontDlg.Font.FontFamily, size, fontDlg.Font.Style);
                iniConfig.SetFont(selectedFont); // 保存字体设置到配置文件
                //更新所有编辑器的字体
                foreach (TabPage p in tabControl1.TabPages)
                {
                    var ed = p.Controls.OfType<ScintillaEditor>().FirstOrDefault();
                    if (ed != null)
                    {
                        ed.SetFont(selectedFont);
                        ed.SetSyntaxHighlighting(IniConfig.Instance.GetSyntaxHighlighting());
                    }
                }
            }
        }

        public void SetSyntaxHighlightingForAllEditors(SyntaxHighlighting syh)
        {
            foreach (TabPage p in tabControl1.TabPages)
            {
                var ed = p.Controls.OfType<ScintillaEditor>().FirstOrDefault();
                if (ed != null)
                {
                    ed.SetSyntaxHighlighting(syh);
                }
            }
        }

        //显示配色方案设置窗体ColorSchemeForm
        private void 配色方案ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorSchemeForm colorSchemeForm = new ColorSchemeForm(this);
            colorSchemeForm.ShowDialog(this);  // 将 Form1 作为父窗体，确保对话框在主窗体前面
            colorSchemeForm.BringToFront();

        }
    }
}
