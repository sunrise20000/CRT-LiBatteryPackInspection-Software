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
    public class DoSensor : BaseDevice, IDevice
    {
        private DOAccessor _do = null;

        public DOAccessor SensorDO => _do;
       
        private R_TRIG _trigTextOut = new R_TRIG();

      
        private bool _textOutTrigValue;
        public bool AlarmTrigValue
        {
            get { return _textOutTrigValue && !string.IsNullOrEmpty(_alarmText); }
        }

        private string _warningText;
        private string _alarmText;
        private string _infoText;

        public Action WarningAction
        {
            get;
            set;
        }

        public event Action<DoSensor, bool> OnSignalChanged;

        public bool Value
        {
            get
            {
                if (_do != null)
                    return _do.Value;

                return false;
            }
        }

        private AITSensorData DeviceData
        {
            get
            {
                AITSensorData data = new AITSensorData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    Value = Value,
                };

                return data;
            }
        }

        private bool _previous;

        public DoSensor(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _do = ParseDoNode("do", node, ioModule);

            _infoText = node.GetAttribute("infoText");
            _warningText = node.GetAttribute("warningText");
            _alarmText = node.GetAttribute("alarmText");

            _textOutTrigValue = Convert.ToBoolean(string.IsNullOrEmpty(node.GetAttribute("textOutTrigValue")) ? "false" : node.GetAttribute("textOutTrigValue"));
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.Value", () => Value);

            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            try
            {
                _trigTextOut.CLK = (Value == _textOutTrigValue);

                if (_trigTextOut.Q)
                {
                    if (WarningAction != null)
                    {
                        WarningAction();
                    }
                    else if (!string.IsNullOrEmpty(_warningText.Trim()))
                    {
                        EV.PostWarningLog(Module, _warningText);
                    }
                    else if (!string.IsNullOrEmpty(_alarmText.Trim()))
                    {
                        EV.PostAlarmLog(Module, _alarmText);
                    }
                    else if (!string.IsNullOrEmpty(_infoText.Trim()))
                    {
                        EV.PostInfoLog(Module, _infoText);
                    }
                }

                if (_previous != Value)
                {
                    if (OnSignalChanged != null)
                        OnSignalChanged(this, Value);
                    _previous = Value;
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }
        }

        public void Reset()
        {
            _trigTextOut.RST = true;
        }
    }
}
