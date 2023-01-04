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
    public enum ChamberMoveBodyStateEnum
    {
        Normal,
        Error,
    }
    public partial  class IoChamberMoveBody : BaseDevice, IDevice
    {
        public ChamberMoveBodyStateEnum State
        {
            get
            {
                if ((_diUpFaceback.Value  && _diDownFaceback.Value)|| (_diFront.Value && _diEndFaceback.Value))
                    return ChamberMoveBodyStateEnum.Error;
                return ChamberMoveBodyStateEnum.Normal;
            }
        }

        private DIAccessor _diUpFaceback = null;
        private DIAccessor _diDownFaceback = null;
        private DIAccessor _diFront = null;
        private DIAccessor _diEndFaceback = null;
        private DIAccessor _diForwardLatch = null;
        private DIAccessor _diBackwardLatch = null;
        private DIAccessor _diLock = null;
        private DIAccessor _diRemote = null;
        private DIAccessor _diUpDownEnable = null;

        private DOAccessor _doUpSetpoint = null;
        private DOAccessor _doDownSetpoint = null;
        private DOAccessor _doUpLatch = null;
        private DOAccessor _doForward = null;
        private DOAccessor _doBackward = null;
        private DOAccessor _doForwardLatch = null;
        private DOAccessor _doBackwardLatch = null;
        private DOAccessor _doUpDownEnable = null;

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _RemoteToLocal = new R_TRIG();
        private R_TRIG _LocalToRemote = new R_TRIG();
        private R_TRIG _EnableReset = new R_TRIG();     //UpDownEnable 使能
        private R_TRIG _DisbaleReset = new R_TRIG();  //UpDownEnable 禁止

        private R_TRIG _upTrig = new R_TRIG();
        private R_TRIG _downTrig = new R_TRIG();
        private R_TRIG _forwardTrig = new R_TRIG();
        private R_TRIG _backwardTrig = new R_TRIG();

        public Func<bool> FuncCheckSwingUnlock;
        public Func<bool, bool> FuncUpDownEnable;

        enum DeviceState
        {
            Idle,
            MovingUp,
            MovingDown,
            MovingUpLatch,
            MovingFront,
            MovingEnd,
            UpDownBrake,
            FrontBackBrake,
            Error,

            AutoUpAfter,
            AutoDownPre,
        }
        private DeviceState _state = DeviceState.Idle;
        private DeviceTimer _timer = new DeviceTimer();

        private SCConfigItem _scUpTimeout,_scDownTimeout, _scUpLatchTimeout, _scForwardTimeout, _scBackwardTimeout,_scByPassInterLock;

        public bool IsUp { get { return _diUpFaceback.Value && !_diDownFaceback.Value ; } }
        public bool IsDown { get { return !_diUpFaceback.Value && _diDownFaceback.Value; } }
        public bool IsFront { get { return  _diFront.Value && !_diEndFaceback.Value; } }
        public bool IsEnd { get { return !_diFront.Value && _diEndFaceback.Value; } }

        #region DI
        public bool UpDownEnableFaceback
        {
            get
            {
                if (_diUpDownEnable != null)
                    return _diUpDownEnable.Value;

                return false;
            }
        }


        public bool RemoteModelFeedback
        {
            get
            {
                if (_diRemote != null)
                    return _diRemote.Value;

                return false;
            }
        }

        public bool IsLockedFeedback
        {
            get
            {
                if (_diLock != null)
                    return _diLock.Value;

                return false;
            }
        }

        public bool UpFaceback
        {
            get 
            {
                if (_diUpFaceback != null)
                    return _diUpFaceback.Value;

                return false;
            }
        }

        public bool BackwardLatchFaceback
        {
            get
            {
                if (_diBackwardLatch != null)
                    return _diBackwardLatch.Value;

                return false;
            }
        }

        public bool ForwardLatchFaceback
        {
            get
            {
                if (_diForwardLatch != null)
                    return _diForwardLatch.Value;

                return false;
            }
        }

        public bool UpLatchFeceback
        {
            get
            {
                if (_doUpLatch != null)
                    return _doUpLatch.Value;
                return false;
            }
        }

        public bool DownFaceback
        {
            get
            {
                if (_diDownFaceback != null)
                    return _diDownFaceback.Value;

                return false;
            }
        }

        public bool FrontFaceback
        {
            get
            {
                if (_diFront != null)
                    return _diFront.Value;

                return false;
            }
        }

        public bool EndFaceback
        {
            get
            {
                if (_diEndFaceback != null)
                    return _diEndFaceback.Value;

                return false;
            }
        }
        public bool LockFaceback
        {
            get
            {
                if (_diLock != null)
                    return _diLock.Value;

                return false;
            }
        }

        public bool GasConnectIsLoosen
        {
            get
            {
                return DATA.Poll($"{Module}.GasConnector.GasConnectorLoosenFeedback") == null ? false : Convert.ToBoolean(DATA.Poll($"{Module}.GasConnector.GasConnectorLoosenFeedback").ToString());
            }
        }

        #endregion

        #region DO
        public bool UpSetpoint
        {
            get
            {
                if (_doUpSetpoint != null)
                    return _doUpSetpoint.Value;

                return false;
            }
        }

        public bool DownSetpoint
        {
            get
            {
                if (_doDownSetpoint != null)
                    return _doDownSetpoint.Value;

                return false;
            }
        }

        public bool UpBrakeSetPoint
        {
            get
            {
                if (_doUpLatch != null)
                    return _doUpLatch.Value;

                return false;
            }
        }

        public bool ForwardSetPoint
        {
            get
            {
                if (_doForward != null)
                    return _doForward.Value;

                return false;
            }
        }

        public bool BackwardSetPoint
        {
            get
            {
                if (_doBackward != null)
                    return _doBackward.Value;

                return false;
            }
        }

        #endregion

        //public AITDeviceData DeviceData
        //{
        //    get
        //    {
        //        AITDeviceData data = new AITDeviceData()
        //        {
        //            Module = Module,
        //            DeviceName = Name,
        //            DisplayName = Display,
        //            DeviceSchematicId = DeviceID,
        //            UniqueName = UniqueName,

        //        };
        //        data.AttrValue["UpFaceback"] = _diUpFaceback.Value;
        //        data.AttrValue["DownFaceback"] = _diDownFaceback.Value;
        //        data.AttrValue["FrontFaceback"] = _diFront.Value;
        //        data.AttrValue["EndFaceback"] = _diEndFaceback.Value;
        //        data.AttrValue["UpSetpoint"] = _doUpSetpoint.Value;
        //        data.AttrValue["DownSetpoint"] = _doDownSetpoint.Value;
        //        data.AttrValue["UpBrakeSetpoint"] = _doUpBrake.Value;
        //        data.AttrValue["ForwardSetpoint"] = _doForward.Value;
        //        data.AttrValue["BackwardSetpoint"] = _doBackward.Value;
        //        //data.AttrValue["PV"] = _aiFeedBack.Value;
        //        return data;
        //    }
        //}

        public IoChamberMoveBody(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");
        
            _diUpFaceback = ParseDiNode("diUpFaceback", node, ioModule);
            _diDownFaceback = ParseDiNode("diDownFaceback", node, ioModule);
            _diFront = ParseDiNode("diFront", node, ioModule);
            _diEndFaceback = ParseDiNode("diEndFaceback", node, ioModule);
            _diForwardLatch = ParseDiNode("diForwardLatch", node, ioModule);
            _diBackwardLatch = ParseDiNode("diBackwardLatch", node, ioModule);
            _diRemote = ParseDiNode("diRemote", node, ioModule);
            _diLock = ParseDiNode("diLock", node, ioModule);
            _diUpDownEnable = ParseDiNode("diUpDownEnable", node, ioModule);


            _doUpSetpoint = ParseDoNode("doUpSetpoint", node, ioModule);
            _doDownSetpoint = ParseDoNode("doDownSetpoint", node, ioModule);
            _doUpLatch = ParseDoNode("doUpBrake", node, ioModule);
            _doForward = ParseDoNode("doForward", node, ioModule);
            _doBackward = ParseDoNode("doBackward", node, ioModule);
            _doForwardLatch = ParseDoNode("doForwardLatch", node, ioModule);
            _doBackwardLatch = ParseDoNode("doBackwardLatch", node, ioModule);
            _doUpDownEnable = ParseDoNode("doUpDownEnable", node, ioModule);

            _scUpTimeout = ParseScNode("LidTimeout", node, "PM", "PM.LidMotionTimeout");
            _scDownTimeout = ParseScNode("ChamberMoveBodyDownTimeOut", node, "PM", "PM.LidMotionTimeout");
            _scUpLatchTimeout = ParseScNode("ChamberMoveBodyUpLatchTimeOut", node, "PM", "PM.LidMotionTimeout");
            _scForwardTimeout = ParseScNode("ChamberMoveBodyForwardTimeOut", node, "PM", "PM.LidMotionTimeout");
            _scBackwardTimeout = ParseScNode("ChamberMoveBodyBackwardTimeOut", node, "PM", "PM.LidMotionTimeout");
            _scByPassInterLock= ParseScNode("BypassInterlock", node, "PM", "System.BypassInterlock");

        }
        public bool Initialize()
        {
            //DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.IsRemoteFeceback", () => RemoteModelFeedback);
            DATA.Subscribe($"{Module}.{Name}.IsLockedFeceback", () => IsLockedFeedback);
            DATA.Subscribe($"{Module}.{Name}.UpDownEnableFaceback", () => UpDownEnableFaceback);

            DATA.Subscribe($"{Module}.{Name}.UpFaceback", () => UpFaceback);
            DATA.Subscribe($"{Module}.{Name}.DownFaceback", () => DownFaceback);
            DATA.Subscribe($"{Module}.{Name}.FrontFaceback", () => FrontFaceback);
            DATA.Subscribe($"{Module}.{Name}.EndFaceback", () => EndFaceback);
            DATA.Subscribe($"{Module}.{Name}.BackwardLatchFaceback", () => BackwardLatchFaceback);
            DATA.Subscribe($"{Module}.{Name}.ForwardLatchFaceback", () => ForwardLatchFaceback);
            DATA.Subscribe($"{Module}.{Name}.UpLatchFaceback", () => UpLatchFeceback); 

            DATA.Subscribe($"{Module}.{Name}.UpSetpoint", () => UpSetpoint);
            DATA.Subscribe($"{Module}.{Name}.DownSetpoint", () => DownSetpoint);
            DATA.Subscribe($"{Module}.{Name}.UpBrakeSetpoint", () => UpBrakeSetPoint);
            DATA.Subscribe($"{Module}.{Name}.ForwardSetpoint", () => ForwardSetPoint);
            DATA.Subscribe($"{Module}.{Name}.BackwardSetpoint", () => BackwardSetPoint);

            OP.Subscribe($"{Module}.{Name}.SetUpSetpoint", (function, args) =>
            {
                MoveUpDown(true,out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetDownSetpoint", (function, args) =>
            {
                MoveUpDown(false,out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetUpLatch", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetUpLatch(isTrue, out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetForward", (function, args) =>
            {
                MoveForwardEnd(false,out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetBackward", (function, args) =>
            {
                MoveForwardEnd(true,out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetBackwardLatch", (function, args) =>
            {
                SetFWBWLatch(false, out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetForwardLatch", (function, args) =>
            {
                SetFWBWLatch(true, out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetUpDownEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetUpDownEnable(isTrue, out reason);
                return true;
            });
            return true;
        }
        string reason = string.Empty;

        /// <summary>
        /// 上下移动
        /// </summary>
        /// <param name="isup"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool MoveUpDown(bool isup,out string reason)
        {
            if (!BackwardLatchFaceback)
            {
                reason = "Can not move chamber up/down while DI_ChamMoveBodyEndLatchBW is false!";
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!FrontFaceback)
            {
                reason = "Can not move chamber up/down while DI_ChamMoveBodyFront is false!";
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!UpLatchFeceback)
            {
                reason = "Can not move chamber up/down while UpLatch is false!";
                EV.PostWarningLog(Module, reason);
                return false;
            }

            if (!_doDownSetpoint.Check(!isup, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doUpSetpoint.Check(isup, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            if (!_doDownSetpoint.SetValue(!isup, out reason))
                return false;
            if (!_doUpSetpoint.SetValue(isup, out reason))
                return false;

            _timer.Start(_scUpTimeout.IntValue * 1000);

            _state = IsUp ? DeviceState.MovingUp : DeviceState.MovingDown;
            return true;
        }

        /// <summary>
        /// 在高位时持续给Up DO信号
        /// </summary>
        /// <param name="setValue"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool SetUpDo(bool setValue)
        {
            reason = "";
            if (UpFaceback && FrontFaceback)
            {
                if (!_doUpSetpoint.Check(setValue, out reason))
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                if (!_doUpSetpoint.SetValue(setValue, out reason))
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }

                return true;
            }
            return false;
        }

        public bool SetDownDo(bool setValue)
        {
            reason = "";
            if (DownFaceback && FrontFaceback)
            {
                if (!_doDownSetpoint.Check(setValue, out reason))
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                if (!_doDownSetpoint.SetValue(setValue, out reason))
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }

                return true;
            }
            return false;
        }

        public bool SetForwardDo(bool setValue)
        {
            reason = "";
            if (UpFaceback && FrontFaceback)
            {
                if (!_doForward.Check(setValue, out reason))
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                if (!_doForward.SetValue(setValue, out reason))
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }

                return true;
            }
            return false;
        }

        public bool SetBackwardDo(bool setValue)
        {
            reason = "";
            if (UpFaceback && EndFaceback)
            {
                if (!_doBackward.Check(setValue, out reason))
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                if (!_doBackward.SetValue(setValue, out reason))
                {
                    EV.PostWarningLog(Module, reason);
                    return false;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// 气锁
        /// </summary>
        /// <param name="setValue"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool SetUpLatch(bool setValue,out string reason)
        {
            reason = "";

            if (!_doUpLatch.Check(setValue, out reason))
                return false;
            if (!_doUpLatch.SetValue(setValue, out reason))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 插销
        /// </summary>
        /// <param name="fwTrue">为True则为锁住ForwardLatch</param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool SetFWBWLatch(bool fwTrue, out string reason)
        {
            reason = "";

            if (ForwardLatchFaceback == fwTrue)
            {
                return true;
            }

            if (!_doForwardLatch.Check(fwTrue, out reason) || !_doBackwardLatch.Check(!fwTrue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doForwardLatch.SetValue(fwTrue, out reason) || !_doBackwardLatch.SetValue(!fwTrue, out reason))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 前后移动
        /// </summary>
        /// <param name="toEnd"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool MoveForwardEnd(bool toEnd,out string reason)
        {
            if (!UpFaceback)
            {
                reason = "";
                EV.PostWarningLog(Module, "Can not set to forward/end while chamber is not Front!");
                return false;
            }

            if ((FrontFaceback && !toEnd) || (EndFaceback && toEnd))
            {
                reason = "";
                return true;
            }

            if (!_doForward.Check(!toEnd, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doBackward.Check(toEnd, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            if (!_doForward.SetValue(!toEnd, out reason))
                return false;
            if (!_doBackward.SetValue(toEnd, out reason))
                return false;

            _timer.Start(_scDownTimeout.IntValue * 1000);

            _state = toEnd ? DeviceState.MovingEnd : DeviceState.MovingFront;

            return true;
        }
       

        public bool SetUpDownEnable(bool eValue,out string reason)
        {
            string sEnable = " Enable ";
            if(eValue)
            {

            }
            else
            {
                sEnable = " Disable ";
            }
            //
            reason = "";

            if (UpDownEnableFaceback == eValue)
            {
                return true;
            }

            if (FuncUpDownEnable != null && !_scByPassInterLock.BoolValue)
            {
                if (!FuncUpDownEnable(eValue))
                {
                    EV.PostWarningLog(Module, $"Set UpDown "+sEnable+" failed for check condition!");
                    return false;
                }
            }

            if (!_doUpDownEnable.Check(eValue, out reason) )
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doUpDownEnable.SetValue(eValue, out reason))
            {
                EV.PostWarningLog(Module, $"Set UpDown "+sEnable+" failed at last!");
                return false;
            }

            return true;
        }

        //public bool ForwardBackBrake()
        //{
        //    if (!_doForward.Check(false, out reason))
        //    {
        //        EV.PostWarningLog(Module, reason);
        //        return false;
        //    }
        //    if (!_doBackward.Check(false, out reason))
        //    {
        //        EV.PostWarningLog(Module, reason);
        //        return false;
        //    }

        //    if (!_doForward.SetValue(false, out reason))
        //        return false;
        //    if (!_doBackward.SetValue(false, out reason))
        //        return false;

        //    _timer.Start(_scUpLatchTimeout.IntValue * 1000);

        //    _state = DeviceState.FrontBackBrake;

        //    return true;
        //}

        public void Monitor()
        {
            switch (_state)
            {
                case DeviceState.MovingUp:
                    if (IsUp)
                    {
                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (!_doUpSetpoint.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        EV.PostWarningLog(Module, $"{Module} {Name} Can not move up in {_scUpTimeout.IntValue} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                case DeviceState.MovingDown:
                    if (IsDown)
                    {
                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (!_doDownSetpoint.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        EV.PostWarningLog(Module, $"{Module} {Name} Can not move down in {_scDownTimeout.IntValue} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                case DeviceState.MovingFront:
                    if (IsFront)
                    {
                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (!_doForward.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        EV.PostWarningLog(Module, $"{Module} {Name} Can not move up in {_scForwardTimeout.IntValue} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                case DeviceState.MovingEnd:
                    if (IsEnd)
                    {
                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (!_doBackward.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        EV.PostWarningLog(Module, $"{Module} {Name} Can not move up in {_scBackwardTimeout.IntValue} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                case DeviceState.FrontBackBrake:
                    if (!BackwardLatchFaceback && !ForwardLatchFaceback)
                    {
                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (_diBackwardLatch.Value)
                        {
                            EV.PostWarningLog(Module, $"{Module} {Name} Can not Set BackwardLatch to false in {_scUpLatchTimeout.IntValue} seconds");
                        }
                        else if(_diForwardLatch.Value)
                        {
                            EV.PostWarningLog(Module, $"{Module} {Name} Can not Set ForwardLatch to false in {_scUpLatchTimeout.IntValue} seconds");
                        }
                        _state = DeviceState.Error;
                    }
                    break;
                case DeviceState.UpDownBrake:
                    if (!DownFaceback && !UpFaceback)
                    {
                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (_diDownFaceback.Value)
                        {
                            EV.PostWarningLog(Module, $"{Module} {Name} Can not Set ChamMoveBodyDown to false in {_scUpLatchTimeout.IntValue} seconds");
                        }
                        else if (_diUpFaceback.Value)
                        {
                            EV.PostWarningLog(Module, $"{Module} {Name} Can not Set ChamMoveBodyUp to false in {_scUpLatchTimeout.IntValue} seconds");
                        }
                        _state = DeviceState.Error;
                    }
                    break;

                case DeviceState.Idle:

                    break;
                default:
                    break;
            }

            //在高位时，点lock键，自动插销，气锁关闭，DO UP持续给1
            if (UpFaceback && FrontFaceback && !RemoteModelFeedback)
            {
                _RemoteToLocal.CLK = LockFaceback;
                if (_RemoteToLocal.Q)
                {
                    _LocalToRemote.RST = true;

                    SetUpDo(true); //持续给1
                    SetUpLatch(false, out reason); //气锁锁住
                    SetFWBWLatch(true, out  reason);
                }

                _LocalToRemote.CLK = !LockFaceback;
                if (_LocalToRemote.Q)
                {
                    _RemoteToLocal.RST = true;

                    //SetUpDo(false); //清除0
                    SetUpLatch(true, out string reason); //气锁解锁
                    SetFWBWLatch(false, out reason);
                }
            }

            //在低位时,有一层Swing松开，点Local或Remote,自动给UpDownEnable使能或禁止
            if ((FuncCheckSwingUnlock != null  && FuncCheckSwingUnlock() || _scByPassInterLock.BoolValue || FuncCheckSwingUnlock == null) && DownFaceback && !LockFaceback )
            {
                _EnableReset.CLK = RemoteModelFeedback;
                if (_EnableReset.Q)
                {
                    _DisbaleReset.RST = true;

                    SetUpDownEnable(false, out string reason);
                    SetUpLatch(false, out reason); //气锁锁住
                }

                _DisbaleReset.CLK = !RemoteModelFeedback;
                if (_DisbaleReset.Q)
                {
                    _EnableReset.RST = true;

                    SetUpDownEnable(true, out string reason);
                    SetUpLatch(true, out reason); //气锁锁住
                }
            }

            _forwardTrig.CLK = _diForwardLatch.Value && GasConnectIsLoosen;
            if (_forwardTrig.Q)
            {
                SetForwardDo(true);
            }
            _backwardTrig.CLK = _diBackwardLatch.Value && GasConnectIsLoosen;
            if (_backwardTrig.Q)
            {
                SetBackwardDo(true);
            }
            _upTrig.CLK = _diUpFaceback.Value && GasConnectIsLoosen;
            if (_upTrig.Q)
            {
                SetUpDo(true);
            }
            _downTrig.CLK = _diDownFaceback.Value && GasConnectIsLoosen;
            if (_downTrig.Q)
            {
                SetDownDo(true);
            }
        }

        public void Reset()
        {
            _trigError.RST = true;
        }

        public void Terminate()
        {
            
        }
    }
}
