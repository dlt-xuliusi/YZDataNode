namespace YZDataNode
{
    internal class AppMain
    {
        public static NetManage _netManage = new NetManage();
        public static SignalStatManage _statManage = new SignalStatManage();

        public static MainWindow MainWnd = null;

        public static void Init()
        {
            AppParam.Init();
            _statManage.Init();

            _netManage.EventRcvPacket += _statManage.PutPacket;
            _netManage.EventRcvPacket += MainWnd.PutPacket;

            _netManage.Init();
        }
    }
}