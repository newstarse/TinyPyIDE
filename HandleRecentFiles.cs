using System;
using System.Collections.Generic;
using System.Linq;
namespace TinyPyIDE
{
    //这个类用来处理本程序最近打开的文件
    public sealed class HandleRecentFiles
    {
        //首先定义一个类，用于记录最近打开的文件名（包括完整路径）以及它对应的显示名称（不超过40个字符）
        public class FileNameEntry
        {
            public string FullName { get; set; }   //包含完整路径的文件名
            public string ShowName { get; set; }   //用于显示的名称，不超过40个字符
        }
        private List<FileNameEntry> recentFiles;  //用于存储最近打开的文件的列表
        private const int maxRecentFiles = 20;  //最多存储20个最近打开的文件
        private const int maxShowNameLength = 40;  //显示名称的最大长度

        internal bool IsChanged { get; set; } = false;     // 用于标记最近文件列表是否发生了变化

        // 最简单的单例实现（非线程安全场景下足够）
        private static HandleRecentFiles _instance = null;
        public static HandleRecentFiles Instance
        {
            get
            {
                if (_instance == null) _instance = new HandleRecentFiles();
                return _instance;
            }
        }

        private HandleRecentFiles()
        {
            recentFiles = new List<FileNameEntry>();
        }

        private string GetShowName(string fullName)
        {
            if (fullName.Length <= maxShowNameLength)
            {
                return fullName;
            }
            //如果文件名过长，则截取前面和最后的部分，并在中间加上省略号
            //具体做法是：前面取17个字符，后面取20个字符，中间加上"..."，总长度为40个字符
            return fullName.Substring(0, maxShowNameLength / 2 - 3) + "..." + fullName.Substring(fullName.Length - maxShowNameLength / 2);
        }

        public void AddRecentFile(string fullName)
        {
            //检查文件是否已经存在于列表中，如果存在，则将其移动到列表的开头
            var existingFile = recentFiles.FirstOrDefault(f => f.FullName == fullName);
            if (existingFile != null)
            {
                recentFiles.Remove(existingFile);
            }
            else if (recentFiles.Count >= maxRecentFiles)
            {
                //如果列表已满，移除最后一个文件
                recentFiles.RemoveAt(recentFiles.Count - 1);
            }

            string showName = GetShowName(fullName);
            //添加新的文件到列表的开头
            recentFiles.Insert(0, new FileNameEntry { FullName = fullName, ShowName = showName });

            IsChanged = true; // 标记列表已更改
        }
        public List<FileNameEntry> GetRecentFiles()
        {
            return new List<FileNameEntry>(recentFiles);
        }

        //返回最近打开的文件全名，如果没有找到对应的显示名称，则返回null
        public string GetFullName(string showName)
        {
            var fileEntry = recentFiles.FirstOrDefault(f => f.ShowName == showName);
            IsChanged = false;
            return fileEntry?.FullName;
        }

        //返回所有最近打开的文件的全名列表
        public List<string> GetFullNames()
        {
            return recentFiles.Select(f => f.FullName).ToList();
        }

        //返回指定索引的最近打开的文件全名，如果索引无效，则返回null
        public string GetFullName(int index)
        {
            
            if (index >= 0 && index < recentFiles.Count)
            {
                IsChanged = false;
                return recentFiles[index].FullName;
            }
            return null;
        }

        //返回最近打开的所有文件的显示名称列表
        public List<string> GetShowNames()
        {
            IsChanged = false;
            return recentFiles.Select(f => f.ShowName).ToList();
        }
    }
}
