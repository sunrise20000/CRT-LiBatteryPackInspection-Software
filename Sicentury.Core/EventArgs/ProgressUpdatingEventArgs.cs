namespace Sicentury.Core.EventArgs
{
    public class ProgressUpdatingEventArgs : System.EventArgs
    {
        #region Constructors

        public ProgressUpdatingEventArgs(int currentProgress, int totalProgress, string message)
        {
            CurrentProgress = currentProgress;
            TotalProgress = totalProgress;
            Message = message;
        }

        #endregion

        #region Properites

        /// <summary>
        /// 总进度。
        /// </summary>
        public int TotalProgress { get; }
        
        /// <summary>
        /// 当前进度。
        /// </summary>
        public int CurrentProgress { get; }

        /// <summary>
        /// 消息。
        /// </summary>
        public string Message { get; }

        #endregion
    }
}
