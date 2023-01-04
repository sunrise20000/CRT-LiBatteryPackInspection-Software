using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    public partial class SicServo : BaseDevice, IDevice
    {
        private DIAccessor _diServoReady;
        private DIAccessor _diServoError;

        private DOAccessor _doServoEnable;
        private DOAccessor _doServoInital;
        private DOAccessor _doServoReset;

        private AIAccessor _aiActualSpeed = null;
        private AIAccessor _aiActualCurrent = null;
        private AIAccessor _aiAccSpeed = null;
        private AIAccessor _aiDecSpeed = null;

        private AOAccessor _aoActualSpeedSetPoint = null;
        private AOAccessor _aoAccSpeedSetPoint = null;
        private AOAccessor _aoDecSpeedSetPoint = null;

        private R_TRIG _trigInital = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigDisable = new R_TRIG();
        private R_TRIG _trigReady = new R_TRIG();

        private R_TRIG _trigEnable = new R_TRIG();


        #region AI
        public bool ServoEnable
        {
            get
            {
                if (_doServoEnable != null)
                    return _doServoEnable.Value;

                return false;
            }
        }

        public bool ServoReady
        {
            get
            {
                if (_diServoReady != null)
                    return _diServoReady.Value;

                return false;
            }
        }
        public bool ServoError
        {
            get
            {
                if (_diServoError != null)
                    return !_diServoError.Value;

                return false;
            }
        }
        public float ActualSpeedFeedback
        {
            get
            {
                if (_aiActualSpeed != null)
                    return _aiActualSpeed.FloatValue;

                return 0;
            }
        }
        public float ActualCurrentFeedback
        {
            get
            {
                if (_aiActualCurrent != null)
                    return _aiActualCurrent.FloatValue;

                return 0;
            }
        }
        public float AccSpeedFeedback
        {
            get
            {
                if (_aiAccSpeed != null)
                    return _aiAccSpeed.FloatValue;

                return 0;
            }
        }
        public float DecSpeedFeedback
        {
            get
            {
                if (_aiDecSpeed != null)
                    return _aiDecSpeed.FloatValue;

                return 0;
            }
        }
        #endregion AI

        #region AO

        public float SpeedSetpoint
        {
            get
            {
                return _aoActualSpeedSetPoint == null ? 0 : _aoActualSpeedSetPoint.FloatValue;
            }
            set
            {
                _aoActualSpeedSetPoint.FloatValue = value;
            }
        }

        #endregion

        private DeviceTimer _rampTimer = new DeviceTimer();
        private DeviceTimer _setTimer = new DeviceTimer();
        private float _rampTarget;
        private float _rampInitValue;
        private int _rampTime;
        private bool _setValueIsZero = false;

        public SicServo(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diServoReady = ParseDiNode("diServoReady", node, ioModule);
            _diServoError = ParseDiNode("diServoError", node, ioModule);
            _doServoEnable = ParseDoNode("doServoEnable", node, ioModule);
            _doServoInital = ParseDoNode("doServoInital", node, ioModule);
            _doServoReset = ParseDoNode("doServoReset", node, ioModule);

            _aiActualSpeed = ParseAiNode("aiActualSpeed", node, ioModule);
            _aiActualCurrent = ParseAiNode("aiActualCurrent", node, ioModule);
            _aiAccSpeed = ParseAiNode("aiAccSpeed", node, ioModule);
            _aiDecSpeed = ParseAiNode("aiDecSpeed", node, ioModule);
            _aoActualSpeedSetPoint = ParseAoNode("aoActualSpeedSetPoint", node, ioModule);
            _aoAccSpeedSetPoint = ParseAoNode("aoAccSpeedSetPoint", node, ioModule);
            _aoDecSpeedSetPoint = ParseAoNode("aoDecSpeedSetPoint", node, ioModule);
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.ServoReady", () => ServoReady);
            DATA.Subscribe($"{Module}.{Name}.ServoError", () => ServoError); 
            DATA.Subscribe($"{Module}.{Name}.ServoEnable", () => ServoEnable);

            DATA.Subscribe($"{Module}.{Name}.ActualSpeedFeedback", () => ActualSpeedFeedback);
            DATA.Subscribe($"{Module}.{Name}.ActualCurrentFeedback", () => ActualCurrentFeedback);
            DATA.Subscribe($"{Module}.{Name}.AccSpeedFeedback", () => AccSpeedFeedback);
            DATA.Subscribe($"{Module}.{Name}.DecSpeedFeedback", () => DecSpeedFeedback);

            OP.Subscribe($"{Module}.{Name}.SetServoEnable", (function, args) =>
            {
                bool enable = Convert.ToBoolean(args[0].ToString());
                SetServoEnable(enable, out string reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetServoInital", (function, args) =>
            {
                SetServoInital();
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetServoReset", (function, args) =>
            {
                SetServoReset();
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetActualSpeed", (out string reason, int time, object[] param) =>
            {
                reason = "";
                float target = Convert.ToSingle(param[0].ToString());
                SetActualSpeed(target, time);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetAccSpeed", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetAccSpeed(target);
                return true;
            }); 
            OP.Subscribe($"{Module}.{Name}.SetDecSpeed", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetDecSpeed(target);
                return true;
            });

            return true;
        }

        public bool SetServoEnable(bool enable, out string reason)
        {            
            if (!_doServoEnable.Check(enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doServoEnable.SetValue(enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            if (enable)
            {
                _trigDisable.RST = true;
            }
            return true;
        }
        public bool SetServoInital()
        {
            if (!ServoEnable)
            {
                EV.PostWarningLog(Module, "Warning40 Servo Not Ready  [DI-288]");
                return false;
            }

            bool dValue = true;
            string reason = "";
            if (!_doServoInital.Check(dValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doServoInital.SetValue(dValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _trigInital.RST = true;
            _setTimer.Start(2000);
            return true;
        }
        public bool SetServoReset()
        {
            if (!ServoEnable)
            {
                EV.PostWarningLog(Module, "Servo is disable!");
                return false;
            }
            

            bool dValue = true;
            string reason = "";
            if (!_doServoReset.Check(dValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doServoReset.SetValue(dValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _trigInital.RST = true;
            _trigError.RST = true;
            _setTimer.Start(2000);
            return true;
        }

        public void SetActualSpeed(float target, int time)
        {
            //取消检查
            //if (!ServoReady)
            //{
            //    EV.PostWarningLog(Module, "Servo is not ready!");
            //    return;
            //}

            if (target == 0 && ServoEnable)
            {
                _setValueIsZero = true;
            }
            else
            {
                _setValueIsZero = false;
            }

            _rampTimer.Stop();
            _rampInitValue = _aoActualSpeedSetPoint.FloatValue;    //ramp 初始值取当前设定值，而非实际读取值.零漂问题
            _rampTime = time;
            _rampTarget = target;
            _rampTimer.Start(_rampTime);
        }

        public void SetAccSpeed(float dValue)
        {
            _aoAccSpeedSetPoint.FloatValue = (float)dValue;
        }

        public void SetDecSpeed(float dValue)
        {
            _aoDecSpeedSetPoint.FloatValue = (float)dValue;
        }

        private void MonitorRamping()
        {
            if (!_rampTimer.IsIdle())
            {
                if (_rampTimer.IsTimeout() || _rampTime == 0)
                {
                    _rampTimer.Stop();
                    SpeedSetpoint = _rampTarget;
                }
                else
                {
                    SpeedSetpoint = _rampInitValue + (_rampTarget - _rampInitValue) * (float)_rampTimer.GetElapseTime() / _rampTime;
                }
            }
        }

        private void MonitorSet()
        {
            if (_setTimer.IsTimeout())
            {
                //Reset和Inital 保持两秒清除
                _trigInital.CLK = true;
                if (_trigInital.Q)
                {
                    _doServoInital.SetValue(false, out string reason);
                    _doServoReset.SetValue(false, out reason);
                }

                _setTimer.Stop();
            }
            

            //错误将速度清0,Enable清0
            _trigError.CLK = ServoError;
            if (_trigError.Q)
            {
                SpeedSetpoint = 0;
                _doServoEnable.SetValue(false, out string reason);
                EV.PostAlarmLog(Module,"Alarm29 Rotation Servo Driver Error [DI-45]");
            }

            //Disable将速度清0
            _trigDisable.CLK = !ServoEnable;
            if (_trigDisable.Q)
            {
                SpeedSetpoint = 0;
            }

            //ServoReady
            _trigReady.CLK = !ServoReady;
            if(_trigReady.Q)
            {
                EV.PostWarningLog(Module, "Warning40 Servo Not Ready  [DI-288]");
            }
    }

        public void Monitor()
        {
            MonitorRamping();
            MonitorSet();

            _trigEnable.CLK = ServoEnable;
            if (_trigEnable.Q)
            {
                _setValueIsZero = false;
            }

            if (_setValueIsZero && ActualSpeedFeedback <= 1 && ServoEnable)
            {
                _doServoEnable.SetValue(false, out string reason);
                _setValueIsZero = false;

                EV.PostWarningLog(Module, "servo set speed is 0,Force set servoEnable false");
            }
        }

        public void Reset()
        {
            //throw new NotImplementedException();
            _trigReady.RST = true;
        }

        public void StopRamp()
        {
            SetActualSpeed(SpeedSetpoint, 0);
        }

        public void Terminate()
        {
            //throw new NotImplementedException();
        }
    }
}
