using ScintillaNET;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;

namespace TinyPyIDE
{
    // 负责处理 "." 触发的成员补全逻辑，独立封装以便维护和测试
    public class CompletMember
    {
        private readonly Scintilla scintilla;
        private readonly FunctionSignatureConfig config;

        public CompletMember(FunctionSignatureConfig config, Scintilla scintilla)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.scintilla = scintilla ?? throw new ArgumentNullException(nameof(scintilla));
        }

        // 主入口：在当前光标处处理成员补全，返回 true 表示处理完毕（已显示补全或处理过）
        // 处理 '.' 触发的类/对象成员补全，返回 true 表示处理完毕
        public bool HandleMemberCompletion()
        {
            int pos = scintilla.CurrentPosition;
            int scanStart = Math.Max(0, pos - 500);
            string left = scintilla.GetTextRange(scanStart, pos - scanStart);
            var m = Regex.Match(left, "(?<obj>[A-Za-z_][A-Za-z0-9_]*)\\.$");
            // 先检测光标前直接跟随的字面量表达式（例如 "hello". 或 [1,2]. 或 (1,2). 或 {1,2}. 或 {"a":1}.)
            try
            {
                if (!m.Success)
                {
                    // 1) 字符串字面量
                    var strLit = Regex.Match(left, "(?:\"\"\"[\\s\\S]*?\"\"\"|'''[\\s\\S]*?'''|\"(?:[^\"\\\\]|\\\\.)*\"|'(?:[^'\\\\]|\\\\.)*')\\.$");
                    if (strLit.Success)
                    {
                        if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue("str", out var cmapStr))
                        {
                            scintilla.AutoCShow(0, string.Join(" ", cmapStr.Keys));
                            return true;
                        }
                    }

                    // 2) 以 ] . 结尾的表达式，视作 list 或通过索引得到的列表，优先当作 list
                    if (left.EndsWith("]."))
                    {
                        if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue("list", out var cmapList))
                        {
                            scintilla.AutoCShow(0, string.Join(" ", cmapList.Keys));
                            return true;
                        }
                    }

                    // 3) 以 } . 结尾的表达式，可能是 dict 或 set；查找匹配的 '{' 并判断内容是否包含 ':' 来判断为 dict
                    if (left.EndsWith("}."))
                    {
                        int closeIdx = left.LastIndexOf('}');
                        int openIdx = FindMatchingOpening(left, closeIdx, '{', '}');
                        if (openIdx >= 0)
                        {
                            string inner = left.Substring(openIdx + 1, closeIdx - openIdx - 1);
                            string typ = inner.Contains(":") ? "dict" : "set";
                            if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue(typ, out var cmap))
                            {
                                scintilla.AutoCShow(0, string.Join(" ", cmap.Keys));
                                return true;
                            }
                        }
                    }

