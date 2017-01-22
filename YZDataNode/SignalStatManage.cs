using System;
using System.Collections.Generic;
using System.Threading;
using YZNetPacket;

namespace YZDataNode
{
    //处理信令统计
    internal class SignalStatManage
    {
        private bool _start;
        public ObjectPool<NetHead> _listPacket = new ObjectPool<NetHead>();

        private MacStatManage _macStatManage = new MacStatManage();
        private CgiStatSaveThread _cgiStatSaveThread = new CgiStatSaveThread();

        public long RcvPacketCount { get; set; }

        public void Init()
        {
            _start = true;

            _cgiStatSaveThread.Init();
            _macStatManage.OnStatSaveToFile += _macStatManage_OnStatSaveToFile;

            Thread thread1 = new Thread(new ThreadStart(StatProcess));
            thread1.Start();
        }

        private void _macStatManage_OnStatSaveToFile(CgiStatToFile info)
        {
            _cgiStatSaveThread.AddStat(info);
        }

        private void StatProcess()
        {
            while (_start)
            {
                int n = DealPacket();
                if (n == 0)
                    Thread.Sleep(1);
            }
        }

        private int DealPacket()
        {
            int count = 0;
            while (true)
            {
                NetHead packet = _listPacket.GetObj();
                if (packet == null)
                    break;

                count++;
                DealPacket(packet);
            }
            return count;//统计计数的
        }

        private void DealPacket(NetHead packet)
        {
            if (packet.PacketType == En_NetType.EN_NetSignalData)
            {
                _macStatManage.DealPacket(packet as NetSignalData);
            }
        }

        public void PutPacket(NetHead packet)
        {
            RcvPacketCount++;
            _listPacket.PutObj(packet);
        }
    }

    public class CgiStatToFile
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<CgiStatInfo> ListCgiStat { get; set; }
    }
}