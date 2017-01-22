using System;
using System.Globalization;
using System.Text;

namespace YZNetPacket
{
    public class SignalItem
    {
        public DateTime timeStamp;
        public EN_Action action;
        public ushort lac;
        public ushort ci;
        public string msisdn = string.Empty;
        public string msisdn2 = string.Empty;

        public UInt32 areaCode; //区号
        public EN_MsisdnIndex msisdnIndex;
        public EN_SortStat sortStat;

        public CGI CGI
        {
            get
            {
                return new CGI(lac, ci);
            }
        }

        public string MsisdnOfCurAction
        {
            get
            {
                if (msisdnIndex == EN_MsisdnIndex.first)
                    return msisdn;
                if (msisdnIndex == EN_MsisdnIndex.second)
                    return msisdn2;
                return string.Empty;
            }
        }

        public bool ContainMisdn(ulong misdn)
        {
            if (this.msisdn == misdn.ToString())
                return true;

            if (!string.IsNullOrEmpty(msisdn2))
            {
                return msisdn2.Contains(misdn.ToString());
            }
            return false;
        }

        public bool IsHaveTwoMsisdn()
        {
            return !string.IsNullOrEmpty(msisdn2);
        }

        public override string ToString()
        {
            try
            {
                EN_Action enAction = (EN_Action)action;
                return string.Format("{0}|{1}|{2}|{3}|{4}|{5}",
                    SignalDefine.DateTimeToStr(timeStamp), enAction, lac, ci, msisdn, msisdn2);
            }
            catch (Exception ex)
            {
                return string.Format("{0}|{1}|{2}|{3}|{4}|{5}",
                   SignalDefine.DateTimeToStr(timeStamp), action, lac, ci, msisdn, msisdn2);
            }
        }

        public void MsisdnAdjust()
        {
            SignalDefine.MsisdnAdjust(ref msisdn);
            SignalDefine.MsisdnAdjust(ref msisdn2);
        }

        public void ParseAction()
        {
            //两个手机号 才值得分析
            if (!IsHaveTwoMsisdn())
            {
                msisdnIndex = EN_MsisdnIndex.first;
                return;
            }
            msisdnIndex = SignalDefine.GetActionMsisdnIndex((EN_Action)action);
        }

