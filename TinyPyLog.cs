using System;
//定义一个log类，用来保存程序中的日志信息
public class TinyPyLog : IDisposable
{
    //private string logFilePath ;
    //定义文件句柄
    private System.IO.StreamWriter logWriter;
    private System.IO.FileStream logFileStream;

    public void WriteLog(string message)
    {
        //获取当前时间
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        message = $"[{timeStamp}] {message}";
        logWriter.WriteLine(message);
        logWriter.Flush();
    }

    //以追加方式打开日志文件logFilePath，如果文件不存在则创建新文件
    public TinyPyLog()
    {
        var logFilePath = Path.Combine(AppContext.BaseDirectory, "TinyPy.log");
        if (!System.IO.File.Exists(logFilePath))
        {
            System.IO.File.Create(logFilePath).Close(); 
        }
        logFileStream = new System.IO.FileStream(logFilePath, System.IO.FileMode.Append);
        logWriter = new System.IO.StreamWriter(logFileStream);
    }

    //退出时要关闭文件句柄
    public void Dispose()
    {
        logWriter?.Flush();
        logWriter?.Close();
        logFileStream?.Close();
    }

}
