using System;
using MECF.Framework.Common.ControlDataContext;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.DataHistory
{
    public class TimeChartDataLine : ChartDataLine
    {
        public string Module { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime TokenTime { get; set; }

        private string[] _dbModules = { "PM1", "PM2", "PM3", "PM4" };

        public TimeChartDataLine(string dataName, DateTime startTime, DateTime endTime) : base(dataName)
        {
            StartTime = startTime;
            EndTime = endTime;
            TokenTime = startTime;
            Module = "System";
            foreach (var dbModule in _dbModules)
            {
                if (dataName.StartsWith(dbModule) || dataName.StartsWith("IO." + dbModule))
                {
                    Module = dbModule;
                    break;
                }
            }
        }
    }
}
