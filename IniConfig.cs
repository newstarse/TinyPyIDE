using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// 基于 Microsoft.Extensions.Configuration.Ini 的 INI 配置管理类（单例，非线程安全）
/// </summary>

namespace TinyPyIDE
{
    public sealed class IniConfig
    {
        private static readonly IniConfig _instance = new IniConfig();
        public static IniConfig Instance => _instance;

        private IConfigurationRoot _configuration;
        private string _filePath= "TinyPy.ini";
        private Font curFont;
        private SyntaxHighlighting curSyntaxHighlighting = new SyntaxHighlighting();    //用于存储当前的语法高亮设置


        private IniConfig() { }

        /// <summary>
        /// 设置配置文件路径（应在 Load 前调用）
        /// </summary>
        //TinyPy.ini文件放在exe同目录下，但是exe文件启动时的工作目录可能不是exe所在目录，所以要转换成绝对路径
        public void SetFilePath(string path)            // => _filePath = path;
        {
            if (!Path.IsPathRooted(path))
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                path = Path.Combine(exeDir,path);
            }
            _filePath = path;
        }

        /// <summary>
        /// 加载配置文件，若文件不存在则创建空配置
        /// </summary>
        public void Load()
        {
            // 确保基础路径为应用目录，以便相对路径能够被解析
            var builder = new ConfigurationBuilder()        
                                .AddIniFile(_filePath, optional: true, reloadOnChange: false);
            _configuration = builder.Build();
            GetFontFromConfig();    //读入配置文件中的字体设置
            GetSyntaxHighlightingFromConfig();    //读入配置文件中的语法高亮设置
        }

        /// <summary>
        /// 保存配置到文件（注意：会丢失原文件中的注释和空行）
        /// </summary>
        public void Save()
        {
            // 从 _configuration 中提取所有键值对，并按小节和键排序重组
            var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in _configuration.AsEnumerable())
            {
                if (kv.Value == null) continue; // 忽略值为 null 的项

                string key = kv.Key;
                // 解析节名和键名（格式：Section:Key）
                int colonIndex = key.IndexOf(':');
                if (colonIndex == -1) continue; // 忽略没有节的键（通常不会出现）

                string section = key.Substring(0, colonIndex);
                string subKey = key.Substring(colonIndex + 1);

                if (!sections.TryGetValue(section, out var dict))
                {
                    dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    sections[section] = dict;
                }
                dict[subKey] = kv.Value;
            }

