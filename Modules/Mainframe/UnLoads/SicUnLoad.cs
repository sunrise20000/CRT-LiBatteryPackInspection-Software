using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UnLoad;
using System;
using System.Xml;
using MECF.Framework.RT.EquipmentLibrary.Core;

namespace Mainframe.UnLoads
{
    public class SicUnLoad : UnLoad
    {
        private IoLift4 _lift;
        private IoClaw _waferClaw;
        private IoSensor _lid;
        private IoValve _ventValve;
        private IoValve _slowPumpValve;
        private IoValve _fastPumpValve;
        private IoSensor _unLoadWaferPlaced;
        private IoSensor _unLoadTrayPlaed;

        private IoPressureMeter3 _forelineGuage;
        private IoPressureMeter3 _chamberGuage;


        private R_TRIG _trigWafer = new R_TRIG();
        private F_TRIG _trigNoWafer = new F_TRIG();
        private R_TRIG _trigSlowPumpLockerError = new R_TRIG();
        private R_TRIG _trigSlowPumpLockerNotExists = new R_TRIG();

        private IoSlitValve _EfemDoor;
        private IoSlitValve _VacDoor;

        private DeviceTimer _timerJobDone = new DeviceTimer();

        private double _balancePressureDiff = 0;

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


        public SicUnLoad(string module, XmlElement node, string ioModule = "") : base(module)
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
        }

        public override bool Initialize()
        {
            _slowPumpValve = DEVICE.GetDevice<IoValve>("TM.UnLoadSlowPump");
            _fastPumpValve = DEVICE.GetDevice<IoValve>("TM.UnLoadFastPump");

            _ventValve = DEVICE.GetDevice<IoValve>("TM.UnLoadVent");
            _lid = DEVICE.GetDevice<IoSensor>("UnLoad.UnloadLidClosed");
            _lift = DEVICE.GetDevice<IoLift4>("UnLoad.UnLoadLift");
            _waferClaw = DEVICE.GetDevice<IoClaw>("UnLoad.UnLoadWaferClaw");
            _unLoadWaferPlaced = DEVICE.GetDevice<IoSensor>("TM.UnLoadWaferPlaced");
            _unLoadTrayPlaed = DEVICE.GetDevice<IoSensor>("TM.UnLoadTrayPlaced");

            _chamberGuage = DEVICE.GetDevice<IoPressureMeter3>("TM.UnLoadPressure");
            _forelineGuage = DEVICE.GetDevice<IoPressureMeter3>("TM.ForelinePressure");
 
            _VacDoor = DEVICE.GetDevice<IoSlitValve>("TM.UnLoadDoor");
            _EfemDoor = DEVICE.GetDevice<IoSlitValve>("EFEM.UnLoadSubDoor");

            _balancePressureDiff = SC.GetValue<double>("TM.PressureBalance.BalanceMaxDiffPressure");

            return base.Initialize();
        }

        public override bool CheckAtm()
        {
            return _chamberGuage.Value >= SC.GetValue<double>("UnLoad.AtmPressureBase");
        }

        public override bool CheckVacuum()
        {
            return _chamberGuage.Value <= SC.GetValue<double>("UnLoad.VacuumPressureBase");
        }

        public override bool CheckTransferPressure()
        {
            return Math.Abs(SC.GetValue<double>("UnLoad.VacuumPressureBase") - _chamberGuage.Value) < _balancePressureDiff;
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
            if(isUp)
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
            return _unLoadWaferPlaced.Value;
        }
        public override bool CheckTrayPlaced()
        {
            return _unLoadTrayPlaed.Value;
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
            return _VacDoor.IsClose && _EfemDoor.IsClose;
        }


        public override bool CheckEnableTransfer(EnumTransferType type)
        {
            return true;// CheckLiftUp() || CheckLiftDown();
        }

        public override void Monitor()
        {
            //增加过冲关闭Vent阀门
            if (_chamberGuage.FeedBack >= SC.GetValue<double>("UnLoad.VentMaxPressure") && SC.GetValue<double>("UnLoad.VentMaxPressure") > SC.GetValue<double>("UnLoad.AtmPressureBase"))
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
    }
}

