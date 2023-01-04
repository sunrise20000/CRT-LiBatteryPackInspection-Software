using Aitex.Core.UI.ControlDataContext;
using MECF.Framework.Common.DBCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SicPM.RecipeExecutions
{
    public partial class Process
    {
        private Dictionary<string, string> processDataDictionary;
        private List<HistoryDataItem> dbFeedBack = new List<HistoryDataItem>();
        private List<HistoryDataItem> dbSetPoint = new List<HistoryDataItem>();

        private List<HistoryDataItem> dbMfcGap = new List<HistoryDataItem>();
        private List<HistoryDataItem> dbPcGap = new List<HistoryDataItem>();

        private List<RecipeEndDataItem> dbMfcGapResults = new List<RecipeEndDataItem>();
        private List<RecipeEndDataItem> dbPcGapResults = new List<RecipeEndDataItem>();

        private List<RecipeEndDataItem> dbMfcSetPointResults = new List<RecipeEndDataItem>();
        private List<RecipeEndDataItem> dbPcSetPointResults = new List<RecipeEndDataItem>();


        private void Initialize()
        {
            processDataDictionary = new Dictionary<string, string>()
            {
                { $"{Module}.Mfc1.FeedBack",  $"{Module}.Mfc1.SetPoint" },
                { $"{Module}.Mfc2.FeedBack",  $"{Module}.Mfc2.SetPoint" },
                { $"{Module}.Mfc3.FeedBack",  $"{Module}.Mfc3.SetPoint" },
                { $"{Module}.Mfc4.FeedBack",  $"{Module}.Mfc4.SetPoint" },
                { $"{Module}.Mfc5.FeedBack",  $"{Module}.Mfc5.SetPoint" },
                { $"{Module}.Mfc6.FeedBack",  $"{Module}.Mfc6.SetPoint" },
                { $"{Module}.Mfc7.FeedBack",  $"{Module}.Mfc7.SetPoint" },
                { $"{Module}.Mfc8.FeedBack",  $"{Module}.Mfc8.SetPoint" },
                { $"{Module}.Mfc9.FeedBack",  $"{Module}.Mfc9.SetPoint" },
                { $"{Module}.Mfc10.FeedBack", $"{Module}.Mfc10.SetPoint" },
                { $"{Module}.Mfc11.FeedBack", $"{Module}.Mfc11.SetPoint" },
                { $"{Module}.Mfc12.FeedBack", $"{Module}.Mfc12.SetPoint" },
                { $"{Module}.Mfc13.FeedBack", $"{Module}.Mfc13.SetPoint" },
                { $"{Module}.Mfc14.FeedBack", $"{Module}.Mfc14.SetPoint" },
                { $"{Module}.Mfc15.FeedBack", $"{Module}.Mfc15.SetPoint" },
                { $"{Module}.Mfc16.FeedBack", $"{Module}.Mfc16.SetPoint" },

                { $"{Module}.Mfc19.FeedBack", $"{Module}.Mfc19.SetPoint" },
                { $"{Module}.Mfc20.FeedBack", $"{Module}.Mfc20.SetPoint" },

                { $"{Module}.Mfc22.FeedBack", $"{Module}.Mfc22.SetPoint" },
                { $"{Module}.Mfc23.FeedBack", $"{Module}.Mfc23.SetPoint" },

                { $"{Module}.Mfc25.FeedBack", $"{Module}.Mfc25.SetPoint" },
                { $"{Module}.Mfc26.FeedBack", $"{Module}.Mfc26.SetPoint" },
                { $"{Module}.Mfc27.FeedBack", $"{Module}.Mfc27.SetPoint" },
                { $"{Module}.Mfc28.FeedBack", $"{Module}.Mfc28.SetPoint" },
                { $"{Module}.Mfc29.FeedBack", $"{Module}.Mfc29.SetPoint" },

                { $"{Module}.Mfc31.FeedBack", $"{Module}.Mfc31.SetPoint" },
                { $"{Module}.Mfc32.FeedBack", $"{Module}.Mfc32.SetPoint" },
                { $"{Module}.Mfc33.FeedBack", $"{Module}.Mfc33.SetPoint" },

                { $"{Module}.Mfc35.FeedBack", $"{Module}.Mfc35.SetPoint" },
                { $"{Module}.Mfc36.FeedBack", $"{Module}.Mfc36.SetPoint" },
                { $"{Module}.Mfc37.FeedBack", $"{Module}.Mfc37.SetPoint" },
                { $"{Module}.Mfc38.FeedBack", $"{Module}.Mfc38.SetPoint" },

                { $"{Module}.Pressure1.FeedBack", $"{Module}.Pressure1.SetPoint" },
                { $"{Module}.Pressure2.FeedBack", $"{Module}.Pressure2.SetPoint" },
                { $"{Module}.Pressure3.FeedBack", $"{Module}.Pressure3.SetPoint" },
                { $"{Module}.Pressure4.FeedBack", $"{Module}.Pressure4.SetPoint" },
                { $"{Module}.Pressure5.FeedBack", $"{Module}.Pressure5.SetPoint" },
                { $"{Module}.Pressure6.FeedBack", $"{Module}.Pressure6.SetPoint" },
                { $"{Module}.Pressure7.FeedBack", $"{Module}.Pressure7.SetPoint" },

            };
        }

        private bool Calculte()
        {
            GetStepHistoryData();

            GetGapList();

            dbMfcGapResults = CalculateProcessData(dbMfcGap);
            dbPcGapResults = CalculateProcessData(dbPcGap);

            dbMfcSetPointResults = CalculateProcessData(dbSetPoint.Where(m => m.dbName.Contains("Mfc")).ToList());
            dbPcSetPointResults = CalculateProcessData(dbSetPoint.Where(m => m.dbName.Contains("Pressure")).ToList());

            return true;
        }

        private void GetStepHistoryData()
        {
            if (_stepTimer.GetElapseTime() > 5 * 60 * 1000)
            {
                DateTime begin = DateTime.Now.AddMilliseconds(-5 * 60 * 1000);
                DateTime end = DateTime.Now;

                dbFeedBack = ProcessDataRecorder.GetHistoryDataFromStartToEnd(processDataDictionary.Keys, begin, end, Module);

                List<string> processDataValueList = new List<string>();
                foreach (var item in processDataDictionary.Values)
                {
                    if (!string.IsNullOrEmpty(item))
                        processDataValueList.Add(item);
                }

                dbSetPoint = ProcessDataRecorder.GetHistoryDataFromStartToEnd(processDataValueList, begin, end, Module);
            }
        }

        private void GetGapList()
        {
            foreach (var setPoint in dbSetPoint)
            {
                foreach (var feedBack in dbFeedBack)
                {
                    if (setPoint.dateTime == feedBack.dateTime && setPoint.dbName == feedBack.dbName)
                    {
                        var gap = new HistoryDataItem();

                        gap.dateTime = feedBack.dateTime;
                        gap.dbName = feedBack.dbName;
                        gap.value = feedBack.value - setPoint.value;

                        if (gap.dbName.Contains("Mfc"))
                        {
                            dbMfcGap.Add(gap);
                        }
                        else if (gap.dbName.Contains("Pressure"))
                        {
                            dbPcGap.Add(gap);
                        }
                    }
                }
            }
        }

        private List<RecipeEndDataItem> CalculateProcessData(List<HistoryDataItem> dbGap)
        {
            List<string> feedBackNames = new List<string>(processDataDictionary.Keys);
            List<RecipeEndDataItem> dbGapResults = new List<RecipeEndDataItem>();

            foreach (var feedBackName in feedBackNames)
            {
                var gapList = dbGap.FindAll(item => item.dbName == feedBackName);

                dbGapResults.Add(new RecipeEndDataItem(feedBackName, gapList));
            }

            return dbGapResults;
        }

        
       
    }
}
