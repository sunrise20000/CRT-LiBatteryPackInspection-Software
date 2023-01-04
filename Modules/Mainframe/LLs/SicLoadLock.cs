using Aitex.Core.Common;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Mainframe.Devices;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using System;
using System.Xml;
using MECF.Framework.RT.EquipmentLibrary.Core;

namespace Mainframe.LLs
{
    public class SicLoadLock : LoadLock
    {
        private IoLift4  _lift;
        private IoClaw   _waferClaw;
        private IoClaw   _trayClaw;
        private IoSensor _lid;
        private IoValve  _ventValve;
        private IoValve  _slowPumpValve;
        private IoValve  _fastPumpValve;
        private IoSensor _LLWaferPlaced;
        private IoSensor _LLTrayPlaed;

        private IoValve _loadBanlance_V85;

        private IoPressureMeter3 _forelineGuage;
        private IoPressureMeter3 _chamberGuage;

        private IoSlitValve _LeftDoor;
        private IoSlitValve _RightDoor;
        private IoSlitValve _VacDoor;

        private double _balancePressureDiff = 0;

        private IoLoadRotation _IoLoadRotation;
        
        private R_TRIG _trigSlowPumpLockerError = new R_TRIG();
        private R_TRIG _trigSlowPumpLockerNotExists = new R_TRIG();

        public override double ChamberPressure
        {
            get 
            {
                return _chamberGuage.Value; 
            }
        }

        public override double ForelinePressure
        {
            get 
            {
                return _forelineGuage.Value; 
            }
        }
        public bool LidClosed
        {
            get
            {
                return CheckLidClose();
            }
        }

        public SicLoadLock(string module, XmlElement node, string ioModule = "") : base(module)
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
        }

        public override bool Initialize()
        {
            _slowPumpValve = DEVICE.GetDevice<IoValve>("TM.LoadSlowPump");
            _fastPumpValve = DEVICE.GetDevice<IoValve>("TM.LoadFastPump");
            _ventValve = DEVICE.GetDevice<IoValve>("TM.LoadVent");
            _loadBanlance_V85 = DEVICE.GetDevice<IoValve>("TM.TMLoadBanlance");

            _lid =  DEVICE.GetDevice<IoSensor>("LoadLock.LLLidClosed");

            _lift = DEVICE.GetDevice<IoLift4>("LoadLock.LLLift");
            _waferClaw= DEVICE.GetDevice<IoClaw>("LoadLock.LLWaferClaw");
            _trayClaw = DEVICE.GetDevice<IoClaw>("LoadLock.LLTrayClaw");
            _LLWaferPlaced = DEVICE.GetDevice<IoSensor>("TM.LLWaferPlaced");
            _LLTrayPlaed = DEVICE.GetDevice<IoSensor>("TM.LLTrayPresence");

            _chamberGuage = DEVICE.GetDevice<IoPressureMeter3>("TM.LLPressure");
            _forelineGuage = DEVICE.GetDevice<IoPressureMeter3>("TM.ForelinePressure");

            _VacDoor = DEVICE.GetDevice<IoSlitValve>("TM.LoadLockDoor");
            _LeftDoor = DEVICE.GetDevice<IoSlitValve>("EFEM.LoadLockLSideDoor");
            _RightDoor = DEVICE.GetDevice<IoSlitValve>("EFEM.LoadLockRSideDoor");

            _balancePressureDiff = SC.GetValue<double>("TM.PressureBalance.BalanceMaxDiffPressure");

            _IoLoadRotation  = DEVICE.GetDevice<Mainframe.Devices.IoLoadRotation>("Load.Rotation");

            return base.Initialize();
        }

        public override bool CheckRotationState()
        {
            return !_IoLoadRotation.IsServoError && !_IoLoadRotation.IsServoBusy && _IoLoadRotation.IsServoOn;
        }

        public override bool CheckAtm()
        {
            return _chamberGuage.Value >= SC.GetValue<double>("LoadLock.AtmPressureBase");
        }

