using System.Collections.Generic;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Mainframe.Aligners;
using Mainframe.Buffers;
using Mainframe.Cassettes;
using Mainframe.EFEMs;
using Mainframe.LLs;
using Mainframe.TMs;
using Mainframe.UnLoads;
using MECF.Framework.Common.Equipment;
using SicPM;
using SicRT.Equipments.Systems;

namespace SicRT.Modules
{
    public class HomeAll
    {
        private bool IsPM2Install = SC.GetConfigItem("System.SetUp.IsPM2Installed").BoolValue;

        List<List<IModuleDevice>> _lstModules = new List<List<IModuleDevice>>();
        List<IModuleDevice> _homingModules = new List<IModuleDevice>();

        public HomeAll()
        {

        }

        public Result Start(params object[] objs)
        {
            _lstModules.Clear();
            _homingModules.Clear();

            var dicModules = new Dictionary<string, bool>();

            var tm = Singleton<EquipmentManager>.Instance.Modules[ModuleName.TM] as TMModule;
            List<IModuleDevice> lstA = new List<IModuleDevice>();
            if (tm.IsInstalled)
            {
                lstA.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.TM] as IModuleDevice );
                dicModules.Add(tm.Module, tm.IsBusy);
            }

            var lstModules = new List<IModuleDevice>();
            var EFEM = Singleton<EquipmentManager>.Instance.Modules[ModuleName.EFEM] as EFEMModule;
            var waferRobot = Singleton<EquipmentManager>.Instance.Modules[ModuleName.WaferRobot] as WaferRobotModule;
            var trayRobot = Singleton<EquipmentManager>.Instance.Modules[ModuleName.TrayRobot] as TrayRobotModule;
            if (EFEM.IsInstalled)
            {
                lstA.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.EFEM] as IModuleDevice);
                lstA.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.TrayRobot] as IModuleDevice);
                lstA.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.WaferRobot] as IModuleDevice);
                
                dicModules.Add(EFEM.Module, EFEM.IsBusy);
                dicModules.Add(waferRobot.Module, waferRobot.IsBusy);
                dicModules.Add(trayRobot.Module, trayRobot.IsBusy);

            }
            _lstModules.Add(lstA);


            var ll = Singleton<EquipmentManager>.Instance.Modules[ModuleName.LoadLock] as LoadLockModule;
            if (ll.IsInstalled)
            {
                lstModules.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.LoadLock] as IModuleDevice);
                dicModules.Add(ll.Module, ll.IsBusy);
            }

            var unLoad = Singleton<EquipmentManager>.Instance.Modules[ModuleName.UnLoad] as UnLoadModule;
            if (unLoad.IsInstalled)
            {
                lstModules.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.UnLoad] as IModuleDevice);
                dicModules.Add(unLoad.Module, unLoad.IsBusy);
            }

            var buffer = Singleton<EquipmentManager>.Instance.Modules[ModuleName.Buffer] as BufferModule;
            if (buffer.IsInstalled)
            {
                lstModules.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.Buffer] as IModuleDevice);
                dicModules.Add(buffer.Module, buffer.IsBusy);
            }

            var pm1 = Singleton<EquipmentManager>.Instance.Modules[ModuleName.PM1] as PMModule;
            if (pm1.IsInstalled)
            {
                lstModules.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.PM1] as IModuleDevice);
                dicModules.Add(pm1.Module, pm1.IsBusy);
            }

            //var pm2 = Singleton<EquipmentManager>.Instance.Modules[ModuleName.PM2] as PMModule;
            //if (pm2.IsInstalled)
            //{
            //    lstModules.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.PM2] as IModuleDevice);
            //    dicModules.Add(pm2.Module, pm2.IsBusy);
            //}

            var aligner = Singleton<EquipmentManager>.Instance.Modules[ModuleName.Aligner] as AlignerModule;
            if (aligner.IsInstalled)
            {
                lstModules.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.Aligner] as IModuleDevice);
                dicModules.Add(aligner.Module, aligner.IsBusy);
            }

            var cassAL = Singleton<EquipmentManager>.Instance.Modules[ModuleName.CassAL] as CassetteModule;
            if (cassAL.IsInstalled)
            {
                lstModules.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.CassAL] as IModuleDevice);
                dicModules.Add(cassAL.Module, cassAL.IsBusy);
            }

            var cassAR = Singleton<EquipmentManager>.Instance.Modules[ModuleName.CassAR] as CassetteModule;
            if (cassAR.IsInstalled)
            {
                lstModules.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.CassAR] as IModuleDevice);
                dicModules.Add(cassAR.Module, cassAR.IsBusy);
            }

            var cassBL = Singleton<EquipmentManager>.Instance.Modules[ModuleName.CassBL] as CassetteModule;
            if (cassBL.IsInstalled)
            {
                lstModules.Add(Singleton<EquipmentManager>.Instance.Modules[ModuleName.CassBL] as IModuleDevice);
                dicModules.Add(cassBL.Module, cassBL.IsBusy);
            }

            _lstModules.Add(lstModules);


            foreach (var item in dicModules)
            {
                if (item.Value)
                {
                    EV.PostWarningLog("System", $"{item.Key} is busy，can not do Initialize");
                    return Result.FAIL;
                }
            }

            return Result.RUN;
        }

        public Result Monitor(params object[] objs)
        {
            if (_homingModules.Count == 0 && _lstModules.Count == 0)
                return Result.DONE;

            if (_homingModules.Count > 0)
            {
                foreach (var module in _homingModules)
                {
                    if (module.IsError)
                        return Result.FAIL;

                    if (!module.IsReady)
                        return Result.RUN;
                }

                _homingModules.Clear();
                if (_lstModules.Count == 0)
                    return Result.DONE;
            }

            if (_homingModules.Count == 0)
            {
                foreach (var moduleEntity in _lstModules[0])
                {
                    if (!moduleEntity.IsReady)
                    {
                        if (!moduleEntity.Home(out string reason))
                            return Result.FAIL;

                        _homingModules.Add(moduleEntity);
                    }
                }

                _lstModules.RemoveAt(0);
            }


            return Result.RUN;
        }

        public void Clear()
        {
            _lstModules.Clear();
            _homingModules.Clear();
        }


    }
}
