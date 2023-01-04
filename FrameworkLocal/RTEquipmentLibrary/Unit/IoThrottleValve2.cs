using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoThrottleValve2 : BaseDevice, IDevice
    {
        public PressureCtrlMode PressureMode
        {
            get
            {
                if (_aoPressureMode != null)
                {
                    foreach (var ioMode in _ctrlModeIoValue)
                    {
                        if (_isFloatAioType)
                        {
                            if ((int)_aoPressureMode.FloatValue == ioMode.Value)
                            {
                                return ioMode.Key;
                            }
                        }
                        else
                        {
                            if (_aoPressureMode.Value == ioMode.Value)
                            {
                                return ioMode.Key;
                            }
                        }

                    }
                    return PressureCtrlMode.Undefined;
                }

                return PressureCtrlMode.Undefined;
            }
            set
            {
                if (_isFloatAioType)
                    _aoPressureMode.FloatValue = _ctrlModeIoValue[value];
                else
                {
                    _aoPressureMode.Value = (short)_ctrlModeIoValue[value];
                }
            }
        }

        public PressureCtrlMode ControlModeFeedback
        {
            get
            {
                if (_aiPressureMode != null)
                {
                    foreach (var ioMode in _ctrlModeIoValue)
                    {
                        if (_isFloatAioType)
                        {
                            if ((int)_aiPressureMode.FloatValue == ioMode.Value)
                            {
                                return ioMode.Key;
                            }
                        }
                        else
                        {
                            if (_aiPressureMode.Value == ioMode.Value)
                            {
                                return ioMode.Key;
                            }
                        }

                    }
                    return PressureCtrlMode.Undefined;
                }

                return PressureCtrlMode.Undefined;
            }
        }

        public bool TVValveEnable
        {
            get
            {
                if (_doTVValveEnable!=null)
                {
                    return _doTVValveEnable.Value;
                }
                return false;
            }
        }

        public float PositionSetpoint
        {
            get
            {
                return _aoPositionSetPoint == null ? 0 : (_isFloatAioType? _aoPositionSetPoint.FloatValue: _aoPositionSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPositionSetPoint.FloatValue = value;
                }
                else
                {
                    _aoPositionSetPoint.Value = (short)value;
                }
            }
        }

        public float PositionFeedback
        {
            get
            {
                return _aiPositionFeedback == null ? 0 : (_isFloatAioType ? _aiPositionFeedback.FloatValue: _aiPositionFeedback.Value);
            }
        }

        public float PressureSetpoint
        {
            get
            {
                return _aoPressureSetPoint == null ? 0 : (_isFloatAioType ? _aoPressureSetPoint.FloatValue:_aoPressureSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPressureSetPoint.FloatValue = value;
                }
                else
                {
                    _aoPressureSetPoint.Value = (short)value;
                }
            }
        }

        public float PressureFeedback
        {
            get
            {
                return _aiPressureFeedback == null ? 0 : (_isFloatAioType ? _aiPressureFeedback.FloatValue:_aiPressureFeedback.Value);
            }
        }

        private float _pressureSetPointByUse;
        public float PressureSetpointByUse
        {
            get
            {
                return _pressureSetPointByUse;
            }
            set
            {
                _pressureSetPointByUse = value;
            }
        }

        private float _positionSetPointByUse;
        public float PositionSetpointByUse
        {
            get
            {
                return _positionSetPointByUse;
            }
            set
            {
                _positionSetPointByUse = value;
            }
        }

        public float PositionDefaultValue
        {
            get
            {
                if (_positionDefaultValue != null)
                {
                    return (float)_positionDefaultValue.DoubleValue;
                }
                else
                {
                    return 0;
                }
            }
        }

        private float _pressureDefaultValue2;
        public float PressureDefaultValue
        {
            get
            {
                //return _pressureDefaultValue2;
                if (_pressureDefaultValue != null)
                {
                    return (float)_pressureDefaultValue.DoubleValue;
                }
                else
                {
                    return 1100;
                }
            }
            //set
            //{
            //    _pressureDefaultValue2 = value;
            //}
        }


        private SCConfigItem _positionDefaultValue;
        private SCConfigItem _pressureDefaultValue;

        private DeviceTimer _closeTimer = new DeviceTimer();
        private DeviceTimer _rampTimer = new DeviceTimer();
        private double _rampTarget;
        private double _rampInitValue;
        private int _rampTime;


        private DeviceTimer _rampTimer1 = new DeviceTimer();
        private double _rampTarget1;
        private double _rampInitValue1;
        private int _rampTime1;

        private AITThrottleValveData DeviceData
        {
            get
            {
                AITThrottleValveData data = new AITThrottleValveData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    Mode = (int)ControlModeFeedback,
                    PositionFeedback = PositionFeedback,
                    PositionSetPoint = PositionSetpointByUse,
                    PositionSetPointCurrent = PositionSetpoint,

                    PressureFeedback = PressureFeedback,
                    PressureSetPoint = PressureSetpointByUse,
                    PressureSetPointCurrent = PressureSetpoint,

                    TVEnable = TVValveEnable,

                    MaxValuePosition = 100,
                    MaxValuePressure = _unitPressure == "mbar" ? 1500 : 1500,

                    UnitPressure = _unitPressure,
                };

                return data;
            }
        }

        private AIAccessor _aiPressureMode = null;

        private AOAccessor _aoPressureMode = null;

        private AIAccessor _aiPositionFeedback = null;

        private AOAccessor _aoPositionSetPoint = null;

        private AIAccessor _aiPressureFeedback = null;

        private AOAccessor _aoPressureSetPoint = null;

        private DOAccessor _doTVValveEnable = null;

        private string _unitPressure;

        private Dictionary<PressureCtrlMode, int> _ctrlModeIoValue = new Dictionary<PressureCtrlMode, int>();
        
        private bool _isFloatAioType = false;
        private R_TRIG _setTvCloseTrig = new R_TRIG();

        public IoThrottleValve2(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }

            _unitPressure = node.GetAttribute("unit");
            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _aiPressureMode = ParseAiNode("aiStatus", node, ioModule);
            _aoPressureMode = ParseAoNode("aoPressureMode", node, ioModule);

            _aiPositionFeedback = ParseAiNode("aiPositionFeedback", node, ioModule);
            _aoPositionSetPoint = ParseAoNode("aoPositionSetPoint", node, ioModule);
            _aiPressureFeedback = ParseAiNode("aiPressureFeedback", node, ioModule);
            _aoPressureSetPoint = ParseAoNode("aoPressureSetPoint", node, ioModule);
            _doTVValveEnable = ParseDoNode("doTVValveEnable", node, ioModule);

            _positionDefaultValue = ParseScNode("positionDefaultValue", node, ioModule, $"{scBasePath}.ThrottlePositionDefaultValue");
            _pressureDefaultValue = ParseScNode("pressureDefaultValue", node, ioModule, $"{scBasePath}.ThrottlePressureDefaultValue");

            EnumLoop<PressureCtrlMode>.ForEach(x => _ctrlModeIoValue[x] = -1);

            if (!string.IsNullOrEmpty(node.GetAttribute("modeValuePressure")))
            {
                _ctrlModeIoValue[PressureCtrlMode.TVPressureCtrl] = int.Parse(node.GetAttribute("modeValuePressure"));
            }
            if (!string.IsNullOrEmpty(node.GetAttribute("modeValuePosition")))
            {
                _ctrlModeIoValue[PressureCtrlMode.TVPositionCtrl] = int.Parse(node.GetAttribute("modeValuePosition"));
            }
            if (!string.IsNullOrEmpty(node.GetAttribute("modeValueOpen")))
            {
                _ctrlModeIoValue[PressureCtrlMode.TVOpen] = int.Parse(node.GetAttribute("modeValueOpen"));
            }
            if (!string.IsNullOrEmpty(node.GetAttribute("modeValueClose")))
            {
                _ctrlModeIoValue[PressureCtrlMode.TVClose] = int.Parse(node.GetAttribute("modeValueClose"));
            }
            //PressureDefaultValue = (float)_pressureDefaultValue.DoubleValue;
        }


        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.PositionSetPoint", () => PositionSetpoint);
            DATA.Subscribe($"{Module}.{Name}.PositionFeedback", () => PositionFeedback);
            DATA.Subscribe($"{Module}.{Name}.PressureSetPoint", () => PressureSetpoint);
            DATA.Subscribe($"{Module}.{Name}.PressureFeedback", () => PressureFeedback);
            DATA.Subscribe($"{Module}.{Name}.Mode", () => ControlModeFeedback);
            DATA.Subscribe($"{Module}.{Name}.TVValveEnable", () => TVValveEnable);

            OP.Subscribe($"{Module}.{Name}.{AITThrottleValveOperation.SetMode}", SetMode);

            OP.Subscribe($"{Module}.{Name}.{AITThrottleValveOperation.SetPosition}", SetPosition);

            OP.Subscribe($"{Module}.{Name}.{AITThrottleValveOperation.SetPressure}", SetPressure);

            OP.Subscribe($"{Module}.{Name}.SetPositionToZero", SetPositionToZero);

            OP.Subscribe($"{Module}.{Name}.SetTVEnableRoutine", SetTVEnableRoutine);

            OP.Subscribe($"{Module}.{Name}.SetTVValveEnable", (function, args) =>
            {
                bool bOn = Convert.ToBoolean((string)args[0]);
                SetTVEnable(bOn, out string reason);
                return true;
            });

            return true;
        }

        

        public bool SetMode(PressureCtrlMode mode, out string reason)
        {
            reason = String.Empty;
            if (mode == ControlModeFeedback)
            {
                return true;
            }
            if (mode == PressureCtrlMode.TVClose)
            {
                _rampTimer.Stop();
                _rampTimer1.Stop();
                reason = $"{Display} set to {mode}";
                PressureMode = mode;
            }
            else
            {
                if (!_rampTimer.IsIdle() || !_rampTimer1.IsIdle())
                {
                    //在压力正在调节过程中不允许切换模式
                    reason = "Can not set mode while pressure is ramping!";
                    return false;
                }

                reason = $"{Display} set to {mode}";
                PressureMode = mode;
            }

            EV.PostInfoLog(Module, reason);

            return true;
        }

        private bool SetMode(out string reason, int time, object[] param)
        {
            return SetMode((PressureCtrlMode)Enum.Parse(typeof(PressureCtrlMode), (string)param[0], true),
                out reason);
        }

        //private bool SetPosition(float position, out string reason)
        //{
        //    PositionSetpoint = position;

        //    reason = $"{Display} position set to {position}";

        //    EV.PostInfoLog(Module, reason);

        //    return true;
        //}

        public bool SetPosition(out string reason, int time, object[] param)
        {
            reason = String.Empty;
            if (ControlModeFeedback != PressureCtrlMode.TVPositionCtrl)
            {
                reason = "TV is not in position control mode";
                return false;
            }

            PositionSetpointByUse = (float)Convert.ToDouble(param[0]);

            if (time > 0)
            {
                _rampTimer1.Stop();
                Ramp(Convert.ToDouble(param[0]), time);
            }
            else
            {
                _rampTimer1.Stop();
                double rate = SC.GetValue<double>($"PM.{Module}.ThrottlePositionRate");
                RampByRatePosition(Convert.ToDouble(param[0]), rate);
            }
            return true;
        }

        public bool SetPressure(out string reason, int time, object[] param)
        {
            reason = String.Empty;

            if (ControlModeFeedback != PressureCtrlMode.TVPressureCtrl)
            {
                reason = "TV is not in pressure control mode";
                return false;
            }

            PressureSetpointByUse = (float)Convert.ToDouble(param[0]);

            _rampTimer.Stop();

            if (time > 0)
            {
                Ramp(Convert.ToDouble(param[0]), time);
            }
            else
            {
                double rate = SC.GetValue<double>($"PM.{Module}.ThrottlePressureRate");
                RampByRate(Convert.ToDouble(param[0]), rate);
            }
            return true;
        }

        //public bool SetPressureToDefault(string reason, object[] param)
        //{
        //    _rampTimer.Stop();

        //    if (PressureMode != PressureCtrlMode.TVPositionCtrl)
        //    {
        //        PressureMode = PressureCtrlMode.TVPositionCtrl;
        //    }

        //    PositionSetpoint = PositionDefaultValue;
        //    PressureSetpoint = PressureDefaultValue;

        //    PositionSetpointByUse = PositionDefaultValue;
        //    PressureSetpointByUse = PressureDefaultValue;
        //    return true;
        //}

        public bool SetPositionToZero(string reason, object[] param)
        {
            _rampTimer.Stop();
            _rampTimer1.Stop();

            if (ControlModeFeedback != PressureCtrlMode.TVPositionCtrl)
            {
                PressureMode = PressureCtrlMode.TVPositionCtrl;
            }

            PositionSetpoint = 0;
            //PressureSetpoint = PressureDefaultValue;

            PositionSetpointByUse = 0;
            //PressureSetpointByUse = PressureDefaultValue;

            return true;
        }

        //public bool SetPositionImmediately(string reason, object[] param)
        //{
        //    _rampTimer.Stop();
        //    reason = String.Empty;
        //    PositionSetpoint = 0;
        //    return true;
        //}

        //private bool SetPressure(float pressure, out string reason)
        //{
        //    PressureSetpoint = pressure;

        //    reason = $"{Display}  pressure set to {pressure} {_unitPressure}";

        //    EV.PostInfoLog(Module, reason);

        //    return true;
        //}

        public bool SetTVEnable(bool isEnable, out string reason)
        {
            reason = String.Empty;

            _rampTimer.Stop();
            _rampTimer1.Stop();
            //开阀时设置Ai的默认值
            if (isEnable)
            {
                if (!_doTVValveEnable.SetValue(isEnable, out reason))
                    return false;

                Thread.Sleep(50);

                if (ControlModeFeedback != PressureCtrlMode.TVPressureCtrl)
                {
                    PressureMode = PressureCtrlMode.TVPressureCtrl;
                }
                
                Thread.Sleep(50);

                //PositionSetpoint = 0;
                PressureSetpoint = PressureDefaultValue;

                //PositionSetpointByUse = 0;
                PressureSetpointByUse = PressureDefaultValue;
            }
            else
            {
                //PositionSetpoint = 0;
                PressureSetpoint = PressureDefaultValue;

                //PositionSetpointByUse = 0;
                PressureSetpointByUse = PressureDefaultValue;

                Thread.Sleep(50);

                if (ControlModeFeedback != PressureCtrlMode.TVClose)//强关模式
                {
                    PressureMode = PressureCtrlMode.TVClose;
                }

                if (!_doTVValveEnable.SetValue(isEnable, out reason))
                    return false;

            }            

            reason = $"{Display} set to {isEnable}";

            EV.PostInfoLog(Module, reason);
            return true;
        }

        public bool SetTVEnableState(bool isEnable, out string reason)
        {
            _rampTimer.Stop();
            _rampTimer1.Stop();

            if (!_doTVValveEnable.SetValue(isEnable, out reason))
                return false;

            reason = $"{Display} set to {isEnable}";

            EV.PostInfoLog(Module, reason);
            return true;
        }

        public bool SetTVEnableRoutine(out string reason,int time,object[] param)
        {
            _rampTimer.Stop();
            _rampTimer1.Stop();

            bool isEnable = Convert.ToBoolean(param[0]);

            if (!_doTVValveEnable.SetValue(isEnable, out reason))
                return false;

            reason = $"{Display} set to {isEnable}";

            EV.PostInfoLog(Module, reason);
            return true;
        }

        public void Terminate()
        {
        }

        public void StopRamp()
        {
            Ramp(PressureSetpoint, 0);
        }

        public void Monitor()
        {
            try
            {
                MonitorRamping();
                MonitorRampingPosition();
                //MonitorClose();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        private void MonitorClose()
        {
            //InterLock 
            if (!_doTVValveEnable.Value && ControlModeFeedback != PressureCtrlMode.TVClose)
            {
                PressureMode = PressureCtrlMode.TVClose;
            }

        }

        public void RampByRate(double target, double rate)
        {
            if (ControlModeFeedback == PressureCtrlMode.TVPressureCtrl)
            {
                _rampInitValue = PressureFeedback;// PressureSetpoint; //ramp 初始值取实际读取值
                _rampTime = 0;
                if (rate > 0)
                {
                    _rampTime = Math.Abs((int)((target - PressureFeedback) / rate) * 1000);   //根据速率计算超时时间
                }
                _rampTarget = target;
                _rampTimer.Start(_rampTime);
            }
        }

        private void RampByRatePosition(double target, double rate)
        {
            if (ControlModeFeedback == PressureCtrlMode.TVPositionCtrl)
            {
                _rampInitValue1 = PositionFeedback;//PositionSetpoint;
                _rampTime1 = 0;
                if (rate > 0)
                {
                    _rampTime1 = Math.Abs((int)((target - PositionFeedback) / rate) * 1000);
                }
                _rampTarget1 = target;
                _rampTimer1.Start(_rampTime1);
            }
        }


        private void Ramp(double target, int time)
        {
            if (ControlModeFeedback == PressureCtrlMode.TVPositionCtrl)
            {
                _rampInitValue1 = PositionFeedback; //PositionSetpoint;                              //ramp 初始值取实际读取值
                _rampTime1 = time;
                _rampTarget1 = target;
                _rampTimer1.Start(_rampTime1);
            }
            else
            {
                _rampInitValue = PressureFeedback;// PressureSetpoint;                              //ramp 初始值取当前设定值，而非实际读取值.零漂问题
                _rampTime = time;
                _rampTarget = target;
                _rampTimer.Start(_rampTime);
            }
        }

        private void MonitorRamping()
        {
            if (!_rampTimer.IsIdle())
            {
                if (ControlModeFeedback == PressureCtrlMode.TVPressureCtrl)
                {
                    //压力模式
                    if (_rampTimer.IsTimeout() || _rampTime == 0)
                    {
                        _rampTimer.Stop();
                        PressureSetpoint = (float)_rampTarget;
                    }
                    else
                    {
                        PressureSetpoint = (float)(_rampInitValue + (_rampTarget - _rampInitValue) * _rampTimer.GetElapseTime() / _rampTime);
                    }
                }
            }
        }


        private void MonitorRampingPosition()
        {
            if (!_rampTimer1.IsIdle())
            {
                if (ControlModeFeedback == PressureCtrlMode.TVPositionCtrl)
                {
                    if (_rampTimer1.IsTimeout() || _rampTime1 == 0)
                    {
                        _rampTimer1.Stop();
                        PositionSetpoint = (float)_rampTarget1;
                    }
                    else
                    {
                        PositionSetpoint = (float)(_rampInitValue1 + (_rampTarget1 - _rampInitValue1) * _rampTimer1.GetElapseTime() / _rampTime1);
                    }
                }               
            }
        }


        public void Reset()
        {
            _setTvCloseTrig.RST = true;
        }
    }
}
