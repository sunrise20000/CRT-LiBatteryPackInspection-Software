using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.Common.Event;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs;

namespace SicPM.Devices
{
   public partial class IoMFC : BaseDevice, IDevice, IMfc
    {
        public string Unit
        {
            get; set;
        }

        public double Scale
        {
            get
            {
                if (_scN2Scale == null )
                    return 0;
                return _scN2Scale.DoubleValue ;
            }
        }

        public double SetPoint
        {
            get
            {
                if (_aoFlow != null)
                {
                    return _isFloatAioType ? _aoFlow.FloatValue : _aoFlow.Value;
                }
                return 0;
            }
            set
            {
                if (_aoFlow != null)
                {
                    if (_isFloatAioType)
                        _aoFlow.FloatValue = (float)value;
                    else
                        _aoFlow.Value = (short)value;
                }
            }
        }

        public double DefaultSetPoint
        {
            get
            {
                if (_scDefaultSetPoint != null)
                    return _scDefaultSetPoint.DoubleValue;
                return 0;
            }
        }

        public double FeedBack
        {
            get
            {
                if (_aiFlow != null)
                {
                    double aiValue = _isFloatAioType ? _aiFlow.FloatValue : _aiFlow.Value;

                    return aiValue; // (_scRegulationFactor != null && _scRegulationFactor.DoubleValue > 0.001) ? aiValue / _scRegulationFactor.DoubleValue : aiValue;
                }
                return 0;
            }
        }

        public double ActMode
        {
            get
            {
                if (_aiActMode != null)
                {
                    return _aiActMode.FloatValue;
                }
                return 0;
            }
        }

        public double SetMode
        {
            get
            {
                if (_aoSetMode != null)
                {
                    return _aoSetMode.FloatValue;
                }
                return 0;
            }
            set
            {
                if (_aoSetMode != null)
                {
                    if (_isFloatAioType)
                        _aoSetMode.FloatValue = (float)value;
                    else
                        _aoSetMode.Value = (short)value;
                }
            }
        }


        public bool EnableAlarm
        {
            get
            {
                if (_scEnableAlarm != null)
                    return _scEnableAlarm.BoolValue;
                return false;
            }
        }

        public double AlarmRange
        {
            get
            {
                if (_scAlarmRange != null)
                    return _scAlarmRange.DoubleValue;
                return 0;
            }
        }

        public double AlarmTime
        {
            get
            {
                if (_scAlarmTime != null)
                    return _scAlarmTime.IntValue;
                return 0;
            }
        }
        public double WarningRange
        {
            get
            {
                if (_scWarningRange != null)
                    return _scWarningRange.DoubleValue;
                return 0;
            }
        }

        public double WarningTime
        {
            get
            {
                if (_scWarningTime != null)
                    return _scWarningTime.IntValue;
                return 0;
            }
        }

        private AITMfcData DeviceData
        {
            get
            {
                AITMfcData data = new AITMfcData()
                {
                    UniqueName = $"{Module}.{Name}",
                    Type = "MFC",
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = DisplayName,
                    FeedBack = FeedBack,
                    SetPoint = SetPoint,
                    Scale = Scale,
                    IsWarning = !AlarmToleranceWarning.IsAcknowledged,
                    IsError = !AlarmToleranceAlarm.IsAcknowledged,
                    DefaultValue = DefaultSetPoint,
                    ActMode = ActMode,
                    SetMode = SetMode,
                };
                //if (Module.Equals("PM1") && Name.Equals("Mfc1"))
                //    Console.WriteLine("Mfc3:" + FeedBack);
                return data;
            }
        }


        public string DisplayName
        {
            get
            {
                if (_scGasName != null)
                    return _scGasName.StringValue;
                return Display;
            }
        }

        private DeviceTimer _rampTimer = new DeviceTimer();
        private double _rampTarget;
        private double _rampInitValue;
        private double _rampSetMode;
        private int _rampTime;

        private ToleranceChecker _toleranceCheckerWarning = new ToleranceChecker();
        private ToleranceChecker _toleranceCheckerAlarm = new ToleranceChecker();

        private AIAccessor _aiFlow;
        private AIAccessor _aiActMode;
        private AOAccessor _aoFlow;
        private AOAccessor _aoSetMode;
        private DIAccessor _diAlarm;

        protected SCConfigItem _scGasName;
        protected SCConfigItem _scEnable;
        private SCConfigItem _scN2Scale;
        private SCConfigItem _scScaleFactor;
        private SCConfigItem _scAlarmRange;
        private SCConfigItem _scEnableAlarm;
        private SCConfigItem _scAlarmTime;
        private SCConfigItem _scDefaultSetPoint;
        private SCConfigItem _scRegulationFactor;
        protected SCConfigItem _scWarningTime;
        protected SCConfigItem _scWarningRange;

