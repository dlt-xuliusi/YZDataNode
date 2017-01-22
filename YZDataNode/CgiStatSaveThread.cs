using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace YZDataNode
{
    //两小时数据保存
    internal class CgiStatSaveThread
    {
        private bool _start;

        public void Init()
        {
            _start = true;
            Thread thread1 = new Thread(new ThreadStart(StatProcess));
            thread1.Start();
        }

        private void StatProcess()
        {
            while (_start)
            {
                while (true)
                {
                    CgiStatToFile info = _listStat.GetObj();
                    if (info == null)
                    {
                        break;
                    }
                    SaveToFile(info);
                }

                Thread.Sleep(100);
            }
        }

        private void SaveToFile(CgiStatToFile info)
        {
            string fileName = GetFileName(info.StartTime, info.EndTime);//生成文件路径及文件名
            using (FileStream fileStream = new FileStream(fileName, FileMode.Append))
            {
                foreach (CgiStatInfo cgiInfo in info.ListCgiStat)
                {
                    //排序（访问次数，降序排列）
                    List<CgiStatInfo.MsisdnInfo> msisdnGroup = cgiInfo.MsisdnGroup.Values.OrderByDescending(o => o.VisitCount).ToList();
                    WriteValue(fileStream, cgiInfo._CurCgi.lac);
                    WriteValue(fileStream, cgiInfo._CurCgi.ci);
                    WriteValue(fileStream, (uint)msisdnGroup.Count);
                    foreach (CgiStatInfo.MsisdnInfo msisdn in msisdnGroup)
                    {
                        WriteValue(fileStream, AppHelper.Uint64Parse(msisdn.Msisdn));
                        WriteValue(fileStream, msisdn.StaySecond);
                        WriteValue(fileStream, msisdn.VisitCount);
                    }
                }
            }
        }

        public void WriteValue(FileStream file, ushort n)
        {
            byte[] data = BitConverter.GetBytes(n);
            file.Write(data, 0, data.Length);//写入值
        }

        public void WriteValue(FileStream file, uint n)
        {
            byte[] data = BitConverter.GetBytes(n);
            file.Write(data, 0, data.Length);
        }

        public void WriteValue(FileStream file, UInt64 n)
        {
            byte[] data = BitConverter.GetBytes(n);
            file.Write(data, 0, data.Length);
        }

        //在指定目录生成两小时文件
        private string GetFileName(DateTime startTime, DateTime endTime)
        {
            int hour = startTime.Hour;
            if (hour % 2 != 0)
                hour--;
            //创建保存2小时文件文件夹路径
            string dir = startTime.ToString("yyyy/MMdd");
            string newPath = Path.Combine(AppParam.StatFileDir, dir);
            Directory.CreateDirectory(newPath);
            //可以更改文件名字
            string result = Path.Combine(newPath, hour.ToString().PadLeft(2, '0'));
            return result;
        }

        private ObjectPool<CgiStatToFile> _listStat = new ObjectPool<CgiStatToFile>();

        public void AddStat(CgiStatToFile info)
        {
            _listStat.PutObj(info);
        }
    }
}