            // 写入文件
            using var writer = new StreamWriter(_filePath, false, Encoding.UTF8);
            foreach (var sectionPair in sections.OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase))
            {
                writer.WriteLine($"[{sectionPair.Key}]");

                var keys = sectionPair.Value.Keys.ToList();
                // 对 RecentFiles 小节特殊处理：按 menu 后的数字排序
                if (sectionPair.Key.Equals("RecentFiles", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Sort((a, b) =>
                    {
                        int numA = ExtractMenuNumber(a);
                        int numB = ExtractMenuNumber(b);
                        return numA.CompareTo(numB);
                    });
                }
                else
                {
                    keys.Sort(StringComparer.OrdinalIgnoreCase);
                }

                foreach (var key in keys)
                {
                    writer.WriteLine($"{key}={sectionPair.Value[key]}");
                }
                writer.WriteLine(); // 小节之间空一行
            }
        }

        // 从 "menu1" 提取数字，若格式不符返回 0
        private int ExtractMenuNumber(string key)
        {
            if (key.StartsWith("menu", StringComparison.OrdinalIgnoreCase))
            {
                string numPart = key.Substring(4);
                if (int.TryParse(numPart, out int num))
                    return num;
            }
            return 0;
        }

        // -------- 通用读写方法 ----------
        public string GetValue(string section, string key, string defaultValue = "")
        {
            string fullKey = $"{section}:{key}";
            return _configuration[fullKey] ?? defaultValue;
        }

        public void SetValue(string section, string key, string value)
        {
            string fullKey = $"{section}:{key}";
            _configuration[fullKey] = value;
        }

        // -------- RecentFiles 专用方法 ----------
        public List<string> GetRecentFileNames()
        {
            var list = new List<(int number, string path)>();

            // 遍历配置中的所有键值对（扁平化格式：Section:Key）
            foreach (var kv in _configuration.AsEnumerable())
            {
                // 忽略值为 null 的项（可能被设为 null）
                if (kv.Value == null) continue;

                string fullKey = kv.Key;
                // 检查是否属于 "RecentFiles" 小节（不区分大小写）
                if (fullKey.StartsWith("RecentFiles:", StringComparison.OrdinalIgnoreCase))
                {
                    // 提取键名（如 "menu1"）
                    string keyName = fullKey.Substring("RecentFiles:".Length);
                    // 检查是否以 "menu" 开头（不区分大小写）
                    if (keyName.StartsWith("menu", StringComparison.OrdinalIgnoreCase))
                    {
                        // 提取数字部分
                        string numPart = keyName.Substring(4);
                        if (int.TryParse(numPart, out int number))
                        {
                            list.Add((number, kv.Value));
                        }
                    }
                }
            }

            // 按数字升序排序（保证 menu1, menu2, ... 的顺序）
            list.Sort((a, b) => a.number.CompareTo(b.number));

            // 返回文件路径列表（只返回非空路径，过滤掉空字符串）
            return list
                .Where(item => !string.IsNullOrEmpty(item.path))
                .Select(item => item.path)
                .ToList();
        }


        /// <summary>
        /// 替换 [RecentFiles] 小节中的所有文件路径（覆盖式更新）
        /// </summary>
        /// <param name="filePaths">新的文件路径列表（按顺序，第一个将变为 menu1）</param>
        public void UpdateRecentFileNames(List<string> filePaths)
        {
            // 1. 清除所有现有的 menu* 键
            var keysToRemove = _configuration.AsEnumerable()
                .Where(kv => kv.Key.StartsWith("RecentFiles:menu", StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _configuration[key] = null; // 删除该键
            }

            // 2. 写入新列表（过滤掉空字符串）
            int index = 1;
            foreach (var path in filePaths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    _configuration[$"RecentFiles:menu{index}"] = path;
                    index++;
                }
            }
        }

        public void GetFontFromConfig()
        {
            string fontFamily = GetValue("Font", "Family", "Consolas");
            float fontSize = float.TryParse(GetValue("Font", "Size", "14"), out float size) ? size : 12f;
            FontStyle fontStyle = FontStyle.Regular;
            string styleStr = GetValue("Font", "Style", "Regular");
            if (Enum.TryParse(styleStr, true, out FontStyle parsedStyle))
            {
                fontStyle = parsedStyle;
            }
            curFont = new Font(fontFamily, fontSize, fontStyle);
        }



        public Font GetFont()
        {
            return curFont;
        }

        public void SetFont(Font font)
        {
            curFont = font;
            SetValue("Font", "Family", font.FontFamily.Name);
            SetValue("Font", "Size", font.Size.ToString());
            SetValue("Font", "Style", font.Style.ToString());
        }

        public SyntaxHighlighting GetSyntaxHighlighting()
        {
            return curSyntaxHighlighting;
        }

        public void GetSyntaxHighlightingFromConfig()
        {
            curSyntaxHighlighting.Default = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "Default", "#000000"));
            curSyntaxHighlighting.CommentLine = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "CommentLine", "#008000"));
            curSyntaxHighlighting.Number = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "Number", "#FF0000"));
            curSyntaxHighlighting.String = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "String", "#A31515"));
            curSyntaxHighlighting.Character = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "Character", "#A31515"));
            curSyntaxHighlighting.Word = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "Word", "#0000FF"));
            curSyntaxHighlighting.Triple = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "Triple", "#800080"));
            curSyntaxHighlighting.ClassName = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "ClassName", "#2B91AF"));
            curSyntaxHighlighting.Operator = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "Operator", "#000000"));
            curSyntaxHighlighting.Identifier = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "Identifier", "#000000"));
            curSyntaxHighlighting.BackColor = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "BackColor", "#FFFFFF"));
            curSyntaxHighlighting.CaretForeColor = ColorTranslator.FromHtml(GetValue("SyntaxHighlighting", "CaretForeColor", "#000000"));
        }

        public void SetSyntaxHighlighting(SyntaxHighlighting syh)
        {
            curSyntaxHighlighting = syh;
            SetValue("SyntaxHighlighting", "Default", ColorTranslator.ToHtml(syh.Default));
            SetValue("SyntaxHighlighting", "CommentLine", ColorTranslator.ToHtml(syh.CommentLine));
            SetValue("SyntaxHighlighting", "Number", ColorTranslator.ToHtml(syh.Number));
            SetValue("SyntaxHighlighting", "String", ColorTranslator.ToHtml(syh.String));
            SetValue("SyntaxHighlighting", "Character", ColorTranslator.ToHtml(syh.Character));
            SetValue("SyntaxHighlighting", "Word", ColorTranslator.ToHtml(syh.Word));
            SetValue("SyntaxHighlighting", "Triple", ColorTranslator.ToHtml(syh.Triple));
            SetValue("SyntaxHighlighting", "ClassName", ColorTranslator.ToHtml(syh.ClassName));
            SetValue("SyntaxHighlighting", "Operator", ColorTranslator.ToHtml(syh.Operator));
            SetValue("SyntaxHighlighting", "Identifier", ColorTranslator.ToHtml(syh.Identifier));
            SetValue("SyntaxHighlighting", "BackColor", ColorTranslator.ToHtml(syh.BackColor));
            SetValue("SyntaxHighlighting", "CaretForeColor", ColorTranslator.ToHtml(syh.CaretForeColor));
        }
    }
}