using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    public partial class IoConfinementRing : BaseDevice, IDevice
    {
        private DIAccessor _diRingDownFaceback = null;
        private DIAccessor _diRingUpFaceback = null;
        private DIAccessor _diRingDone = null;
        private DIAccessor _diRingServoOn = null;
        private DIAccessor _diRingHomed = null;
        private DIAccessor _diRingBusy = null;
        private DIAccessor _diRingServoError = null;
        private DIAccessor _diHeaterTempLowLimitSW = null;

        private DOAccessor _doRingServoOn = null;
        private DOAccessor _doRingJogUp = null;
        private DOAccessor _doRingJogDown = null;
        private DOAccessor _doRingStop = null;
        private DOAccessor _doRingTeachUp = null;
        private DOAccessor _doRingTeachDown = null;
        private DOAccessor _doRingMoveUp = null;
        private DOAccessor _doRingMoveDown = null;
        private DOAccessor _doRingServoReset = null;

        private AIAccessor _aiRingCurPos = null;
        private AIAccessor _aiRingUpPos = null;
        private AIAccessor _aiRingDownPos = null;

        private AOAccessor _aoRingMode = null;
        private AOAccessor _aoRingUpPos = null;
        private AOAccessor _aoRingDownPos = null;
        private AOAccessor _aoRingSpeed = null;
        private AOAccessor _aoRingDistance = null;

        #region DI
        public bool RingServoError
        {
            get
            {
                if (_diRingServoError != null)
                    return _diRingServoError.Value;

                return false;
            }
        }

        public bool RingDone
        {
            get
            {
                if (_diRingDone != null)
                    return _diRingDone.Value;

                return false;
            }
        }

        public bool RingIsBusy
        {
            get
            {
                if (_diRingBusy != null)
                    return _diRingBusy.Value;

                return false;
            }
        }

        public bool RingServoOn
        {
            get
            {
                if (_diRingServoOn != null)
                    return _diRingServoOn.Value;

                return false;
            }
        }

        public bool RingDownSensor
        {
            get
            {
                if (_diRingDownFaceback != null)
                    return _diRingDownFaceback.Value;

                return false;
            }
        }

        public bool RingUpSensor
        {
            get
            {
                if (_diRingUpFaceback != null)
                    return _diRingUpFaceback.Value;

                return false;
            }
        }

        #endregion

        private bool? IsPmServiceIdle
        {
            get
            {
                try
                {
                    return DATA.Poll<bool>(Module, "IsService");
                }
                catch (Exception)
                {
                    // 避免PMModule未注册该参数造成的异常
                    return null;
                }
            }
        }

        public float RingCurPos
        {
            get
            {
                if (_aiRingCurPos != null)
                    return _aiRingCurPos.FloatValue;

                return 0;
            }
        }

        public float RingUpPos
        {
            get
            {
                if (_aiRingUpPos != null)
                    return _aiRingUpPos.FloatValue;

                return 0;
            }
        }

        public float RingDownPos
        {
            get
            {
                if (_aiRingDownPos != null)
                    return _aiRingDownPos.FloatValue;

                return 0;
            }
        }

        private DeviceTimer _timer = new DeviceTimer();

        private SCConfigItem _scUpPos;
        private SCConfigItem _scDownPos;
        private SCConfigItem _scMoveSpeed;

        public IoConfinementRing(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diRingDownFaceback = ParseDiNode("diRingDownFaceback", node, ioModule);
            _diRingUpFaceback = ParseDiNode("diRingUpFaceback", node, ioModule);
            _diRingDone = ParseDiNode("diRingDone", node, ioModule);
            _diRingServoOn = ParseDiNode("diRingServoOn", node, ioModule);
            _diRingHomed = ParseDiNode("diRingHomed", node, ioModule);
            _diRingBusy = ParseDiNode("diRingBusy", node, ioModule);
            _diRingServoError = ParseDiNode("diRingServoError", node, ioModule);
            _diHeaterTempLowLimitSW = ParseDiNode("diHeaterTempLowLimitSW", node, ioModule);

            _doRingServoOn = ParseDoNode("doRingServoOn", node, ioModule);
            _doRingJogUp = ParseDoNode("doRingJogUp", node, ioModule);
            _doRingJogDown = ParseDoNode("doRingJogDown", node, ioModule);
            _doRingStop = ParseDoNode("doRingStop", node, ioModule);
            _doRingTeachUp = ParseDoNode("doRingTeachUp", node, ioModule);
            _doRingTeachDown = ParseDoNode("doRingTeachDown", node, ioModule);
            _doRingMoveUp = ParseDoNode("doRingMoveUp", node, ioModule);
            _doRingMoveDown = ParseDoNode("doRingMoveDown", node, ioModule);
            _doRingServoReset = ParseDoNode("doRingServoReset", node, ioModule);

            _aiRingCurPos = ParseAiNode("aiRingCurPos", node, ioModule);
            _aiRingUpPos = ParseAiNode("aiRingUpPos", node, ioModule);
            _aiRingDownPos = ParseAiNode("aiRingDownPos", node, ioModule);

            _aoRingMode = ParseAoNode("aoRingMode", node, ioModule);
            _aoRingUpPos = ParseAoNode("aoRingUpPos", node, ioModule);
            _aoRingDownPos = ParseAoNode("aoRingDownPos", node, ioModule);
            _aoRingSpeed = ParseAoNode("aoRingSpeed", node, ioModule);
            _aoRingDistance = ParseAoNode("aoRingDistance", node, ioModule);

            _scUpPos = ParseScNode("ConfinementRingUpPos", node, "PM", "PM.PM1.ConfinementRing.UpPos");
            _scDownPos = ParseScNode("ConfinementRingDownPos", node, "PM", "PM.PM1.ConfinementRing.DownPos");
            _scMoveSpeed = ParseScNode("ConfinementRingMoveSpeed", node, "PM", "PM.PM1.ConfinementRing.MoveSpeed");
        }
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.RingCurPos", () => RingCurPos);
            DATA.Subscribe($"{Module}.{Name}.RingUpPos", () => RingUpPos);
            DATA.Subscribe($"{Module}.{Name}.RingDownPos", () => RingDownPos);

            DATA.Subscribe($"{Module}.{Name}.RingUpSensor", () => RingUpSensor);
            DATA.Subscribe($"{Module}.{Name}.RingDownSensor", () => RingDownSensor);

            DATA.Subscribe($"{Module}.{Name}.RingDone", () => RingDone);
            DATA.Subscribe($"{Module}.{Name}.RingIsServoOn", () => RingServoOn);
            DATA.Subscribe($"{Module}.{Name}.RingIsBusy", () => RingIsBusy);
            DATA.Subscribe($"{Module}.{Name}.RingServoError", () => RingServoError);

            OP.Subscribe($"{Module}.{Name}.ServoStop", (function, args) =>
            {
                bool ret = ServoStop(out string reason);
                if (!ret)
                {
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.ServoOn", (function, args) =>
            {
                bool ret = ServoOn(out string reason);
                if (!ret)
                {
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.ServoReset", (function, args) =>
            {
                Reset();

                return true;
            });

            OP.Subscribe($"{Module}.{Name}.JogUp", (function, args) =>
            {
                bool ret = JogUp((float)args[0],out string reason);
                if (!ret)
                {
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.JogDown", (function, args) =>
            {
                bool ret = JogDown((float)args[0], out string reason);
                if (!ret)
                {
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.MoveUpPos", (function, args) =>
            {
                bool ret = MoveUpPos(out string reason);
                if (!ret)
                {
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.MoveDownPos", (function, args) =>
            {
                bool ret = MoveDownPos(out string reason);
                if (!ret)
                {
                    return false;
                }
                return true;
            });

            return true;
        }
        string reason = string.Empty;

        public bool ServoStop(out string reason)
        {
            if (!_doRingStop.SetValue(true, out reason))
                return false;

            _timer.Start(500);

            return true;
        }

        public bool ServoOn(out string reason)
        {
            bool flag = !_diRingServoOn.Value;

            if (!_doRingServoOn.SetValue(flag, out reason))
                return false;

            _timer.Start(500);

            return true;
        }

        public bool JogUp(float relativeDis,out string reason)
        {
            if (_diRingBusy.Value || _diRingServoError.Value)
            {
                reason = "ConfinementRing is Busy or Error";
                EV.PostAlarmLog(Module, reason);
                return false;
            }

            //传进来为相对值
            _aoRingSpeed.FloatValue = (float)_scMoveSpeed.DoubleValue;
            _aoRingDistance.FloatValue = relativeDis;

            if (!_doRingJogUp.SetValue(true, out reason))
            {
                EV.PostAlarmLog(Module, reason);
                return false;
            }

            _timer.Start(500);

            return true;
        }

        public bool JogDown(float relativeDis, out string reason)
        {
            if (_diRingBusy.Value || _diRingServoError.Value)
            {
                reason = "ConfinementRing is Busy or Error";
                EV.PostAlarmLog(Module, reason);
                return false;
            }

            //传进来为相对值
            _aoRingSpeed.FloatValue = (float)_scMoveSpeed.DoubleValue;
            _aoRingDistance.FloatValue = relativeDis;

            if (!_doRingJogDown.SetValue(true, out reason))
            {
                EV.PostAlarmLog(Module, reason);
                return false;
            }
                

            _timer.Start(500);

            return true;
        }

        public bool MoveUpPos(out string reason)
        {
            if (_diRingBusy.Value || _diRingServoError.Value)
            {
                reason = "ConfinementRing is Busy or Error";
                EV.PostAlarmLog(Module, reason);
                return false;
            }

            if (!_doRingMoveUp.Check(true, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _aoRingSpeed.FloatValue = (float)_scMoveSpeed.DoubleValue;
            _aoRingUpPos.FloatValue = (float)_scUpPos.DoubleValue;

            if (!_doRingMoveUp.SetValue(true, out reason))
            {
                EV.PostWarningLog(Module, $"Ring move Up  failed!");
                return false;
            }

            _timer.Start(500);

            return true;
        }

        public bool MoveDownPos(out string reason)
        {
            if (_diRingBusy.Value || _diRingServoError.Value)
            {
                reason = "ConfinementRing is Busy or Error";
                EV.PostAlarmLog(Module, reason);
                return false;
            }

            /*// PMModule中未注册IsService数据。
            if (IsPmServiceIdle.HasValue == false)
            {
                reason = $"{Module} Data [IsService] is not registered";
                EV.PostAlarmLog(Module, reason);
                return false;
            }
            
            if (IsPmServiceIdle == false)
            {
                reason = $"{Module} is not at [Service] mode";
                EV.PostAlarmLog(Module, reason);
                return false;
            }*/
            
            /*  在Interlock中配置。
             if (_diHeaterTempLowLimitSW.Value != true)
            {
                reason = "DI_HeaterTempLowLimitSW(DI-11) must be true";
                EV.PostAlarmLog(Module, reason);
                return false;
            }*/

            if (!_doRingMoveDown.Check(true, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _aoRingSpeed.FloatValue = (float)_scMoveSpeed.DoubleValue;
            _aoRingDownPos.FloatValue = (float)_scDownPos.DoubleValue;

            if (!_doRingMoveDown.SetValue(true, out reason))
            {
                EV.PostWarningLog(Module, $"Ring move down  failed!");
                return false;
            }

            _timer.Start(500);

            return true;
        }

        public void Monitor()
        {
            if (_timer.IsTimeout())
            {
                _doRingJogUp.Value = false;
                _doRingJogDown.Value = false;
                _doRingMoveUp.Value = false;
                _doRingMoveDown.Value = false;
                _doRingServoReset.Value = false;
                _doRingStop.Value = false;

                _timer.Stop();
            }
        }

        public void Terminate()
        {

        }

        public void Reset()
        {
            if (!_doRingServoReset.SetValue(true, out reason))
                return;

            _timer.Start(500);
        }
    }
}
