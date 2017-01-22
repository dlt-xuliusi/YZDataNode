using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace YZNetPacket
{
    /// <summary>
    /// 缓冲数据流，合成一个完整的包
    /// </summary>
    public class NetDataBuffer
    {
        public int MaxPacketLen { get; set; } = 10240;

        private byte[] _dataPool = new byte[1024];
        private int _offset = 0;
        private int _dataLen = 0;

        public event Action<byte[]> EventRcvPacket;

        public event Action<int> EventRcvPacketLenError;

        public void AddData(byte[] data, int offset, int len)
        {
            int totalLen = len + _dataLen;
            int packetLen;
            if (totalLen >= 4)
            {
                //是否包含一个完整的包
                if (_dataLen >= 4)
                {
                    packetLen = BitConverter.ToInt32(_dataPool, _offset);
                }
                else if (_dataLen == 0)
                {
                    packetLen = BitConverter.ToInt32(data, offset);
                }
                else
                {
                    byte[] lenData = new byte[4];
                    Array.Copy(_dataPool, _offset, lenData, 0, _dataLen);
                    int left = 4 - _dataLen;

                    Array.Copy(data, offset, lenData, _dataLen, left);
                    packetLen = BitConverter.ToInt32(lenData, 0);
                }
                if (packetLen < 4 || packetLen > MaxPacketLen)
                {
                    EventRcvPacketLenError?.Invoke(packetLen);
                    return;
                }

                if (packetLen > totalLen)
                {
                    AddDataToPool(data, offset, len);
                }
                else
                {
                    byte[] packet = new byte[packetLen];
                    int copyCount = 0;
                    if (_dataLen > 0)
                    {
                        copyCount = Math.Min(_dataLen, packetLen);
                        Array.Copy(_dataPool, _offset, packet, 0, copyCount);
                        _offset += copyCount;
                        _dataLen -= copyCount;
                        if (_dataLen == 0)
                        {
                            _offset = 0;
                        }
                    }
                    if (copyCount < packetLen)
                    {
                        int left = packetLen - copyCount;
                        Array.Copy(data, offset, packet, copyCount, left);
                        offset += left;
                        len -= left;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                    EventRcvPacket?.Invoke(packet);

                    if (len > 0)
                    {
                        AddData(data, offset, len);
                    }
                }
            }
            else
            {
                AddDataToPool(data, offset, len);
            }
        }

        private void AddDataToPool(byte[] data, int offset, int len)
        {
            int space = _dataPool.Length - _offset - _dataLen;
            if (space < len)
            {
                AddSpace(len - space);
            }

            Array.Copy(data, offset, _dataPool, _offset + _dataLen, len);
            _dataLen += len;
        }

        public bool IsHavePacket
        {
            get
            {
                if (_dataLen < 4)
                    return false;

                int n = BitConverter.ToInt32(_dataPool, _offset);
                return n <= _dataLen;
            }
        }

        //private bool GetOnePacket(out byte[] data)
        //{
        //    data = null;
        //    if (_dataLen < 4)
        //        return false;

        //    int n = BitConverter.ToInt32(_dataPool, _offset);
        //    if (n > _dataLen)
        //        return false;

        //    data = new byte[n];
        //    Array.Copy(_dataPool, _offset, data, 0, n);
        //    _offset += n;
        //}

        private void AddSpace(int space)
        {
            int newCount = _dataPool.Length + Math.Max(space, 1024);

            byte[] newData = new byte[newCount];
            Array.Copy(_dataPool, newData, _dataPool.Length);

            _dataPool = newData;
        }
    }

    public class NetDataPool
    {
        public List<byte[]> _listData = new List<byte[]>();

        private int _offset = 0;
        private int _dataLen = 0;

        public int LEN_PER_ITEM = 1024; //每个数据片的长度

        public int LeftSpace
        {
            get
            {
                if (_listData.Count == 0)
                    return 0;

                //offset为数据起始位置
                return _listData.Count * LEN_PER_ITEM - _offset - _dataLen;
            }
        }

        public void AddData(byte[] data)
        {
            int space = LeftSpace;
            if (space < data.Length)
            {
                AddDataCapacity(data.Length - space);
            }

            //复制数据
            int itemIndex = (_offset + _dataLen) / LEN_PER_ITEM;
            int itemOffset = (_offset + _dataLen) % LEN_PER_ITEM;

            int sourceOffset = 0;
            while (true)
            {
                byte[] pool = _listData[itemIndex];
                int maxCopy = Math.Min(data.Length - sourceOffset, LEN_PER_ITEM - itemOffset);
                Array.Copy(data, sourceOffset, pool, itemOffset, maxCopy);

                sourceOffset += maxCopy;
                _dataLen += maxCopy;
                itemIndex++;
                itemOffset = 0;

                if (sourceOffset >= data.Length)
                    break;
            }
        }

        //扩容
        private void AddDataCapacity(int count)
        {
            int n = count / LEN_PER_ITEM;
            if ((count % LEN_PER_ITEM) != 0)
                n++;

            for (int i = 0; i < n; i++)
            {
                _listData.Add(new byte[LEN_PER_ITEM]);
            }
        }

        private void CopyFromPool(byte[] data, int count)
        {
            int itemIndex = (_offset + _dataLen) / LEN_PER_ITEM;

            int copyCount = 0;
            while (true)
            {
                int itemOffset = (copyCount + _offset + _dataLen) % LEN_PER_ITEM;
                int n = Math.Min(LEN_PER_ITEM - itemOffset, count - copyCount);
                byte[] pool = _listData[itemIndex];

                Array.Copy(pool, itemOffset, data, copyCount, n);

                copyCount += n;
                if (copyCount >= count)
                    return;
            }
        }

        public bool IsHavePacket
        {
            get
            {
                if (_dataLen < 4)
                    return false;

                byte[] data = new byte[4];
                CopyFromPool(data, 4);

                int n = BitConverter.ToInt32(data, 0);
                return n <= _dataLen;
            }
        }
    }
}