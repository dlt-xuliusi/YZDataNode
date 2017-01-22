using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using YZNetPacket;

namespace YZDataNode
{
    internal class MacStatManage
    {
        private Dictionary<string, MacStatInfo> _macStatGroup = new Dictionary<string, MacStatInfo>();

        private DateTime _startStatTime = DateTime.MinValue;
        private DateTime _endStatTime = DateTime.MinValue;

        public event Action<CgiStatToFile> OnStatSaveToFile;

        public void DealPacket(NetSignalData signal)
        {
            signal.ListSignal.ForEach(o => DealPacket(o));
        }

        private bool DealPacket(SignalItem item)
        {
            if (item.sortStat != EN_SortStat.sort)
            {
                return false;
            }
            if (UpdateSignalTime(item.timeStamp))//返回值是true，(说明这两小时的文件已经读完)执行保存到文件；如果是false 不执行保存到文件函数
            {
                StatToFile();
                ResetStatTime(item.timeStamp);
            }

            string msisdn = item.MsisdnOfCurAction;
            if (string.IsNullOrEmpty(msisdn))
                return false;

            //非手机号 则不处理
            if (!SignalDefine.IsMsisdnOfCN(msisdn))
                return false;

            MacStatInfo macInfo;
            if (_macStatGroup.ContainsKey(msisdn))
            {
                macInfo = _macStatGroup[msisdn];
            }
            else
            {
                macInfo = new MacStatInfo(msisdn);
                _macStatGroup.Add(msisdn, macInfo);
            }
            macInfo.DealPacket(item);
            return true;
        }

        //将2小时数据保存到文件
        private void StatToFile()
        {
            DateTime curTime = _endStatTime;

            Dictionary<CGI, CgiStatInfo> cgiStat = new Dictionary<CGI, CgiStatInfo>();
            foreach (MacStatInfo info in _macStatGroup.Values) //手机号统计完整信息
            {
                foreach (MacStatInfo.StayTimeCal stay in info.CgiStat.Values)//手机号在各个扇区情况
                {
                    CgiStatInfo statInfo;
                    if (cgiStat.ContainsKey(stay.CurCgi))
                    {
                        statInfo = cgiStat[stay.CurCgi];
                    }
                    else
                    {
                        statInfo = new CgiStatInfo(stay.CurCgi);
                        cgiStat.Add(stay.CurCgi, statInfo);//key:CurCgi,value：statInfo信息(手机好、时间、访问次数)
                    }
                    statInfo.AddMsisdn(info._msisdn, stay.StaySecond(curTime), stay.VisiCount);//添加手机号、时间、访问次数
                }

                info.NextStat(curTime);//清理缓存
            }

            CgiStatToFile stat = new CgiStatToFile() { };
            stat.StartTime = _startStatTime;
            stat.EndTime = _endStatTime;
            stat.ListCgiStat = cgiStat.Values.ToList();//手机号、时间、访问次数
            OnStatSaveToFile?.Invoke(stat);//？？？stat  ---info
        }

        private bool UpdateSignalTime(DateTime timeStamp)
        {
            if (_startStatTime == DateTime.MinValue)
            {
                _startStatTime = timeStamp;
                int endHour = (_startStatTime.Hour % 2 == 0) ? (_startStatTime.Hour + 2) : (_startStatTime.Hour + 1);
                if (endHour == 24)
                {
                    endHour = 0;
                    _endStatTime = new DateTime(_startStatTime.Year, _startStatTime.Month, _startStatTime.Day, endHour, 0, 0);
                    _endStatTime = _endStatTime.AddDays(1);
                }
                else
                {
                    _endStatTime = new DateTime(_startStatTime.Year, _startStatTime.Month, _startStatTime.Day, endHour, 0, 0);
                }
                return false;
            }
            else
            {
                if (timeStamp >= _endStatTime)
                {
                    return true;
                }
                return false;
            }
        }

        private void ResetStatTime(DateTime timeStamp)
        {
            _startStatTime = timeStamp;
            int endHour = (_startStatTime.Hour % 2 == 0) ? (_startStatTime.Hour + 2) : (_startStatTime.Hour + 1);
            if (endHour == 24)
            {
                endHour = 0;
                _endStatTime = new DateTime(_startStatTime.Year, _startStatTime.Month, _startStatTime.Day, endHour, 0, 0);
                _endStatTime = _endStatTime.AddDays(1);
            }
            else
            {
                _endStatTime = new DateTime(_startStatTime.Year, _startStatTime.Month, _startStatTime.Day, endHour, 0, 0);
            }
        }
    }

    internal class MacStatInfo
    {
        public string _msisdn;

        private SignalItem _lastSignal;

        //msidn 在各个扇区信息
        private Dictionary<CGI, StayTimeCal> _cgiStat = new Dictionary<CGI, StayTimeCal>();

        public Dictionary<CGI, StayTimeCal> CgiStat
        {
            get
            {
                return _cgiStat;
            }
        }

        public void NextStat(DateTime curTime)
        {
            if (_lastSignal != null)
            {
                //只保留手机号最后一次所在的cgi
                if (_cgiStat.ContainsKey(_lastSignal.CGI))
                {
                    StayTimeCal item = _cgiStat[_lastSignal.CGI];
                    _cgiStat.Clear();

                    item.Restart(curTime);
                    _cgiStat.Add(_lastSignal.CGI, item);
                }
                else
                {
                    _cgiStat.Clear();
                }
            }
            else
            {
                _cgiStat.Clear();
            }
        }

