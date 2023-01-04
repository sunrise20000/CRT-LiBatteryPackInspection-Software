using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.EFEM;
using System.Collections.Generic;
using System.Xml;

namespace Mainframe.EFEMs
{
    public class SicEFEM : EFEM
    {
        private ModuleName _module;
        private IoSlitValve _slitvalveUnLoad;
        private IoSlitValve _slitvalveLoadLockLeft;
        private IoSlitValve _slitvalveLoadLockRight;


        public SicEFEM(string module, XmlElement node, string ioModule = "") : base(module)
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _module = ModuleHelper.Converter(Module);
        }


        public override bool Initialize()
        {
            _slitvalveUnLoad = DEVICE.GetDevice<IoSlitValve>("EFEM.UnLoadSubDoor");
            _slitvalveLoadLockLeft = DEVICE.GetDevice<IoSlitValve>("EFEM.LoadLockLSideDoor");
            _slitvalveLoadLockRight = DEVICE.GetDevice<IoSlitValve>("EFEM.LoadLockRSideDoor");

            return base.Initialize();
        }

        public override double ChamberPressure
        {
            get
            {
                return SC.GetValue<double>("EFEM.AtmPressureBase"); 
            }
        }

        public override bool CheckSlitValveOpen(ModuleName module,ModuleName robot)
        {
            if (robot == ModuleName.WaferRobot)
            {
                if (module == ModuleName.LoadLock)
                {
                    return _slitvalveLoadLockLeft.IsOpen;
                }
                else if (module == ModuleName.UnLoad)
                {
                    return _slitvalveUnLoad.IsOpen;
                }
                return true;
            }
            else if (robot == ModuleName.TrayRobot && module == ModuleName.LoadLock)
            {
                return _slitvalveLoadLockRight.IsOpen;
            }
            return true;
        }

        public override bool CheckSlitValveClose(ModuleName module, ModuleName robot)
        {
            if (robot == ModuleName.WaferRobot)
            {
                if (module == ModuleName.LoadLock)
                {
                    return _slitvalveLoadLockLeft.IsClose;
                }
                else if (module == ModuleName.UnLoad)
                {
                    return _slitvalveUnLoad.IsClose;
                }
                return true;
            }
            else if (robot == ModuleName.TrayRobot && module == ModuleName.LoadLock)
            {
                return _slitvalveLoadLockRight.IsClose;
            }
            return true;
        }


        public override bool SetSlitValve(ModuleName module, ModuleName robot, bool isOpen, out string reason)
        {
            reason = string.Empty;
            if (robot == ModuleName.WaferRobot)
            {
                if (module == ModuleName.LoadLock)
                {
                    return _slitvalveLoadLockLeft.SetSlitValve(isOpen, out reason);
                }
                else if (module == ModuleName.UnLoad)
                {
                    return _slitvalveUnLoad.SetSlitValve(isOpen, out reason);
                }
                return true;
            }
            else if (robot == ModuleName.TrayRobot && module == ModuleName.LoadLock)
            {
                return _slitvalveLoadLockRight.SetSlitValve(isOpen, out reason);
            }
            return true;
        }
    }
}
