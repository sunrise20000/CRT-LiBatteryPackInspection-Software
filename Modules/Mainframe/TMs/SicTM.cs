using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Mainframe.Devices;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;
using SicPM.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Mainframe.TMs
{
    public class SicTM : TM
    {

        private Dictionary<ModuleName, bool> _slitValveInstalled = new Dictionary<ModuleName, bool>();
        private Dictionary<ModuleName, IoSlitValve> _slitValves = new Dictionary<ModuleName, IoSlitValve>();

        private IoValve _ventValveV77;
        private IoValve _pumpValveV81;
        private IoValve _pumpSlowValveV82;
        private IoValve _bufferVentValveV80;

        //四个联通阀
        private IoValve _tmLoadBanlance_V85;
        private IoValve _tmUnLoadBanlance_V124;
        private IoValve _tmPMABanlance_V70_1;
        //private IoValve _tmPMABanlance_V70_2;

        private IoPressureMeter3 _forelineGuage_PS7;
        private IoPressureMeter3 _chamberGuage_TM;
        private IoPressureMeter3 _loadLockGuage_PT3;


        private IoInterLock _tmIoInterLock;
        private SicPM.Devices.IoInterLock _pm1InterLock;

        private SicPM.Devices.IoMFC _tmMfc;

        public AIAccessor AILoadLockTempAlarm { get; set; }
        public AIAccessor AIBufferTempAlarm { get; set; }


        private SCConfigItem _scMaxPressureDiffOpenSlitValve;
        private SCConfigItem _scLoadLockWarnTemp;
        private SCConfigItem _scBufferWarnTemp;
        private R_TRIG _loadLoakTempAlarmTrig = new R_TRIG();
        private R_TRIG _bufferTempAlarmTrig = new R_TRIG();

        public override double ChamberPressure
        {
            get 
            {
                return _chamberGuage_TM.Value; 
            }
        }

        public override double ForelinePressure
        {
            get 
            {
                return _forelineGuage_PS7.Value; 
            }
        }

        public SicTM(string module, XmlElement node, string ioModule = "") : base(module)
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");

          
            AILoadLockTempAlarm = ParseAiNode("aiLLTemp", node, ioModule);
            AIBufferTempAlarm = ParseAiNode("aiBufferTemp", node, ioModule);

            _scMaxPressureDiffOpenSlitValve = ParseScNode("MaxPressureDiffOpenSlitValve", node, "TM", "TM.MaxPressureDiffOpenSlitValve");
            _scLoadLockWarnTemp = ParseScNode("MaxPressureDiffOpenSlitValve", node, "TM", "TM.LoadLockWarnTemp");
            _scBufferWarnTemp = ParseScNode("MaxPressureDiffOpenSlitValve", node, "TM", "TM.BufferWarnTemp");
        }

        public void GetSlitValves(Dictionary<string, object> dict)
        {
            foreach (var item in _slitValves)
                dict[$"SlitValves.{item.Key.ToString()}"] = ((IoSlitValve)item.Value).State;
        }

        public override IoSlitValve GetSlitValve(ModuleName module)
        {
            if (_slitValves.ContainsKey(module))
            {
                return _slitValves[module];
            }
            return null;
        }

        public override bool Initialize()
        {
            _slitValves[ModuleName.PM1] =       DEVICE.GetDevice<IoSlitValve>("TM.PM1Door");
            _slitValves[ModuleName.PM2] =       DEVICE.GetDevice<IoSlitValve>("TM.PM2Door");
            _slitValves[ModuleName.UnLoad] =    DEVICE.GetDevice<IoSlitValve>("TM.UnLoadDoor");
            _slitValves[ModuleName.LoadLock] =  DEVICE.GetDevice<IoSlitValve>("TM.LoadLockDoor");
            _slitValves[ModuleName.Buffer] =    DEVICE.GetDevice<IoSlitValve>("TM.BufferDoor");

            _slitValveInstalled[ModuleName.PM1] =       SC.GetValue<bool>("TM.PM1SlitValveEnabled");
            _slitValveInstalled[ModuleName.PM2] =       SC.GetValue<bool>("TM.PM2SlitValveEnabled");
            _slitValveInstalled[ModuleName.UnLoad] =    SC.GetValue<bool>("TM.UnLoadSlitValveEnabled");
            _slitValveInstalled[ModuleName.LoadLock] =  SC.GetValue<bool>("TM.LoadLockSlitValveEnabled");

            _slitValveInstalled[ModuleName.Buffer] =    SC.GetValue<bool>("TM.BufferSlitValveEnabled");

            _ventValveV77        = DEVICE.GetDevice<IoValve>("TM.TMVent");
            _pumpValveV81        = DEVICE.GetDevice<IoValve>("TM.TMFastPump");
            _pumpSlowValveV82    = DEVICE.GetDevice<IoValve>("TM.TMSlowPump");
            _tmLoadBanlance_V85   = DEVICE.GetDevice<IoValve>("TM.TMLoadBanlance");
            _tmUnLoadBanlance_V124 = DEVICE.GetDevice<IoValve>("TM.TMUnLoadBanlance");
            _tmPMABanlance_V70_1 =    DEVICE.GetDevice<IoValve>("PM1.V70");
            //_tmPMABanlance_V70_2 =    DEVICE.GetDevice<IoValve>("TM.TMPMBBanlance");
            _bufferVentValveV80 = DEVICE.GetDevice<IoValve>("TM.BufferVent");


            _chamberGuage_TM =  DEVICE.GetDevice<IoPressureMeter3>("TM.TMPressure");
            _forelineGuage_PS7 = DEVICE.GetDevice<IoPressureMeter3>("TM.ForelinePressure");
            _loadLockGuage_PT3 = DEVICE.GetDevice<IoPressureMeter3>("TM.LLPressure");

            _tmMfc = DEVICE.GetDevice<SicPM.Devices.IoMFC>("TM.Mfc60");

            _tmIoInterLock = DEVICE.GetDevice<IoInterLock>("TM.IoInterLock");
            _pm1InterLock = DEVICE.GetDevice<SicPM.Devices.IoInterLock>("PM1.PMInterLock");

            return base.Initialize();
        }


        public override bool CheckAtm()
        {
            return _chamberGuage_TM.FeedBack >= SC.GetValue<double>("TM.AtmPressureBase");
        }

        public override bool CheckVacuum()
        {
            return _chamberGuage_TM.FeedBack <= SC.GetValue<double>("TM.VacuumPressureBase");
        }

        public override void Monitor()
        {
            //增加过冲关闭Vent阀门
            if (_chamberGuage_TM.FeedBack >= SC.GetValue<double>("TM.VentMaxPressure") && 
                SC.GetValue<double>("TM.VentMaxPressure") > SC.GetValue<double>("TM.AtmPressureBase"))
            {
                if (_ventValveV77.Status)
                {
                    SetFastVentValve(false, out string reason);
                }
            }

            _loadLoakTempAlarmTrig.CLK = AILoadLockTempAlarm != null && AILoadLockTempAlarm.FloatValue >= _scLoadLockWarnTemp.DoubleValue;
            if (_loadLoakTempAlarmTrig.Q)
            {
                EV.PostAlarmLog(Module, $"Waring 10 LoadLock Temp High");
            }

            _bufferTempAlarmTrig.CLK = AIBufferTempAlarm != null && AIBufferTempAlarm.FloatValue >= _scBufferWarnTemp.DoubleValue;
            if (_bufferTempAlarmTrig.Q)
            {
                EV.PostAlarmLog(Module, $"Waring 11 Buffer Temp High");
            }
        }

        public override void Reset()
        {
            _loadLoakTempAlarmTrig.RST = true;
            _bufferTempAlarmTrig.RST = true;

        }

        public override bool SetSlitValve(ModuleName module, bool isOpen, out string reason)
        {
            reason = "";

            System.Diagnostics.Debug.Assert(_slitValves.ContainsKey(module), $" {module} has no slit valve");
            if (!_slitValveInstalled[module])
            {
                reason = string.Empty;
                return true;
            }
            string open = isOpen ? "Open" : "Close";
            EV.PostInfoLog(Module, $"Set {module} slit valve {open}");

            if (module == ModuleName.LoadLock && isOpen == true)
            {
                if (Math.Abs(_loadLockGuage_PT3.Value - _chamberGuage_TM.Value) > _scMaxPressureDiffOpenSlitValve.DoubleValue)
                {
                    EV.PostAlarmLog(Module, $"Can not set {module} slit valve {open},The pressure difference of {module} and TM is greater than { _scMaxPressureDiffOpenSlitValve.DoubleValue}");
                    return false;
                }
            }
            //检查InterLock
            if (!SC.GetConfigItem("System.BypassInterlock").BoolValue && (module == ModuleName.PM1 || module == ModuleName.PMA) )
            {
                if (!isOpen)
                {
                    if (_pm1InterLock != null && _pm1InterLock.DiChamLidClosed == false)
                    {
                        EV.PostAlarmLog(Module, $"Can not set {module} slit valve {open},InterLock check failed [DI-00(PM1)] ");
                        return false;
                    }
                }
            }

            return _slitValves[module].SetSlitValve(isOpen, out reason);
        }

        public override bool CheckSlitValveOpen(ModuleName module)
        {
            System.Diagnostics.Debug.Assert(_slitValves.ContainsKey(module), $" {module} has no slit valve");

            if (!_slitValveInstalled[module])
                return false;

            return _slitValves[module].IsOpen;
        }

        public override bool CheckSlitValveClose(ModuleName module)
        {            
            System.Diagnostics.Debug.Assert(_slitValves.ContainsKey(module), $" {module} has no slit valve");

            if (!_slitValveInstalled[module])
                return true;

            return _slitValves[module].IsClose;
        }


        public override bool SetFastPumpValve(bool isOpen, out string reason)
        {
            return _pumpValveV81.TurnValve(isOpen, out reason);
        }

        public override bool SetFastVentValve(bool isOpen, out string reason)
        {
            return _ventValveV77.TurnValve(isOpen, out reason);
        }

        public override bool SetSlowVentValve(bool isOpen, out string reason)
        {
            return _ventValveV77.TurnValve(isOpen, out reason);
        }

        public override bool SetBufferVentValve(bool isOpen, out string reason)
        {
            return _bufferVentValveV80.TurnValve(isOpen, out reason);
        }

        public override bool SetSlowPumpValve(bool isOpen, out string reason)
        {
            return _pumpSlowValveV82.TurnValve(isOpen, out reason);
        }

        public bool CheckSlowPumpValve(bool isOpen)
        {
            if (isOpen)
            {
                if (!_ventValveV77.Status)
                    return false;
            }
            else
            {
                if (_ventValveV77.Status)
                    return false;
            }

            return true;
        }

        public bool CheckSlowVentValve(bool isOpen)
        {
            if (isOpen)
            {
                if (!_ventValveV77.Status)
                    return false;
            }
            else
            {
                if (_ventValveV77.Status)
                    return false;
            }

            return true;
        }


        public bool CloseAllVentPumpValue()
        {
            _pumpSlowValveV82.TurnValve(false, out string reason);
            _pumpValveV81.TurnValve(false, out reason); 
            _ventValveV77.TurnValve(false, out reason);
            _tmLoadBanlance_V85.TurnValve(false, out reason);
            _tmPMABanlance_V70_1.TurnValve(false, out reason);            
            _tmUnLoadBanlance_V124.TurnValve(false, out reason);

            return true;
        }

        public override bool SetTmToLLVent(bool isOpen, out string reason)
        {
            return _tmLoadBanlance_V85.TurnValve(isOpen, out reason);
        }

        public override bool SetTmToUnLoadVent(bool isOpen, out string reason)
        {
            return _tmUnLoadBanlance_V124.TurnValve(isOpen, out reason);
        }

        public bool SetPIDValve(bool isOpen, out string reason)
        {
            if (!_ventValveV77.TurnValve(isOpen, out reason))
                return false;

            //if (!_roughBypassValve.TurnValve(isOpen, out reason))
            //    return false;

            return true;
        }

        public bool CheckPIDValve(bool isOpen)
        {
            if (isOpen)
            {
                if (!_ventValveV77.Status)
                    return false;

                //if (!_roughBypassValve.Status)
                //    return false;
            }
            else
            {
                if (_ventValveV77.Status)
                    return false;

                //if (_roughBypassValve.Status)
                //    return false;
            }

            return true;
        }

        //public override bool SetTurboPumpIsoValve(bool isOpen, out string reason)
        //{
        //    return _turboIsoValve.SetCylinder(isOpen, out reason);
        //}

        //public override bool SetTurboPumpBackingValve(bool isOpen, out string reason)
        //{
        //    return _turboBackingValve.TurnValve(isOpen, out reason);
        //}

        //public override bool SetMfcVentValve(bool isOpen, out string reason)
        //{
        //    return _ventValveV77.TurnValve(isOpen, out reason);
        //    //return _mfcValve.TurnValve(isOpen, out reason);
        //}

        //public override bool SetAllValves(bool isOpen, out string reason)
        //{
        //    if (!SetTurboPumpBackingValve(isOpen, out reason))
        //        return false;

        //    if (!SetTurboPumpIsoValve(isOpen, out reason))
        //        return false;

        //    if (!SetFastVentValve(isOpen, out reason))
        //        return false;

        //    if (!SetFastPumpValve(isOpen, out reason))
        //        return false;

        //    if (!SetMfcVentValve(isOpen, out reason))
        //        return false;

        //    reason = string.Empty;
        //    return true;
        //}

        //public override bool SetVentMfc(double flow, out string reason)
        //{
        //   // _mfc.Ramp(flow, 0);

        //    reason = string.Empty;
        //    return true;
        //}

        //public override bool SetVentMfcFullFlow(out string reason)
        //{
        //   // _mfc.Ramp(_mfc.Scale, 0);

        //    reason = string.Empty;
        //    return true;
        //}


        //public override bool CheckTurboPumpStable()
        //{

        //    return _turboPump.IsStable;
        //}

        //public override bool CheckTurboPumpOn()
        //{
        //    return _turboPump.IsOn;
        //}
        //public override bool CheckTurboPumpOff()
        //{
        //    return !_turboPump.IsOn;
        //}
        //public override bool CheckTurboPumpError()
        //{
        //    return _turboPump.IsError;
        //}
        //public override bool TurboPumpOn()
        //{
        //    _turboPump.SetPumpOnOff(true);
        //    return true;
        //}
        //public override bool TurboPumpOff()
        //{
        //    _turboPump.SetPumpOnOff(false);
        //    return true;
        //}

        //public override bool CheckIsoValveClose()
        //{
        //    return _turboIsoValve.CloseSetPoint && !_turboIsoValve.OpenSetPoint;
        //}

        //public override bool CheckIsoValveOpen()
        //{
        //    return _turboIsoValve.OpenSetPoint && !_turboIsoValve.CloseSetPoint;
        //}


        public override bool SetVentMfc(double flow, out string reason)
        {
            reason = "";
            _tmMfc.Ramp(flow, 0);
            return true;
        }

        public override bool SetAllValvesClose(out string reason)
        {
            reason = string.Empty;

            _ventValveV77.TurnValve(false, out reason);
            _pumpValveV81.TurnValve(false, out reason);
            _pumpSlowValveV82.TurnValve(false, out reason);
            //_bufferVentValveV80.TurnValve(false, out reason);
            _tmLoadBanlance_V85.TurnValve(false, out reason);
            _tmUnLoadBanlance_V124.TurnValve(false, out reason);
            _tmPMABanlance_V70_1.TurnValve(false, out reason);
            //_tmPMABanlance_V70_2.TurnValve(false, out reason);

            return true;
        }


    }
}
