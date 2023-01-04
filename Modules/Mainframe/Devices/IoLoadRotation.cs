using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.RT.EquipmentLibrary.Core.Extensions;
using System.Threading;
using System.Xml;

namespace Mainframe.Devices
{
    public class IoLoadRotation : BaseDevice, IDevice
    {
        private DIAccessor _diServoOn = null;
        private DIAccessor _diServoBusy = null;
        private DIAccessor _diServoError = null;
        private DIAccessor _diMoveDone = null;
        private DIAccessor _diRelativeHomeDone = null;
        private DIAccessor _diOneCircleDone = null;
        private DIAccessor _diCCD1PosDone = null;
        private DIAccessor _diCCD2PosDone = null;

        private DOAccessor _doServoOn = null;
        private DOAccessor _doServoReset = null;
        private DOAccessor _doJogCW = null;
        private DOAccessor _doJogCCW = null;
        private DOAccessor _doStop = null;
        private DOAccessor _doMoveRelativeHome = null;
        private DOAccessor _doMoveOneCircle = null;
        private DOAccessor _doMoveCCD1Pos = null;
        private DOAccessor _doMoveCCD2Pos = null;

        private AIAccessor _aiCurPos = null;
        private AIAccessor _aiCCD1Degree = null;
        private AIAccessor _aiCCD2Degree = null;

        private AOAccessor _aoHomeOffset = null;
        private AOAccessor _aoJogDegree = null;
        private AOAccessor _aoCCD1Degree = null;
        private AOAccessor _aoHomeSpeed = null;
        private AOAccessor _aoPosSpeed = null;
        private AOAccessor _aoCCD2Degree = null;

        private SCConfigItem _scHomeSpeed;
        private SCConfigItem _scHomeOffset;
        private SCConfigItem _scPosSpeed;
        private SCConfigItem _scCCD1Pos;
        private SCConfigItem _scCCD2Pos;

        private DeviceTimer _timer = new DeviceTimer();

        public bool IsServoOn
        {
            get { return _diServoOn != null ? _diServoOn.Value : false; }
        }

        public bool IsServoBusy
        {
            get { return _diServoBusy != null ? _diServoBusy.Value : false; }
        }

        public bool IsServoError
        {
            get { return _diServoError != null ? _diServoError.Value : false; }
        }

        public bool IsMoveDone
        {
            get { return _diMoveDone != null ? _diMoveDone.Value : false; }
        }

        public double CurPos
        {
            get { return _aiCurPos != null ? _aiCurPos.FloatValue : 0; }
        }

        public double CCD1Degree
        {
            get { return _aiCCD1Degree != null ? _aiCCD1Degree.FloatValue : 0; }
        }

        public double CCD2Degree
        {
            get { return _aiCCD2Degree != null ? _aiCCD2Degree.FloatValue : 0; }
        }

