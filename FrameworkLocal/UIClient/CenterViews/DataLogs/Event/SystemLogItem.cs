namespace MECF.Framework.UI.Client.CenterViews.DataLogs.Event
{
    public class SystemLogItem
    {
        /// <summary>
        /// 时间
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// ICON 
        /// </summary>
        public object Icon { get; set; }

        /// <summary>
        /// 类型：操作日志|事件|其他
        /// </summary>
        public string LogType { get; set; }

        /// <summary>
        /// 针对腔体
        /// </summary>
        public string TargetChamber { get; set; }

        /// <summary>
        /// 发起方
        /// </summary>
        public string Initiator { get; set; }

        /// <summary>
        /// 详情
        /// </summary>
        public string Detail { get; set; }
    }
}