        public string ToStringDetail()
        {
            try
            {
                return string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
                    SignalDefine.DateTimeToStr(timeStamp), action, lac, ci, msisdn, msisdn2, sortStat, msisdnIndex);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private bool HaveMsidn2
        {
            get
            {
                return !string.IsNullOrEmpty(msisdn2);
            }
        }

        public static string DateTimeToStr(DateTime time)
        {
            return time.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public LvShowItem ToShowItem()
        {
            LvShowItem item = new LvShowItem();
            item.TimeStamp = DateTimeToStr(timeStamp);
            try
            {
                EN_Action enAction = (EN_Action)action;
                item.Action = enAction.ToString();
            }
            catch (Exception ex)
            {
                item.Action = action.ToString();
            }

            item.Lac = lac.ToString();
            item.Ci = ci.ToString();
            item.Msisdn = msisdn.ToString();
            item.Msisdn2 = msisdn2;
            item.MsIndex = ((int)msisdnIndex).ToString();
            item.SortFlag = ((sortStat == EN_SortStat.sort) ? 1 : 0).ToString();
            return item;
        }

        public static SignalItem FromTxtLine(string txt)
        {
            try
            {
                string[] items = txt.Split('|');
                if (items.Length < 5)
                    return null;

                SignalItem item = new SignalItem();
                item.timeStamp = SignalDefine.ParseSignalTime(items[0]);
                item.action = (EN_Action)byte.Parse(items[1]);
                item.lac = ushort.Parse(items[2]);
                item.ci = ushort.Parse(items[3]);
                item.msisdn = items[4].Trim();

                if (items.Length > 5
                    && !string.IsNullOrEmpty(items[5]))
                {
                    item.msisdn2 = items[5].Trim();
                }
                else
                {
                    item.msisdnIndex = EN_MsisdnIndex.first;
                }
                return item;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }

    public class LvShowItem
    {
        public string NO { get; set; }
        public string TimeStamp { get; set; }
        public string Action { get; set; }
        public string Lac { get; set; }
        public string Ci { get; set; }
        public string Msisdn { get; set; }
        public string Msisdn2 { get; set; }

        public string MsIndex { get; set; }
        public string SortFlag { get; set; }

        public string ToFileString()
        {
            EN_Action actValue = 0;
            try
            {
                actValue = (EN_Action)Enum.Parse(typeof(EN_Action), Action);
            }
            catch (Exception)
            { }
            return string.Format("{0}|{1}|{2}|{3}|{4}|{5}",
                  TimeStamp.Replace("-", ""), (byte)actValue, Lac, Ci, Msisdn, Msisdn2);
        }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                NO, TimeStamp, Action, Lac, Ci, Msisdn, Msisdn2);
        }
    }

    public enum EN_Action : byte
    {
        未知 = 0,
        主叫 = 1,
        紧急呼叫 = 2,
        被叫 = 3,
        视频主叫 = 4,
        视频被叫 = 5,
        发短信 = 6,
        收短信 = 7,
        切入 = 8,
        切出 = 9,
        BSC内切换 = 10,
        正常位置更新 = 12,
        周期性位置更新 = 13,
        IMSI附着 = 14,
        IMSI分离 = 15,
        短信状态报 = 25,
        切出3G2G = 26,
    }

    //手机号码指示
    public enum EN_MsisdnIndex : byte
    {
        unknown = 0,
        first = 1,
        second = 2
    }

    public enum EN_SortStat : byte
    {
        unsort = 0,
        sort = 1,
        unsort_too_old = 2 //信令时间偏差太大，不能参与排序
    }

    public class SignalDefine
    {
        //20160831 23:52:16.624
        public static DateTime ParseSignalTime(string txt)
        {
            try
            {
                DateTime dt = DateTime.ParseExact(txt, "yyyyMMdd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                return dt;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        //去除毫秒，只保留秒
        public static DateTime RemoveMillisecond(DateTime dt)
        {
            return dt.AddMilliseconds(-dt.Millisecond);
        }

        public static ulong ParseUlong(string txt)
        {
            try
            {
                return ulong.Parse(txt);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static EN_MsisdnIndex GetActionMsisdnIndex(EN_Action action)
        {
            switch (action)
            {
                case EN_Action.主叫:
                case EN_Action.紧急呼叫:
                case EN_Action.视频主叫:
                case EN_Action.发短信:
                    {
                        return EN_MsisdnIndex.first;
                    }

                case EN_Action.被叫:
                case EN_Action.视频被叫:
                case EN_Action.收短信:
                    {
                        return EN_MsisdnIndex.second;
                    }
            }
            return EN_MsisdnIndex.unknown;
        }

        //是不是 表示离开某个扇区的信令类型
        public static bool IsOutAction(EN_Action action)
        {
            switch (action)
            {
                case EN_Action.切出:
                case EN_Action.切出3G2G:
                    {
                        return true;
                    }
            }
            return false;
        }

        public static ulong ParseUshort(string txt)
        {
            try
            {
                return ushort.Parse(txt);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static ulong ParseByte(string txt)
        {
            try
            {
                return byte.Parse(txt);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static string DateTimeToStr_Second(DateTime time)
        {
            return time.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string DateTimeToStr(DateTime time)
        {
            return time.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        //对手机号规范化
        public static bool MsisdnAdjust(ref string msisdn)
        {
            if (msisdn.Length == 13)
            {
                if (msisdn.StartsWith("86"))
                {
                    msisdn = msisdn.Substring(2, 11);
                    return true;
                }
            }
            return false;
        }

        public static bool IsMsisdnOfCN(string msisdn)
        {
            return msisdn.Length == 11;
        }

        public static string StringFromNetByte(byte[] data, int offset, out int copyLen)
        {
            byte strLen = data[offset];
            offset++;

            string result = string.Empty;
            if (strLen > 0)
            {
                result = Encoding.ASCII.GetString(data, offset, strLen);
            }
            copyLen = 1 + strLen;
            return result;
        }

        public static void StringToNetByte(string str, byte[] data, int offset, out int copyLen)
        {
            copyLen = 0;
            data[offset] = (byte)str.Length;
            offset++;

            copyLen++;

            if (str.Length > 0)
            {
                byte[] strData = Encoding.ASCII.GetBytes(str);

                Array.Copy(strData, 0, data, offset, strData.Length);
                offset += strData.Length;

                copyLen += strData.Length;
            }
        }
    }

    public struct CGI
    {
        public ushort lac;
        public ushort ci;

        public CGI(ushort lac, ushort ci)
        {
            this.lac = lac;
            this.ci = ci;
        }

        public static bool IsSameCgi(CGI n1, CGI n2)
        {
            return n1.lac == n2.lac && n1.ci == n2.ci;
        }
    }
}