        public IoLoadRotation(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diServoOn = ParseDiNode("diServoOn", node, ioModule);
            _diServoBusy = ParseDiNode("diServoBusy", node, ioModule);
            _diServoError = ParseDiNode("diServoError", node, ioModule);
            _diMoveDone = ParseDiNode("diMoveDone", node, ioModule);
            _diRelativeHomeDone = ParseDiNode("diRelativeHomeDone", node, ioModule);
            _diOneCircleDone = ParseDiNode("diOneCircleDone", node, ioModule);
            _diCCD1PosDone = ParseDiNode("diCCD1PosDone", node, ioModule);
            _diCCD2PosDone = ParseDiNode("diCCD2PosDone", node, ioModule);

            _doServoOn = ParseDoNode("doServoOn", node, ioModule);
            _doServoReset = ParseDoNode("doServoReset", node, ioModule);
            _doJogCW = ParseDoNode("doJogCW", node, ioModule);
            _doJogCCW = ParseDoNode("doJogCCW", node, ioModule);
            _doStop = ParseDoNode("doStop", node, ioModule);
            _doMoveRelativeHome = ParseDoNode("doMoveRelativeHome", node, ioModule);
            _doMoveOneCircle = ParseDoNode("doMoveOneCircle", node, ioModule);
            _doMoveCCD1Pos = ParseDoNode("doMoveCCD1Pos", node, ioModule);
            _doMoveCCD2Pos = ParseDoNode("doMoveCCD2Pos", node, ioModule);

            _aiCurPos = ParseAiNode("aiCurPos", node, ioModule);
            _aiCCD1Degree = ParseAiNode("aiCCD1Degree", node, ioModule);
            _aiCCD2Degree = ParseAiNode("aiCCD2Degree", node, ioModule);

            _aoHomeOffset = ParseAoNode("aoHomeOffset", node, ioModule);
            _aoJogDegree = ParseAoNode("aoJogDegree", node, ioModule);
            _aoCCD1Degree = ParseAoNode("aoCCD1Degree", node, ioModule);
            _aoHomeSpeed = ParseAoNode("aoHomeSpeed", node, ioModule);
            _aoPosSpeed = ParseAoNode("aoPosSpeed", node, ioModule);
            _aoCCD2Degree = ParseAoNode("aoCCD2Degree", node, ioModule);

            _scHomeOffset = ParseScNode("HomeOffset", node, "LoadLock", "LoadLock.LoadRotation.HomeOffset");
            _scHomeSpeed = ParseScNode("MoveHomeSpeed", node, "LoadLock", "LoadLock.LoadRotation.MoveHomeSpeed");
            _scPosSpeed = ParseScNode("MoveHomeSpeed", node, "LoadLock", "LoadLock.LoadRotation.MovePosSpeed");
            _scCCD1Pos = ParseScNode("CCD1Pos", node, "LoadLock", "LoadLock.LoadRotation.CCD1Pos");
            _scCCD2Pos = ParseScNode("CCD2Pos", node, "LoadLock", "LoadLock.LoadRotation.CCD2Pos");
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.IsServoOn", () => IsServoOn);
            DATA.Subscribe($"{Module}.{Name}.IsServoBusy", () => IsServoBusy);
            DATA.Subscribe($"{Module}.{Name}.IsServoError", () => IsServoError);
            DATA.Subscribe($"{Module}.{Name}.IsMoveDone", () => IsMoveDone);
            DATA.Subscribe($"{Module}.{Name}.CurPos", () => CurPos);
            DATA.Subscribe($"{Module}.{Name}.CCD1Degree", () => CCD1Degree);
            DATA.Subscribe($"{Module}.{Name}.CCD2Degree", () => CCD2Degree);

            OP.Subscribe($"{Module}.{Name}.ServoOn", (function, args) =>
            {
                bool ret = ServoOn(out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.ServoReset", (function, args) =>
            {
                bool ret = ServoReset(out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.HomeOffset", (function, args) =>
            {
                bool ret = HomeOffset((float)_scHomeOffset.DoubleValue, out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.JogCW", (function, args) =>
            {
                bool ret = JogCW((float)args[0],out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.JogCCW", (function, args) =>
            {
                bool ret = JogCCW((float)args[0], out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Stop", (function, args) =>
            {
                bool ret = Stop(out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.MoveRelativeHome", (function, args) =>
            {
                bool ret = MoveRelativeHome(out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.MoveOneCircle", (function, args) =>
            {
                bool ret = MoveOneCircle(out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.MoveCCD1Pos", (function, args) =>
            {
                bool ret = MoveCCD1Pos(out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.MoveCCD2Pos", (function, args) =>
            {
                bool ret = MoveCCD2Pos(out string reason);
                if (!ret)
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                return true;
            });

            return true;
        }

        public bool ServoOn(out string reason)
        {
            if (!_doServoOn.Check(!_diServoOn.Value, out reason))
                return false;

            if (!_doServoOn.SetValue(!_diServoOn.Value, out reason))
                return false;

            _timer.Start(200);

            return true;
        }

        public bool ServoReset(out string reason)
        {
            if (!_doServoReset.Check(true, out reason))
                return false;

            if (!_doServoReset.SetValue(true, out reason))
                return false;

            _timer.Start(200);

            return true;
        }

        public bool HomeOffset(float relativeDis, out string reason)
        {
            if (_diServoBusy.Value || !_diServoOn.Value)
            {
                reason = "Load Rotation Busy or not ServoOn";
                return false;
            }

            //传进来为相对值
            _aoJogDegree.FloatValue = relativeDis;
            _aoPosSpeed.FloatValue = (float)_scPosSpeed.DoubleValue;

            if (!_doJogCW.Check(true, out reason))
            {
                return false;
            }

            if (!_doJogCW.SetValue(true, out reason))
            {
                return false;
            }

            _timer.Start(200);

            return true;
        }

        public bool JogCW(float relativeDis, out string reason)
        {
            if (_diServoBusy.Value || !_diServoOn.Value)
            {
                reason = "Load Rotation Busy or not ServoOn";
                return false;
            }

            //传进来为相对值
            _aoJogDegree.FloatValue = relativeDis;
            _aoPosSpeed.FloatValue = (float)_scPosSpeed.DoubleValue;

            if (!_doJogCW.Check(true, out reason))
            {
                return false;
            }
               
            if (!_doJogCW.SetValue(true, out reason))
            {
                return false;
            }

            _timer.Start(200);

            return true;
        }

        public bool JogCCW(float relativeDis, out string reason)
        {
            if (_diServoBusy.Value || !_diServoOn.Value)
            {
                reason = "Load Rotation Busy or not ServoOn";
                return false;
            }

            //传进来为相对值
            _aoJogDegree.FloatValue = relativeDis;
            _aoPosSpeed.FloatValue = (float)_scPosSpeed.DoubleValue;

            if (!_doJogCCW.Check(true, out reason))
            {
                return false;
            }

            if (!_doJogCCW.SetValue(true, out reason))
            {
                return false;
            }

            _timer.Start(200);

            return true;
        }

        public bool Stop(out string reason)
        {
            if (!_doStop.SetValue(true, out reason))
                return false;

            _timer.Start(200);

            return true;
        }

        public bool MoveRelativeHome(out string reason)
        {
            if (_diServoBusy.Value || !_diServoOn.Value)
            {
                reason = "Load Rotation Busy or not ServoOn";
                return false;
            }

            if (!_doMoveRelativeHome.Check(true, out reason))
            {
                return false;
            }

            _aoHomeSpeed.FloatValue = (float)_scHomeSpeed.DoubleValue;
            _aoHomeOffset.FloatValue = (float)_scHomeOffset.DoubleValue;

            if (!_doMoveRelativeHome.SetValue(true, out reason))
            {
                return false;
            }

            _timer.Start(200);

            return true;
        }

        public bool MoveOneCircle(out string reason)
        {
            if (_diServoBusy.Value || !_diServoOn.Value)
            {
                reason = "Load Rotation Busy or not ServoOn";
                return false;
            }

            if (!_doMoveOneCircle.Check(true, out reason))
            {
                return false;
            }

            _aoPosSpeed.FloatValue = (float)_scPosSpeed.DoubleValue;

            if (!_doMoveOneCircle.SetValue(true, out reason))
            {
                return false;
            }

            _timer.Start(200);

            return true;
        }

        public bool MoveCCD1Pos(out string reason)
        {
            if (_diServoBusy.Value || !_diServoOn.Value)
            {
                reason = "Load Rotation Busy or not ServoOn";
                return false;
            }

            //将参数写入到对应AO中
            _aoPosSpeed.FloatValue = (float)_scPosSpeed.DoubleValue;
            _aoCCD1Degree.FloatValue = (float)_scCCD1Pos.DoubleValue;

            if (!_doMoveCCD1Pos.Check(true, out reason))
            {
                return false;
            }

            if (!_doMoveCCD1Pos.SetValue(true, out reason))
            {
                return false;
            }

            _timer.Start(200);

            return true;
        }

        public bool MoveCCD2Pos(out string reason)
        {
            if (_diServoBusy.Value || !_diServoOn.Value)
            {
                reason = "Load Rotation Busy or not ServoOn";
                return false;
            }

            if (!_doMoveCCD2Pos.Check(true, out reason))
            {
                return false;
            }

            //将参数写入到对应AO中
            _aoPosSpeed.FloatValue = (float)_scPosSpeed.DoubleValue;
            _aoCCD2Degree.FloatValue = (float)_scCCD2Pos.DoubleValue;

            if (!_doMoveCCD2Pos.SetValue(true, out reason))
            {
                return false;
            }

            _timer.Start(200);

            return true;
        }

        public void Monitor()
        {
            if (_timer.IsTimeout())
            {
                _doServoReset.Value = false;
                _doJogCW.Value = false;
                _doJogCCW.Value = false;
                _doStop.Value = false;
                _doMoveRelativeHome.Value = false;
                _doMoveOneCircle.Value = false;
                _doMoveCCD1Pos.Value = false;
                _doMoveCCD2Pos.Value = false;

                _timer.Stop();
            }

            if(_diServoError.Value)
            {
                _doServoOn.SetValue(false, out _);
            }
        }

        public void Reset()
        {
        }

        public void Terminate()
        {
        }
    }
}
