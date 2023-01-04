using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Equipment;
using RTDefine = Aitex.Core.Common;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class ModuleDataMonitor
    {
        public Dictionary<string, ModuleInfo> _mapWaferDataModule = new Dictionary<string, ModuleInfo>();

        public List<string> _lstWaferDataName = new List<string>();

        private PeriodicJob _monitorThread;

        public ModuleDataMonitor()
        {
            foreach (var moduleInfo in ModuleManager.ModuleInfos)
            {
                if (!string.IsNullOrEmpty(moduleInfo.Value.WaferDataName))
                {
                    _lstWaferDataName.Add(moduleInfo.Value.WaferDataName);
                    _mapWaferDataModule[moduleInfo.Value.WaferDataName] = moduleInfo.Value;
                }
            }

            _monitorThread = new PeriodicJob(500, OnTimer, "Monitor Module Data Thread", true);
        }

        private bool OnTimer()
        {
            try
            {
                Dictionary<string, object> data = QueryDataClient.Instance.Service.PollData(_lstWaferDataName);

                if (data != null && Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        foreach (var waferData in data)
                        {
                            if (!_mapWaferDataModule.ContainsKey(waferData.Key))
                                continue;

                            RTDefine.WaferInfo[] wafers = waferData.Value as RTDefine.WaferInfo[];
                            if (wafers == null)
                                continue;

                            ModuleInfo info = _mapWaferDataModule[waferData.Key];
                            if (info.WaferManager.Wafers.Count == 0)
                            {
                                for (int i = 0; i < wafers.Length; i++)
                                {
                                    info.WaferManager.Wafers.Add(WaferInfoConverter(wafers[i], info.WaferModuleID, i));
                                }

                                if (info.IsWaferReverseDisplay)
                                {
                                    info.WaferManager.Wafers = new ObservableCollection<WaferInfo>(info.WaferManager.Wafers.Reverse());
                                }
                                continue;
                            }

                            if (wafers.Length == info.WaferManager.Wafers.Count)
                            {
                                int index;
                                for (int i = 0; i < wafers.Length; i++)
                                {
                                    if (info.IsWaferReverseDisplay)
                                        index = wafers.Length - i - 1;
                                    else
                                        index = i;

                                    var convertedWafer = WaferInfoConverter(wafers[index], info.WaferModuleID, index);
                                    info.WaferManager.Wafers[i].WaferStatus = convertedWafer.WaferStatus;
                                    info.WaferManager.Wafers[i].WaferID = convertedWafer.WaferID;
                                    info.WaferManager.Wafers[i].SourceName = convertedWafer.SourceName;
                                    info.WaferManager.Wafers[i].WaferTrayStatus = convertedWafer.WaferTrayStatus;
                                    info.WaferManager.Wafers[i].LotID = convertedWafer.LotID;
                                    info.WaferManager.Wafers[i].IsTrayExhausted = wafers[index].TrayProcessCount <= 0;
                                    info.WaferManager.Wafers[i].TrayProcessCount = wafers[index].TrayProcessCount;
                                    info.WaferManager.Wafers[i].RecipeName = convertedWafer.RecipeName;
                                    info.WaferManager.Wafers[i].ToolTip = new ToolTip()
                                    {
                                        Content = $"LotId : {wafers[index].LotId}\r\nWaferId : {wafers[index].WaferID}\r\nSequenceName : {wafers[index].ProcessJobID}\r\nWaferStatus : {wafers[index].Status.ToString()}\r\nNextSequenceStep : {wafers[index].NextSequenceStep + 1}\r\nTrayState : {wafers[index].TrayState.ToString()}\r\nTrayProcessCount : {wafers[index].TrayProcessCount}\r\nTrayOriginStation : {((ModuleName)wafers[index].TrayOriginStation).ToString()}\r\nTrayOriginSlot : {wafers[index].TrayOriginSlot + 1}"
                                    }; 

                                }
                            }
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }

            return true;
        }


        private WaferInfo WaferInfoConverter(RTDefine.WaferInfo awafer, string modid, int slotid)
        {
            WaferInfo wafer = new WaferInfo();
            wafer.ModuleID = modid;
            wafer.SlotID = slotid;
            wafer.SlotIndex = slotid + 1;
            wafer.WaferID = awafer.WaferID;
            wafer.SourceName = awafer.WaferOrigin;
            wafer.LotID = awafer.LotId == "" ? awafer.WaferOrigin : awafer.LotId;
            wafer.WaferStatus = WaferStatusConverter(awafer);
            wafer.WaferTrayStatus = (int)awafer.TrayState; //awafer.TrayState == RTDefine.WaferTrayStatus.Normal ? 1 : 0;
            wafer.RecipeName = "";
            if (awafer.ProcessJob != null && awafer.ProcessJob.Sequence != null)
            {
                for (int i = 0; i < awafer.ProcessJob.Sequence.Steps.Count; i++)
                {
                    if (!String.IsNullOrEmpty(awafer.ProcessJob.Sequence.Steps[i].RecipeName))
                    {
                        wafer.RecipeName = awafer.ProcessJob.Sequence.Steps[i].RecipeName;
                        break;
                    }
                }
            }
            return wafer;
        }

        //0: no wafer
        //1:light blue  == idle no pj
        //2:blue    = idle with pj
        //3:cyan      = in process
        //4:green = complete
        //5:error  = error
        //6:dummy   
        //7:warning  = has error
        //8:partial processed
        private int WaferStatusConverter(RTDefine.WaferInfo awafer)
        {
            if (awafer.Status == RTDefine.WaferStatus.Empty)
                return 0;

            if (awafer.Status == RTDefine.WaferStatus.Normal)
            {
                switch (awafer.ProcessState)
                {
                    case RTDefine.EnumWaferProcessStatus.InProcess: return 3;
                    case RTDefine.EnumWaferProcessStatus.Completed: return awafer.HasWarning ? 7 : 4;
                    case RTDefine.EnumWaferProcessStatus.Failed: return 5;
                    case RTDefine.EnumWaferProcessStatus.Wait: return 3;
                    case RTDefine.EnumWaferProcessStatus.Idle: return awafer.ProcessJob == null ? 1 : 2;
                }
            }

            if (awafer.Status == RTDefine.WaferStatus.Dummy)
            {
                return 6;
            }

            return 5;
        }

    }

}
