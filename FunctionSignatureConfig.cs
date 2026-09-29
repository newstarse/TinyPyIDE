using System;
using System.Collections.Generic;
using System.IO;

namespace TinyPyIDE
{
    // 负责读取/生成函数签名配置文件
    public partial class FunctionSignatureConfig
    {

        public static readonly string[] PythonKeywordArray = new[]
        {
            "and","as","assert","break","class","continue","def","del","elif","else","except","finally",
            "for","from","global","if","import","in","is","lambda","nonlocal","not","or","pass","raise",
            "return","try","while","with","yield","True","False","None"
        };

        public static readonly string[] PythonBuiltins = new[]
        {
            "abs","all","any","ascii","bin","bool","bytearray","bytes","callable","chr","classmethod",
            "compile","complex","dict","dir","divmod","enumerate","eval","exec","filter","float","format",
            "frozenset","getattr","globals","hasattr","hash","help","hex","id","input","int","isinstance",
            "issubclass","iter","len","list","locals","map","max","min","next","object","oct","open","ord",
            "pow","print","property","range","repr","reversed","round","set","setattr","slice","sorted",
            "staticmethod","str","sum","super","tuple","type","vars","zip","__import__"
        };
        public Dictionary<string, string> Signatures { get; private set; }
        // 存储类及其成员签名：ClassName -> (member -> signature)
        public Dictionary<string, Dictionary<string, string>> ClassSignatures { get; private set; }

