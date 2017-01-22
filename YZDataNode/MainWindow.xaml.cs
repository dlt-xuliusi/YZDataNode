using System;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using YZNetPacket;

namespace YZDataNode
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private DispatcherTimer _timer;

        //加载 窗体初始化
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AppMain.MainWnd = this;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);   //间隔1秒
            _timer.Tick += new EventHandler(TimeUp);
            _timer.Start();

            AppMain.Init();
        }

        private DateTime _startTime = DateTime.Now;

        private void TimeUp(object sender, EventArgs e)
        {
            txtAppTime.Text = AppHelper.GetTimeSpanStr(DateTime.Now - _startTime);

            UpdateStat();
            ShowLog();
            ShowSignal();
        }

        private long _rcvPacketCount = 0;

        private void UpdateStat()
        {
            //状态日志
            ListStatInfo.Items.Clear();
            ListItemValue stat = new ListItemValue();
            stat.Str1 = "收包个数:速度";

            long speed = AppMain._statManage.RcvPacketCount - _rcvPacketCount;
            _rcvPacketCount = AppMain._statManage.RcvPacketCount;

            stat.Str2 = string.Format("{0}:{1}", _rcvPacketCount, speed);
            ListStatInfo.Items.Add(stat);
        }

        private void menuItemCopySel_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder txt = new StringBuilder();
            foreach (ListItemValue item in ListLogInfo.SelectedItems)
            {
                txt.AppendFormat("{0}\r\n", item.ToString());
            }
            Clipboard.SetDataObject(txt.ToString());
        }

        private void menuItemCopyAll_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder txt = new StringBuilder();
            foreach (ListItemValue item in ListLogInfo.Items)
            {
                txt.AppendFormat("{0}\r\n", item.ToString());
            }
            Clipboard.SetDataObject(txt.ToString());
        }

        private void menuItemClearAll_Click(object sender, RoutedEventArgs e)
        {
            ListLogInfo.Items.Clear();
        }

        private void ShowLog()
        {
            //显示日志
            while (true)
            {
                string item = AppLog.GetLog();
                if (item == string.Empty)
                    break;

                ListItemValue strs = new ListItemValue();
                strs.tag = item;
                strs.Str1 = (ListLogInfo.Items.Count + 1).ToString();
                strs.Str2 = AppHelper.DateTimeToStr(DateTime.Now);
                strs.Str3 = item;
                ListLogInfo.Items.Add(strs);

                while (ListLogInfo.Items.Count > 2000)
                    ListLogInfo.Items.RemoveAt(0);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("确定退出程序？", "询问", MessageBoxButton.YesNo,
              MessageBoxImage.Question, MessageBoxResult.No);
            //关闭窗口
            if (result == MessageBoxResult.Yes)
            {
                Environment.Exit(0);
            }
            //不关闭窗口
            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }

        private WndSignalShow _wndSignalShow = new WndSignalShow();

        private void btnSignalShow_Click(object sender, RoutedEventArgs e)
        {
            if (_wndSignalShow == null)
            {
                _wndSignalShow = new WndSignalShow();
                _wndSignalShow.Owner = this;
                _wndSignalShow.WindowStartupLocation = WindowStartupLocation.Manual;
                _wndSignalShow.Left = 800;
                _wndSignalShow.Top = 300;
            }
            _wndSignalShow.Show();
        }

        private ObjectPool<NetHead> _listPacket = new ObjectPool<NetHead>();

        public void PutPacket(NetHead packet)
        {
            if (_wndSignalShow == null || _wndSignalShow.Visibility != Visibility.Visible)
                return;

            _listPacket.PutObj(packet);
        }

        private void ShowSignal()
        {
            while (true)
            {
                NetHead packet = _listPacket.GetObj();
                if (packet == null)
                {
                    break;
                }

                DealRcvPacket(packet);
            }
        }

        private void DealRcvPacket(NetHead packet)
        {
            if (_wndSignalShow != null
                && _wndSignalShow.Visibility == Visibility.Visible
                && _wndSignalShow.IsShowSignal
                && packet.PacketType == En_NetType.EN_NetSignalData)
            {
                NetSignalData data = (NetSignalData)packet;
                foreach (SignalItem item in data.ListSignal)
                {
                    _wndSignalShow.PutSignal(item);
                }
            }
        }
    }

    public class ListItemValue
    {
        public string Str1 { get; set; }
        public string Str2 { get; set; }
        public string Str3 { get; set; }

        public object tag;

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Str1, Str2, Str3);
        }
    }
}