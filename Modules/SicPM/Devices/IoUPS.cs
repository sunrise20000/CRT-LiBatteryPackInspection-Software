using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using System;
using System.Xml;

namespace SicPM.Devices
{
    public class IoUPS : BaseDevice, IDevice
    {
        private DOAccessor _doEnable = null;
        private DOAccessor _doLowBattery = null;

        private R_TRIG _trigEnable = new R_TRIG();
        private R_TRIG _trigLowBattery = new R_TRIG();

        public bool UPSEnable
        {
            get
            {
                return _doEnable != null ? !_doEnable.Value : false;
            }
            set
            {
                _doEnable.Value = value;
            }
        }

        public bool UPSLowBattery
        {
            get
            {
                return _doLowBattery != null ? !_doLowBattery.Value : false;
            }
            set
            {
                _doLowBattery.Value = value;
            }
        }

        

        public IoUPS(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _doEnable = ParseDoNode("doEnable", node, ioModule);
            _doLowBattery = ParseDoNode("doLowBattery", node, ioModule);
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.UPSEnable", () => UPSEnable);
            DATA.Subscribe($"{Module}.{Name}.UPSLowBattery", () => UPSLowBattery);


            OP.Subscribe($"{Module}.{Name}.SetUPSEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetUPSEnable(isTrue, out string reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetUPSLowBattery", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetUPSLowBattery(isTrue, out string reason);
                return true;
            });

            return true;
        }

        public bool SetUPSEnable(bool setValue, out string reason)
        {
            if (!_doEnable.Check(setValue, out reason))
            {
                return false;
            }
            if (!_doEnable.SetValue(setValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetUPSLowBattery(bool setValue, out string reason)
        {
            if (!_doLowBattery.Check(setValue, out reason))
            {
                return false;
            }
            if (!_doLowBattery.SetValue(setValue, out reason))
            {
                return false;
            }

            return true;
        }

        public void Monitor()
        {
            _trigEnable.CLK = UPSEnable;
            if (_trigEnable.Q)
            {
                EV.PostAlarmLog(Module, $"Alarm:[DO-193] UPS Enable");
            }

            _trigLowBattery.CLK = UPSLowBattery;
            if (_trigLowBattery.Q)
            {
                EV.PostAlarmLog(Module, $"Alarm:[DO-194] UPS LowBattery");
            }    
        }

        public void Reset()
        {
            _trigEnable.RST = true;
            _trigLowBattery.RST = true;
            //
            
        }

        public void Terminate()
        {
            //throw new NotImplementedException();
        }
    }
}
