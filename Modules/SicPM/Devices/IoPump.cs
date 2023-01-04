using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
  public partial class IoPump : BaseDevice, IDevice
    {
        public DIAccessor _diDryPumpAlarm = null;
        public DIAccessor _diPumpExhaustPress = null;
        public DIAccessor _diPumpCabExhaustDP = null;
        //public DIAccessor _diDryPumpFlow = null;
        //public DIAccessor _diPumpForelineTemp = null;
        //public DIAccessor _diPumpExhaustTemp = null;
        public DIAccessor _diDryPump1Running = null;
        public DIAccessor _diDryPump1Warning = null;

        #region DI

        public bool DryPumpAlarm
        {
            get 
            {
                if (_diDryPumpAlarm != null) return !_diDryPumpAlarm.Value;
                return false;
            }
        }

        public bool PumpExhaustPress
        {
            get
            {
                if (_diPumpExhaustPress != null) return _diPumpExhaustPress.Value;
                return false;
            }
        }

        //public bool PumpCabExhaustDP
        //{
        //    get
        //    {
        //        if (_diPumpCabExhaustDP != null) return _diPumpCabExhaustDP.Value;
        //        return false;
        //    }
        //}

        //public bool DryPumpFlow
        //{
        //    get
        //    {
        //        if (_diDryPumpFlow != null) return _diDryPumpFlow.Value;
        //        return false;
        //    }
        //}

        //public bool PumpForelineTemp
        //{
        //    get
        //    {
        //        if (_diPumpForelineTemp != null) return _diPumpForelineTemp.Value;
        //        return false;
        //    }
        //}

        //public bool PumpExhaustTemp
        //{
        //    get
        //    {
        //        if (_diPumpExhaustTemp != null) return _diPumpExhaustTemp.Value;
        //        return false;
        //    }
        //}

        public bool DryPump1Running
        {
            get
            {
                if (_diDryPump1Running != null) return _diDryPump1Running.Value;
                return false;
            }
        }

        public bool DryPump1Warning
        {
            get
            {
                if (_diDryPump1Warning != null) return !_diDryPump1Warning.Value;
                return false;
            }
        }

        #endregion
       
        private R_TRIG _trigWarning = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private F_TRIG _fTrigPumpOff = new F_TRIG();

        public bool IsOn => DryPump1Running;
        public bool IsError => DryPumpAlarm;
        public bool IsWarning => DryPump1Warning;

        public AITPumpData DeviceData
        {
            get 
            {
                var data = new AITPumpData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    DeviceModule = Module,
                    Module = Module,

                    IsOn = DryPump1Running,

                    IsError = DryPumpAlarm,

                    IsWarning = DryPump1Warning,

                    //IsOverLoad = IsPumpOverloadAlarm,
                };
                data.AttrValue["DryPumpAlarm"] = _diDryPumpAlarm.Value;
                data.AttrValue["PumpExhaustPress"] = _diPumpExhaustPress.Value;
                //data.AttrValue["PumpCabExhaustDP"] = _diPumpCabExhaustDP.Value;
                //data.AttrValue["DryPumpFlow"] = _diDryPumpFlow.Value;
                //data.AttrValue["PumpForelineTemp"] = _diPumpForelineTemp.Value;
                //data.AttrValue["PumpExhaustTemp"] = _diPumpExhaustTemp.Value;
                data.AttrValue["DryPump1Running"] = _diDryPump1Running.Value;
                data.AttrValue["DryPump1Warning"] = _diDryPump1Warning.Value;
                return data;
            }
        
        }

        public IoPump(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");

            _diDryPumpAlarm = ParseDiNode("diDryPumpAlarm", node, ioModule);
            _diPumpExhaustPress = ParseDiNode("diPumpExhaustPress", node, ioModule);
            //_diPumpCabExhaustDP = ParseDiNode("diPumpCabExhaustDP", node, ioModule);
            //_diDryPumpFlow = ParseDiNode("diDryPumpFlow", node, ioModule);
            //_diPumpForelineTemp = ParseDiNode("diPumpForelineTemp", node, ioModule);
            //_diPumpExhaustTemp = ParseDiNode("diPumpExhaustTemp", node, ioModule);
            _diDryPump1Running = ParseDiNode("diDryPump1Running", node, ioModule);
            _diDryPump1Warning = ParseDiNode("diDryPump1Warning", node, ioModule);

        }
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.DryPumpAlarm", () => DryPumpAlarm);
            DATA.Subscribe($"{Module}.{Name}.PumpExhaustPress", () => PumpExhaustPress);
            //DATA.Subscribe($"{Module}.{Name}.PumpCabExhaustDP", () => PumpCabExhaustDP);
            //DATA.Subscribe($"{Module}.{Name}.DryPumpFlow", () => DryPumpFlow);
            //DATA.Subscribe($"{Module}.{Name}.PumpForelineTemp", () => PumpForelineTemp);
            //DATA.Subscribe($"{Module}.{Name}.PumpExhaustTemp", () => PumpExhaustTemp);
            DATA.Subscribe($"{Module}.{Name}.DryPump1Running", () => DryPump1Running);
            DATA.Subscribe($"{Module}.{Name}.DryPump1Warning", () => DryPump1Warning);

            return false;
        }

        public void Monitor()
        {
            try
            {
                //_trigWarning.CLK = DryPump1Warning;
                //if (_trigWarning.Q)
                //{
                //    EV.PostAlarmLog(Module, $"Alarm562 DryPump Warning");
                //}

                //_trigError.CLK = DryPumpAlarm;
                //if (_trigError.Q)
                //{
                //    EV.PostAlarmLog(Module, $"{Name} error");
                //}

                //_fTrigPumpOff.CLK = DryPump1Running;
                //if (_fTrigPumpOff.Q)
                //{
                //    EV.PostWarningLog(Module, $"{Name} Off");
                //}
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void Reset()
        {
           
        }

        public void Terminate()
        {
           
        }
    }
}
