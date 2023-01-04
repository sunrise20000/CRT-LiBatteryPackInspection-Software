using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;

namespace Aitex.Core.RT.Device.Unit
{
    /// <summary>
    /// 
    /// </summary>
    public class IoTurnOver : BaseDevice, IDevice
    {
        private readonly DIAccessor _di0Degree;
        private readonly DIAccessor _di180Degree;

        private readonly DIAccessor _diAlarm;
        private readonly DIAccessor _diBusy;
        private readonly DIAccessor _diGrip;
        private readonly DIAccessor _diOrigin;

        private readonly DIAccessor _diPlacement;
        private readonly DIAccessor _diPressureError;
        private readonly DIAccessor _diUngrip;

        private readonly DOAccessor _doGrip;
        private readonly DOAccessor _doM0;
        private readonly DOAccessor _doM1;

        private readonly DOAccessor _doOrigin;
        private readonly DOAccessor _doResetError;
        private readonly DOAccessor _doStop;
        private readonly DOAccessor _doUngrip;

        //private bool _cmdLoop;

        //private int _interval;
        private DeviceTimer _loopTimeout = new DeviceTimer();
        private readonly DeviceTimer _loopTimer = new DeviceTimer();

        private readonly SCConfigItem _scLoopInterval;
        //private SCConfigItem _scLoopTimeout;

        private TurnOverState _state = TurnOverState.Idle;
        public TurnOverState State => _state;
        

        private R_TRIG _trigCloseError = new R_TRIG();
        private R_TRIG _trigOpenError = new R_TRIG();
        private R_TRIG _trigReset = new R_TRIG();
        private R_TRIG _trigSetPointDone = new R_TRIG();
        
        public const string EventWaferTurnOverStart = "WAFER_TURN_OVER_START";
        public const string EventWaferTurnOverEnd = "WAFER_TURN_OVER_COMPLETE";

        // DI_TurnOverPlacement
        // DI_TurnOverGrip
        // DI_TurnOverUngrip
        // DI_TurnOverBusy
        // DI_TurnOverPressureError
        //
        // DI_TurnOverAlarm
        // DI_TurnOverOrigin
        // DI_TurnOver0Degree
        // DI_TurnOver180Degree
        //
        // DO_TurnOverGrip
        // DO_TurnOverUngrip
        // DO_TurnOverM0
        // DO_TurnOverM1
        // DO_TurnOverStop
        //
        // DO_TurnOverOrigin
        // DO_TurnOverResetError
        public IoTurnOver(string module, XmlElement node, string ioModule = "")
        {
            Module = node.GetAttribute("module");
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");

            _diPlacement = ParseDiNode("DI_TurnOverPlacement", node, ioModule);
            _diGrip = ParseDiNode("DI_TurnOverGrip", node, ioModule);
            _diUngrip = ParseDiNode("DI_TurnOverUngrip", node, ioModule);
            _diBusy = ParseDiNode("DI_TurnOverBusy", node, ioModule);
            _diPressureError = ParseDiNode("DI_TurnOverPressureError", node, ioModule);

            _diAlarm = ParseDiNode("DI_TurnOverAlarm", node, ioModule);
            _diOrigin = ParseDiNode("DI_TurnOverOrigin", node, ioModule);
            _di0Degree = ParseDiNode("DI_TurnOver0Degree", node, ioModule);
            _di180Degree = ParseDiNode("DI_TurnOver180Degree", node, ioModule);

            _doGrip = ParseDoNode("DO_TurnOverGrip", node, ioModule);
            _doUngrip = ParseDoNode("DO_TurnOverUngrip", node, ioModule);
            _doM0 = ParseDoNode("DO_TurnOverM0", node, ioModule);
            _doM1 = ParseDoNode("DO_TurnOverM1", node, ioModule);
            _doStop = ParseDoNode("DO_TurnOverStop", node, ioModule);

            _doOrigin = ParseDoNode("DO_TurnOverOrigin", node, ioModule);
            _doResetError = ParseDoNode("DO_TurnOverResetError", node, ioModule);

            _scLoopInterval = SC.GetConfigItem("Turnover.IntervalTimeLimit");
        }

       
        public int TimelimitAction
        {
            get
            {
                if (SC.ContainsItem($"Turnover.TimeLimitTurnoverAction"))
                    return SC.GetValue<int>($"Turnover.TimeLimitTurnoverAction");
                return 60;
            }
        }

        public bool IsHomed => _diOrigin.Value;
        public bool IsPlacement => !_diPlacement.Value;
        public bool IsAlarm => !_diAlarm.Value;
        public bool IsPressureError => _diPressureError ==null? false: !_diPressureError.Value;

