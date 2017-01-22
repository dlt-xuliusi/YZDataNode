using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using YZNetPacket;

namespace YZDataNode
{
    public class NetClient
    {
        private bool _start;
        private TcpClient _tcpClient;
        private ObjectPool<byte[]> _listSendData = new ObjectPool<byte[]>();

        public string NetServerIp { get; set; }
        public int NetServerPort { get; set; }

        private NetDataBuffer _netDataBuffer;

        public DateTime ConnectTime { get; private set; } = DateTime.MinValue;

        public event Action<NetHead> EventRcvPacket;

        public void Init()
        {
            _start = true;
            Thread thread1 = new Thread(new ThreadStart(SocketSend));
            thread1.Start();

            Thread thread2 = new Thread(new ThreadStart(SocketRcv));
            thread2.Start();
        }

        public void Close()
        {
            _start = false;
        }

        public bool IsConnect()
        {
            lock (this)
            {
                try
                {
                    if (_tcpClient != null)
                        return _tcpClient.Connected;
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool CloseConnect()
        {
            lock (this)
            {
                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient = null;
                }
            }
            return true;
        }

        public bool ConnectServer()
        {
            if (IsConnect())
                return true;

            //防止频繁连接
            TimeSpan span = DateTime.Now - ConnectTime;
            if (span.TotalMilliseconds <= 2)
                return false;

            ConnectTime = DateTime.Now;
            _listSendData.Clear();

            lock (this) //锁定当前的实例
            {
                _netDataBuffer = new NetDataBuffer(); //缓冲数据流，合成一个完整的包
                _netDataBuffer.EventRcvPacket += _netDataBuffer_EventRcvPacket;
                _netDataBuffer.EventRcvPacketLenError += _netDataBuffer_EventRcvPacketLenError;

                try
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(IPAddress.Parse(NetServerIp), NetServerPort);//连接地址，端口
                    if (IsConnect())
                    {
                        OnSocketEvent(true);
                        AppLog.Log(string.Format("连接交换中心成功！"));
                    }
                    else
                    {
                        AppLog.Log(string.Format("连接交换中心失败！"));
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Log(string.Format("连接交换中心异常：{0}", ex.Message));
                    return false;
                }
            }
            return true;
        }

        private void OnSocketEvent(bool connect)
        {
            if (connect)
            {         //放到缓冲池
                NetMsisdnSch packet = new NetMsisdnSch(true);
                packet.AddMsisdnString(AppParam.MsisdnSubscript);
                _listSendData.PutObj(packet.ToBytes());

                NetCgiSch cgiSch = new NetCgiSch(true);
                cgiSch.AddCgiString(AppParam.CgiSubscript);
                _listSendData.PutObj(cgiSch.ToBytes());

                NetAreaCodeSch areaCodeSch = new NetAreaCodeSch(true);
                areaCodeSch.AddString(AppParam.AreaCodeSubscript);
                _listSendData.PutObj(areaCodeSch.ToBytes());
            }
        }

        private void _netDataBuffer_EventRcvPacketLenError(int obj)
        {
        }

        private void _netDataBuffer_EventRcvPacket(byte[] buffer)
        {
            NetHead packet = NetHead.FromBytes(buffer, 0);
            if (packet == null)
            {
                Debug.Assert(false);
                return;
            }
            EventRcvPacket?.Invoke(packet);//EventRcvPacket是null不执行这句，EventRcvPacket不是null,执行
            if (packet.PacketType == En_NetType.EN_NetSignalData)
            {
                DealSignalPacket((NetSignalData)packet);
            }
            else if (packet.PacketType == En_NetType.EN_NetMsisdnSch)
            {
                DealMsisdnSchAck((NetMsisdnSch)packet);
            }
        }

        private void DealMsisdnSchAck(NetMsisdnSch packet)
        {
        }

        private void DealSignalPacket(NetSignalData packet)
        {
        }

        private void OnSocketClose()
        {
            lock (this)
            {
                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient = null;
                }
            }
            OnSocketEvent(false);
        }

        //发送数据线程
        private void SocketSend()
        {
            while (_start)
            {
                if (!IsConnect())
                {
                    Thread.Sleep(1);
                    continue;
                }

                byte[] data = _listSendData.GetObj();
                if (data == null)
                {
                    Thread.Sleep(1);
                    continue;
                }
                try
                {
                    NetworkStream stream = _tcpClient.GetStream();
                    stream.Write(data, 0, data.Length);
                }
                catch (Exception)
                {
                    OnSocketClose();
                }
            }
        }

        //接收数据线程
        private void SocketRcv()
        {
            byte[] data = new byte[1024];
            Int32 readLen = 0;
            while (_start)
            {
                //不停的链接服务器
                if (!IsConnect())
                {
                    ConnectServer();
                    Thread.Sleep(1);
                    continue;
                }

                try
                {
                    NetworkStream stream = _tcpClient.GetStream();
                    readLen = stream.Read(data, 0, data.Length);
                    OnRcvNetBytes(data, readLen);
                }
                catch (Exception)
                {
                    OnSocketClose();
                }
            }
            OnSocketClose();
        }

        /// <summary>
        /// 从服务端收到字节流
        /// </summary>
        /// <param name="data"></param>
        /// <param name="readLen"></param>
        private void OnRcvNetBytes(byte[] data, int readLen)
        {
            _netDataBuffer.AddData(data, 0, readLen);
        }

        //发送数据放到缓冲
        public bool PutSendData(byte[] bytes)
        {
            if (!IsConnect())
                return false;

            return _listSendData.PutObj(bytes);
        }
    }
}