        public FunctionSignatureConfig()
        {
            Signatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ClassSignatures = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        // 在应用程序目录下查找或创建 function_signatures.txt，并加载到 Signatures 字典
        public void LoadFromAppDirectory()
        {
            try
            {
                var fileName = Path.Combine(AppContext.BaseDirectory, "function_signatures.txt");
                if (!File.Exists(fileName))
                {
                    CreateDefaultFile(fileName);
                }

                foreach (var line in File.ReadAllLines(fileName))
                {
                    var trimmed = (line ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    if (trimmed.StartsWith("#")) continue;
                    var idx = trimmed.IndexOf('=');
                    if (idx <= 0) continue;
                    var key = trimmed.Substring(0, idx).Trim();
                    var val = trimmed.Substring(idx + 1).Trim();
                    if (!string.IsNullOrEmpty(key) && !Signatures.ContainsKey(key))
                    {
                        Signatures[key] = val;
                    }
                }
                // 尝试加载类签名文件（class_signatures.txt）
                LoadClassSignaturesFromAppDirectory();
            }
            catch
            {
                // 忽略加载错误，保持不抛出，Signatures 将保持为空
            }
        }

        private void CreateDefaultFile(string fileName)
        {
            var defaultLines = new[]
            {
                "# 函数签名配置示例: name=signature",
                "abs=abs(x) -> number",
                "all=all(iterable) -> bool",
                "any=any(iterable) -> bool",
                "ascii=ascii(obj) -> str",
                "bin=bin(x) -> str",
                "bool=bool(x=False) -> bool",
                "bytearray=bytearray(source=b'') -> bytearray",
                "bytes=bytes(source=b'') -> bytes",
                "callable=callable(obj) -> bool",
                "chr=chr(i) -> str",
                "classmethod=classmethod(func) -> classmethod",
                "compile=compile(source, filename, mode, flags=0, dont_inherit=0, optimize=-1) -> code",
                "complex=complex(real=0, imag=0) -> complex",
                "delattr=delattr(obj, name)",
                "dict=dict(**kwargs) -> dict",
                "dir=dir(obj=None) -> list",
                "divmod=divmod(a, b) -> tuple",
                "enumerate=enumerate(iterable, start=0) -> enumerate",
                "eval=eval(expression, globals=None, locals=None)",
                "exec=exec(object, globals=None, locals=None)",
                "float=float(x=0.0) -> float",
                "format=format(value, format_spec='') -> str",
                "frozenset=frozenset(iterable=()) -> frozenset",
                "getattr=getattr(obj, name, default=...)",
                "globals=globals() -> dict",
                "hasattr=hasattr(obj, name) -> bool",
                "hash=hash(obj) -> int",
                "help=help(obj=None)",
                "hex=hex(x) -> str",
                "id=id(obj) -> int",
                "input=input(prompt='') -> str",
                "int=int(x=0, base=10) -> int",
                "isinstance=isinstance(obj, classinfo) -> bool",
                "issubclass=issubclass(cls, classinfo) -> bool",
                "iter=iter(obj, sentinel=...) -> iterator",
                "len=len(s) -> int",
                "list=list(iterable=()) -> list",
                "locals=locals() -> dict",
                "map=map(func, *iterables) -> map",
                "max=max(*args, key=None)",
                "memoryview=memoryview(obj) -> memoryview",
                "min=min(*args, key=None)",
                "next=next(iterator, default=...)",
                "object=object() -> object",
                "oct=oct(x) -> str",
                "open=open(file, mode='r', buffering=-1, encoding=None, errors=None, newline=None, closefd=True, opener=None) -> file",
                "ord=ord(c) -> int",
                "pow=pow(base, exp, mod=...)",
                "print=print(*objects, sep=' ', end='\\n', file=sys.stdout, flush=False)",
                "property=property(fget=None, fset=None, fdel=None, doc=None) -> property",
                "range=range(stop) | range(start, stop[, step])",
                "repr=repr(obj) -> str",
                "reversed=reversed(seq) -> reverseiterator",
                "round=round(number, ndigits=...) -> number",
                "set=set(iterable=()) -> set",
                "setattr=setattr(obj, name, value)",
                "slice=slice(start=..., stop=..., step=...) -> slice",
                "sorted=sorted(iterable, *, key=None, reverse=False) -> list",
                "staticmethod=staticmethod(func) -> staticmethod",
                "str=str(object='') -> str",
                "sum=sum(iterable, start=0)",
                "super=super(type=..., obj=...) -> super",
                "tuple=tuple(iterable=()) -> tuple",
                "type=type(object) | type(name, bases, dict) -> type",
                "vars=vars(obj=...) -> dict",
                "zip=zip(*iterables) -> zip"
            };

            try
            {
                File.WriteAllLines(fileName, defaultLines);
            }
            catch
            {
                // 忽略写入错误
            }
        }

        // 读取可执行目录下的 class_signatures.txt，解析示例格式并填充 ClassSignatures 字典
        public void LoadClassSignaturesFromAppDirectory()
        {
            try
            {
                var fileName = Path.Combine(AppContext.BaseDirectory, "class_signatures.txt");
                if (!File.Exists(fileName)) return;

                var lines = File.ReadAllLines(fileName);
                string ? currentClass = null;
                Dictionary<string, string> ?currentMap = null;
                foreach (var raw in lines)
                {
                    var line = (raw ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("#")) continue; // 注释

                    // 匹配 "classname" : [
#if NET7_0_OR_GREATER
                    var headerMatch = HeaderRegex().Match(line);
#else
                    var headerMatch = System.Text.RegularExpressions.Regex.Match(line, @"^""(?<cls>[^""]+)""\s*:\s*\[");
#endif
                    if (headerMatch.Success)
                    {
                        currentClass = headerMatch.Groups["cls"].Value;
                        currentMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        continue;
                    }

                    // 匹配结束 ]
                    if (line.StartsWith("]"))
                    {
                        if (!string.IsNullOrEmpty(currentClass) && currentMap != null)
                        {
                            if (!ClassSignatures.ContainsKey(currentClass))
                                ClassSignatures[currentClass] = currentMap;
                        }
                        currentClass = null;
                        currentMap = null;
                        continue;
                    }

                    if (currentMap != null)
                    {
                        // 匹配 "member" => "signature",
#if NET7_0_OR_GREATER
                        var m = MemberRegex().Match(line);
#else
                        var m = System.Text.RegularExpressions.Regex.Match(line, "^\"(?<mem>[^\"]+)\"\s*=>\s*\"(?<sig>[^\"]+)\"\s*,?$");
#endif
                        if (m.Success)
                        {
                            var mem = m.Groups["mem"].Value;
                            var sig = m.Groups["sig"].Value;
                            if (!currentMap.ContainsKey(mem)) currentMap[mem] = sig;
                        }
                    }
                }
            }
            catch
            {
                // 忽略解析错误
            }
        }

#if NET7_0_OR_GREATER
        [System.Text.RegularExpressions.GeneratedRegex(@"^""(?<cls>[^""]+)""\s*:\s*\[")]
        private static partial System.Text.RegularExpressions.Regex HeaderRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"^""(?<mem>[^""]+)""\s*=>\s*""(?<sig>[^""]+)""\s*,?$")]
        private static partial System.Text.RegularExpressions.Regex MemberRegex();
#endif
    }
}
