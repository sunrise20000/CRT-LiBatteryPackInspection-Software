using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData.DeviceData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    public class IoPSU : BaseDevice, IDevice
    {

        public float OutputVoltageFeedBack
        {
            get
            {
                return _aiOutputVoltage == null ? 0 : (_isFloatAioType ? _aiOutputVoltage.FloatValue : _aiOutputVoltage.Value);
            }
        }

        public float OutputArmsFeedBack
        {
            get
            {
                return _aiOutputArms == null ? 0 : (_isFloatAioType ? _aiOutputArms.FloatValue : _aiOutputArms.Value);
            }
        }

        public float OutputPowerFeedBack
        {
            get
            {
                return _aiOutputPower == null ? 0 : (_isFloatAioType ? _aiOutputPower.FloatValue : _aiOutputPower.Value);
            }
        }

        public bool StatusFeedBack
        {
            get
            {
                return _diStatus == null ? false : _diStatus.Value;
            }
        }

        public float SimVoltageFeedBack
        {
            get
            {
                return _aiSimVoltage == null ? 0 : (_isFloatAioType ? _aiSimVoltage.FloatValue : _aiSimVoltage.Value);
            }
        }

        public float SimArmsFeedBack
        {
            get
            {
                return _aiSimArms == null ? 0 : (_isFloatAioType ? _aiSimArms.FloatValue : _aiSimArms.Value);
            }
        }

        public bool AlarmFeedBack
        {
            get
            {
                return _diAlarm == null ? false : _diAlarm.Value;
            }
        }


        public float ConstantSetPoint
        {
            get
            {
                return _aoConstant == null ? 0 : (_isFloatAioType ? _aoConstant.FloatValue : _aoConstant.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoConstant.FloatValue = value;
                }
                else
                {
                    _aoConstant.Value = (short)value;
                }
            }
        }

        public bool AllHeatEnable
        {
            get
            {
                return _diHeatEnable == null ? false : _diHeatEnable.Value;
            }
        }



        private bool _isFloatAioType = false;
        private AIAccessor _aiOutputVoltage = null;
        private AIAccessor _aiOutputArms = null;
        private AIAccessor _aiOutputPower = null;
        private AIAccessor _aiSimVoltage = null;
        private AIAccessor _aiSimArms = null;

        //private AOAccessor _aoEnable = null;
        //private AOAccessor _aoReset = null;
        private AOAccessor _aoConstant = null;

        private DIAccessor _diStatus = null;
        private DIAccessor _diAlarm = null;
        private DIAccessor _diHeatEnable = null;
        private DIAccessor _diCommunicationError = null;

        private DOAccessor _doReset;
        private DOAccessor _doStatus;
        private DOAccessor _doHeatEnable;
        private DOAccessor _doRelatedEnable; //每个Enable同时关联的Inner，Middle，Out Enable

        private string _infoText = "";
        private string _commInfoText = "";

        private R_TRIG _alarmTrig = new R_TRIG();
        private R_TRIG _commAlarmTrig = new R_TRIG();
        private R_TRIG _enableTrig = new R_TRIG();
        private R_TRIG _enableTrig2 = new R_TRIG();

        private DeviceTimer _timer = new DeviceTimer();
        private SCConfigItem _AETempEnable;

        public Func<bool, bool> FuncCheckInterLock;

        public IoPSU(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _aiOutputVoltage = ParseAiNode("aiOutputVoltage", node, ioModule);
            _aiOutputArms = ParseAiNode("aiOutputArms", node, ioModule);
            _aiOutputPower = ParseAiNode("aiOutputPower", node, ioModule);
            _aiSimVoltage = ParseAiNode("aiSimVoltage", node, ioModule);
            _aiSimArms = ParseAiNode("aiSimArms", node, ioModule);
            _aoConstant = ParseAoNode("aoConstant", node, ioModule);

            _doReset = ParseDoNode("doReset", node, ioModule);
            _doStatus = ParseDoNode("doStatus", node, ioModule);
            _diStatus = ParseDiNode("diStatus", node, ioModule);
            _diAlarm = ParseDiNode("diAlarm", node, ioModule);
            _doHeatEnable = ParseDoNode("doHeatEnable", node, ioModule);
            _diHeatEnable = ParseDiNode("diHeatEnable", node, ioModule);
            _doRelatedEnable= ParseDoNode("doRelatedEnable", node, ioModule);
            _diCommunicationError = ParseDiNode("diCommunicationError", node, ioModule);

            _infoText = node.GetAttribute("AlarmText");
            _commInfoText = node.GetAttribute("commAlarmText");

            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float"); 
            _AETempEnable = ParseScNode("AETempEnable1", node, ioModule, $"AETemp.EnableDevice");

        }

        string reason = string.Empty;
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.OutputVoltageFeedBack", () => OutputVoltageFeedBack);
            DATA.Subscribe($"{Module}.{Name}.OutputArmsFeedBack", () => OutputArmsFeedBack);
            DATA.Subscribe($"{Module}.{Name}.OutputPowerFeedBack", () => OutputPowerFeedBack);
            DATA.Subscribe($"{Module}.{Name}.StatusFeedBack", () => StatusFeedBack);
            DATA.Subscribe($"{Module}.{Name}.SimVoltageFeedBack", () => SimVoltageFeedBack);
            DATA.Subscribe($"{Module}.{Name}.SimArmsFeedBack", () => SimArmsFeedBack);
            DATA.Subscribe($"{Module}.{Name}.ConstantSetPoint", () => ConstantSetPoint);
            DATA.Subscribe($"{Module}.{Name}.AlarmFeedBack", () => AlarmFeedBack);
            DATA.Subscribe($"{Module}.{Name}.AllHeatEnable", () => AllHeatEnable);

            OP.Subscribe($"{Module}.{Name}.SetHeadHeaterEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetHeadHeaterEnable(isTrue, out reason);
                return true;
            });


            OP.Subscribe($"{Module}.{Name}.SetPSUEnable", (function, args) =>
            {
                var forceDisable = SC.GetValue<bool>($"PM.{Module}.Heater.ForceDisableInnerHeater");
             
                var isTrue = Convert.ToBoolean(args[0]);
               
                // 如果系统设置中将InnerHeater设置为强制关闭，则不要打开InnerHeater输出
                if (forceDisable)
                {
                    isTrue = false;
                    EV.PostWarningLog(Module, "Inner heater is forcibly disabled");
                }

                SetPSUEnable(isTrue, out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetPSUReset", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetPSUReset(isTrue, out reason);
                return true;
            });

            return true;
        }

        public bool SetPSUEnable(bool setValue, out string reason)
        {
            reason = "";

            if (!_doStatus.Check(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doRelatedEnable.Check(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doStatus.SetValue(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doRelatedEnable.SetValue(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            return true;
        }

        public bool SetHeadHeaterEnable(bool setValue, out string reason)
        {
            reason = "";
            if (FuncCheckInterLock != null)
            {
                if (!FuncCheckInterLock(setValue))
                {
                    EV.PostInfoLog(Module, $"Set PSU Enable fialed for Interlock!");
                    return false;
                }
            }

            if (!_doHeatEnable.Check(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            if (!_doHeatEnable.SetValue(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            return true;
        }

        public bool SetPSUReset(bool setValue, out string reason)
        {
            reason = "";

            if (!_doReset.Check(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doReset.SetValue(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            _timer.Start(1000);
            return true;
        }

        public bool CheckPSUEnable()
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
                MonitorEnableTimer();
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
            _alarmTrig.RST = true;
            _commAlarmTrig.RST = true;
        }

        private void MonitorAlarm()
        {
            //_alarmTrig.CLK = _diAlarm != null && _diAlarm.Value && !String.IsNullOrEmpty(_infoText);
            //if (_alarmTrig.Q)
            //{
            //    EV.PostAlarmLog(Module, _infoText);
            //}

            //_commAlarmTrig.CLK = _diCommunicationError != null && _diCommunicationError.Value && !String.IsNullOrEmpty(_commInfoText);
            //if (_commAlarmTrig.Q)
            //{
            //    EV.PostWarningLog(Module, _commInfoText);
            //}
        }



        private bool isActiveRecord = false;
        private void MonitorEnableTimer()
        {
            if (base.Name == "PSU2")
            {
                _enableTrig.CLK = AETemp2 < 650 && !_diHeatEnable.Value;
                if (_enableTrig.Q)
                {
                    if (isActiveRecord)
                    {
                        SC.SetItemValue($"PM.{Module}.OpenLidCountDownTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }

                _enableTrig2.CLK = AETemp2 > 650 || _diHeatEnable.Value;
                if (_enableTrig2.Q)
                {
                    isActiveRecord = true; 
                    SC.SetItemValue($"PM.{Module}.OpenLidCountDownTime", "");
                }
            }
        }

        public double AETemp2
        {
            get
            {
                if (_AETempEnable.BoolValue && base.Name == "PSU2")
                {
                    try
                    {
                        object temp1 = DATA.Poll($"{Module}.AETemp.AETemp2");
                        return temp1 == null ? 0 : (double)temp1;
                    }
                    catch (Exception ex)
                    {
                        return 0;
                    }
                }
                return 0;
            }

        }
    }
}
