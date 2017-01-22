using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace YZNetPacket
{
    public enum En_NetType
    {
        EN_NetMsisdnSch = 1,
        EN_NetCgiSch = 2,
        EN_NetAreaCodeSch = 3,
        EN_NetSignalData = 100
    }

    public class NetHead
    {
        public static readonly int HEAD_LEN = 8;

        public int packetLen;
        public int packetType;

        public static UInt32 _packetId = 0;

        public En_NetType PacketType
        {
            get
            {
                return (En_NetType)packetType;
            }
        }

        public virtual byte[] ToBytes()
        {
            Debug.Assert(false);
            return null;
        }

        public virtual int GetPacketLen()
        {
            Debug.Assert(false);
            return 0;
        }

        public static int GetPacketType(byte[] bytes, int offset)
        {
            int n = BitConverter.ToInt32(bytes, offset + 4);
            return n;
        }

        public static int GetPacketType(byte[] bytes)
        {
            return GetPacketType(bytes, 0);
        }

        public static En_NetType GetPacketType2(byte[] bytes)
        {
            return (En_NetType)GetPacketType(bytes, 0);
        }

        public static En_NetType GetPacketType2(byte[] bytes, int offset)
        {
            return (En_NetType)GetPacketType(bytes, offset);
        }

        protected int HeadToBytes(byte[] bytes, int offset)
        {
            int index = offset;
            Array.Copy(BitConverter.GetBytes(packetLen), 0, bytes, index, 4);
            index += 4;
            Array.Copy(BitConverter.GetBytes(packetType), 0, bytes, index, 4);
            index += 4;
            return index - offset;
        }

        protected int HeadFromBytes(byte[] data, int offset)
        {
            int index = offset;
            packetLen = BitConverter.ToInt32(data, index);
            index += 4;
            packetType = BitConverter.ToInt32(data, index);
            index += 4;
            return index - offset;
        }

        public static NetHead FromBytes(byte[] data, int offset)
        {
            En_NetType netType = GetPacketType2(data, offset);
            switch (netType)
            {
                case En_NetType.EN_NetMsisdnSch:
                    {
                        return NetMsisdnSch.FromBytes(data, offset);
                    }
                case En_NetType.EN_NetSignalData:
                    {
                        return NetSignalData.FromBytes(data, offset);
                    }
                case En_NetType.EN_NetCgiSch:
                    {
                        return NetCgiSch.FromBytes(data, offset);
                    }
                case En_NetType.EN_NetAreaCodeSch:
                    {
                        return NetAreaCodeSch.FromBytes(data, offset);
                    }
            }
            return null;
        }
    }

    //业务订阅
    public class NetMsisdnSch : NetHead
    {
        public byte SchType; //=1 增加订阅；=0 取消订阅
        public UInt32 PacketId;   //包唯一标识，服务端会返回此值

        public List<string> ListMsisdn = new List<string>(); //后缀匹配

        private NetMsisdnSch()
        {
            PacketId = ++_packetId;
        }

        public NetMsisdnSch(bool addMsisdn)
        {
            PacketId = ++_packetId;
            SchType = (byte)(addMsisdn ? 1 : 0);
            packetType = (int)En_NetType.EN_NetMsisdnSch;
        }

        public override byte[] ToBytes()
        {
            packetLen = GetPacketLen();
            byte[] result = new byte[packetLen];
            int offset = 0;

            int n = HeadToBytes(result, offset);
            offset += n;

            result[offset] = SchType;
            offset++;

            Array.Copy(BitConverter.GetBytes(PacketId), 0, result, offset, 4);
            offset += 4;

            int strLenIndex = offset; //此位置 保存字符串长度数值
            offset += 4;

            StringBuilder sb = new StringBuilder();
            foreach (string item in ListMsisdn)
            {
                sb.Append(item);
                sb.Append(";");
            }

            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            Array.Copy(data, 0, result, offset, data.Length);

            //保存字符串长度
            Array.Copy(BitConverter.GetBytes(data.Length), 0, result, strLenIndex, 4);

            return result;
        }

        public void AddPreMsisdn(string msisdn)
        {
            msisdn = msisdn + "*";
            if (ListMsisdn.Contains(msisdn))
                return;
            ListMsisdn.Add(msisdn);
        }

        public void AddPostMsisdn(string msisdn)
        {
            msisdn = "*" + msisdn;
            if (ListMsisdn.Contains(msisdn))
                return;
            ListMsisdn.Add(msisdn);
        }

        public void AddMsisdnString(string msisdn)
        {
            List<string> items = msisdn.Split(';').Where(o => o.Contains("*")).ToList();
            foreach (string item in items)
            {
                if (item.StartsWith("*"))
                {
                    AddPostMsisdn(item.Replace("*", string.Empty));
                }
                else if (item.EndsWith("*"))
                {
                    AddPreMsisdn(item.Replace("*", string.Empty));
                }
            }
        }

        private void BodyFromBytes(byte[] data, int offset)
        {
            SchType = data[offset];
            offset++;

            PacketId = BitConverter.ToUInt32(data, offset);
            offset += 4;

            int strLen = BitConverter.ToInt32(data, offset);
            offset += 4;

            string txt = Encoding.ASCII.GetString(data, offset, strLen);
            ListMsisdn = txt.Split(';').Where(o => o.Contains("*")).ToList();
        }

        public override int GetPacketLen()
        {
            int result = HEAD_LEN;

            result++;//SchType

            result += 4;//PacketId

            result += 4;//字符串长度

            foreach (string item in ListMsisdn)
            {
                result += item.Length;
                result++;//用 ; 隔开
            }

            return result;
        }

        public static new NetMsisdnSch FromBytes(byte[] data, int offset)
        {
            NetMsisdnSch sch = new NetMsisdnSch();
            int n = sch.HeadFromBytes(data, offset);
            offset += n;

            sch.BodyFromBytes(data, offset);
            return sch;
        }
    }

    //扇区订阅
    public class NetCgiSch : NetHead
    {
        public byte SchType; //=1 增加订阅；=0 取消订阅
        public UInt32 PacketId;   //包唯一标识，服务端会返回此值

        public List<CGI> ListCgi = new List<CGI>(); //后缀匹配

        private NetCgiSch()
        {
            PacketId = ++_packetId;
        }

        public NetCgiSch(bool add)
        {
            PacketId = ++_packetId;
            SchType = (byte)(add ? 1 : 0);
            packetType = (int)En_NetType.EN_NetCgiSch;
        }

        public override byte[] ToBytes()
        {
            packetLen = GetPacketLen();
            byte[] result = new byte[packetLen];
            int offset = 0;

            int n = HeadToBytes(result, offset);
            offset += n;

            result[offset] = SchType;
            offset++;

            Array.Copy(BitConverter.GetBytes(PacketId), 0, result, offset, 4);
            offset += 4;

            Array.Copy(BitConverter.GetBytes(ListCgi.Count), 0, result, offset, 4);
            offset += 4;

            foreach (CGI item in ListCgi)
            {
                Array.Copy(BitConverter.GetBytes(item.lac), 0, result, offset, 2);
                offset += 2;
                Array.Copy(BitConverter.GetBytes(item.ci), 0, result, offset, 2);
                offset += 2;
            }
            return result;
        }

        public void AddCgi(CGI cgi)
        {
            if (ListCgi.Contains(cgi))
                return;
            ListCgi.Add(cgi);
        }

        public void AddCgiString(string strCgi)
        {
            List<string> items = strCgi.Split(';').ToList();
            foreach (string item in items)
            {
                List<string> cgi = item.Split(',').ToList();
                if (cgi.Count < 2)
                    continue;
                try
                {
                    CGI n = new CGI(ushort.Parse(cgi[0]), ushort.Parse(cgi[1]));
                    AddCgi(n);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void BodyFromBytes(byte[] data, int offset)
        {
            SchType = data[offset];
            offset++;

            int index = offset;
            PacketId = BitConverter.ToUInt32(data, offset);
            offset += 4;
            int cgiCount = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < cgiCount; i++)
            {
                ushort lac = BitConverter.ToUInt16(data, offset);
                offset += 2;
                ushort ci = BitConverter.ToUInt16(data, offset);
                offset += 2;

                this.AddCgi(new CGI(lac, ci));
            }
        }

        public override int GetPacketLen()
        {
            int result = HEAD_LEN;

            result++;//SchType
            result += 4; //PacketId
            result += 4; //cgi 个数
            result += ListCgi.Count * 4;

            return result;
        }

        public static new NetCgiSch FromBytes(byte[] data, int offset)
        {
            NetCgiSch sch = new NetCgiSch();
            int n = sch.HeadFromBytes(data, offset);
            offset += n;

            sch.BodyFromBytes(data, offset);
            return sch;
        }
    }

    public class NetAreaCodeSch : NetHead
    {
        public byte SchType; //=1 增加订阅；=0 取消订阅
        public UInt32 PacketId;   //包唯一标识，服务端会返回此值

        public List<UInt32> ListAreaCode = new List<UInt32>(); //后缀匹配

        private NetAreaCodeSch()
        {
            PacketId = ++_packetId;
        }

        public NetAreaCodeSch(bool add)
        {
            PacketId = ++_packetId;
            SchType = (byte)(add ? 1 : 0);
            packetType = (int)En_NetType.EN_NetAreaCodeSch;
        }

        public override byte[] ToBytes()
        {
            packetLen = GetPacketLen();
            byte[] result = new byte[packetLen];
            int offset = 0;

            int n = HeadToBytes(result, offset);
            offset += n;

            result[offset] = SchType;
            offset++;

            Array.Copy(BitConverter.GetBytes(PacketId), 0, result, offset, 4);
            offset += 4;

            Array.Copy(BitConverter.GetBytes(ListAreaCode.Count), 0, result, offset, 4);
            offset += 4;

            foreach (uint item in ListAreaCode)
            {
                Array.Copy(BitConverter.GetBytes(item), 0, result, offset, 4);
                offset += 4;
            }
            return result;
        }

        public void AddAreaCode(uint areaCode)
        {
            if (ListAreaCode.Contains(areaCode))
                return;
            ListAreaCode.Add(areaCode);
        }

        public void AddString(string strAreaCode)
        {
            List<string> items = strAreaCode.Split(';').ToList();
            foreach (string item in items)
            {
                try
                {
                    UInt32 n = uint.Parse(item.Trim());
                    AddAreaCode(n);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void BodyFromBytes(byte[] data, int offset)
        {
            SchType = data[offset];
            offset++;

            int index = offset;
            PacketId = BitConverter.ToUInt32(data, offset);
            offset += 4;
            int count = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < count; i++)
            {
                uint areaCode = BitConverter.ToUInt32(data, offset);
                offset += 4;
                this.AddAreaCode(areaCode);
            }
        }

        public override int GetPacketLen()
        {
            int result = HEAD_LEN;

            result++;//SchType
            result += 4; //PacketId
            result += 4; //areacode 个数
            result += ListAreaCode.Count * 4;

            return result;
        }

        public static new NetAreaCodeSch FromBytes(byte[] data, int offset)
        {
            NetAreaCodeSch sch = new NetAreaCodeSch();
            int n = sch.HeadFromBytes(data, offset);
            offset += n;

            sch.BodyFromBytes(data, offset);
            return sch;
        }
    }

    //从交换中心发出的信令信息
    public class NetSignalData : NetHead
    {
        public List<SignalItem> ListSignal = new List<SignalItem>();

        public void AddSignal(SignalItem item)
        {
            ListSignal.Add(item);
        }

        public override byte[] ToBytes()
        {
            int n = 0;
            List<byte[]> listSignalData = new List<byte[]>();
            foreach (SignalItem item in ListSignal)
            {
                byte[] bytes = ToNetBytes(item);
                n += bytes.Length;
                listSignalData.Add(bytes);
            }

            packetLen = NetHead.HEAD_LEN + n + 1;
            packetType = (int)En_NetType.EN_NetSignalData;

            byte[] result = new byte[packetLen];
            int offset = 0;

            int headLen = HeadToBytes(result, offset);
            offset += headLen;

            result[offset] = (byte)listSignalData.Count;
            offset++;

            foreach (byte[] item in listSignalData)
            {
                Array.Copy(item, 0, result, offset, item.Length);
                offset += item.Length;
            }

            return result;
        }

        public static new NetSignalData FromBytes(byte[] data, int offset)
        {
            NetSignalData result = new NetSignalData();
            int n = result.HeadFromBytes(data, offset);
            offset += n;

            result.BodyFromBytes(data, offset);
            return result;
        }

        private void BodyFromBytes(byte[] data, int offset)
        {
            byte count = data[offset];
            offset++;

            SignalItem item;
            for (byte i = 0; i < count; i++)
            {
                int n = FromNetBytes(data, offset, out item);
                offset += n;
                ListSignal.Add(item);
            }
        }

        public static byte[] ToNetBytes(SignalItem item)
        {
            int bytesLen = 8 + 1 + 2 + 2 + item.msisdn.Length + 1 + item.msisdn2.Length + 1 + 1 + 1;

            byte[] bytes = new byte[bytesLen];
            int index = 0;

            Array.Copy(BitConverter.GetBytes(item.timeStamp.Ticks), 0, bytes, index, 8);
            index += 8;

            bytes[index] = (byte)item.action;
            index++;

            Array.Copy(BitConverter.GetBytes(item.lac), 0, bytes, index, 2);
            index += 2;

            Array.Copy(BitConverter.GetBytes(item.ci), 0, bytes, index, 2);
            index += 2;

            int copyLen;
            SignalDefine.StringToNetByte(item.msisdn, bytes, index, out copyLen);
            index += copyLen;

            SignalDefine.StringToNetByte(item.msisdn2, bytes, index, out copyLen);
            index += copyLen;

            bytes[index] = (byte)item.msisdnIndex;
            index++;
            bytes[index] = (byte)item.sortStat;
            index++;

            return bytes;
        }

        public static int FromNetBytes(byte[] data, int offset, out SignalItem item)
        {
            item = new SignalItem();

            int start = offset;
            long tick = BitConverter.ToInt64(data, offset);
            offset += 8;
            item.timeStamp = new DateTime(tick);

            item.action = (EN_Action)data[offset];
            offset++;

            item.lac = BitConverter.ToUInt16(data, offset);
            offset += 2;
            item.ci = BitConverter.ToUInt16(data, offset);
            offset += 2;

            int copyLen;
            item.msisdn = SignalDefine.StringFromNetByte(data, offset, out copyLen);
            offset += copyLen;

            item.msisdn2 = SignalDefine.StringFromNetByte(data, offset, out copyLen);
            offset += copyLen;

            item.msisdnIndex = (EN_MsisdnIndex)data[offset];
            offset++;

            item.sortStat = (EN_SortStat)data[offset];
            offset++;

            return offset - start;
        }

        public override int GetPacketLen()
        {
            return 0;
        }
    }
}