using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using Caliburn.Micro.Core;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using SciChart.Core.Extensions;
using SicUI.Client.Models.Platform.LeakCheck;

namespace SicUI.Models.PMs
{
    public class PMLeakCheckViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        [Subscription("Status")]
        public string LeakCheckStatus { get; set; }

        [Subscription("LeakCheckElapseTime")]
        public int LeakCheckElapseTime { get; set; }

        [Subscription("ChamberPressure")]
        public double ChamberPressure { get; set; }

        public int PumpTimeSetPoint { get; set; }

        public int LeakCheckTimeSetPoint { get; set; }

        public bool PumpTimeTextSaved { get; set; }

        public bool LeakCheckTimeTextSaved { get; set; }

        public ObservableCollection<LeakCheckItem> LeakCheckData { get; set; }

        private string LeakCheckPreviousStatus { get; set; }

        public List<string> Types
        {
            get { return new List<string>() { "ChamberOnly", "ChamberAndGasline", "ChamberAndGaslineToFacility" }; }
        }

        private string _selectedType;
        public string SelectedType
        {
            get { return _selectedType; }
            set
            {
                _selectedType = value;
                NotifyOfPropertyChange("SelectedType");
            }
        }

        public bool GasLineTypeSelected => SelectedType != Types[0]; // 选择类型不是ChamberOnly，MFC选项 Enable
        public double OpacityGasLine => GasLineTypeSelected ? 1 : 0.3;

        public bool IsMfc1Checked { get; set; }
        public bool IsMfc2Checked { get; set; }
        public bool IsMfc3Checked { get; set; }
        public bool IsMfc4Checked { get; set; }
        
        private bool[] IsMfcChecked
        {
            get
            {
                bool[] isMfcCheck = new bool[]
                {
                    IsMfc1Checked, 
                    IsMfc2Checked,
                    IsMfc3Checked,
                    IsMfc4Checked,
                };

                return isMfcCheck;
            }
        }

        public PMLeakCheckViewModel()
        {
            this.DisplayName = "PM";

            PumpTimeTextSaved = true;
            LeakCheckTimeTextSaved = true;

            LeakCheckData = new ObservableCollection<LeakCheckItem>();

        }
        protected override void OnInitialize()
        {
            base.OnInitialize();

            SelectedType = Types[0];

            QueryHistory();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            GetConfig();
        }

        private void GetConfig()
        {
            string PumpDelayTime = QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.LeakCheck.ContinuePumpTime").ToString();
            string LeakCheckDelayTime = QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.LeakCheck.LeakCheckDelayTime").ToString();

            if (!int.TryParse(PumpDelayTime, out int pumpDelayTime) || pumpDelayTime < 0)
            {
                DialogBox.ShowWarning($"{PumpDelayTime} is not a valid pump delay time value");
                return;
            }

            if (!int.TryParse(LeakCheckDelayTime, out int leakCheckDelayTime) || leakCheckDelayTime < 0)
            {
                DialogBox.ShowWarning($"{LeakCheckDelayTime} is not a valid leak check delay time value");
                return;
            }

            PumpTimeSetPoint = pumpDelayTime;
            LeakCheckTimeSetPoint = leakCheckDelayTime;
        }

        private void QueryHistory()
        {
            try
            {
                LeakCheckData.Clear();
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    string sql = $"SELECT * FROM \"leak_check_data\" order by \"operate_time\" ASC;";

                    DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

                    if (dbData == null || dbData.Rows.Count == 0)
                        return;

                    for (int i = 0; i < dbData.Rows.Count; i++)
                    {
                        LeakCheckItem item = new LeakCheckItem();

                        item.Guid = dbData.Rows[i]["guid"].ToString();

                        if (!dbData.Rows[i]["operate_time"].Equals(DBNull.Value))
                            item.OperateTime = ((DateTime)dbData.Rows[i]["operate_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                        item.Status = dbData.Rows[i]["status"].ToString();

                        if (!dbData.Rows[i]["leak_rate"].Equals(DBNull.Value))
                            item.LeakRate = Convert.ToDouble(dbData.Rows[i]["leak_rate"]).ToString("F2");

                        if (!dbData.Rows[i]["start_pressure"].Equals(DBNull.Value))
                            item.StartPressure = Convert.ToDouble(dbData.Rows[i]["start_pressure"]).ToString("F2");

                        if (!dbData.Rows[i]["stop_pressure"].Equals(DBNull.Value))
                            item.StopPressure = Convert.ToDouble(dbData.Rows[i]["stop_pressure"]).ToString("F2");

                        item.LeakCheckTime = dbData.Rows[i]["leak_check_time"].ToString();

                        item.Mode = dbData.Rows[i]["mode"].ToString();

                        item.ModuleName = dbData.Rows[i]["module_name"].ToString();

                        item.GaslineSelection = dbData.Rows[i]["gasline_selection"].ToString();

                        if (item.ModuleName == SystemName)
                            LeakCheckData.Add(item);
                    }
                }));
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
        }

        public void DeleteLeakCheck(LeakCheckItem item)
        {
            InvokeClient.Instance.Service.DoOperation($"System.DBExecute", $"delete from \"leak_check_data\" where \"guid\"='{item.Guid}'");

            LeakCheckData.Remove(item);
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            if (LeakCheckPreviousStatus == "LeakCheck" && LeakCheckStatus != LeakCheckPreviousStatus)
            {
                QueryHistory();
            }

            LeakCheckPreviousStatus = LeakCheckStatus;
        }

        public void LeakCheck()
        {
            if ((PumpTimeSetPoint <= 0) || (LeakCheckTimeSetPoint <= 0))
            {
                DialogBox.ShowWarning("Time not valid");
                return;
            }

            //if (SelectedType == Types[2])
            //{
            //    MessageBoxResult result = MessageBox.Show("Please confirm that the N2 manual valve is closed.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //    if (result == MessageBoxResult.No)
            //    {
            //        return;
            //    }     
            //}

            InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"PM.{SystemName}.LeakCheck.ContinuePumpTime", PumpTimeSetPoint);
            InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"PM.{SystemName}.LeakCheck.LeakCheckDelayTime", LeakCheckTimeSetPoint);
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.LeakCheck", PumpTimeSetPoint, LeakCheckTimeSetPoint);

            PumpTimeTextSaved = true;
            NotifyOfPropertyChange(nameof(PumpTimeTextSaved));


            LeakCheckTimeTextSaved = true;
            NotifyOfPropertyChange(nameof(LeakCheckTimeTextSaved));
        }

        public void Abort()
        {
            if (DialogBox.Confirm($"Are you sure you want to abort {SystemName} leak check service?"))
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.Abort");
            }

        }

    }
}
