using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class ThrottleValveBase : BaseDevice, IDevice
	{
		public uint MaxValuePressure = 100u;

		public float PressureParam = 0.1f;

		public uint MaxValuePosi = 5000u;

		public float PosiParam = 0.1f;

		protected double _currentPressureFineTuningValue;

		protected double _currentPositionFineTuningValue;

		protected SCConfigItem _scFineTuningEnable;

		protected SCConfigItem _scPressureFineTuningValue;

		protected SCConfigItem _scPositionFineTuningValue;

		private float _currentPressureWarningRange;

		private float _currentPressureAlarmRange;

		private float _currentPositionWarningRange;

		private float _currentPositionAlarmRange;

		protected SCConfigItem _scEnableAlarm;

		protected SCConfigItem _scPressureAlarmTime;

		protected SCConfigItem _scPressureAlarmRange;

		protected SCConfigItem _scPressureWarningTime;

		protected SCConfigItem _scPressureWarningRange;

		protected SCConfigItem _scPositionAlarmTime;

		protected SCConfigItem _scPositionAlarmRange;

		protected SCConfigItem _scPositionWarningTime;

		protected SCConfigItem _scPositionWarningRange;

		protected ToleranceChecker _tolerancePressureAlarmChecker = new ToleranceChecker();

		protected ToleranceChecker _tolerancePressureWarningChecker = new ToleranceChecker();

		protected ToleranceChecker _tolerancePositionAlarmChecker = new ToleranceChecker();

		protected ToleranceChecker _tolerancePositionWarningChecker = new ToleranceChecker();

		public virtual PressureCtrlMode Mode { get; set; }

		public virtual float PositionFeedback { get; set; }

		public virtual float PressureFeedback { get; set; }

		public virtual float PressureSetPoint { get; set; }

		public virtual float PositionSetPoint { get; set; }

		public virtual AITThrottleValveData DeviceData { get; set; }

		public virtual double PressureFineTuningValue
		{
			get
			{
				if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
				{
					return 1.0;
				}
				if (_currentPressureFineTuningValue != 0.0)
				{
					return 1.0 + _currentPressureFineTuningValue / 100.0;
				}
				return (_scPressureFineTuningValue != null) ? (1.0 + _scPressureFineTuningValue.DoubleValue / 100.0) : 1.0;
			}
		}

		public virtual double PositionFineTuningValue
		{
			get
			{
				if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
				{
					return 1.0;
				}
				if (_currentPositionFineTuningValue != 0.0)
				{
					return 1.0 + _currentPositionFineTuningValue / 100.0;
				}
				return (_scPositionFineTuningValue != null) ? (1.0 + _scPositionFineTuningValue.DoubleValue / 100.0) : 1.0;
			}
		}

		public virtual bool EnableAlarm
		{
			get
			{
				if (_scEnableAlarm != null)
				{
					return _scEnableAlarm.BoolValue;
				}
				return false;
			}
		}

		public virtual double PressureAlarmTime
		{
			get
			{
				if (_scPressureAlarmTime != null)
				{
					return _scPressureAlarmTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double PressureAlarmRange
		{
			get
			{
				if (_currentPressureAlarmRange > 0f)
				{
					return _currentPressureAlarmRange;
				}
				if (_scPressureAlarmRange != null)
				{
					return _scPressureAlarmRange.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double PressureWarningTime
		{
			get
			{
				if (_scPressureWarningTime != null)
				{
					return _scPressureWarningTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double PressureWarningRange
		{
			get
			{
				if (_currentPressureWarningRange > 0f)
				{
					return _currentPressureWarningRange;
				}
				if (_scPressureWarningRange != null)
				{
					return _scPressureWarningRange.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double PositionAlarmTime
		{
			get
			{
				if (_scPositionAlarmTime != null)
				{
					return _scPositionAlarmTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double PositionAlarmRange
		{
			get
			{
				if (_currentPositionAlarmRange > 0f)
				{
					return _currentPositionAlarmRange;
				}
				if (_scPositionAlarmRange != null)
				{
					return _scPositionAlarmRange.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double PositionWarningTime
		{
			get
			{
				if (_scPositionWarningTime != null)
				{
					return _scPositionWarningTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double PositionWarningRange
		{
			get
			{
				if (_currentPositionWarningRange > 0f)
				{
					return _currentPositionWarningRange;
				}
				if (_scPositionWarningRange != null)
				{
					return _scPositionWarningRange.DoubleValue;
				}
				return 0.0;
			}
		}

		protected ThrottleValveBase()
		{
		}

		protected ThrottleValveBase(string module, string name, XmlElement node = null, string ioModule = "")
			: base(module, name, name, name)
		{
			if (node != null)
			{
				_scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, base.Module + "." + base.Name + ".EnableAlarm");
				_scPressureAlarmTime = ParseScNode("scPressureAlarmTime", node, ioModule, base.Module + "." + base.Name + ".PressureAlarmTime");
				_scPressureAlarmRange = ParseScNode("scPressureAlarmRange", node, ioModule, base.Module + "." + base.Name + ".PressureAlarmRange");
				_scPressureWarningTime = ParseScNode("scPressureWarningTime", node, ioModule, base.Module + "." + base.Name + ".PressureWarningTime");
				_scPressureWarningRange = ParseScNode("scPressureWarningRange", node, ioModule, base.Module + "." + base.Name + ".PressureWarningRange");
				_scPositionAlarmTime = ParseScNode("scPositionAlarmTime", node, ioModule, base.Module + "." + base.Name + ".PositionAlarmTime");
				_scPositionAlarmRange = ParseScNode("scPositionAlarmRange", node, ioModule, base.Module + "." + base.Name + ".PositionAlarmRange");
				_scPositionWarningTime = ParseScNode("scPositionWarningTime", node, ioModule, base.Module + "." + base.Name + ".PositionWarningTime");
				_scPositionWarningRange = ParseScNode("scPositionWarningRange", node, ioModule, base.Module + "." + base.Name + ".PositionWarningRange");
				_scPressureFineTuningValue = ParseScNode("scPressureFineTuningValue", node, ioModule, base.Module + ".FineTuning." + base.Name + "Pressure");
				_scPositionFineTuningValue = ParseScNode("scPositionFineTuningValue", node, ioModule, base.Module + ".FineTuning." + base.Name + "Position");
				_scFineTuningEnable = ParseScNode("scFineTuningEnable", node, ioModule, base.Module + ".FineTuning.IsEnable");
			}
		}

		public virtual bool Initialize()
		{
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			DATA.Subscribe(base.Module + "." + base.Name + ".ControlMode", () => (int)Mode);
			DATA.Subscribe(base.Module + "." + base.Name + ".PositionFeedback", () => PositionFeedback);
			DATA.Subscribe(base.Module + "." + base.Name + ".PressureFeedback", () => PressureFeedback);
			DATA.Subscribe(base.Module + "." + base.Name + ".PositionSetPoint", () => PositionSetPoint);
			DATA.Subscribe(base.Module + "." + base.Name + ".PressureSetPoint", () => PressureSetPoint);
			OP.Subscribe(base.Module + "." + base.Name + ".SetPressure", delegate(string function, object[] args)
			{
				SetPressure(Convert.ToSingle(args[0]));
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetPosition", delegate(string function, object[] args)
			{
				SetPosition(Convert.ToSingle(args[0]));
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetOpen", delegate
			{
				SetOpen();
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetClose", delegate
			{
				SetClose();
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetMode", delegate(string function, object[] args)
			{
				if (!Enum.TryParse<PressureCtrlMode>((string)args[0], out var result))
				{
					EV.PostWarningLog(base.Module, $"Argument {args[0]}not valid");
					return false;
				}
				SetControlMode(result);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetFineTuning", delegate(out string reason, int time, object[] param)
			{
				reason = string.Empty;
				SetFineTuning(Convert.ToSingle(param[0]), Convert.ToSingle(param[1]));
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetTolerance", delegate(out string reason, int time, object[] param)
			{
				reason = string.Empty;
				float num = Convert.ToSingle(param[0]);
				float num2 = Convert.ToSingle(param[1]);
				float num3 = Convert.ToSingle(param[2]);
				float num4 = Convert.ToSingle(param[3]);
				SetTolerance(num, num2, num3, num4);
				return true;
			});
			return true;
		}

		public virtual void SetFineTuning(float pressureFineTuning, float positionFineTuning)
		{
			_currentPressureFineTuningValue = pressureFineTuning;
			_currentPositionFineTuningValue = positionFineTuning;
		}

		public virtual void SetTolerance(float pressureWarning, float pressureAlarm, float positionWarning, float positionAlarm)
		{
			_currentPressureWarningRange = pressureWarning;
			_currentPressureAlarmRange = pressureAlarm;
			_tolerancePressureAlarmChecker.Reset(PressureAlarmTime);
			_tolerancePressureWarningChecker.Reset(PressureWarningTime);
			_currentPositionWarningRange = positionWarning;
			_currentPositionAlarmRange = positionAlarm;
			_tolerancePositionAlarmChecker.Reset(PositionAlarmTime);
			_tolerancePositionWarningChecker.Reset(PositionWarningTime);
		}

		public virtual void CheckTolerance()
		{
			if (EnableAlarm)
			{
				if (Mode == PressureCtrlMode.TVPressureCtrl && PressureSetPoint > 0f)
				{
					_tolerancePressureAlarmChecker.Monitor(PressureFeedback, (double)PressureSetPoint * (1.0 - PressureAlarmRange / 100.0), (double)PressureSetPoint * (1.0 + PressureAlarmRange / 100.0), PressureAlarmTime);
					_tolerancePressureWarningChecker.Monitor(PressureFeedback, (double)PressureSetPoint * (1.0 - PressureWarningRange / 100.0), (double)PressureSetPoint * (1.0 + PressureWarningRange / 100.0), PressureWarningTime);
				}
				if (Mode == PressureCtrlMode.TVPositionCtrl && PositionSetPoint > 0f)
				{
					_tolerancePressureAlarmChecker.Monitor(PositionFeedback, (double)PositionSetPoint * (1.0 - PositionAlarmRange / 100.0), (double)PositionSetPoint * (1.0 + PositionAlarmRange / 100.0), PositionAlarmTime);
					_tolerancePressureWarningChecker.Monitor(PositionFeedback, (double)PositionSetPoint * (1.0 - PositionWarningRange / 100.0), (double)PositionSetPoint * (1.0 + PositionWarningRange / 100.0), PositionWarningTime);
				}
			}
		}

		public virtual bool CheckPressureToleranceAlarm()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _tolerancePressureAlarmChecker.Result;
		}

		public virtual bool CheckPressureToleranceWarning()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _tolerancePressureWarningChecker.Result;
		}

		public virtual bool CheckPositionToleranceAlarm()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _tolerancePositionAlarmChecker.Result;
		}

		public virtual bool CheckPositionToleranceWarning()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _tolerancePositionWarningChecker.Result;
		}

		public virtual void SetClose()
		{
			throw new NotImplementedException();
		}

		public virtual void SetOpen()
		{
			throw new NotImplementedException();
		}

		public virtual void SetControlMode(PressureCtrlMode mode)
		{
		}

		public virtual void SetPressure(float isOn)
		{
		}

		public virtual void SetPosition(float position)
		{
		}

		public virtual void Terminate()
		{
		}

		public virtual void Monitor()
		{
			CheckTolerance();
		}

		public virtual void Reset()
		{
		}
	}
}