        public override bool CheckTransferPressure()
        {
            return Math.Abs(SC.GetValue<double>("LoadLock.VacuumPressureBase") - _chamberGuage.Value) < _balancePressureDiff;
        }


        public override bool CheckVacuum()
        {
            return _chamberGuage.Value <= SC.GetValue<double>("LoadLock.VacuumPressureBase");
        }

        public override bool CheckIsPumping()
        {
            return _slowPumpValve.Status || _fastPumpValve.Status;
        }

        public override bool CheckLidClose()
        {
            return _lid.Value == true;
        }

        public override bool CheckLidOpen()
        {
            return _lid.Value != true;
        }

        public override bool SetLift(bool isUp, out string reason)
        {
            if (isUp)
            {
                return _lift.MoveUp(out reason);
            }
            else
            {
                return _lift.MoveDown(out reason);
            }
        }

        public override bool CheckLiftDown()
        {
            return _lift.IsDown;
        }

        public override bool CheckLiftUp()
        {
            return _lift.IsUp;
        }

        public override bool CheckTrayClamped()
        {
            return _trayClaw.IsClamp;
        }

        public override bool CheckTrayUnClamped()
        {
            return _trayClaw.IsUnClamp;
        }

        public override bool SetTrayClamped(bool clamp, out string reason)
        {
            return _trayClaw.SetValue(clamp,out reason);
        }

        public override bool CheckWaferClamped()
        {
            return _waferClaw.IsClamp;
        }
        public override bool CheckWaferUnClamped()
        {
            return _waferClaw.IsUnClamp;
        }
        public override bool SetWaferClamped(bool clamp, out string reason)
        {
            return _waferClaw.SetValue(clamp, out reason);
        }

        public override bool CheckWaferPlaced()
        {
            return _LLWaferPlaced.Value;
        }
        public override bool CheckTrayPlaced()
        {
            return _LLTrayPlaed.Value;
        }

        public override bool SetSlowPumpValve(bool isOpen, out string reason)
        {
            return _slowPumpValve.TurnValve(isOpen, out reason);
        }

        public override bool SetFastPumpValve(bool isOpen, out string reason)
        {
            return _fastPumpValve.TurnValve(isOpen, out reason);
        }

        public override bool SetFastVentValve(bool isOpen, out string reason)
        {
            return _ventValve.TurnValve(isOpen, out reason);
        }

        public override bool SetSlowVentValve(bool isOpen, out string reason)
        {
            return _ventValve.TurnValve(isOpen, out reason);
        }

        public bool CheckSlitValveClosed()
        {
            return _VacDoor.IsClose && _LeftDoor.IsClose && _RightDoor.IsClose;
        }

        public override bool CheckEnableTransfer(EnumTransferType type)
        {
            return CheckLiftUp() || CheckLiftDown();
        }


        public override void Monitor()
        {
            //增加过冲关闭Vent阀门
            if (_chamberGuage.FeedBack >= SC.GetValue<double>("LoadLock.VentMaxPressure") && 
                SC.GetValue<double>("LoadLock.VentMaxPressure") > SC.GetValue<double>("LoadLock.AtmPressureBase"))
            {
                if (_ventValve.Status)
                {
                    SetFastVentValve(false, out string reason);
                }
            }
        }

        public override void Reset()
        {
            DeviceLockerManager.Instance.GetLocker(DeviceLockerManager.LockerNames.Pump2)?.Reset();
        }

        public override void AutoCreatWafer()
        {
            if (_LLTrayPlaed.Value)
            {
                if (!WaferManager.Instance.CheckHasTray(ModuleName.LoadLock, 0))
                {
                    if (WaferManager.Instance.CheckHasWafer(ModuleName.LoadLock, 0))
                    {
                        WaferManager.Instance.CreateWafer(ModuleName.LoadLock, 0,WaferStatus.Normal);
                    }
                    else
                    {
                        WaferManager.Instance.CreateTray(ModuleName.LoadLock, 0);
                    }
                }
            }
        }

    }
}
