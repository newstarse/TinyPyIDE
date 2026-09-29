import sys
import json
import traceback
import code
from pathlib import Path
from datetime import datetime

# ===================== 通信协议常量 =====================
MSG_START = "\x02"
MSG_END = "\x03"

# 原始系统流保存
_original_stdout = sys.stdout
_original_stderr = sys.stderr
_original_input = input


# 全局执行命名空间（所有代码共享环境，关键！）
_global_env = {"__name__": "__main__"}

_console = code.InteractiveConsole(_global_env)

# ===================== 消息收发工具 =====================
def send_message(msg_dict: dict):
    """发送结构化消息给C#前端"""
    try:
        data = json.dumps(msg_dict, ensure_ascii=False)
        # 包裹边界标记，防止多条消息粘连
        packet = f"{MSG_START}{data}{MSG_END}"
        _original_stdout.write(packet)
        _original_stdout.flush()
    except Exception:
        pass


def read_message() -> dict | None:
    """持续读取stdin，解析一条完整消息"""
    buffer = []
    while True:
        ch = sys.stdin.read(1)
        if not ch:
            # 管道关闭，进程退出
            send_message({"type": "exit"})
            sys.exit(0)
        if ch == MSG_START:
            buffer.clear()
        elif ch == MSG_END:
            raw = "".join(buffer)
            try:
                return json.loads(raw)
            except json.JSONDecodeError:
                continue
        else:
            buffer.append(ch)

# ===================== IO劫持：捕获print输出 =====================
class ProxyStdStream:
    def __init__(self, stream_type: str):
        self._stream_type: str = stream_type

    def write(self, s: str):
        if not s:
            return
        send_message({
            "type": self._stream_type,
            "data": s
        })

    def flush(self):
        pass

# 替换全局标准输出
sys.stdout = ProxyStdStream("stdout")
sys.stderr = ProxyStdStream("stderr")

# ===================== 重载input() 实现前端交互输入 =====================
def input(prompt: str = "") -> str:
    # 通知前端：需要用户输入
    send_message({
        "type": "need_input",
        "prompt": prompt
    })
    # 阻塞等待前端传回输入内容
    while True:
        msg = read_message()
        if msg["type"] == "submit_input":
            return msg["text"]

# 覆盖内置input到全局环境
_global_env["input"] = input

# ========= 执行代码核心函数，不能返回表达式的值==============
'''
def execute_code(code: str):
    try:
        exec(code, _global_env)
        send_message({"type": "exec_done", "success": True})
    except Exception:
        err_text = traceback.format_exc()
        send_message({
            "type": "stderr",
            "data": err_text
        })
        send_message({"type": "exec_done", "success": False})
'''

# =====模拟python交互式环境，执行函数和表达式，并返回执行结果，有错误====
def execute_repl_code(code: str):
    """
    复刻Python REPL交互逻辑
    【重要】函数内部直接发送消息，不返回结果
    """
    source = code.rstrip("\r\n")
    if not source.strip():
        send_message({"type": "exec_complete"})
        return

    try:
        result = None
        result = eval(source)
        if result!=None :
            print(result)
        send_message({
            "type": "expr_output",
            "data": repr(result)
        })
    except SyntaxError as ex:
        send_message({
            "type": "SyntaxError",
            "message": repr(ex)
        })
    except Exception as ex:
        send_message({
            "type": "exception",
            "message": repr(ex)
        })


#=======执行交互式输入的代码，自动处理表达式/语句========
def execute_code(code_str):
    global _console
    try :
        _console.runsource(code_str)
        send_message({"type": "exec_done", "success": True})
    except Exception:
        err_text = traceback.format_exc()
        send_message({
            "type": "stderr",
            "data": err_text
        })
        send_message({"type": "exec_done", "success": False})


def write_log(msg : str):
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")
    logfile.write(f"[{timestamp}] {msg}\n")
    logfile.flush()

#==============执行本地 .py 文件====================
def run_source_file(file_path: str):
    path = Path(file_path)
    if not path.exists():
        send_message({
            "type": "stderr",
            "data": f"错误：文件不存在 {file_path}\n"
        })
        send_message({"type": "exec_done", "success": False})
        write_log(f"{path} is not exist")
        return

    try:
        # 读取源码文本
        source_text = path.read_text(encoding="utf-8")
        exec(source_text, _global_env)
        send_message({"type": "exec_done", "success": True})
        write_log(f"file : {path} executed success") 
        
    except Exception:
        err_text = traceback.format_exc()
        send_message({
            "type": "stderr",
            "data": err_text
        })
        send_message({"type": "exec_done", "success": False})
        write_log(f"file : {path} executed failed") 
        

# ===================== 主事件循环 =====================
def main():
    send_message({"type": "bridge_ready"})
    while True:
        msg = read_message()
        if msg is None:
            continue

        cmd_type = msg.get("type")

        if cmd_type == "execute_code":
            code_text = msg.get("code", "")
            execute_code(code_text)
            
        elif cmd_type == "execute_file":
            file_path = msg.get("path", "")
            run_source_file(file_path)

        elif cmd_type == "shutdown":
            send_message({"type": "exit"})
            return

if __name__ == "__main__":
    logfile = open("runfile.log", mode="a", encoding="utf-8")
    main()
    logfile.close()