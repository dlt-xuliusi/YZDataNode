using System;
using System.IO;

namespace YZDataNode
{
    internal class AppLog
    {
        private static FileStream logStream;
        private static StreamWriter logWriter;

        private static object fileLock = new object();

        private static ObjectPool<string> logPool = new ObjectPool<string>();

        public static void LogToFile(string txt)
        {
            try
            {
                lock (fileLock)
                {
                    if (logStream == null)
                    {
                        logStream = new FileStream("AppLog.txt", FileMode.OpenOrCreate);
                        logStream.Position = logStream.Length;
                        logWriter = new StreamWriter(logStream);
                    }
                    logWriter.Write(txt);
                    //清空缓冲区
                    logWriter.Flush();
                }
            }
            catch (Exception Ex)
            {
            }
        }

        public static void Log(string txt, bool onlyToFile = false)
        {
            if (onlyToFile)
            {
                LogToFile(string.Format("{0}\t{1}\r\n", AppHelper.DateTimeToStr(DateTime.Now), txt));
                return;
            }
            LogToFile(string.Format("{0}\t{1}\r\n", AppHelper.DateTimeToStr(DateTime.Now), txt));
            logPool.PutObj(txt);
        }

        public static string GetLog()
        {
            string str = logPool.GetObj();
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            return str;
        }
    }
}