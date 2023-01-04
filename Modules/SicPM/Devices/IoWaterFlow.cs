using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.IOCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Aitex.Core.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;

namespace SicPM.Devices
{
    class IoWaterFlow : BaseDevice, IDevice
    {
        private AIAccessor _aiFeedBack = null;
        private DIAccessor _diFlowSW = null;

        private bool _isFloatAioType;
        private R_TRIG _trigTextOut = new R_TRIG();

        private string _warningText;
        private string _alarmText;
        private string _infoText;

        public AITDeviceData DeviceData
        {
            get 
            {
                AITDeviceData data = new AITDeviceData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DisplayName = Display,
                    DeviceSchematicId = DeviceID,
                    UniqueName = UniqueName,
                    
                };
                data.AttrValue["FeedBack"] = FeedBack;
                data.AttrValue["FlowSW"] = FlowSW;
                return data;
            }
        }
        public float FeedBack
        {
            get
            {
                if (_aiFeedBack != null)
                    return _isFloatAioType ? _aiFeedBack.FloatValue : _aiFeedBack.Value;

                return 0;
            }
        }
        public bool FlowSW
        {
            get
            {
                if (_diFlowSW != null)
                    return _diFlowSW.Value;

                return false;
            }
        }
        public IoWaterFlow(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");
            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _aiFeedBack = ParseAiNode("aiFeedback", node, ioModule);
            _diFlowSW = ParseDiNode("diFlowSW", node, ioModule);

            _infoText = "";
            _warningText = "";
            _alarmText = "";
            //if (base.Name == "TMPump2FlowTemp")
            //{
            //    _alarmText = "Alarm9  TMPump2 Water Low Flow -U6  [DI-19]";
            //}

            UniqueName = Module + "." + Name;
        }
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.FeedBack", () => FeedBack);
            DATA.Subscribe($"{Module}.{Name}.FlowSW", () => FlowSW);
            return false;
        }

        public void Monitor()
        {
            try
            {
                //if (base.Name == "TMPump2FlowTemp")//DI-19
                //{
                //    _trigTextOut.CLK = _diFlowSW.Value;

                //    if (_trigTextOut.Q)
                //    {
                //        if (!string.IsNullOrEmpty(_warningText.Trim()))
                //        {
                //            EV.PostWarningLog(Module, _warningText);
                //        }
                //        else if (!string.IsNullOrEmpty(_alarmText.Trim()))
                //        {
                //            EV.PostAlarmLog(Module, _alarmText);
                //        }
                //        else if (!string.IsNullOrEmpty(_infoText.Trim()))
                //        {
                //            EV.PostInfoLog(Module, _infoText);
                //        }
                //    }
                //}
              
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

        public void Terminate()
        {
            
        }
    }
}
