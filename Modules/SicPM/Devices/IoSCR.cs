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
    public class IoSCR : BaseDevice, IDevice
    {

        public float VoltageFeedBack
        {
            get
            {
                return _aiVoltage == null ? 0 : (_isFloatAioType ? _aiVoltage.FloatValue : _aiVoltage.Value);
            }
        }
        
        public float ArmsFeedBack
        {
            get
            {
                return _aiArms == null ? 0 : (_isFloatAioType ? _aiArms.FloatValue : _aiArms.Value);
            }
        }

        public float PowerFeedBack
        {
            get
            {
                return _aiPower == null ? 0 : (_isFloatAioType ? _aiPower.FloatValue : _aiPower.Value);
            }
        }

        public bool StatusFeedBack
        {
            get
            {
                return _diStatus == null ? false : _diStatus.Value;
            }
        }



        private bool _isFloatAioType = true;

        private AIAccessor _aiVoltage = null;
        private AIAccessor _aiArms = null;
        private AIAccessor _aiPower = null;
        private DIAccessor _diStatus = null;
        private DIAccessor _diAlarm = null;

        private DOAccessor _doStatus = null;
        private DOAccessor _doReset = null;

        private DeviceTimer _timer = new DeviceTimer();
        private string _infoText = "";
        private R_TRIG _alarmTrig = new R_TRIG();
        public IoSCR(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _aiVoltage = ParseAiNode("aiVoltage", node, ioModule);
            _aiArms = ParseAiNode("aiArms", node, ioModule);
            _aiPower = ParseAiNode("aiPower", node, ioModule);
            _diStatus = ParseDiNode("diStatus", node, ioModule);

            _doReset = ParseDoNode("doReset", node, ioModule);
            _doStatus = ParseDoNode("doStatus", node, ioModule); 
            _diAlarm = ParseDiNode("diAlarm", node, ioModule);
            _infoText = node.GetAttribute("AlarmText");

            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");
        }

        string reason = string.Empty;
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.VoltageFeedBack", () => VoltageFeedBack);
            DATA.Subscribe($"{Module}.{Name}.ArmsFeedBack", () => ArmsFeedBack);
            DATA.Subscribe($"{Module}.{Name}.PowerFeedBack", () => PowerFeedBack);
            DATA.Subscribe($"{Module}.{Name}.StatusFeedBack", () => StatusFeedBack);


            OP.Subscribe($"{Module}.{Name}.SetEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetEnable(isTrue, out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetReset", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetReset(isTrue, out reason);
                return true;
            });

            return true;
        }


        public bool SetEnable(bool setValue, out string reason)
        {
            reason = "";
            if (!_doStatus.Check(setValue, out reason))
                return false;
            if (!_doStatus.SetValue(setValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetReset(bool setValue, out string reason)
        {
            reason = "";

            if (!_doReset.Check(setValue, out reason))
                return false;
            if (!_doReset.SetValue(setValue, out reason))
            {
                return false;
            }

            _timer.Start(1000);

            return true;
        }

        public bool CheckSCREnable()
        {
            return _doStatus.Value;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            try
            {
                MonitorAlarm();
                if (_timer.IsTimeout())
                {
                    _timer.Stop();
                }
                else if (_timer.IsIdle())
                {
                    if (_doReset.Value)
                    {
                        _doReset.Value = false;
                    }
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }


        public void Reset()
        {

        }

        private void MonitorAlarm()
        {
            //_alarmTrig.CLK = _diAlarm != null && _diAlarm.Value && !String.IsNullOrEmpty(_infoText);
            //if (_alarmTrig.Q)
            //{
            //    EV.PostWarningLog(Module, _infoText);
            //}
        }
    }
}