        public AlarmEventItem AlarmToleranceWarning { get; set; }
        public AlarmEventItem AlarmToleranceAlarm { get; set; }

        private string _infoText = "";
        private bool _isFloatAioType = false;
        private R_TRIG _alamrTrig = new R_TRIG();

        public IoMFC(string module, XmlElement node, string ioModule = "")
        {
            Unit = node.GetAttribute("unit");
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _aiFlow = ParseAiNode("aiFlow", node, ioModule);
            _aoFlow = ParseAoNode("aoFlow", node, ioModule);
            _aiActMode = ParseAiNode("aiActMode", node, ioModule);
            _aoSetMode = ParseAoNode("aoSetMode", node, ioModule);
            _diAlarm = ParseDiNode("diAlarm", node, ioModule);

            _infoText = node.GetAttribute("AlarmText");
            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }

            _scGasName = SC.GetConfigItem($"{scBasePath}.{Name}.GasName");
            //_scEnable = SC.GetConfigItem($"{scBasePath}.{Name}.Enable");

            _scN2Scale = ParseScNode("scN2Scale", node, ioModule, $"{scBasePath}.{Name}.N2Scale");

            //_scScaleFactor = ParseScNode("scScaleFactor", node, ioModule, $"{scBasePath}.{Name}.ScaleFactor");
            _scAlarmRange = ParseScNode("scAlarmRange", node, ioModule, $"{scBasePath}.{Name}.AlarmRange");
            _scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, $"{scBasePath}.{Name}.EnableAlarm");
            _scAlarmTime = ParseScNode("scAlarmTime", node, ioModule, $"{scBasePath}.{Name}.AlarmTime");
            _scDefaultSetPoint = ParseScNode("scDefaultSetPoint", node, ioModule, $"{scBasePath}.{Name}.DefaultSetPoint");


            _scWarningRange = SC.GetConfigItem($"{scBasePath}.{Name}.WarningRange");
            _scWarningTime = SC.GetConfigItem($"{scBasePath}.{Name}.WarningTime");

            //_scRegulationFactor = ParseScNode("scFlowRegulationFactor", node, ioModule, $"{scBasePath}.{Name}.RegulationFactor");

        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.FeedBack", () => FeedBack);
            DATA.Subscribe($"{Module}.{Name}.SetPoint", () => SetPoint);
            DATA.Subscribe($"{Module}.{Name}.SetMode", () => SetMode);
           