                    // 4) 以 ) . 结尾的表达式，可能是 tuple 字面量或普通函数调用的返回值。尝试匹配前面的 '('，若其内容在顶层包含逗号，视作 tuple
                    if (left.EndsWith(")."))
                    {
                        int closeIdx = left.LastIndexOf(')');
                        int openIdx = FindMatchingOpening(left, closeIdx, '(', ')');
                        if (openIdx >= 0)
                        {
                            string inner = left.Substring(openIdx + 1, closeIdx - openIdx - 1);
                            // 简单判断是否为 tuple：在顶层有逗号
                            if (ContainsTopLevelComma(inner))
                            {
                                if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue("tuple", out var cmapTuple))
                                {
                                    scintilla.AutoCShow(0, string.Join(" ", cmapTuple.Keys));
                                    return true;
                                }
                            }
                            else
                            {
                                // 不是 tuple，则可能是函数调用返回值，无法推断
                            }
                        }
                    }
                }
            }
            catch { }
            if (m.Success)
            {
                var obj = m.Groups["obj"].Value;
                if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue(obj, out var members))
                {
                    var memberList = string.Join(" ", members.Keys);
                    scintilla.AutoCShow(0, memberList);
                    return true;
                }

                try
                {
                    // 在光标之前的文档文本中寻找最近一次赋值，优先匹配函数/构造调用，然后匹配常见字面量
                    var docLeft = scintilla.GetTextRange(0, pos);

                    // 1) 匹配形如: obj = SomeClass(... 或 module.SomeClass(...)
                    var funcPattern = "\\b" + Regex.Escape(obj) + "\\s*=\\s*(?<cls>[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*)\\s*\\(";
                    var funcMatches = Regex.Matches(docLeft, funcPattern, RegexOptions.Singleline);
                    if (funcMatches.Count > 0)
                    {
                        var last = funcMatches[funcMatches.Count - 1];
                        var clsFull = last.Groups["cls"].Value;
                        var cls = clsFull.Split('.').Last();
                        Debug.WriteLine($"HandleMemberCompletion: obj={obj} ctor cls={clsFull} -> {cls}");
                        if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue(cls, out var cmap))
                        {
                            var memberList2 = string.Join(" ", cmap.Keys);
                            scintilla.AutoCShow(0, memberList2);
                            return true;
                        }
                    }

                    // 2) 匹配字符串、列表、字典、元组、集合等字面量赋值：obj = "..." 或 obj = '...' => str；obj = [...] => list；obj = {...} => dict/set；obj = (...) => tuple
                    var litPatterns = new (string pattern, string type)[] {
                        ("\\b" + Regex.Escape(obj) + "\\s*=\\s*[@]?(?:\"\"\"|\'\'\'|\"|\')", "str"),
                        ("\\b" + Regex.Escape(obj) + "\\s*=\\s*\\[", "list"),
                        ("\\b" + Regex.Escape(obj) + "\\s*=\\s*\\{", "dict_or_set"),
                        ("\\b" + Regex.Escape(obj) + "\\s*=\\s*\\(", "tuple_literal"),
                    };
                    foreach (var (pattern, type) in litPatterns)
                    {
                        var matches = Regex.Matches(docLeft, pattern, RegexOptions.Singleline);
                        if (matches.Count > 0)
                        {
                            var last = matches[matches.Count - 1];
                            if (type == "dict_or_set")
                            {
                                // 判断最后一次 '{' 到对应 '}' 之间是否包含 ':'
                                int idx = last.Index + last.Length - 1;
                                int closeIdx = docLeft.IndexOf('}', idx);
                                if (closeIdx >= 0)
                                {
                                    string inner = docLeft.Substring(idx + 1, closeIdx - idx - 1);
                                    var typ = inner.Contains(":") ? "dict" : "set";
                                    if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue(typ, out var cmap2))
                                    {
                                        scintilla.AutoCShow(0, string.Join(" ", cmap2.Keys));
                                        return true;
                                    }
                                }
                            }
                            else if (type == "tuple_literal")
                            {
                                int idx = last.Index + last.Length - 1;
                                int closeIdx = docLeft.IndexOf(')', idx);
                                if (closeIdx >= 0)
                                {
                                    string inner = docLeft.Substring(idx + 1, closeIdx - idx - 1);
                                    if (ContainsTopLevelComma(inner))
                                    {
                                        if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue("tuple", out var cmap2))
                                        {
                                            scintilla.AutoCShow(0, string.Join(" ", cmap2.Keys));
                                            return true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue(type, out var cmap2))
                                {
                                    var memberList3 = string.Join(" ", cmap2.Keys);
                                    scintilla.AutoCShow(0, memberList3);
                                    return true;
                                }
                            }
                        }
                    }

                    // 3) 如果赋值是对另一个变量的引用（obj = otherVar），尝试递归查找 otherVar 的类型（简单一次递归以避免复杂分析）
                    var refPattern = "\\b" + Regex.Escape(obj) + "\\s*=\\s*(?<other>[A-Za-z_][A-Za-z0-9_]*)\\b";
                    var refMatches = Regex.Matches(docLeft, refPattern, RegexOptions.Singleline);
                    if (refMatches.Count > 0)
                    {
                        var lastRef = refMatches[refMatches.Count - 1];
                        var other = lastRef.Groups["other"].Value;
                        // 查找 other 的构造或字面量赋值
                        var otherFuncMatches = Regex.Matches(docLeft, "\\b" + Regex.Escape(other) + "\\s*=\\s*(?<cls>[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*)\\s*\\(", RegexOptions.Singleline);
                        if (otherFuncMatches.Count > 0)
                        {
                            var clsFull = otherFuncMatches[otherFuncMatches.Count - 1].Groups["cls"].Value;
                            var cls = clsFull.Split('.').Last();
                            Debug.WriteLine($"HandleMemberCompletion: obj={obj} is ref to other={other} ctor cls={clsFull} -> {cls}");
                            if (config.ClassSignatures != null && config.ClassSignatures.TryGetValue(cls, out var cmap3))
                            {
                                var memberList4 = string.Join(" ", cmap3.Keys);
                                scintilla.AutoCShow(0, memberList4);
                                return true;
                            }
                        }
                        // 还尝试 other 的字面量赋值
                        var otherLitMatches = Regex.Matches(docLeft, "\\b" + Regex.Escape(other) + "\\s*=\\s*([@]?(?:\"\"\"|'''|\"|'|\\[|\\{|\\())", RegexOptions.Singleline);
                        if (otherLitMatches.Count > 0)
                        {
                            var litLast = otherLitMatches[otherLitMatches.Count - 1];
                            var litStart = litLast.Index + litLast.Length - 1;
                            char startChar = docLeft[litStart];
                            string typ = null;
                            if (startChar == '"' || startChar == '\'') typ = "str";
                            else if (startChar == '[') typ = "list";
                            else if (startChar == '{')
                            {
                                int closeIdx = FindMatchingClosing(docLeft, litStart, '{', '}');
                                if (closeIdx >= 0)
                                {
                                    string inner = docLeft.Substring(litStart + 1, closeIdx - litStart - 1);
                                    typ = inner.Contains(":") ? "dict" : "set";
                                }
                            }
                            else if (startChar == '(')
                            {
                                int closeIdx = FindMatchingClosing(docLeft, litStart, '(', ')');
                                if (closeIdx >= 0)
                                {
                                    string inner = docLeft.Substring(litStart + 1, closeIdx - litStart - 1);
                                    if (ContainsTopLevelComma(inner)) typ = "tuple";
                                }
                            }
                            if (!string.IsNullOrEmpty(typ) && config.ClassSignatures != null && config.ClassSignatures.TryGetValue(typ, out var cmap4))
                            {
                                Debug.WriteLine($"HandleMemberCompletion: obj={obj} is ref to other={other} literal type={typ}");
                                scintilla.AutoCShow(0, string.Join(" ", cmap4.Keys));
                                return true;
                            }
                        }
                    }
                }
                catch { }
            }
            return true;
        }

        // 在字符串中找到与 closeChar 匹配的最近的开括号位置，考虑嵌套。返回开括号索引或 -1
        private int FindMatchingOpening(string s, int closeIndex, char openChar, char closeChar)
        {
            if (closeIndex < 0 || closeIndex >= s.Length) return -1;
            int depth = 0;
            for (int i = closeIndex; i >= 0; i--)
            {
                char c = s[i];
                if (c == closeChar) depth++;
                else if (c == openChar)
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        // 从 openIndex 向前扫描并找到对应的闭合括号位置（考虑嵌套与字符串），返回闭合索引或 -1
        private int FindMatchingClosing(string s, int openIndex, char openChar, char closeChar)
        {
            if (openIndex < 0 || openIndex >= s.Length) return -1;
            int depth = 0;
            bool inSingle = false, inDouble = false;
            for (int i = openIndex; i < s.Length; i++)
            {
                char c = s[i];
                // 处理引号，简单状态机
                if (c == '\'' && !inDouble) inSingle = !inSingle;
                else if (c == '"' && !inSingle) inDouble = !inDouble;
                if (inSingle || inDouble) continue;
                if (c == openChar) depth++;
                else if (c == closeChar)
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        // 判断字符串中在顶层是否包含逗号（不在嵌套的括号/方括号/花括号内）
        private bool ContainsTopLevelComma(string s)
        {
            int depthPar = 0, depthBr = 0, depthCurly = 0;
            bool inSingle = false, inDouble = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\'' && !inDouble) inSingle = !inSingle;
                else if (c == '"' && !inSingle) inDouble = !inDouble;
                if (inSingle || inDouble) continue;
                if (c == '(') depthPar++;
                else if (c == ')') depthPar = Math.Max(0, depthPar - 1);
                else if (c == '[') depthBr++;
                else if (c == ']') depthBr = Math.Max(0, depthBr - 1);
                else if (c == '{') depthCurly++;
                else if (c == '}') depthCurly = Math.Max(0, depthCurly - 1);
                else if (c == ',' && depthPar == 0 && depthBr == 0 && depthCurly == 0) return true;
            }
            return false;
        }
    }
}
