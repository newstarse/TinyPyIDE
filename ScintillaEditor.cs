using ScintillaNET;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TinyPyIDE
{
    // 可复用的Scintilla编辑器控件，封装原来在Form1中的Scintilla配置与事件逻辑
    public class ScintillaEditor : UserControl
    {
        private readonly Scintilla scintilla;
        private readonly FunctionSignatureConfig config;
        private readonly List<string> defaultCompletions;
        private readonly CompletMember completionHelper;
        // 静态：确保 Lexilla 动态库只加载一次
        private static bool s_lexerLoaded = false;
        private static string? s_loadedLexerPath = null;
        private static readonly object s_lexerLock = new();

        public string? FilePath { get; private set; } = null;
        public bool IsDirty { get; private set; }

        public event EventHandler? DocumentChanged;

        public Scintilla ScintillaControl => scintilla;

        // lexillaPath 可选，若为 null 则使用默认位置
        public ScintillaEditor(FunctionSignatureConfig config, string? lexillaPath = null)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            // 尝试在创建实际控件前加载 lexer 动态库（只做一次）
            EnsureLexerLoaded(lexillaPath);

            scintilla = new Scintilla();
            scintilla.Dock = DockStyle.Fill;
            this.Controls.Add(scintilla);

            // 合并关键字与内置函数作为默认补全词表
            defaultCompletions = new ();
            if (FunctionSignatureConfig.PythonKeywordArray != null) defaultCompletions.AddRange(FunctionSignatureConfig.PythonKeywordArray);
            if (FunctionSignatureConfig.PythonBuiltins != null) defaultCompletions.AddRange(FunctionSignatureConfig.PythonBuiltins);

            InitializeScintilla();
            scintilla.CharAdded += Scintilla_CharAdded;
            scintilla.TextChanged += Scintilla_TextChanged;
            scintilla.KeyDown += Scintilla_KeyDown;

            // 成员补全处理器（封装到单独类 CompletMember）
            completionHelper = new CompletMember(config, scintilla);

            // 订阅焦点相关事件，用户在编辑器获得输入焦点时通知宿主窗体（Form1）记录为最后活跃编辑控件
            scintilla.GotFocus += (s, e) =>
            {
                var f = this.FindForm() as Form1;
                f?.NotifyEditorGotFocus("editor");
            };
            this.GotFocus += (s, e) =>
            {
                var f = this.FindForm() as Form1;
                f?.NotifyEditorGotFocus("editor");
            };
        }

        private void Scintilla_TextChanged(object? sender, EventArgs e)
        {
            IsDirty = true;
            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void InitializeScintilla()
        {
            try
            {
                // 基本样式与 lexer
                scintilla.LexerName = "python";
                SetupPythonStyling();
                ClearSomeKeys();
                ShowLineNumbers();
            }
            catch
            {
                // 忽略错误，保持可用
            }
        }
        // 处理 KeyDown事件，主要用于阻止某些默认行为或自定义输入处理
        private void Scintilla_KeyDown(object? sender, KeyEventArgs e)
        {
            //如果用户按下了ctrl+#，则为选中的行添加或删除注释符号
            //if (e.KeyCode == (Keys.Control | Keys.Shift | Keys.D3))
            if (e.Control && e.KeyCode == Keys.D3)
            {
                ToggleComment();
                e.Handled = true; // 阻止在历史区域输入
                return;
            }
        }

        private void ToggleComment()
        {
            int selStart = scintilla.SelectionStart;
            int selEnd = scintilla.SelectionEnd;

            // 获取选中范围对应的起始行号、结束行号
            int startLine = scintilla.LineFromPosition(Math.Min(selStart, selEnd));
            int endLine = scintilla.LineFromPosition(Math.Max(selStart, selEnd));

            bool allHasComment = true;

            //----------第一轮扫描：判断选中范围内所有行是否全部带#注释----------
            for (int line = startLine; line <= endLine; line++)
            {
                string text = scintilla.Lines[line].Text;
                //跳过空行，空行不参与判断
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                //查找该行第一个非空白字符
                int firstCharPos = GetFirstNonWhitespaceIndex(text);
                if (firstCharPos >= text.Length || text[firstCharPos] != '#')
                {
                    allHasComment = false;
                    break;
                }
            }

            //----------第二轮：执行添加 / 删除注释----------
            scintilla.BeginUndoAction(); //把全部修改合并成一步撤销操作（Ctrl+Z一次撤销）
            try
            {
                for (int line = startLine; line <= endLine; line++)
                {
                    Line ln = scintilla.Lines[line];
                    string text = ln.Text;

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    int firstCharPos = GetFirstNonWhitespaceIndex(text);
                    int lineStartPos = ln.Position; //该行在文档中的字节起始位置

                    if (allHasComment)
                    {
                        // 删除注释：移除#号
                        int hashPos = lineStartPos + firstCharPos;
                        scintilla.SetSel(hashPos, hashPos + 1);
                        scintilla.ReplaceSelection("");
                    }
                    else
                    {
                        // 添加注释：在第一个非空白字符前面插入 #
                        int insertPos = lineStartPos;        //+ firstCharPos;
                        scintilla.SetSel(insertPos, insertPos);
                        scintilla.ReplaceSelection("#");
                    }
                }
            }
            finally
            {
                scintilla.EndUndoAction();
            }

            // 保持原来的选中区域不变
            scintilla.SetSel(selStart, selEnd);
            scintilla.ScrollCaret();
        }

        /// 获取字符串第一个非空格、Tab字符的下标
        private int GetFirstNonWhitespaceIndex(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c != ' ' && c != '\t')
                    return i;
            }
            return s.Length;
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
            }
            catch { }
        }

        private void ShowLineNumbers()
        {
            try
            {
                scintilla.Margins[0].Type = MarginType.Number;
                scintilla.Margins[0].Sensitive = true;
                UpdateLineNumberMargin();
                scintilla.TextChanged += (s, e) => UpdateLineNumberMargin();
            }
            catch { }
        }

        private void UpdateLineNumberMargin()
        {
            var lines = scintilla.Lines.Count;
            var digits = Math.Max(2, lines.ToString().Length);
            var sample = new string('9', digits);
            var width = scintilla.TextWidth(Style.LineNumber, sample) + 6;
            // 把行号宽度设置为精确宽度
            scintilla.Margins[0].Width = width;
        }

        private void SetupPythonStyling()
        {
            scintilla.StyleResetDefault();
            SetFont(IniConfig.Instance.GetFont());
            scintilla.SetKeywords(0, string.Join(" ", FunctionSignatureConfig.PythonKeywordArray));
            SetSyntaxHighlighting(IniConfig.Instance.GetSyntaxHighlighting());
        }

        public void SetSyntaxHighlighting(SyntaxHighlighting syh)
        {
            // 将默认样式复制到所有子样式，确保空格/未着色区域使用相同背景颜色
            scintilla.Styles[Style.Default].ForeColor = syh.Default;
            scintilla.Styles[Style.Default].BackColor = syh.BackColor;
            scintilla.StyleClearAll();
            // 设置各个 token 的前景色，并确保它们的背景色与默认背景一致，避免出现空格/未着色字符显示成其它颜色的问题
            scintilla.Styles[ScintillaNET.Style.Python.Default].ForeColor = syh.Default;
            scintilla.Styles[ScintillaNET.Style.Python.Default].BackColor = syh.BackColor;
            scintilla.Styles[ScintillaNET.Style.Python.CommentLine].ForeColor = syh.CommentLine;
            scintilla.Styles[ScintillaNET.Style.Python.CommentLine].BackColor = syh.BackColor;
            scintilla.Styles[ScintillaNET.Style.Python.Number].ForeColor = syh.Number;
            scintilla.Styles[ScintillaNET.Style.Python.Number].BackColor = syh.BackColor;
            scintilla.Styles[ScintillaNET.Style.Python.String].ForeColor = syh.String;
            scintilla.Styles[ScintillaNET.Style.Python.String].BackColor = syh.BackColor;
            scintilla.Styles[ScintillaNET.Style.Python.Character].ForeColor = syh.Character;
            scintilla.Styles[ScintillaNET.Style.Python.Character].BackColor = syh.BackColor;
            scintilla.Styles[ScintillaNET.Style.Python.Word].ForeColor = syh.Word;
            scintilla.Styles[ScintillaNET.Style.Python.Word].BackColor = syh.BackColor;
            scintilla.Styles[ScintillaNET.Style.Python.Triple].ForeColor = syh.Triple;  //三引号
            scintilla.Styles[ScintillaNET.Style.Python.Triple].BackColor = syh.BackColor;
            scintilla.Styles[ScintillaNET.Style.Python.ClassName].ForeColor = syh.ClassName;
            scintilla.Styles[ScintillaNET.Style.Python.ClassName].BackColor = syh.BackColor;
            scintilla.Styles[ScintillaNET.Style.Python.Operator].ForeColor = syh.Operator;
            scintilla.Styles[ScintillaNET.Style.Python.Operator].BackColor = syh.BackColor;
            scintilla.Styles[ScintillaNET.Style.Python.Identifier].ForeColor = syh.Identifier;
            scintilla.Styles[ScintillaNET.Style.Python.Identifier].BackColor = syh.BackColor;
            scintilla.CaretForeColor = syh.CaretForeColor;
            scintilla.Styles[Style.LineNumber].ForeColor = Color.Black;
            // 同步控件背景颜色，避免窗口与文本区域背景不一致
            scintilla.BackColor = syh.BackColor;
        }

        public void SetFont(Font selectedFont)
        {
            scintilla.Styles[Style.Default].Font = selectedFont.Name;
            scintilla.Styles[Style.Default].Size = (int)selectedFont.Size;
            scintilla.StyleClearAll();  
        }

        public Font GetFont()
        {
            var style = scintilla.Styles[Style.Default];
            return new Font(style.Font, style.Size);
        }

        // 构建补全列表：默认词 + 文档中出现的标识符
        private IEnumerable<string> BuildCompletionList()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in defaultCompletions) set.Add(s);
            try
            {
                var text = scintilla.Text ?? string.Empty;
                foreach (Match m in Regex.Matches(text, "\\b[A-Za-z_][A-Za-z0-9_]*\\b"))
                {
                    set.Add(m.Value);
                }
            }
            catch { }
            return set;
        }

        private string? GetSignatureForName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (config == null || config.Signatures == null) return null;
            return config.Signatures.TryGetValue(name, out var sig) ? sig : null;
        }

        // 处理输入换行时的自动缩进，返回 true 表示处理并应结束事件处理
        private bool HandleNewLineIndent()
        {
            try
            {
                int pos = scintilla.CurrentPosition;
                int line = scintilla.LineFromPosition(pos);
                int prevLine = line - 1;
                while (prevLine >= 0 && string.IsNullOrWhiteSpace(scintilla.Lines[prevLine].Text)) prevLine--;
                if (prevLine >= 0)
                {
                    var refText = scintilla.Lines[prevLine].Text ?? string.Empty;
                    var leadingMatch = Regex.Match(refText, "^\\s*");
                    var leading = leadingMatch.Success ? leadingMatch.Value : string.Empty;
                    var spacePerTab = 4;
                    var leadingNormalized = leading.Replace("\t", new string(' ', spacePerTab));
                    var extraIndent = refText.TrimEnd().EndsWith(":") ? new string(' ', spacePerTab) : string.Empty;
                    var insert = leadingNormalized + extraIndent;
                    if (!string.IsNullOrEmpty(insert))
                    {
                        scintilla.InsertText(pos, insert);
                        scintilla.CurrentPosition = pos + insert.Length;
                        scintilla.SelectionStart = scintilla.CurrentPosition;
                    }
                }
            }
            catch { }
            return true;
        }

        // 处理配对符号自动补全并在 '(' 时显示 CallTip，返回 true 表示处理完毕
        private bool HandleAutoPairAndCallTip(char opener)
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

            if (opener == '(')
            {
                int scanStart = Math.Max(0, pos - 500);
                string left = scintilla.GetTextRange(scanStart, pos - scanStart);
                var m = Regex.Match(left, @"(?:(?<obj>[A-Za-z_][A-Za-z0-9_]*)\.)?(?<mem>[A-Za-z_][A-Za-z0-9_]*)\($");
                if (m.Success)
                {
                    var obj = m.Groups["obj"].Value;
                    var mem = m.Groups["mem"].Value;
                    string? sig = null;
                    if (!string.IsNullOrEmpty(obj) && config.ClassSignatures != null)
                    {
                        if (config.ClassSignatures.TryGetValue(obj, out var map) && map != null && map.TryGetValue(mem, out sig))
                        {
                            // found
                        }
                        else
                        {
                            try
                            {
                                var docText = scintilla.Text ?? string.Empty;
                                var assign = Regex.Match(docText, "\\b" + Regex.Escape(obj) + "\\s*=\\s*(?<cls>[A-Za-z_][A-Za-z0-9_]*)\\s*\\(");
                                if (assign.Success)
                                {
                                    var cls = assign.Groups["cls"].Value;
                                    if (config.ClassSignatures.TryGetValue(cls, out var cmap) && cmap != null)
                                        cmap.TryGetValue(mem, out sig);
                                }
                            }
                            catch { }
                        }
                    }

                    if (string.IsNullOrEmpty(sig)) sig = GetSignatureForName(mem);
                    if (!string.IsNullOrEmpty(sig)) scintilla.CallTipShow(pos, sig);
                }
                else
                {
                    int wordStart = scintilla.WordStartPosition(pos - 1, true);
                    int len = (pos - 1) - wordStart;
                    if (len > 0)
                    {
                        string name = scintilla.GetTextRange(wordStart, len);
                        var tip = GetSignatureForName(name);
                        if (!string.IsNullOrEmpty(tip)) scintilla.CallTipShow(pos, tip);
                    }
                }
            }

            return true;
        }

        // HandleMemberCompletion 已迁移到独立类 CompletMember

        // 确保Lexilla.dll已经被加载到Scintilla中,只在第一次调用时尝试加载。
        private void EnsureLexerLoaded(string? lexillaPath)
        {
            if (s_lexerLoaded) return;
            lock (s_lexerLock)
            {
                if (s_lexerLoaded) return;
                try
                {
                    var extPath = "runtimes\\win-x64\\native";
                    var candidate = !string.IsNullOrEmpty(lexillaPath)
                        ? lexillaPath
                        : Path.Combine(AppContext.BaseDirectory + extPath, "Lexilla.dll");

                    if (!File.Exists(candidate))
                    {
                        // 如果没有找到，不抛出，仅记录路径为空，后续设置 LexerName 仍可工作（但可能不支持语法高亮）
                        return;
                    }

                    // LoadLexerLibrary 是实例方法，因此用临时 Scintilla 实例来加载库
                    using (var tmp = new Scintilla())
                    {
                        tmp.LoadLexerLibrary(candidate);
                    }

                    s_loadedLexerPath = candidate;
                    s_lexerLoaded = true;
                }
                catch
                {
                    // 忽略加载错误，保持应用继续运行；可在需要时扩展为日志或提示
                }
            }
        }

        // 处理补全/CallTip 逻辑，沿用Form1中的思路但引用本地config
        private void Scintilla_CharAdded(object? sender, CharAddedEventArgs e)
        {
            char ch = (char)e.Char;
            if (ch == '\n')
            {
                if (HandleNewLineIndent()) return;
            }

            if (ch == '(' || ch == '[' || ch == '"' || ch == '\'' || ch == '{')
            {
                if (HandleAutoPairAndCallTip(ch)) return;
            }

            if (ch == '.')
            {
                if (completionHelper.HandleMemberCompletion()) return;
            }

            // 如果正在显示成员补全列表，不要在用户输入普通字母时切换到全局补全
            try
            {
                if (scintilla.AutoCActive)
                {
                    return;
                }
            }
            catch { }

            if (!char.IsLetterOrDigit(ch) && ch != '_') return;

            int curPos = scintilla.CurrentPosition;
            int startPos = scintilla.WordStartPosition(curPos, true);
            int prefixLen = curPos - startPos;
            if (prefixLen <= 0) return;

            string prefix = scintilla.GetTextRange(startPos, prefixLen);

            var matches = BuildCompletionList()
                .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Take(200)
                .ToArray();

            if (matches.Length == 0) return;

            string list = string.Join(" ", matches);
            scintilla.AutoCShow(prefixLen, list);
        }

        // 文件加载/保存
        public void LoadFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                scintilla.Text = File.ReadAllText(path);
                FilePath = path;
                IsDirty = false;
            }
            catch { }
        }

        public void SaveFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                File.WriteAllText(path, scintilla.Text);
                FilePath = path;
                IsDirty = false;
            }
            catch { }
        }

        // Save current buffer. If there is an associated FilePath, save to it; otherwise prompt Save As.
        public bool Save()
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                try
                {
                    SaveFile(FilePath);
                    return true;
                }
                catch { return false; }
            }
            return SaveAs();
        }

        // Save as. If path is null, show SaveFileDialog. Returns true if file was saved.
        public bool SaveAs(string? path = null)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    using (var dlg = new SaveFileDialog())
                    {
                        dlg.Filter = "Python Files|*.py|All Files|*.*";
                        dlg.FileName = string.IsNullOrEmpty(FilePath) ? "untitled.py" : Path.GetFileName(FilePath);
                        if (dlg.ShowDialog() != DialogResult.OK) return false;
                        path = dlg.FileName;
                    }
                }

                SaveFile(path);
                return true;
            }
            catch { return false; }
        }

        // Open a file via OpenFileDialog and load into this editor. Returns true if a file was loaded.
        public bool OpenFromDialog()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Python Files|*.py|All Files|*.*";
                if (dlg.ShowDialog() != DialogResult.OK) return false;
                try
                {
                    LoadFile(dlg.FileName);
                    return true;
                }
                catch { return false; }
            }
        }

        // If buffer is dirty, prompt user to save. Returns true if operation may continue (Yes -> saved, No -> continue, Cancel -> abort)
        public bool PromptSaveIfDirty()
        {
            if (!IsDirty) return true;
            var name = string.IsNullOrEmpty(FilePath) ? this.Parent?.Text : Path.GetFileName(FilePath);
            var res = MessageBox.Show($"是否保存文件：{name}?", "保存并退出", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                return Save();
            }
            if (res == DialogResult.No) return true;
            return false; // Cancel
        }

        // Close this editor. If document is dirty, prompt to save. Returns true if editor was closed (or closed after save/no), false if cancelled.
        public bool CloseEditor()
        {
            // Ask to save if needed
            if (!PromptSaveIfDirty()) return false;

            try
            {
                // If this editor is hosted in a TabPage, remove that TabPage from the TabControl
                var tab = this.Parent as TabPage;
                if (tab != null)
                {
                    var tc = tab.Parent as TabControl;
                    if (tc != null)
                    {
                        // Remove and dispose the tab (which will dispose contained controls)
                        tc.TabPages.Remove(tab);
                        tab.Dispose();
                    }
                    else
                    {
                        tab.Controls.Remove(this);
                        this.Dispose();
                    }
                }
                else
                {
                    // Otherwise just remove from parent control collection
                    var p = this.Parent;
                    p?.Controls.Remove(this);
                    this.Dispose();
                }
            }
            catch
            {
                // ignore
            }

            return true;
        }

        public void Copy() => scintilla.Copy();

        public void Cut() => scintilla.Cut();

        public void Paste() => scintilla.Paste();

        public void Undo() => scintilla.Undo();

        public void Redo() => scintilla.Redo();

        public void Clear() => scintilla.Clear();

        public void SelectAll() => scintilla.SelectAll();

    }
}
