using System;
//这个类用于记录用户输入的命令，包括在模拟终端输入的命令和通过菜单执行的Python程序。
//当用户在模拟终端按向上键时，程序会从这个类中获取用户之前输入的命令，并显示在模拟终端中。
namespace TinyPyIDE
{
    public class CommandEntry
    {
        public string Command { get; set; }
        public bool IsExeFile { get; set; } // true表示执行文件，false表示命令
    }
    public class RecordCommand
	{
        // 用于存储用户输入的命令的列表，只用string不行，无法区分用户输入的命令和要执行的文件，需要加入一个标记，区分这两类命令
        private List<CommandEntry> commands;      
        private int currentCommandIndex;  // 用于存储当前命令的索引
        public RecordCommand()
		{
			commands = new List<CommandEntry>();
			currentCommandIndex = -1;
		}
		public CommandEntry getCurrentComm()
        {
            if (currentCommandIndex >= 0 && currentCommandIndex < commands.Count)
            {
                return commands[currentCommandIndex];
            }
            else
            {
                return null;
            }
        }
        public CommandEntry getPriorComm()
        {
            if (currentCommandIndex > 0 && currentCommandIndex <= commands.Count)
            {
                //currentCommandIndex--;
                return commands[--currentCommandIndex];
            }
            else
            {
                return null;
            }
        }
        public CommandEntry getNextComm()
        {
            if (currentCommandIndex >= 0 && currentCommandIndex < commands.Count - 1)
            {
                //currentCommandIndex++;
                return commands[++currentCommandIndex];
            }
            else
            {
                return null;
            }
        }

        public void addCommand(string command, bool isExeFile)
        {
            commands.Add(new CommandEntry { Command = command, IsExeFile = isExeFile });
            currentCommandIndex = commands.Count;  // - 1; // 更新当前索引为最新命令的索引
        }
    }
}