        public bool IsBusy => _diBusy.Value || _state != TurnOverState.Idle;
        
        public bool IsIdle => !_diBusy.Value;
        public bool IsGrip => _diGrip.Value;
        public bool IsUnGrip => _diUngrip.Value;
        public bool Is0Degree => _di0Degree.Value;
        public bool Is180Degree => _di180Degree.Value;

        public bool IsEnableWaferTransfer => Is0Degree && !IsBusy && IsUnGrip && !IsAlarm &&
                                             IsPlacement;
        
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.State", () => _state.ToString());
            DATA.Subscribe($"{Module}.{Name}.IsHomed", () => IsHomed);
            DATA.Subscribe($"{Module}.{Name}.IsBusy", () => IsBusy);
            DATA.Subscribe($"{Module}.{Name}.IsGrip", () => IsGrip);
            DATA.Subscribe($"{Module}.{Name}.IsUnGrip", () => IsUnGrip);
            DATA.Subscribe($"{Module}.{Name}.Is0Degree", () => Is0Degree);
            DATA.Subscribe($"{Module}.{Name}.Is180Degree", () => Is180Degree);
            DATA.Subscribe($"{Module}.{Name}.IsPlacement", () => IsPlacement);
            DATA.Subscribe($"{Module}.{Name}.IsAlarm", () => IsAlarm);
            DATA.Subscribe($"{Module}.{Name}.IsPressureError", () => IsPressureError);

            DATA.Subscribe($"{Module}.{Name}.GripCmd", () => _doGrip.Value);
            DATA.Subscribe($"{Module}.{Name}.TurnTo0Cmd", () => _doM0.Value);
            DATA.Subscribe($"{Module}.{Name}.TurnTo180Cmd", () => _doM1.Value);
            DATA.Subscribe($"{Module}.{Name}.HomeCmd", () => _doOrigin.Value);
            DATA.Subscribe($"{Module}.{Name}.ResetCmd", () => _doResetError.Value);
            DATA.Subscribe($"{Module}.{Name}.StopCmd", () => _doStop==null?false: _doStop.Value);
            DATA.Subscribe($"{Module}.{Name}.UnGripCmd", () => _doUngrip.Value);






            EV.Subscribe(new EventItem("Event", EventWaferTurnOverStart, "Start Turn Over"));
            EV.Subscribe(new EventItem("Event", EventWaferTurnOverEnd, "Turn Over End"));
            
