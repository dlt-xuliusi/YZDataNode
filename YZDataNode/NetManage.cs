using System;
using YZNetPacket;

namespace YZDataNode
{
    internal class NetManage
    {
        private NetClient _netClient = new NetClient();

        public event Action<NetHead> EventRcvPacket;

        public void Init()
        {
            _netClient.NetServerIp = AppParam._dataCenterIp;
            _netClient.NetServerPort = AppParam._dataCenterPort;
            _netClient.EventRcvPacket += _netClient_EventRcvPacket;
            _netClient.Init();
        }

        private void _netClient_EventRcvPacket(NetHead packet)
        {
            EventRcvPacket?.Invoke(packet);
        }
    }
}