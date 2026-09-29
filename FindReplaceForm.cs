using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System;
using System.Windows.Forms;
using ScintillaNET;

namespace TinyPyIDE
{
    public partial class FindReplaceForm : Form
    {
        // 绑定主编辑器
        internal Scintilla Editor = null;

        public void SetEditor(Scintilla editor)
        {
            Editor = editor;
        }

        public FindReplaceForm()
        {
            InitializeComponent();
            radDown.Checked = true;
        }

        /// <summary>
        /// 查找下一个
        /// </summary>
        private void btnFindNext_Click(object sender, EventArgs e)
        {
            msgLabel.Text = "";
            DoSearch();
        }


        /// <summary>
        /// 执行搜索核心逻辑
        /// </summary>
        private void DoSearch()
        {
            if (Editor == null)
            {
                MessageBox.Show("未绑定编辑器！");
                return;
            }
            string findText = txtFind.Text;
            if (string.IsNullOrEmpty(findText))
                return;

            SearchFlags flags = SearchFlags.None;
            if (chkMatchCase.Checked)
                flags |= SearchFlags.MatchCase;

            int pos;
            bool searchDown = radDown.Checked; // 使用单选按钮的状态来决定搜索方向
            if (searchDown)
            {
                // 向下搜索：从当前光标往后
                pos = Editor.FindText(flags, findText, Editor.CurrentPosition, Editor.TextLength);
            }
            else
            {
                // 向上搜索：在光标之前的文本中查找最后一次出现的位置
                // Scintilla.FindText 在给定区间内仍是向前查找并返回第一个匹配项，
                // 因此这里使用字符串操作查找最后一个匹配以实现向上搜索的行为。
                var allText = Editor.Text ?? string.Empty;
                int searchEnd = Math.Max(0, Editor.CurrentPosition - 1);
                var comparison = chkMatchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                pos = allText.LastIndexOf(findText, searchEnd, comparison);
            }

            if (pos != -1)
            {
                // Scintilla.SetSelection 参数顺序可用于控制插入点位置，
                // 使用 (end, start) 可以将插入点放在匹配文本的末尾，
                // 以便下次向下查找时从匹配的后面开始继续查找。
                if(searchDown)
                    Editor.SetSelection(pos + findText.Length, pos);
                else 
                    Editor.SetSelection(pos, pos + findText.Length);
                Editor.ScrollCaret();
                //Editor.Focus();
            }
            else
            {
                msgLabel.Text = "已搜索完毕，未找到匹配内容。";
            }
        }

        /// <summary>
        /// 替换：先替换当前选中，再自动查找下一个
        /// </summary>
        private void btnReplace_Click(object sender, EventArgs e)
        {
            if (Editor == null) return;
            string findText = txtFind.Text;
            if (string.IsNullOrEmpty(findText)) return;

            // 判断当前选中内容是否匹配查找文本
            string selected = Editor.SelectedText;
            SearchFlags flags = chkMatchCase.Checked ? SearchFlags.MatchCase : SearchFlags.None;

            bool isMatch;
            if (flags.HasFlag(SearchFlags.MatchCase))
                isMatch = selected == findText;
            else
                isMatch = selected.Equals(findText, StringComparison.OrdinalIgnoreCase);

            if (isMatch)
            {
                Editor.ReplaceSelection(txtReplace.Text);
            }
            // 替换完成自动查找下一项
            msgLabel.Text = "";
            DoSearch();
        }

        /// <summary>
        /// 全部替换
        /// </summary>
        private void btnReplaceAll_Click(object sender, EventArgs e)
        {
            if (Editor == null || Editor.TextLength == 0) return;

            string findText = txtFind.Text;
            string replaceText = txtReplace.Text;
            if (string.IsNullOrEmpty(findText)) return;
            msgLabel.Text = "";
            SearchFlags flags = chkMatchCase.Checked ? SearchFlags.MatchCase : SearchFlags.None;
            int count = 0;
            
            // 从文档开头开始查找并逐个替换。每次替换后将下次查找的起点移动到替换后的末尾，
            // 避免因替换文本包含查找文本而造成死循环。
            int searchStart = 0;
            int found;
            while ((found = Editor.FindText(flags, findText, searchStart, Editor.TextLength)) != -1)
            {
                // 将目标区间设置为刚找到的匹配范围，然后执行替换
                Editor.TargetStart = found;
                Editor.TargetEnd = found + findText.Length;
                Editor.ReplaceTarget(replaceText);
                count++;

                // 下一次从替换文本的末尾继续查找
                searchStart = found + replaceText.Length;
                if (searchStart > Editor.TextLength) break;
            }
            msgLabel.Text = $"全部替换完成，共替换 {count} 处。";
            Editor.Focus();
        }

        // 按下Esc关闭查找替换窗口
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
