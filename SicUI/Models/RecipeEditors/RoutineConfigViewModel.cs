using Caliburn.Micro;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.ClientBase;
using System;
using System.Collections.ObjectModel;
using MECF.Framework.UI.Client.CenterViews.Editors;

namespace SicUI.Models.RecipeEditors
{
    public enum RoutineType
    {
        ATMIdle = 0,
        VACIdle,
        ProcessIdle,
        Pump,
        Vent,
        Purge,
        Clean,
        ExchangeMOTCS,
        ExchangeMOTMA,
        Isolation,
        LeakCheck,
        Abort
    }

    public class RoutineConfigViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        public ObservableCollection<string> RoutineTypeList { get; set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            RoutineTypeList = new ObservableCollection<string>();

            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.ATMIdleFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.VACIdleFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.ProcessIdleFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.PumpFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.VentFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.PurgeFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.CleanFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.ExchangeMOTCSFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.ExchangeMOTMAFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.IsolationFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.LeakCheckFileName").ToString());
            RoutineTypeList.Add(QueryDataClient.Instance.Service.GetConfig("PM.PM1.AbortFileName").ToString());
        }

        public void Select(string strRoutineType)
        {
            var dialog =
                new RecipeSelectDialogViewModel(false, false, ProcessTypeFileItem.ProcessFileTypes.Routine)
                {
                    DisplayName = "Select Routine"
                };
            var wm = new WindowManager();
            var bret = wm.ShowDialog(dialog);
            if (bret == true)
            {
                var index = (int)(RoutineType)(Enum.Parse(typeof(RoutineType), strRoutineType));
                RoutineTypeList[index] = dialog.DialogResult;
            }
        }

        public void SaveRoutineConfig()
        {
            string RoutineName;
            for (int i = 0; i < RoutineTypeList.Count; i++)
            {
                RoutineName = RoutineType.GetName(typeof(RoutineType), i);
                RoutineName += "FileName";
                InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"PM.PM1.{RoutineName}", RoutineTypeList[i]);
            }
        }
    }
}
