namespace TinyPyIDE
{
    partial class FindReplaceForm
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
            label1 = new Label();
            label2 = new Label();
            txtFind = new TextBox();
            txtReplace = new TextBox();
            chkMatchCase = new CheckBox();
            radDown = new RadioButton();
            radUp = new RadioButton();
            btnFindNext = new Button();
            btnReplace = new Button();
            btnReplaceAll = new Button();
            msgLabel = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 29);
            label1.Name = "label1";
            label1.Size = new Size(86, 31);
            label1.TabIndex = 0;
            label1.Text = "查找：";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 106);
            label2.Name = "label2";
            label2.Size = new Size(110, 31);
            label2.TabIndex = 1;
            label2.Text = "替换为：";
            // 
            // txtFind
            // 
            txtFind.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            txtFind.Location = new Point(128, 22);
            txtFind.Name = "txtFind";
            txtFind.Size = new Size(599, 43);
            txtFind.TabIndex = 2;
            // 
            // txtReplace
            // 
            txtReplace.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            txtReplace.Location = new Point(128, 99);
            txtReplace.Name = "txtReplace";
            txtReplace.Size = new Size(599, 43);
            txtReplace.TabIndex = 3;
            // 
            // chkMatchCase
            // 
            chkMatchCase.AutoSize = true;
            chkMatchCase.Location = new Point(43, 173);
            chkMatchCase.Name = "chkMatchCase";
            chkMatchCase.Size = new Size(166, 35);
            chkMatchCase.TabIndex = 4;
            chkMatchCase.Text = "区分大小写";
            chkMatchCase.UseVisualStyleBackColor = true;
            // 
            // radDown
            // 
            radDown.AutoSize = true;
            radDown.Location = new Point(316, 173);
            radDown.Name = "radDown";
            radDown.Size = new Size(93, 35);
            radDown.TabIndex = 5;
            radDown.TabStop = true;
            radDown.Text = "向下";
            radDown.UseVisualStyleBackColor = true;
            // 
            // radUp
            // 
            radUp.AutoSize = true;
            radUp.Location = new Point(520, 173);
            radUp.Name = "radUp";
            radUp.Size = new Size(93, 35);
            radUp.TabIndex = 6;
            radUp.TabStop = true;
            radUp.Text = "向上";
            radUp.UseVisualStyleBackColor = true;
            // 
            // btnFindNext
            // 
            btnFindNext.Location = new Point(762, 21);
            btnFindNext.Name = "btnFindNext";
            btnFindNext.Size = new Size(150, 46);
            btnFindNext.TabIndex = 7;
            btnFindNext.Text = "查找";
            btnFindNext.UseVisualStyleBackColor = true;
            btnFindNext.Click += btnFindNext_Click;
            // 
            // btnReplace
            // 
            btnReplace.Location = new Point(762, 99);
            btnReplace.Name = "btnReplace";
            btnReplace.Size = new Size(150, 46);
            btnReplace.TabIndex = 9;
            btnReplace.Text = "替换";
            btnReplace.UseVisualStyleBackColor = true;
            btnReplace.Click += btnReplace_Click;
            // 
            // btnReplaceAll
            // 
            btnReplaceAll.Location = new Point(762, 173);
            btnReplaceAll.Name = "btnReplaceAll";
            btnReplaceAll.Size = new Size(150, 46);
            btnReplaceAll.TabIndex = 10;
            btnReplaceAll.Text = "全部替换";
            btnReplaceAll.UseVisualStyleBackColor = true;
            btnReplaceAll.Click += btnReplaceAll_Click;
            // 
            // msgLabel
            // 
            msgLabel.Location = new Point(36, 241);
            msgLabel.Name = "msgLabel";
            msgLabel.Size = new Size(691, 31);
            msgLabel.TabIndex = 11;
            // 
            // FindReplaceForm
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(932, 290);
            Controls.Add(msgLabel);
            Controls.Add(btnReplaceAll);
            Controls.Add(btnReplace);
            Controls.Add(btnFindNext);
            Controls.Add(radUp);
            Controls.Add(radDown);
            Controls.Add(chkMatchCase);
            Controls.Add(txtReplace);
            Controls.Add(txtFind);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "FindReplaceForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "查找/替换";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        internal TextBox txtFind;
        private TextBox txtReplace;
        private CheckBox chkMatchCase;
        private RadioButton radDown;
        private RadioButton radUp;
        private Button btnFindNext;
        private Button btnReplace;
        private Button btnReplaceAll;
        private Label msgLabel;
    }
}