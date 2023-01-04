using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.Common.Event;

namespace SicPM.Devices
{
    public partial class IoPressure : BaseDevice, IDevice, IPressureMeter
    {
        public double FeedBack
        {
            get
            {
                return _isFloatAioType ? _aiValue.FloatValue  : _aiValue.Value ;
            }
        }

        public double SetPoint
        {
            get
            {
                if (_aoValue != null)
                    return _isFloatAioType ? _aoValue.FloatValue : _aoValue.Value;
                else
                    return 0;
            }
            set
            {
                if (_aoValue != null)
                {
                    if (_isFloatAioType)
                        _aoValue.FloatValue = (float)value;
                    else
                        _aoValue.Value = (short)value;
                }
            }
        }

        public double OpenDegree
        {
            get
            {
                if (_aiOpenDegree != null)
                {
                    return _aiOpenDegree.FloatValue;
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


        public double DefaultValue
        {
            get
            {
                return _scDefaultSetPoint == null ? 1000 : _scDefaultSetPoint.DoubleValue;
            }
        }

        private AITPressureMeterData DeviceData
        {
            get
            {
                AITPressureMeterData data = new AITPressureMeterData()
                {
                    Module =Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    SetPoint = SetPoint,
                    FeedBack = FeedBack,
                    Unit = Unit,
                    FormatString = _formatString,
                    DisplayWithUnit = true,
                    IsError = IsError,
                    IsWarning = IsWarning,
                    //Precision = Precision,
                    Scale = MaxPressure,
                    ActMode = ActMode,
                    SetMode = SetMode,
                    OpenDegree = OpenDegree,
                    DefaultValue = DefaultValue
                };

                return data;
            }
        }

        public bool IsWarning
        {
            get
            {
                return _checkWarning.Result;
            }
        }

        public bool IsError
        {
            get
            {
                return _checkAlarm.Result;
            }
        }

        //public double MinPressure
        //{
        //    get
        //    {
        //        return _scMinValue == null ? 0 : _scMinValue.DoubleValue;
        //    }
        //}

        public double MaxPressure
        {
            get
            {
                return _scMaxValue == null ? 100 : _scMaxValue.DoubleValue;
            }
        }

        //public int WarningTime
        //{
        //    get
        //    {
        //        return _scWarningTime == null ? 0 : _scWarningTime.IntValue;
        //    }
        //}

        //public int AlarmTime
        //{
        //    get
        //    {
        //        return _scAlarmTime == null ? 0 : _scAlarmTime.IntValue;
        //    }
        //}

        //public bool EnableAlarm
        //{
        //    get
        //    {
        //        return _scEnableAlarm == null ? true : _scEnableAlarm.BoolValue;
        //    }
        //}

        public bool IsOutOfRange
        {
            get
            {               
                return  (FeedBack > MaxPressure);
            }
        }

        public string Unit { get; set; }

        private AIAccessor _aiValue = null;
        private AOAccessor _aoValue = null;
        private AIAccessor _aiActMode;
        private AOAccessor _aoSetMode;
        private AIAccessor _aiOpenDegree;
        private DIAccessor _diAlarm;

        private string _formatString = "0.0";

        private SCConfigItem _scUnitValue;
        private SCConfigItem _scMaxValue;
        private SCConfigItem _scDefaultSetPoint;
        //private SCConfigItem _scEnableAlarm;
        //private SCConfigItem _scWarningTime;
        //private SCConfigItem _scAlarmTime;
        //private SCConfigItem _scPrecision;

        private ToleranceChecker _checkWarning = new ToleranceChecker();
        private ToleranceChecker _checkAlarm = new ToleranceChecker();


        public AlarmEventItem AlarmToleranceWarning { get; set; }
        public AlarmEventItem AlarmToleranceError { get; set; }

        private bool _isFloatAioType = false;

        private DeviceTimer _rampTimer = new DeviceTimer();
        private double _rampTarget;
        private double _rampInitValue;
        private int _rampTime;
        private string _infoText = "";
        private R_TRIG _alamrTrig = new R_TRIG();

        public IoPressure(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");
            Unit = node.GetAttribute("unit");

            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _aiValue = ParseAiNode("aiValue", node, ioModule);
            _aoValue = ParseAoNode("aoValue", node, ioModule);
            _aiActMode = ParseAiNode("aiActMode", node, ioModule);
            _aoSetMode = ParseAoNode("aoSetMode", node, ioModule);
            _aiOpenDegree = ParseAiNode("aiOpenDegree", node, ioModule);
            _diAlarm = ParseDiNode("diAlarm", node, ioModule); 
            _infoText = node.GetAttribute("AlarmText");

            if (node.HasAttribute("formatString"))
                _formatString = string.IsNullOrEmpty(node.GetAttribute("formatString")) ? "F5" : node.GetAttribute("formatString");

            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }

            _scMaxValue = ParseScNode("", node, "", $"{scBasePath}.{Display}.Scale");
            //_scUnitValue = ParseScNode("", node, "", $"{scBasePath}.{Name}.Unit");
            _scDefaultSetPoint = ParseScNode("scDefaultSetPoint", node, ioModule, $"{scBasePath}.{Display}.DefaultSetPoint");
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.SetPoint", () => SetPoint);
            DATA.Subscribe($"{Module}.{Name}.FeedBack", () => FeedBack);
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.SetMode", () => SetMode);

            OP.Subscribe($"{Module}.{Name}.Ramp", (out string reason, int time, object[] param) =>
            {
                reason = "";
                double target = Convert.ToDouble(param[0].ToString());

                if (target < 0 || target > MaxPressure)
                {
                    reason = $"set {Display} value {target} out of range [0, {MaxPressure}] {Unit}";
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

            AlarmToleranceWarning = SubscribeAlarm($"{Module}.{Name}.OutOfToleranceWarning", "", ResetWarningChecker, EventLevel.Warning);
            AlarmToleranceError = SubscribeAlarm($"{Module}.{Name}.OutOfToleranceError", "", ResetErrorChecker);

            return true;
        }

        private bool SetContrlMode(out string reason, int time, object[] param)
        {
            return SetPcMode((PcCtrlMode)Enum.Parse(typeof(PcCtrlMode), (string)param[0], true),
                out reason);
        }

        public bool SetPcMode(PcCtrlMode mode, out string reason)
        {
            switch (mode)
            {
                case PcCtrlMode.Normal:
                    SetMode = (double)PcCtrlMode.Normal;
                    break;
                case PcCtrlMode.Close:
                    SetMode = (double)PcCtrlMode.Close;
                    break;
                case PcCtrlMode.Open:
                    SetMode = (double)PcCtrlMode.Open;
                    break;
                case PcCtrlMode.Hold:
                    SetMode = (double)PcCtrlMode.Hold;
                    break;
                default:
                    break;

            }
            reason = $"{Display} set to {mode}";
            return true;
        }


        public void Ramp(double target)
        {
            _aoValue.FloatValue = (float)target;
        }

        public void Ramp(double target, int time)
        {
            _rampTimer.Stop();
            target = Math.Max(0, target);
            target = Math.Min(MaxPressure, target);
            _rampInitValue = SetPoint;    //ramp 初始值取当前设定值，而非实际读取值.零漂问题
            _rampTime = time;
            _rampTarget = target;
            _rampTimer.Start(_rampTime);
        }

        public void Terminate()
        {
            _aoValue.FloatValue = (float)DefaultValue;
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
            }

        }

        public void Monitor()
        {
            MonitorRamping();
            MonitorAlarm();

            //if (EnableAlarm && (WarningTime > 0))
            //{
            //    _checkWarning.Monitor(Value, MinPressure, MaxPressure, WarningTime);

            //    if (_checkWarning.Trig)
            //    {
            //        AlarmToleranceWarning.Description =
            //            $"{Display} out of range [{MinPressure},{MaxPressure}]{Unit} for {WarningTime} seconds";
            //        AlarmToleranceWarning.Set();
            //    }

            //    if (!_checkWarning.Result)
            //    {
            //        AlarmToleranceWarning.Reset();
            //    }
            //}
            //else
            //{
            //    AlarmToleranceWarning.Reset();
            //}

            //if (EnableAlarm && (AlarmTime > 0))
            //{
            //    _checkAlarm.Monitor(Value, MinPressure, MaxPressure, AlarmTime);

            //    if (_checkAlarm.Trig)
            //    {
            //        AlarmToleranceError.Description =
            //            $"{Display} out of range [{MinPressure},{MaxPressure}]{Unit} for {AlarmTime} seconds";
            //        AlarmToleranceError.Set();
            //    }

            //    if (!_checkAlarm.Result)
            //    {
            //        AlarmToleranceError.Reset();
            //    }
            //}
            //else
            //{
            //    AlarmToleranceError.Reset();
            //}
        }

        private void MonitorAlarm()
        {
            _alamrTrig.CLK = _diAlarm != null && _diAlarm.Value;
            if (_alamrTrig.Q)
            {
                EV.PostAlarmLog(Module, _infoText);
            }
        }

        public bool ResetWarningChecker()
        {
            _checkWarning.RST = true;

            return true;
        }

        public bool ResetErrorChecker()
        {
            _checkAlarm.RST = true;

            return true;
        }

        public void Reset()
        {
            AlarmToleranceWarning.Reset();
            AlarmToleranceError.Reset();
            _alamrTrig.RST = true;
        }
    }
}
