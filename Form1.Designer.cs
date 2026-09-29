using ScintillaNET;
using System.Runtime.InteropServices;
namespace TinyPyIDE
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            menuStrip1 = new MenuStrip();
            文件StripMenuItem = new ToolStripMenuItem();
            新建ToolStripMenuItem = new ToolStripMenuItem();
            打开ToolStripMenuItem = new ToolStripMenuItem();
            最近的文件ToolStripMenuItem = new ToolStripMenuItem();
            保存ToolStripMenuItem = new ToolStripMenuItem();
            另存为ToolStripMenuItem = new ToolStripMenuItem();
            关闭ToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            退出ToolStripMenuItem = new ToolStripMenuItem();
            编辑ToolStripMenuItem = new ToolStripMenuItem();
            撤销ToolStripMenuItem = new ToolStripMenuItem();
            重做ToolStripMenuItem = new ToolStripMenuItem();
            剪切ToolStripMenuItem = new ToolStripMenuItem();
            复制ToolStripMenuItem = new ToolStripMenuItem();
            粘贴ToolStripMenuItem = new ToolStripMenuItem();
            全选ToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            查找替换ToolStripMenuItem = new ToolStripMenuItem();
            运行ToolStripMenuItem = new ToolStripMenuItem();
            运行程序ToolStripMenuItem = new ToolStripMenuItem();
            在终端中运行ToolStripMenuItem = new ToolStripMenuItem();
            终止重启ToolStripMenuItem = new ToolStripMenuItem();
            进入PythonToolStripMenuItem = new ToolStripMenuItem();
            工具ToolStripMenuItem = new ToolStripMenuItem();
            字体ToolStripMenuItem = new ToolStripMenuItem();
            配色方案ToolStripMenuItem = new ToolStripMenuItem();
            帮助ToolStripMenuItem = new ToolStripMenuItem();
            关于ToolStripMenuItem = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            toolStrip1 = new ToolStrip();
            openButton = new ToolStripButton();
            newFileButton = new ToolStripButton();
            saveButton = new ToolStripButton();
            runButton = new ToolStripButton();
            stopButton = new ToolStripButton();
            quitButton = new ToolStripButton();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tabControl1.SuspendLayout();
            toolStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(32, 32);
            menuStrip1.Items.AddRange(new ToolStripItem[] { 文件StripMenuItem, 编辑ToolStripMenuItem, 运行ToolStripMenuItem, 工具ToolStripMenuItem, 帮助ToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(2053, 42);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // 文件StripMenuItem
            // 
            文件StripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 新建ToolStripMenuItem, 打开ToolStripMenuItem, 最近的文件ToolStripMenuItem, 保存ToolStripMenuItem, 另存为ToolStripMenuItem, 关闭ToolStripMenuItem, toolStripSeparator1, 退出ToolStripMenuItem });
            文件StripMenuItem.Name = "文件StripMenuItem";
            文件StripMenuItem.Size = new Size(82, 38);
            文件StripMenuItem.Text = "文件";
            // 
            // 新建ToolStripMenuItem
            // 
            新建ToolStripMenuItem.Name = "新建ToolStripMenuItem";
            新建ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            新建ToolStripMenuItem.Size = new Size(359, 44);
            新建ToolStripMenuItem.Text = "新建";
            新建ToolStripMenuItem.Click += 新建ToolStripMenuItem_Click;
            // 
            // 打开ToolStripMenuItem
            // 
            打开ToolStripMenuItem.Name = "打开ToolStripMenuItem";
            打开ToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+O";
            打开ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            打开ToolStripMenuItem.Size = new Size(359, 44);
            打开ToolStripMenuItem.Text = "打开...";
            打开ToolStripMenuItem.Click += 打开ToolStripMenuItem_Click;
            // 
            // 最近的文件ToolStripMenuItem
            // 
            最近的文件ToolStripMenuItem.Name = "最近的文件ToolStripMenuItem";
            最近的文件ToolStripMenuItem.Size = new Size(359, 44);
            最近的文件ToolStripMenuItem.Text = "最近的文件";
            最近的文件ToolStripMenuItem.DropDownOpening += 最近的文件ToolStripMenuItem_DropDownOpening;
            // 
            // 保存ToolStripMenuItem
            // 
            保存ToolStripMenuItem.Name = "保存ToolStripMenuItem";
            保存ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            保存ToolStripMenuItem.Size = new Size(359, 44);
            保存ToolStripMenuItem.Text = "保存";
            保存ToolStripMenuItem.Click += 保存ToolStripMenuItem_Click;
            // 
            // 另存为ToolStripMenuItem
            // 
            另存为ToolStripMenuItem.Name = "另存为ToolStripMenuItem";
            另存为ToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.S;
            另存为ToolStripMenuItem.Size = new Size(359, 44);
            另存为ToolStripMenuItem.Text = "另存为...";
            另存为ToolStripMenuItem.Click += 另存为ToolStripMenuItem_Click;
            // 
            // 关闭ToolStripMenuItem
            // 
            关闭ToolStripMenuItem.Name = "关闭ToolStripMenuItem";
            关闭ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.W;
            关闭ToolStripMenuItem.Size = new Size(359, 44);
            关闭ToolStripMenuItem.Text = "关闭";
            关闭ToolStripMenuItem.Click += 关闭ToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(356, 6);
            // 
            // 退出ToolStripMenuItem
            // 
            退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            退出ToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            退出ToolStripMenuItem.Size = new Size(359, 44);
            退出ToolStripMenuItem.Text = "退出";
            退出ToolStripMenuItem.Click += 退出ToolStripMenuItem_Click;
            // 
            // 编辑ToolStripMenuItem
            // 
            编辑ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 撤销ToolStripMenuItem, 重做ToolStripMenuItem, 剪切ToolStripMenuItem, 复制ToolStripMenuItem, 粘贴ToolStripMenuItem, 全选ToolStripMenuItem, toolStripSeparator2, 查找替换ToolStripMenuItem });
            编辑ToolStripMenuItem.Name = "编辑ToolStripMenuItem";
            编辑ToolStripMenuItem.Size = new Size(82, 38);
            编辑ToolStripMenuItem.Text = "编辑";
            // 
            // 撤销ToolStripMenuItem
            // 
            撤销ToolStripMenuItem.Name = "撤销ToolStripMenuItem";
            撤销ToolStripMenuItem.Size = new Size(382, 44);
            撤销ToolStripMenuItem.Text = "撤销                Ctrl+Z";
            撤销ToolStripMenuItem.Click += 撤销ToolStripMenuItem_Click;
            // 
            // 重做ToolStripMenuItem
            // 
            重做ToolStripMenuItem.Name = "重做ToolStripMenuItem";
            重做ToolStripMenuItem.Size = new Size(382, 44);
            重做ToolStripMenuItem.Text = "重做                Ctrl+R";
            重做ToolStripMenuItem.Click += 重做ToolStripMenuItem_Click;
            // 
            // 剪切ToolStripMenuItem
            // 
            剪切ToolStripMenuItem.Name = "剪切ToolStripMenuItem";
            剪切ToolStripMenuItem.Size = new Size(382, 44);
            剪切ToolStripMenuItem.Text = "剪切                Ctrl+X";
            剪切ToolStripMenuItem.Click += 剪切ToolStripMenuItem_Click;
            // 
            // 复制ToolStripMenuItem
            // 
            复制ToolStripMenuItem.Name = "复制ToolStripMenuItem";
            复制ToolStripMenuItem.Size = new Size(382, 44);
            复制ToolStripMenuItem.Text = "复制                Ctrl+C";
            复制ToolStripMenuItem.Click += 复制ToolStripMenuItem_Click;
            // 
            // 粘贴ToolStripMenuItem
            // 
            粘贴ToolStripMenuItem.Name = "粘贴ToolStripMenuItem";
            粘贴ToolStripMenuItem.Size = new Size(382, 44);
            粘贴ToolStripMenuItem.Text = "粘贴                Ctrl+V";
            粘贴ToolStripMenuItem.Click += 粘贴ToolStripMenuItem_Click;
            // 
            // 全选ToolStripMenuItem
            // 
            全选ToolStripMenuItem.Name = "全选ToolStripMenuItem";
            全选ToolStripMenuItem.Size = new Size(382, 44);
            全选ToolStripMenuItem.Text = "全选                Ctrl+A";
            全选ToolStripMenuItem.Click += 全选ToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(379, 6);
            // 
            // 查找替换ToolStripMenuItem
            // 
            查找替换ToolStripMenuItem.Name = "查找替换ToolStripMenuItem";
            查找替换ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            查找替换ToolStripMenuItem.Size = new Size(382, 44);
            查找替换ToolStripMenuItem.Text = "查找/替换...";
            查找替换ToolStripMenuItem.Click += 查找替换ToolStripMenuItem_Click;
            // 
            // 运行ToolStripMenuItem
            // 
            运行ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 运行程序ToolStripMenuItem, 在终端中运行ToolStripMenuItem, 终止重启ToolStripMenuItem, 进入PythonToolStripMenuItem });
            运行ToolStripMenuItem.Name = "运行ToolStripMenuItem";
            运行ToolStripMenuItem.Size = new Size(82, 38);
            运行ToolStripMenuItem.Text = "运行";
            // 
            // 运行程序ToolStripMenuItem
            // 
            运行程序ToolStripMenuItem.Name = "运行程序ToolStripMenuItem";
            运行程序ToolStripMenuItem.ShortcutKeys = Keys.F5;
            运行程序ToolStripMenuItem.Size = new Size(425, 44);
            运行程序ToolStripMenuItem.Text = "运行程序...";
            运行程序ToolStripMenuItem.Click += 运行程序ToolStripMenuItem_Click;
            // 
            // 在终端中运行ToolStripMenuItem
            // 
            在终端中运行ToolStripMenuItem.Name = "在终端中运行ToolStripMenuItem";
            在终端中运行ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.T;
            在终端中运行ToolStripMenuItem.Size = new Size(425, 44);
            在终端中运行ToolStripMenuItem.Text = "在终端中运行程序";
            在终端中运行ToolStripMenuItem.Click += 在终端中运行ToolStripMenuItem_Click;
            // 
            // 终止重启ToolStripMenuItem
            // 
            终止重启ToolStripMenuItem.Name = "终止重启ToolStripMenuItem";
            终止重启ToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+F2";
            终止重启ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F2;
            终止重启ToolStripMenuItem.Size = new Size(425, 44);
            终止重启ToolStripMenuItem.Text = "终止并重启";
            终止重启ToolStripMenuItem.Click += 终止重启ToolStripMenuItem_Click;
            // 
            // 进入PythonToolStripMenuItem
            // 
            进入PythonToolStripMenuItem.Name = "进入PythonToolStripMenuItem";
            进入PythonToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F3;
            进入PythonToolStripMenuItem.Size = new Size(425, 44);
            进入PythonToolStripMenuItem.Text = "进入Python...";
            进入PythonToolStripMenuItem.Click += 进入PythonToolStripMenuItem_Click;
            // 
            // 工具ToolStripMenuItem
            // 
            工具ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 字体ToolStripMenuItem, 配色方案ToolStripMenuItem });
            工具ToolStripMenuItem.Name = "工具ToolStripMenuItem";
            工具ToolStripMenuItem.Size = new Size(82, 38);
            工具ToolStripMenuItem.Text = "工具";
            // 
            // 字体ToolStripMenuItem
            // 
            字体ToolStripMenuItem.Name = "字体ToolStripMenuItem";
            字体ToolStripMenuItem.Size = new Size(261, 44);
            字体ToolStripMenuItem.Text = "字体...";
            字体ToolStripMenuItem.Click += 字体ToolStripMenuItem_Click;
            // 
            // 配色方案ToolStripMenuItem
            // 
            配色方案ToolStripMenuItem.Name = "配色方案ToolStripMenuItem";
            配色方案ToolStripMenuItem.Size = new Size(261, 44);
            配色方案ToolStripMenuItem.Text = "配色方案...";
            配色方案ToolStripMenuItem.Click += 配色方案ToolStripMenuItem_Click;
            // 
            // 帮助ToolStripMenuItem
            // 
            帮助ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 关于ToolStripMenuItem });
            帮助ToolStripMenuItem.Name = "帮助ToolStripMenuItem";
            帮助ToolStripMenuItem.Size = new Size(82, 38);
            帮助ToolStripMenuItem.Text = "帮助";
            // 
            // 关于ToolStripMenuItem
            // 
            关于ToolStripMenuItem.Name = "关于ToolStripMenuItem";
            关于ToolStripMenuItem.Size = new Size(213, 44);
            关于ToolStripMenuItem.Text = "关于...";
            关于ToolStripMenuItem.Click += 关于ToolStripMenuItem_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 42);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(tabControl1);
            splitContainer1.Panel1.Controls.Add(toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(statusStrip1);
            splitContainer1.Size = new Size(2053, 964);
            splitContainer1.SplitterDistance = 696;
            splitContainer1.TabIndex = 1;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 42);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(2053, 654);
            tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.ForeColor = SystemColors.ControlLightLight;
            tabPage1.Location = new Point(8, 45);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(2037, 601);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "untitled";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new Size(32, 32);
            toolStrip1.Items.AddRange(new ToolStripItem[] { openButton, newFileButton, saveButton, runButton, stopButton, quitButton });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(2053, 42);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // openButton
            // 
            openButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            openButton.Image = (Image)resources.GetObject("openButton.Image");
            openButton.ImageTransparentColor = Color.Magenta;
            openButton.Name = "openButton";
            openButton.Size = new Size(46, 36);
            openButton.Text = "打开";
            openButton.TextDirection = ToolStripTextDirection.Horizontal;
            // 
            // newFileButton
            // 
            newFileButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            newFileButton.Image = (Image)resources.GetObject("newFileButton.Image");
            newFileButton.ImageTransparentColor = Color.Magenta;
            newFileButton.Name = "newFileButton";
            newFileButton.Size = new Size(46, 36);
            newFileButton.Text = "新建";
            // 
            // saveButton
            // 
            saveButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            saveButton.Image = (Image)resources.GetObject("saveButton.Image");
            saveButton.ImageTransparentColor = Color.Magenta;
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(46, 36);
            saveButton.Text = "保存";
            // 
            // runButton
            // 
            runButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            runButton.Image = (Image)resources.GetObject("runButton.Image");
            runButton.ImageTransparentColor = Color.Magenta;
            runButton.Name = "runButton";
            runButton.Size = new Size(46, 36);
            runButton.Text = "运行程序";
            // 
            // stopButton
            // 
            stopButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            stopButton.Image = (Image)resources.GetObject("stopButton.Image");
            stopButton.ImageTransparentColor = Color.Magenta;
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(46, 36);
            stopButton.Text = "终止";
            // 
            // quitButton
            // 
            quitButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            quitButton.Image = (Image)resources.GetObject("quitButton.Image");
            quitButton.ImageTransparentColor = Color.Magenta;
            quitButton.Name = "quitButton";
            quitButton.Size = new Size(46, 36);
            quitButton.Text = "退出";
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(32, 32);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new Point(0, 223);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(2053, 41);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(257, 31);
            toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2053, 1006);
            Controls.Add(splitContainer1);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "TinyPy";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem 文件StripMenuItem;
        private ToolStripMenuItem 工具ToolStripMenuItem;
        private ToolStripMenuItem 打开ToolStripMenuItem;
        private ToolStripMenuItem 保存ToolStripMenuItem;
        private ToolStripMenuItem 另存为ToolStripMenuItem;
        private ToolStripMenuItem 编辑ToolStripMenuItem;
        private ToolStripMenuItem 帮助ToolStripMenuItem;
        private ToolStripMenuItem 运行ToolStripMenuItem;
        private SplitContainer splitContainer1;
        private ToolStrip toolStrip1;
        private ToolStripButton openButton;
        private ToolStripButton newFileButton;
        private ToolStripButton saveButton;
        private ToolStripButton runButton;
        private ToolStripButton stopButton;
        private ToolStripButton quitButton;
        private StatusStrip statusStrip1;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private ToolStripMenuItem 新建ToolStripMenuItem;
        private ToolStripMenuItem 关闭ToolStripMenuItem;
        private ToolStripMenuItem 退出ToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem 运行程序ToolStripMenuItem;
        private ToolStripMenuItem 在终端中运行ToolStripMenuItem;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripMenuItem 终止重启ToolStripMenuItem;
        private ToolStripMenuItem 关于ToolStripMenuItem;
        private ToolStripMenuItem 进入PythonToolStripMenuItem;
        private ToolStripMenuItem 最近的文件ToolStripMenuItem;
        private ToolStripMenuItem 撤销ToolStripMenuItem;
        private ToolStripMenuItem 重做ToolStripMenuItem;
        private ToolStripMenuItem 剪切ToolStripMenuItem;
        private ToolStripMenuItem 复制ToolStripMenuItem;
        private ToolStripMenuItem 粘贴ToolStripMenuItem;
        private ToolStripMenuItem 全选ToolStripMenuItem;
        private ToolStripMenuItem 查找替换ToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem 字体ToolStripMenuItem;
        private ToolStripMenuItem 配色方案ToolStripMenuItem;
    }
}
