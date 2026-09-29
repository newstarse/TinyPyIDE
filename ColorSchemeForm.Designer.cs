namespace TinyPyIDE
{
    partial class ColorSchemeForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            splitContainer1 = new SplitContainer();
            CancelBtn = new Button();
            ConfirmBtn = new Button();
            comboBox1 = new ComboBox();
            scintilla1 = new ScintillaNET.Scintilla();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(label1);
            splitContainer1.Panel1.Controls.Add(CancelBtn);
            splitContainer1.Panel1.Controls.Add(ConfirmBtn);
            splitContainer1.Panel1.Controls.Add(comboBox1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(scintilla1);
            splitContainer1.Size = new Size(992, 650);
            splitContainer1.SplitterDistance = 86;
            splitContainer1.TabIndex = 0;
            // 
            // CancelBtn
            // 
            CancelBtn.Location = new Point(821, 20);
            CancelBtn.Name = "CancelBtn";
            CancelBtn.Size = new Size(150, 46);
            CancelBtn.TabIndex = 2;
            CancelBtn.Text = "取消";
            CancelBtn.UseVisualStyleBackColor = true;
            CancelBtn.Click += CancelBtn_Click;
            // 
            // ConfirmBtn
            // 
            ConfirmBtn.Location = new Point(656, 20);
            ConfirmBtn.Name = "ConfirmBtn";
            ConfirmBtn.Size = new Size(150, 46);
            ConfirmBtn.TabIndex = 1;
            ConfirmBtn.Text = "确定";
            ConfirmBtn.UseVisualStyleBackColor = true;
            ConfirmBtn.Click += ConfirmBtn_Click;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(139, 20);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(492, 39);
            comboBox1.TabIndex = 0;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // scintilla1
            // 
            scintilla1.AutocompleteListSelectedBackColor = Color.FromArgb(0, 120, 215);
            scintilla1.Dock = DockStyle.Fill;
            scintilla1.LexerName = null;
            scintilla1.Location = new Point(0, 0);
            scintilla1.Name = "scintilla1";
            scintilla1.ScrollWidth = 93;
            scintilla1.Size = new Size(992, 560);
            scintilla1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 23);
            label1.Name = "label1";
            label1.Size = new Size(110, 31);
            label1.TabIndex = 3;
            label1.Text = "配色方案";
            // 
            // ColorSchemeForm
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(992, 650);
            Controls.Add(splitContainer1);
            Name = "ColorSchemeForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "配色方案";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private ComboBox comboBox1;
        private ScintillaNET.Scintilla scintilla1;
        private Button CancelBtn;
        private Button ConfirmBtn;
        private Label label1;
    }
}