            //System.TurnoverStation.
            OP.Subscribe($"{Module}.{Name}.Home", (cmd, param) =>
            {
                if (!Home(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not home, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Grip", (cmd, param) =>
            {
                if (!Grip(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not grip, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Ungrip", (cmd, param) =>
            {
                if (!UnGrip(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not ungrip, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.TurnTo0", (cmd, param) =>
            {
                if (!TurnTo0(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not turn to 0 degree, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.TurnTo180", (cmd, param) =>
            {
                if (!TurnTo180(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not turn to 180 degree, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Stop", (cmd, param) =>
            {
                if (!Stop(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not turn to 180 degree, {reason}");
                    return false;
                }

                return true;
            });

            return true;
        }


        public void Terminate()
        {
        }


        public void Monitor()
        {
            if (IsAlarm || IsPressureError)
            {
                _state = TurnOverState.Error;
                _doGrip.SetValue(false, out _);
                _doM0.SetValue(false, out _);
                _doM1.SetValue(false, out _);
                _doOrigin.SetValue(false, out _);
                if(_doStop !=null)
                    _doStop.SetValue(false, out _);
                _doUngrip.SetValue(false, out _);
                return;
            }
            switch (_state)
            {
                case TurnOverState.OnHoming:
                    _doM0.SetValue(false, out _);
                    _doM1.SetValue(false, out _);
                    _doOrigin.SetValue(true, out _);
                    if (IsHomed)
                    {
                        if (!_doOrigin.SetValue(false, out var reason))
                            LOG.Error($"{Module} reset DO failed, {reason}");

                        _state = TurnOverState.Idle;
                    }
                    else if (_loopTimer.IsTimeout())
                    {
                        if (!_doOrigin.SetValue(false, out var reason))
                            LOG.Error($"{Module} reset DO failed, {reason}");

                        EV.PostAlarmLog(Module, $"{Module} {Name} Can not Home in {_scLoopInterval.IntValue} seconds");
                        _state = TurnOverState.Error;
                    }
                    break;
                case TurnOverState.OnGripping:
                    if (IsGrip)
                    {
                        //if (!_doGrip.SetValue(false, out var reason)) LOG.Error($"{Module} reset DO failed, {reason}");

                        _state = TurnOverState.Idle;
                    }
                    else if (_loopTimer.IsTimeout())
                    {
                        //if (!_doOrigin.SetValue(false, out var reason))
                        //    LOG.Error($"{Module} reset DO failed, {reason}");

                        EV.PostAlarmLog(Module, $"{Module} {Name} Can not Grip in {_scLoopInterval.IntValue} seconds");
                        _state = TurnOverState.Error;
                    }
                    break;
                case TurnOverState.OnUnGripping:
                    if (IsUnGrip)
                    {
                        //if (!_doUngrip.SetValue(false, out var reason))
                        //    LOG.Error($"{Module} reset DO failed, {reason}");

                        _state = TurnOverState.Idle;
                    }
                    else if (_loopTimer.IsTimeout())
                    {
                        //if (!_doUngrip.SetValue(false, out var reason))
                        //    LOG.Error($"{Module} reset DO failed, {reason}");

                        EV.PostAlarmLog(Module,
                            $"{Module} {Name} Can not UnGrip in {_scLoopInterval.IntValue} seconds");
                        _state = TurnOverState.Error;
                    }
                    break;
                case TurnOverState.OnTurningTo0:
                    if (_doM1.Value == true) _doM1.SetValue(false, out _);
                    if (_doM0.Value == false) _doM0.SetValue(true, out _);
                    if (Is0Degree && !_diBusy.Value)
                    {    if (!_doM0.SetValue(false, out var reason)) LOG.Error($"{Module} reset DO failed, {reason}");

                        _state = TurnOverState.Idle;
                    }
                    else if (_loopTimer.IsTimeout())
                    {
                        ////if (!_doM0.SetValue(false, out var reason))
                        //    LOG.Error($"{Module} reset DO failed, {reason}");

                        EV.PostAlarmLog(Module,
                            $"{Module} {Name} Can not Turn to 0 in {_scLoopInterval.IntValue} seconds");
                        _state = TurnOverState.Error;
                    }
                    break;
                case TurnOverState.OnTurningTo180:
                    if(_doM1.Value == false) _doM1.SetValue(true, out _);
                    if (_doM0.Value == true) _doM0.SetValue(false, out _);
                    if (Is180Degree &&!_diBusy.Value)
                    {
                        if (!_doM1.SetValue(false, out var reason)) LOG.Error($"{Module} reset DO failed, {reason}");
                        _state = TurnOverState.Idle;
                        var wafer = WaferManager.Instance.GetWafer(ModuleName.TurnOverStation, 0);
                        if (!wafer.IsEmpty)
                        {
                            var dvid = new SerializableDictionary<string, string>()
                            {
                                {"LOT_ID", wafer.LotId},
                                {"WAFER_ID", wafer.WaferID},
                                {"ARRIVE_POS_NAME", "TRN1"},
                            };

                            EV.Notify(EventWaferTurnOverEnd, dvid);
                        }
                        

                        
                    }
                    else if (_loopTimer.IsTimeout())
                    {
                        //if (!_doM1.SetValue(false, out var reason)) LOG.Error($"{Module} reset DO failed, {reason}");

                        EV.PostAlarmLog(Module,
                            $"{Module} {Name} Can not Turn to 180 in {_scLoopInterval.IntValue} seconds");
                        _state = TurnOverState.Error;
                    }
                    break;
                case TurnOverState.OnErrorCleaning:
                    if (!IsAlarm)
                    {
                        if (!_doResetError.SetValue(false, out var reason))
                            LOG.Error($"{Module} reset DO failed, {reason}");

                        _state = TurnOverState.Idle;
                    }
                    break;
                case TurnOverState.Stopping:
                    if (!IsBusy)
                    {
                        if (_doStop != null)
                        {
                            if (!_doStop.SetValue(false, out var reason))
                                LOG.Error($"{Module} reset DO failed, {reason}");
                        }
                        _state = TurnOverState.Stop;
                    }
                    break;
                default:
                    //if (!_diBusy.Value && !_doStop.Value) _state = DeviceState.Idle;
                    break;
            }


           
                
            
        }

        public void Reset()
        {
            _doGrip.SetValue(false, out _);
            _doM0.SetValue(false, out _);
            _doM1.SetValue(false, out _);
            _doOrigin.SetValue(false, out _);
            if(_doStop!=null)
                _doStop.SetValue(false, out _);
            _doUngrip.SetValue(false, out _);
            ResetError(out _);
            
            
        }

        public bool Home(out string reason)
        {
            reason = string.Empty;
            EV.PostInfoLog(Module, $"{Module}.{Name} Home");
            //if (IsPressureError)
            //{
            //    reason = "Turn over station pressure error";
            //    return false;
            //}
            _doM0.SetValue(false, out _);
            _doM1.SetValue(false, out _);
            _doOrigin.SetValue(true, out _);
            _state = TurnOverState.OnHoming;
            _loopTimer.Start(_scLoopInterval.IntValue * 1000);
            return true;
        }

        public bool Grip(out string reason)
        {
            reason = string.Empty;
            EV.PostInfoLog(Module, $"{Module}.{Name} Grip");
            if (IsPressureError)
            {
                reason = "Turn over station pressure error";
                EV.PostAlarmLog(Module, $"{Module}.{Name} pressure error.");
                return false;
                
            }

            //if (_doGrip.Value)
            //{
            //    reason = "Gripping, can't do again";
            //    EV.PostAlarmLog(Module, $"{Module}.{Name} {reason}.");
            //    return false;
            //}

            _doUngrip.SetValue(false, out _);
            _doGrip.SetValue(true, out _);
            _state = TurnOverState.OnGripping;
            _loopTimer.Start(_scLoopInterval.IntValue * 1000);
            return true;
        }

        public bool UnGrip(out string reason)
        {
            reason = string.Empty;
            EV.PostInfoLog(Module, $"{Module}.{Name} UnGrip");
            if (IsPressureError)
            {
                reason = "Turn over station pressure error";
                return false;
            }

            //if (_doUngrip.Value)
            //{
            //    reason = "UnGripping, can't do again";
            //    return false;
            //}
            _doGrip.SetValue(false, out _);
            _doUngrip.SetValue(true, out _);
            _state = TurnOverState.OnUnGripping;
            _loopTimer.Start(_scLoopInterval.IntValue * 1000);
            return true;
        }

        public bool TurnTo0(out string reason)
        {
            reason = string.Empty;
            EV.PostInfoLog(Module, $"{Module}.{Name} Turn to 0");
            //if (IsPressureError)
            //{
            //    reason = "Turn over station pressure error";
            //    return false;
            //}

            //if (_doM0.Value)
            //{
            //    reason = "Turning, can't do again";
            //    return false;
            //}
            _state = TurnOverState.OnTurningTo0;
            //if (_doM1.Value == true) _doM1.SetValue(false, out _);
            //if (_doM0.Value == false) _doM0.SetValue(true, out _);

            _loopTimer.Start(_scLoopInterval.IntValue * 1000);
            return true;
        }

        public bool TurnTo180(out string reason)
        {
            reason = string.Empty;
            EV.PostInfoLog(Module, $"{Module}.{Name} Turn to 180");
            //if (IsPressureError)
            //{
            //    reason = "Turn over station pressure error";
            //    return false;
            //}

            //if (_doM1.Value)
            //{
            //    reason = "Turning, can't do again";
            //    return false;
            //}
            _state = TurnOverState.OnTurningTo180;
            //if (_doM0.Value == true) _doM0.SetValue(false, out _);
            //if (_doM1.Value == false) _doM1.SetValue(true, out _);
            _loopTimer.Start(_scLoopInterval.IntValue * 1000);
            var wafer = WaferManager.Instance.GetWafer(ModuleName.TurnOverStation,0);
            if (!wafer.IsEmpty)
            {
                var dvid = new SerializableDictionary<string, string>()
                {
                    {"LOT_ID", wafer.LotId},
                    {"WAFER_ID", wafer.WaferID},
                    {"ARRIVE_POS_NAME", "TRN1"}
                };

                EV.Notify(EventWaferTurnOverStart, dvid);
            }
            
            
            return true;
        }

        public bool Stop(out string reason)
        {
            reason = string.Empty;
            EV.PostInfoLog(Module, $"{Module}.{Name} Stop");
            if (_doStop != null)
            {
                if (_doStop.Value)
                {
                    reason = "Stopping, can't do again";
                    return false;
                }

                _doStop.SetValue(true, out _);
            }
            _state = TurnOverState.Stopping;
            _loopTimer.Start(_scLoopInterval.IntValue * 1000);
            return true;
        }

        private bool ResetError(out string reason)
        {
            reason = string.Empty;
            //EV.PostInfoLog(Module, $"{Module}.{Name} Reset Error");
            if (_doResetError.Value)
                return true;
            _doResetError.SetValue(true, out _);
            _state = TurnOverState.OnErrorCleaning;
            _loopTimer.Start(_scLoopInterval.IntValue * 1000);
            return true;
        }
    }
    public enum TurnOverState
    {
        Idle,
        OnHoming,
        OnTurningTo0,
        OnTurningTo180,
        OnGripping,
        OnUnGripping,
        Error,
        OnErrorCleaning,
        Stopping,
        Stop
    }
}