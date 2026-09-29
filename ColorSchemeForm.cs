using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ScintillaNET;

namespace TinyPyIDE
{
    public partial class ColorSchemeForm : Form
    {
        private IConfigurationRoot? _configuration;
        private IniConfig _ini = IniConfig.Instance;
        private SyntaxHighlighting curSyntaxHighlighting;
        private bool SchemeChanged = false;
        private Form1 host;

        public ColorSchemeForm(Form1 host)
        {
            InitializeComponent();
            this.Shown += ColorSchemeForm_Shown;
            this.host = host;
        }

        private void ColorSchemeForm_Shown(object? sender, EventArgs e)
        {
            try
            {
                var iniPath = Path.Combine(AppContext.BaseDirectory, "ColorScheme.ini");
                if (!File.Exists(iniPath))
                {
                    // 如果未在应用目录找到，则尝试相对路径
                    iniPath = "ColorScheme.ini";
                }

                //基于Microsoft.Extensions.Configuration.Ini的INI配置管理类处理ini文件
                var builder = new ConfigurationBuilder()
                    .AddIniFile(iniPath, optional: true, reloadOnChange: false);
                _configuration = builder.Build();

                //获取所有节名，并保存在sections列表中
                var sections = new List<string>();
                if (_configuration != null)
                {
                    foreach (var section in _configuration.GetChildren())
                    {
                        sections.Add(section.Key);
                    }
                }
                comboBox1.Items.Clear();
                // 第一行保持空
                comboBox1.Items.Add(string.Empty);
                foreach (var s in sections)
                {
                    comboBox1.Items.Add(s);
                }
                comboBox1.SelectedIndex = 0;

                //scintilla1加载示例代码demo.py
                string demoCodePath = Path.Combine(AppContext.BaseDirectory, "Demo.py");
                if (!File.Exists(demoCodePath))
                {
                    // 如果未在应用目录找到，则尝试相对路径
                    demoCodePath = "Demo.py";
                }
                if (File.Exists(demoCodePath))
                {
                    //读入示例代码文件demoCodePath里面的内容到scintilla1控件中来显示
                    scintilla1.Text = File.ReadAllText(demoCodePath);
                }
                //利用GetSyntaxHighlighting()方法获取当前使用的语法高亮设置
                curSyntaxHighlighting = _ini.GetSyntaxHighlighting();

                //scintilla1控件的语法高亮设置
                SetSyntaxHighlighting(curSyntaxHighlighting);

                //把scintilla1设为只读
                scintilla1.ReadOnly = true;
            }
            catch
            {
                // 忽略错误，保持窗体可用
            }
        }

