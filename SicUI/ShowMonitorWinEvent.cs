namespace SicUI
{
    public class ShowCloseMonitorWinEvent
    {
        public ShowCloseMonitorWinEvent(bool isShow)
        {
            IsShow = isShow;
        }

        public bool IsShow { get; }
    }
}
