using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Event;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Mainframe.Devices
{
    public class IoPump: BaseDevice, IDevice
    {
        public bool IsRunning
        {
            get
            {
                if (_diRunning != null)
                    return _diRunning.Value;
                return true;
            }
        }
        public bool IsWarning
        {
            get 
            {
                if(_diWarning!= null)
                    return !_diWarning.Value;
                return false;
            }
        }
        public bool IsAlarm
        {
            get
            {
                return _diAlarm != null && !_diAlarm.Value;
            }
        }
        public bool IsWaterFlow
        {
            get
            {
                return _diWaterFlow!= null && _diWaterFlow.Value;
            }
        }


        private AITPumpData DeviceData
        {
            get
            {
                AITPumpData data = new AITPumpData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    DeviceModule = Module,
                    Module = Module,

                    IsOn = IsRunning,
                    IsError = HasAlarm || IsAlarm,
                    IsWarning = IsWarning,
                   // IsWaterFlowEnable = _diWaterFlow.Value,
                    
                };

                return data;
            }
        }


        private DIAccessor _diRunning = null;
        private DIAccessor _diOverloadAlarm;
        private DIAccessor _diWarning;
        private DIAccessor _diAlarm;
        private DIAccessor _diWaterFlow;

        //private DOAccessor _doStart;
        //private DOAccessor _doPowerOn;
        //private DOAccessor _doResetError;

        private R_TRIG _trigWarning = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigNotRunning = new R_TRIG();

        public AlarmEventItem AlarmOverload { get; set; }
        public AlarmEventItem AlarmFailedStartStop { get; set; }

     

        public IoPump(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diRunning = ParseDiNode("diRunning", node, ioModule);
           // _diOverloadAlarm = ParseDiNode("diOverloadAlarm", node, ioModule);
            _diWarning = ParseDiNode("diWarning", node, ioModule);
            _diAlarm = ParseDiNode("diAlarm", node, ioModule);
            _diRunning = ParseDiNode("diRunning", node, ioModule);
            //_diWaterFlow = ParseDiNode("diWaterFlow", node, ioModule);

           
        }


        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.IsAlarm", () => IsAlarm);
            DATA.Subscribe($"{Module}.{Name}.IsRunning", () => IsRunning);
            DATA.Subscribe($"{Module}.{Name}.IsWarning", () => IsWarning);
           // DATA.Subscribe($"{Module}.{Name},IsWaterFlowEable", () => IsWaterFlow);
            return true;
        }

    

        public void Terminate()
        {
        }

        public void Monitor()
        {
            try
            {
                string s = "";
             
                _trigWarning.CLK = IsWarning;
                if (_trigWarning.Q)
                {
                    if(Name.Contains("1"))
                    {
                        s = "Alarm530 TMDryPump1 Not Runing[DI - 44]"; //"Alarm531 TMDryPump1 Warning  [DI-44]";
                    }
                    else
                    {
                        s = "Alarm533 TMDryPump2 Not Runing [DI-79]"; //"Alarm534 TMDryPump2 Warning[DI - 79]";
                    }
                    EV.PostMessage(Module, EventEnum.DefaultAlarm, s);
                    EV.PostAlarmLog(Module, s);
                }

                _trigError.CLK = IsAlarm;
                if (_trigError.Q)
                {
                    if (Name.Contains("1"))
                    {
                        s = "Alarm529 TMDryPump1 Alarm [DI-43]";
                    }
                    else
                    {
                        s = "Alarm532 TMDryPump2 Alarm [DI-78]";
                    }                    
                    EV.PostAlarmLog(Module, s);
                }

                _trigNotRunning.CLK = !IsRunning;
                if (_trigNotRunning.Q)
                {
                    if (Name.Contains("1"))
                    {
                        s = "Alarm531 TMDryPump1 Warning  [DI-49]"; //"Alarm530 TMDryPump1 Not Runing [DI-49]";
                    }
                    else
                    {
                        s = "Alarm534 TMDryPump2 Warning  [DI-86]"; // Alarm533 TMDryPump2 Not Runing [DI-86]";
                    }
                    EV.PostAlarmLog(Module, s);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void Reset()
        {
            _trigWarning.RST = true;
            _trigError.RST = true;
        }
    }
}
