using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.RT.SCCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    public class IoTempMeter : BaseDevice, IDevice
    {

        private AIAccessor _aiFeedBack = null;

        private bool _isFloatAioType;
        private R_TRIG _trigTextOutUnload = new R_TRIG();
        private R_TRIG _trigTextOutBuffer = new R_TRIG();

        private string _warningText;
        private string _alarmText;
        private string _infoText;

        private float _unloadTemp;
        private float _bufferTemp;
        private SCConfigItem _scUnloadTemp;
        private SCConfigItem _scBufferTemp;

        public float FeedBack
        {
            get
            {
                if (_aiFeedBack != null)
                    return _isFloatAioType ? _aiFeedBack.FloatValue : _aiFeedBack.Value;

                return 0;
            }
        }

        public AITHeaterData DeviceData
        {
            get
            {
                var data = new AITHeaterData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DisplayName = Display,
                    DeviceSchematicId = DeviceID,
                    UniqueName = UniqueName,
                    FeedBack = FeedBack,
                };
               // data.AttrValue["PV"] = _aiFeedBack;
                return data;
            }
        }

        public IoTempMeter(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");
            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");
            _scUnloadTemp = ParseScNode("WarnTemprature", node, "UnLoad", "UnLoad.WarnTemprature");
            _unloadTemp = (float)_scUnloadTemp.DoubleValue;
            _scBufferTemp = ParseScNode("PickTemprature", node, "Buffer", "Buffer.PickTemprature");
            _bufferTemp = (float)_scBufferTemp.DoubleValue;


            _aiFeedBack = ParseAiNode("aiFeedback", node, ioModule);

            _infoText = "";
            _warningText = "";
            _alarmText = "";
            if (base.Name == "UnLoadTemp")
            {
                _warningText = "Waring 10 Unload Temp High [AI-5]";
            }
            else if(base.Name == "BufferTemp")
            {
                _warningText = "Waring 12 Buffer Temp High [AI-7]";
            }

            UniqueName = Module + "." + Name;
        }
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.FeedBack", () => FeedBack);
            return true;
        }



        public void Reset()
        {
            _trigTextOutUnload.RST = true;
            _trigTextOutBuffer.RST = true;
        }

        public void Terminate()
        {

        }

        public void Monitor()
        {
            try
            {
                if (base.Name == "UnLoadTemp" )//AI-5
                {
                    _trigTextOutUnload.CLK = _aiFeedBack.Value >= _unloadTemp;

                    if (_trigTextOutUnload.Q)
                    {
                        if (!string.IsNullOrEmpty(_warningText.Trim()))
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
                }
                if (base.Name == "BufferTemp")//AI-7
                {
                    _trigTextOutBuffer.CLK = _aiFeedBack.Value >= _bufferTemp;

                    if (_trigTextOutBuffer.Q)
                    {
                        if (!string.IsNullOrEmpty(_warningText.Trim()))
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
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }
        }
    }
}