        public MacStatInfo(string msisdn)
        {
            this._msisdn = msisdn;
        }

        private StayTimeCal GetCgiInfo(CGI cgi)
        {
            if (_cgiStat.ContainsKey(cgi))
            {
                return _cgiStat[cgi];
            }
            else
            {
                StayTimeCal info = new StayTimeCal(cgi);
                _cgiStat.Add(cgi, info);
                return info;
            }
        }

        internal void DealPacket(SignalItem item)
        {
            if (_lastSignal == null)
            {
                if (SignalDefine.IsOutAction(item.action))
                    return;
                _lastSignal = item;

                StayTimeCal info = GetCgiInfo(item.CGI);
                info.In(item.timeStamp);
            }
            else
            {
                StayTimeCal info = GetCgiInfo(item.CGI);
                StayTimeCal infoOld = GetCgiInfo(_lastSignal.CGI);
                _lastSignal = item;

                if (SignalDefine.IsOutAction(item.action))
                {
                    info.Out(item.timeStamp);
                    if (!CGI.IsSameCgi(_lastSignal.CGI, item.CGI))
                    {
                        Debug.Assert(false);
                        infoOld.Out(item.timeStamp);
                    }
                }
                else
                {
                    if (!CGI.IsSameCgi(_lastSignal.CGI, item.CGI))
                    {
                        info.In(item.timeStamp);
                        infoOld.Out(item.timeStamp);
                    }
                    else
                    {
                        info.Update(item.timeStamp);
                    }
                }
            }
        }

        //统计在某个扇区下 进入次数，停留时间
        public class StayTimeCal
        {
            private DateTime _lastTimeStamp = DateTime.MinValue;
            private EN_ActionFlag _action = EN_ActionFlag.en_in;
            private TimeSpan _staySpan = TimeSpan.FromSeconds(0);

            private ushort _inCount = 0;
            private CGI _cgi;

            public CGI CurCgi
            {
                get
                {
                    return _cgi;
                }
            }

            public ushort VisiCount
            {
                get
                {
                    return _inCount;
                }
            }

            public ushort StaySecond(DateTime curTime)
            {
                if (_lastTimeStamp != null && curTime > _lastTimeStamp)
                {
                    TimeSpan span = (curTime - _lastTimeStamp);
                    if (span.TotalMinutes < 60)
                        return (ushort)(_staySpan.TotalSeconds + span.TotalSeconds);
                }
                return (ushort)_staySpan.TotalSeconds;
            }

            public StayTimeCal(CGI cgi)
            {
                _cgi = cgi;
            }

            public void Update(DateTime timeStamp)
            {
                UpdateStay(timeStamp);
                _action = EN_ActionFlag.en_in;
            }

            public void In(DateTime timeStamp)
            {
                UpdateStay(timeStamp);
                _inCount++;
                _action = EN_ActionFlag.en_in;
            }

            public void Out(DateTime timeStamp)
            {
                UpdateStay(timeStamp);

                _action = EN_ActionFlag.en_out;
            }

            private void UpdateStay(DateTime timeStamp)
            {
                if (_lastTimeStamp != DateTime.MinValue
                  && timeStamp > _lastTimeStamp
                  && _action == EN_ActionFlag.en_in)
                {
                    TimeSpan span = (timeStamp - _lastTimeStamp);
                    if (span.TotalMinutes < 60)
                        _staySpan += span;
                }
                _lastTimeStamp = timeStamp;
            }

            internal void Restart(DateTime curTime)
            {
                _lastTimeStamp = curTime;
                _staySpan = TimeSpan.FromSeconds(0);
                _inCount = 0;
            }

            private enum EN_ActionFlag
            {
                en_in,
                en_out
            }
        }
    }

    //每个扇区信息
    public class CgiStatInfo
    {
        private Dictionary<string, MsisdnInfo> _msisdnGroup = new Dictionary<string, MsisdnInfo>();
        public CGI _CurCgi;

        public CgiStatInfo(CGI cgi)
        {
            _CurCgi = cgi;
        }

        public Dictionary<string, MsisdnInfo> MsisdnGroup
        {
            get
            {
                return _msisdnGroup;
            }
        }

        public void AddMsisdn(string msisdn, ushort staySecond, ushort visitCount)
        {
            MsisdnInfo info = new MsisdnInfo(msisdn);
            info.SetStat(staySecond, visitCount);

            if (_msisdnGroup.ContainsKey(msisdn))
            {
                Debug.Assert(false);
                _msisdnGroup.Remove(msisdn);
            }
            _msisdnGroup.Add(msisdn, info);
        }

        public class MsisdnInfo
        {
            public string Msisdn;
            public ushort StaySecond;
            public ushort VisitCount;

            public MsisdnInfo(string msisdn)
            {
                Msisdn = msisdn;
            }

            public void SetStat(ushort staySecond, ushort visitCount)
            {
                StaySecond = staySecond;
                VisitCount = visitCount;
            }
        }
    }
}