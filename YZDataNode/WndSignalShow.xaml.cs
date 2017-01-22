using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using YZNetPacket;

namespace YZDataNode
{
    /// <summary>
    /// WndSignalShow.xaml 的交互逻辑
    /// </summary>
    public partial class WndSignalShow : Window
    {
        public bool IsShowSignal = false;

        public WndSignalShow()
        {
            InitializeComponent();
        }

        private void checkShowSignal_Click(object sender, RoutedEventArgs e)
        {
            IsShowSignal = (checkShowSignal.IsChecked == true);
        }

        private List<SignalItem> _listSignal = new List<SignalItem>();
        private DispatcherTimer _timer;

        public void PutSignal(SignalItem item)
        {
            lock (_listSignal)
            {
                _listSignal.Add(item);
            }
        }

        private SignalItem GetSignal()
        {
            lock (_listSignal)
            {
                if (_listSignal.Count == 0)
                    return null;
                SignalItem item = _listSignal[0];
                _listSignal.RemoveAt(0);
                return item;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);   //间隔1秒
            _timer.Tick += new EventHandler(TimeUp);
            _timer.Start();
        }

        private void TimeUp(object sender, EventArgs e)
        {
            while (true)
            {
                SignalItem item = GetSignal();
                if (item == null)
                    break;
                ShowSignal(item);
            }
        }

        private void ShowSignal(SignalItem item)
        {
            try
            {
                LvShowItem showItem = item.ToShowItem();
                showItem.NO = (lvItemFlow.Items.Count + 1).ToString();
                lvItemFlow.Items.Add(showItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnClearSignal_Click(object sender, RoutedEventArgs e)
        {
            lvItemFlow.Items.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}