        public void SetSyntaxHighlighting(SyntaxHighlighting syh)
        {
            // 设置 lexer 和默认样式（先设置 Default 的前景/背景，然后调用 StyleClearAll 将默认样式应用到所有样式）
            scintilla1.LexerName = "python";
            scintilla1.StyleResetDefault();
            scintilla1.Styles[Style.Default].Font = "Consolas";
            scintilla1.Styles[Style.Default].Size = 10;
            scintilla1.Styles[Style.Default].ForeColor = syh.Default;
            scintilla1.Styles[Style.Default].BackColor = syh.BackColor;
            // 将默认样式复制到所有子样式，确保空格/未着色区域使用相同背景颜色
            scintilla1.StyleClearAll();

            scintilla1.SetKeywords(0, string.Join(" ", FunctionSignatureConfig.PythonKeywordArray));

            // 设置各个 token 的前景色，并确保它们的背景色与默认背景一致，避免出现空格/未着色字符显示成其它颜色的问题
            scintilla1.Styles[ScintillaNET.Style.Python.Default].ForeColor = syh.Default;
            scintilla1.Styles[ScintillaNET.Style.Python.Default].BackColor = syh.BackColor;
            scintilla1.Styles[ScintillaNET.Style.Python.CommentLine].ForeColor = syh.CommentLine;
            scintilla1.Styles[ScintillaNET.Style.Python.CommentLine].BackColor = syh.BackColor;
            scintilla1.Styles[ScintillaNET.Style.Python.Number].ForeColor = syh.Number;
            scintilla1.Styles[ScintillaNET.Style.Python.Number].BackColor = syh.BackColor;
            scintilla1.Styles[ScintillaNET.Style.Python.String].ForeColor = syh.String;
            scintilla1.Styles[ScintillaNET.Style.Python.String].BackColor = syh.BackColor;
            scintilla1.Styles[ScintillaNET.Style.Python.Character].ForeColor = syh.Character;
            scintilla1.Styles[ScintillaNET.Style.Python.Character].BackColor = syh.BackColor;
            scintilla1.Styles[ScintillaNET.Style.Python.Word].ForeColor = syh.Word;
            scintilla1.Styles[ScintillaNET.Style.Python.Word].BackColor = syh.BackColor;
            scintilla1.Styles[ScintillaNET.Style.Python.Triple].ForeColor = syh.Triple;  //三引号
            scintilla1.Styles[ScintillaNET.Style.Python.Triple].BackColor = syh.BackColor;
            scintilla1.Styles[ScintillaNET.Style.Python.ClassName].ForeColor = syh.ClassName;
            scintilla1.Styles[ScintillaNET.Style.Python.ClassName].BackColor = syh.BackColor;
            scintilla1.Styles[ScintillaNET.Style.Python.Operator].ForeColor = syh.Operator;
            scintilla1.Styles[ScintillaNET.Style.Python.Operator].BackColor = syh.BackColor;
            scintilla1.Styles[ScintillaNET.Style.Python.Identifier].ForeColor = syh.Identifier;
            scintilla1.Styles[ScintillaNET.Style.Python.Identifier].BackColor = syh.BackColor;
            scintilla1.CaretForeColor = syh.CaretForeColor;
            // 同步控件背景颜色，避免窗口与文本区域背景不一致
            scintilla1.BackColor = syh.BackColor;
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            //关闭窗体
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex > 0 && _configuration != null)
            {
                string selectedSection = comboBox1.SelectedItem.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(selectedSection))
                {
                    var section = _configuration.GetSection(selectedSection);
                    curSyntaxHighlighting.Default = ColorTranslator.FromHtml(section["Default"] ?? "#000000");
                    curSyntaxHighlighting.CommentLine = ColorTranslator.FromHtml(section["CommentLine"] ?? "#008000");
                    curSyntaxHighlighting.Number = ColorTranslator.FromHtml(section["Number"] ?? "#FF0000");
                    curSyntaxHighlighting.String = ColorTranslator.FromHtml(section["String"] ?? "#A31515");
                    curSyntaxHighlighting.Character = ColorTranslator.FromHtml(section["Character"] ?? "#A31515");
                    curSyntaxHighlighting.Word = ColorTranslator.FromHtml(section["Word"] ?? "#0000FF");
                    curSyntaxHighlighting.Triple = ColorTranslator.FromHtml(section["Triple"] ?? "#800080");
                    curSyntaxHighlighting.ClassName = ColorTranslator.FromHtml(section["ClassName"] ?? "#2B91AF");
                    curSyntaxHighlighting.Operator = ColorTranslator.FromHtml(section["Operator"] ?? "#000000");
                    curSyntaxHighlighting.Identifier = ColorTranslator.FromHtml(section["Identifier"] ?? "#000000");
                    curSyntaxHighlighting.BackColor = ColorTranslator.FromHtml(section["BackColor"] ?? "#FFFFFF");
                    curSyntaxHighlighting.CaretForeColor = ColorTranslator.FromHtml(section["CaretForeColor"] ?? "#000000");
                    SetSyntaxHighlighting(curSyntaxHighlighting);
                    SchemeChanged = true;
                }
            }
        }

        //用户点击确定按钮，将用户选择的配色方案保存更新IniConfig实例中的语法高亮设置，
        //并将所有打开的编辑器窗口的语法高亮设置更新为新的配色方案，然后关闭窗体
        private void ConfirmBtn_Click(object sender, EventArgs e)
        {
            if (SchemeChanged && _configuration != null)
            {
                //把用户选择的配色方案保存更新IniConfig实例中的语法高亮设置
                _ini.SetSyntaxHighlighting(curSyntaxHighlighting);
                //把所有的打开的编辑器窗口的语法高亮设置都更新为新的设置
                host.SetSyntaxHighlightingForAllEditors(curSyntaxHighlighting);
            }
            Close();
        }
    }
}
