using System;
using System.Collections.ObjectModel;
using System.Threading;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.Common.Equipment;
using MECF.Framework.UI.Client.ClientBase;

namespace SicUI.Client.Models.Platform.LeakCheck
{
    public class LeakCheckItem : NotifiableItem
    {
        public string Guid { get; set; }
        public string OperateTime { get; set; }
        public string Status { get; set; }
        public string LeakRate { get; set; }
        public string StartPressure { get; set; }
        public string StopPressure { get; set; }
        public string LeakCheckTime { get; set; }
        public string Mode { get; set; }
        public string ModuleName { get; set; }
        public string GaslineSelection { get; set; }
    }


    public class TMLeakCheckViewModel : BaseModel
    {
        private CancellationTokenSource _cts;

        public ObservableCollection<LeakCheckItem> LeakCheckData { get; set; }
        public ObservableCollection<LeakCheckItem> LLLeakCheckData { get; set; }
        public ObservableCollection<LeakCheckItem> PM1LeakCheckData { get; set; }
        public ObservableCollection<LeakCheckItem> UnloadLeakCheckData { get; set; }

        public TMLeakCheckViewModel()
        {
            this.DisplayName = "TM";

            LeakCheckData = new ObservableCollection<LeakCheckItem>(); 
            LLLeakCheckData = new ObservableCollection<LeakCheckItem>();
            PM1LeakCheckData = new ObservableCollection<LeakCheckItem>();
            UnloadLeakCheckData = new ObservableCollection<LeakCheckItem>();
        }
        protected override void OnInitialize()
        {
            base.OnInitialize();
            QueryHistory();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            QueryHistory();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            _cts?.Cancel();
        }

        private void QueryHistory()
        {
            try
            {
                _cts = new CancellationTokenSource();

                LeakCheckData.Clear();
                LLLeakCheckData.Clear();
                PM1LeakCheckData.Clear();
                UnloadLeakCheckData.Clear();

                var sql = $"SELECT * FROM \"leak_check_data\" order by \"operate_time\" ASC limit 500;";
                var dbData = QueryDataClient.Instance.Service.QueryData(sql);

                if (dbData == null || dbData.Rows.Count == 0)
                    return;


                for (var i = 0; i < dbData.Rows.Count; i++)
                {
                    var item = new LeakCheckItem();

                    item.Guid = dbData.Rows[i]["guid"].ToString();

                    if (!dbData.Rows[i]["operate_time"].Equals(DBNull.Value))
                        item.OperateTime =
                            ((DateTime)dbData.Rows[i]["operate_time"]).ToString("yyyy-MM-dd HH:mm:ss.fff");

                    item.Status = dbData.Rows[i]["status"].ToString();

                    if (!dbData.Rows[i]["leak_rate"].Equals(DBNull.Value))
                        item.LeakRate = Convert.ToDouble(dbData.Rows[i]["leak_rate"]).ToString("F3");

                    if (!dbData.Rows[i]["start_pressure"].Equals(DBNull.Value))
                        item.StartPressure = Convert.ToDouble(dbData.Rows[i]["start_pressure"]).ToString("F3");

                    if (!dbData.Rows[i]["stop_pressure"].Equals(DBNull.Value))
                        item.StopPressure = Convert.ToDouble(dbData.Rows[i]["stop_pressure"]).ToString("F3");

                    item.LeakCheckTime = dbData.Rows[i]["leak_check_time"].ToString();

                    item.ModuleName = dbData.Rows[i]["module_name"].ToString();

                    if (item.ModuleName == ModuleName.TM.ToString())
                    {
                        LeakCheckData.Add(item);
                    }
                    else if (item.ModuleName == ModuleName.LoadLock.ToString())
                    {
                        LLLeakCheckData.Add(item);
                    }
                    else if (item.ModuleName == ModuleName.PM1.ToString())
                    {
                        PM1LeakCheckData.Add(item);
                    }
                    else if (item.ModuleName == ModuleName.UnLoad.ToString())
                    {
                        UnloadLeakCheckData.Add(item);
                    }
                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
        }

        public void DeleteLeakCheck(LeakCheckItem item)
        {
            InvokeClient.Instance.Service.DoOperation($"System.DBExecute", $"delete from \"leak_check_data\" where \"guid\"='{item.Guid}'");

            if (item.ModuleName == ModuleName.TM.ToString())
                LeakCheckData.Remove(item);
            else if (item.ModuleName == ModuleName.LoadLock.ToString())
                LLLeakCheckData.Remove(item);
            else if (item.ModuleName == ModuleName.UnLoad.ToString())
                UnloadLeakCheckData.Remove(item);
            else if (item.ModuleName == ModuleName.PM1.ToString())
                PM1LeakCheckData.Remove(item);
        }

       
 
        public void LeakCheck(string module)
        {
            //InvokeClient.Instance.Service.DoOperation($"{module}.LeakCheck");

            //if ((PumpTimeSetPoint <= 0) || (LeakCheckTimeSetPoint <= 0))
            //{
            //    DialogBox.ShowWarning("Time not valid");
            //    return;
            //}

            //if (module == "TM")
            //{
            //    InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"TM.LeakCheck.PumpDelayTime", PumpTimeSetPoint);
            //    InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"TM.LeakCheck.LeakCheckDelayTime", LeakCheckTimeSetPoint);

            //    InvokeClient.Instance.Service.DoOperation($"{module}.LeakCheck", PumpTimeSetPoint, LeakCheckTimeSetPoint);
            //}
            //else
            //{
            //    InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"LoadLock.LeakCheck.PumpDelayTime", LLPumpTimeSetPoint);
            //    InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"LoadLock.LeakCheck.LeakCheckDelayTime", LLLeakCheckTimeSetPoint);

            //    InvokeClient.Instance.Service.DoOperation($"{module}.LeakCheck", LLPumpTimeSetPoint, LLLeakCheckTimeSetPoint);
            //}


            //PumpTimeTextSaved = true;
            //NotifyOfPropertyChange(nameof(PumpTimeTextSaved));

            //LeakCheckTimeTextSaved = true;
            //NotifyOfPropertyChange(nameof(LeakCheckTimeTextSaved));
        }

        public void Abort(string module)
        {
            //if (DialogBox.Confirm($"Are you sure you want to abort {module} leak check service?"))
            //{
            //    InvokeClient.Instance.Service.DoOperation($"{module}.Abort");
            //}
            
        }

    }
}