            OP.Subscribe($"{Module}.{Name}.Ramp", (out string reason, int time, object[] param) =>
            {
                double target = Convert.ToDouble(param[0].ToString());

                if (target < 0 || target > Scale)
                {
                    reason = $"set {Display} value {target} out of range [0, {Scale}] {Unit}";
                    return false;
                }

                Ramp(target, time);

                if (time > 0)
                {
                    reason = $"{Display} ramp to {target} {Unit} in {time} seconds";
                }
                else
                {
                    reason = $"{Display} ramp to {target} {Unit}";
                }
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetMode", SetContrlMode);
            //OP.Subscribe($"{Module}.{Name}.SetMode", (function, args) =>
            //{
            //    if (!Enum.TryParse((string)args[0], out PressureCtrlMode mode))
            //    {
            //        EV.PostWarningLog(Module, $"Argument {args[0]}not valid");
            //        return false;
            //    }
            //    SetMode();
            //    return true;
            //});

            AlarmToleranceWarning = SubscribeAlarm($"{Module}.{Name}.ToleranceWarning", "", ResetWarningChecker, EventLevel.Warning);
            AlarmToleranceAlarm = SubscribeAlarm($"{Module}.{Name}.ToleranceAlarm", "", ResetAlarmChecker);

            return true;
        }

        private bool SetContrlMode(out string reason, int time, object[] param)
        {
            return SetMfcMode((MfcCtrlMode)Enum.Parse(typeof(MfcCtrlMode), (string)param[0], true),
                out reason);
        }

        public bool SetMfcMode(MfcCtrlMode mode, out string reason)
        {
            switch (mode)
            {
                case MfcCtrlMode.Normal:
                    SetMode = (double)MfcCtrlMode.Normal;
                    break;
                case MfcCtrlMode.Close:
                    SetMode = (double)MfcCtrlMode.Close;
                    break;
                case MfcCtrlMode.Open:
                    SetMode = (double)MfcCtrlMode.Open;
                    break;
                case MfcCtrlMode.Hold:
                    SetMode = (double)MfcCtrlMode.Hold;
                    break;
                default:
                    break;

            }
            reason = $"{Display} set to {mode}";
            return true;
        }


        public void Monitor()
        {
            MonitorRamping();

            MonitorTolerance();
        }

        public bool ResetWarningChecker()
        {
            _toleranceCheckerWarning.Reset(WarningTime);

            return true;
        }

        public bool ResetAlarmChecker()
        {
            _toleranceCheckerAlarm.Reset(AlarmTime);

            return true;
        }

        public void Reset()
        {
            AlarmToleranceWarning.Reset();
            AlarmToleranceAlarm.Reset();
            _alamrTrig.RST = true;
        }


        public void SetToDefaultByRamp(int time)
        {
            Ramp(DefaultSetPoint, time * 1000);
        }


        public void Terminate()
        {
            Ramp(DefaultSetPoint, 5000);
        }

        public bool Ramp(double flowSetPoint, int time, out string reason)
        {
            if (HasAlarm)
            {
                reason = $"{DisplayName} in error status, can not flow";
                return false;
            }

            if (flowSetPoint < 0 || flowSetPoint > Scale)
            {
                reason = $"{DisplayName} range is [0, {Scale}], can not flow {flowSetPoint}";
                return false;
            }

            if (time > 0)
            {
                EV.PostInfoLog(Module, $"Set {DisplayName} flow to {flowSetPoint} {Unit} in {time / 1000:F0} seconds");
            }
            else
            {
                EV.PostInfoLog(Module, $"Set {DisplayName} flow to {flowSetPoint} {Unit}");
            }

            Ramp(flowSetPoint, time);
            reason = string.Empty;
            return true;
        }

        public void Ramp(int time)
        {
            Ramp(DefaultSetPoint, time);
        }

        public void Ramp(double target, int time)
        {
            _rampTimer.Stop();
            target = Math.Max(0, target);
            target = Math.Min(Scale, target);
            _rampInitValue = SetPoint;    //ramp 初始值取当前设定值，而非实际读取值.零漂问题
            _rampTime = time;
            _rampTarget = target;
            _rampTimer.Start(_rampTime);
            _rampSetMode = SetMode;

            LOG.Info($"{Name}.Ramp {_rampTarget} in {_rampTime} seconds,Mode {_rampSetMode/1000}");

        }

        public void StopRamp()
        {
            if (!_rampTimer.IsIdle())
            {
                if (_rampTime != 0)
                {
                    Ramp(SetPoint, 0);
                }
            }
        }

        private void MonitorRamping()
        {
            if (!_rampTimer.IsIdle())
            {
                if (_rampTimer.IsTimeout() || _rampTime == 0)
                {
                    _rampTimer.Stop();
                    SetPoint = _rampTarget;
                }
                else
                {
                    SetPoint = _rampInitValue + (_rampTarget - _rampInitValue) * _rampTimer.GetElapseTime() / _rampTime;
                }

                if(_rampSetMode == 0 || _rampSetMode == 1 || _rampSetMode == 2 || _rampSetMode == 3)
                {
                    SetMode = _rampSetMode;
                }
            }
        }

        private void MonitorTolerance()
        {
            _alamrTrig.CLK = _diAlarm != null && _diAlarm.Value;
            if (_alamrTrig.Q)
            {
                EV.PostAlarmLog(Module, _infoText);
            }



            if (!EnableAlarm || SetPoint < 0.01 || !_rampTimer.IsIdle())
            {
                _toleranceCheckerWarning.RST = true;
                _toleranceCheckerAlarm.RST = true;
                return;
            }

            double d1 = (SetPoint * Math.Abs(100 - WarningRange) / 100);
            double d2 = (SetPoint * Math.Abs(100 + WarningRange) / 100);
            _toleranceCheckerWarning.Monitor(FeedBack, d1, d2, WarningTime);
            if (_toleranceCheckerWarning.Trig)
            {
                AlarmToleranceWarning.Description = $"{Display} flow out of range {d1} - {d2} {Unit} in {WarningTime:F0} seconds";
                AlarmToleranceWarning.Set();
            }

            double d3 = (SetPoint * Math.Abs(100 - AlarmRange) / 100);
            double d4 = (SetPoint * Math.Abs(100 + AlarmRange) / 100);
            _toleranceCheckerAlarm.Monitor(FeedBack, d3, d4, AlarmTime);
            if (_toleranceCheckerAlarm.Trig)
            {
                AlarmToleranceAlarm.Description = $"{Display} flow out of range {d3} - {d4} {Unit}  in {AlarmTime:F0} seconds";
                AlarmToleranceAlarm.Set();
            }
        }

    }
}
