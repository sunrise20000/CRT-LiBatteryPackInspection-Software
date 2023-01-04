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
	public abstract class RfPowerBase : BaseDevice, IDevice
	{
		private float _currentWarningRange;

		private float _currentAlarmRange;

		protected SCConfigItem _scEnableAlarm;

		protected SCConfigItem _scAlarmTime;

		protected SCConfigItem _scAlarmRange;

		protected SCConfigItem _scWarningTime;

		protected SCConfigItem _scWarningRange;

		protected ToleranceChecker _toleranceAlarmChecker = new ToleranceChecker();

		protected ToleranceChecker _toleranceWarningChecker = new ToleranceChecker();

		protected double _currentFineTuningValue;

		protected SCConfigItem _scFineTuningEnable;

		protected SCConfigItem _scFineTuningValue;

		public virtual bool IsConnected => true;

		public virtual bool IsPowerOn { get; set; }

		public virtual bool IsError { get; set; }

		public virtual EnumRfPowerWorkMode WorkMode { get; set; }

		public virtual EnumRfPowerRegulationMode RegulationMode { get; set; }

		public virtual float ForwardPower { get; set; }

		public virtual float ReflectPower { get; set; }

		public virtual float PowerSetPoint { get; set; }

		public virtual float Frequency { get; set; }

		public virtual float PulsingFrequency { get; set; }

		public virtual float PulsingDutyCycle { get; set; }

		public virtual AITRfPowerData DeviceData { get; set; }

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

		public virtual double AlarmTime
		{
			get
			{
				if (_scAlarmTime != null)
				{
					return _scAlarmTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double AlarmRange
		{
			get
			{
				if (_currentAlarmRange > 0f)
				{
					return _currentAlarmRange;
				}
				if (_scAlarmRange != null)
				{
					return _scAlarmRange.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double WarningTime
		{
			get
			{
				if (_scWarningTime != null)
				{
					return _scWarningTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double WarningRange
		{
			get
			{
				if (_currentWarningRange > 0f)
				{
					return _currentWarningRange;
				}
				if (_scWarningRange != null)
				{
					return _scWarningRange.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double FineTuningValue
		{
			get
			{
				if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
				{
					return 1.0;
				}
				if (_currentFineTuningValue != 0.0)
				{
					return 1.0 + _currentFineTuningValue / 100.0;
				}
				return (_scFineTuningValue != null) ? (1.0 + _scFineTuningValue.DoubleValue / 100.0) : 1.0;
			}
		}

		protected RfPowerBase()
		{
		}

		protected RfPowerBase(string module, string name, XmlElement node = null, string ioModule = "")
			: base(module, name, name, name)
		{
			if (node != null)
			{
				_scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, base.Module + "." + base.Name + ".EnableAlarm");
				_scAlarmTime = ParseScNode("scAlarmTime", node, ioModule, base.Module + "." + base.Name + ".AlarmTime");
				_scAlarmRange = ParseScNode("scAlarmRange", node, ioModule, base.Module + "." + base.Name + ".AlarmRange");
				_scWarningTime = ParseScNode("scWarningTime", node, ioModule, base.Module + "." + base.Name + ".WarningTime");
				_scWarningRange = ParseScNode("scWarningRange", node, ioModule, base.Module + "." + base.Name + ".WarningRange");
				_scFineTuningValue = ParseScNode("scFineTuningValue", node, ioModule, base.Module + ".FineTuning." + base.Name);
				_scFineTuningEnable = ParseScNode("scFineTuningEnable", node, ioModule, base.Module + ".FineTuning.IsEnable");
			}
		}

		public virtual bool Initialize()
		{
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			DATA.Subscribe(base.Module + "." + base.Name + ".WorkMode", () => WorkMode.ToString());
			DATA.Subscribe(base.Module + "." + base.Name + ".RegulationMode", () => RegulationMode.ToString());
			DATA.Subscribe(base.Module + "." + base.Name + ".ForwardPower", () => ForwardPower);
			DATA.Subscribe(base.Module + "." + base.Name + ".ReflectPower", () => ReflectPower);
			DATA.Subscribe(base.Module + "." + base.Name + ".PowerSetPoint", () => PowerSetPoint);
			DATA.Subscribe(base.Module + "." + base.Name + ".Frequency", () => Frequency);
			DATA.Subscribe(base.Module + "." + base.Name + ".PulsingFrequency", () => PulsingFrequency);
			DATA.Subscribe(base.Module + "." + base.Name + ".PulsingDutyCycle", () => PulsingDutyCycle);
			OP.Subscribe(base.Module + "." + base.Name + ".SetPowerOn", delegate
			{
				if (!SetPowerOnOff(isOn: true, out var reason3))
				{
					EV.PostWarningLog(base.Module, base.Module + " " + base.Name + " RF on failed, for " + reason3);
					return false;
				}
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetPowerOff", delegate
			{
				if (!SetPowerOnOff(isOn: false, out var reason2))
				{
					EV.PostWarningLog(base.Module, base.Module + " " + base.Name + " RF off failed, for " + reason2);
					return false;
				}
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetPower", delegate(string function, object[] args)
			{
				SetPower((float)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetRegulationMode", delegate(string function, object[] args)
			{
				if (!Enum.TryParse<EnumRfPowerRegulationMode>((string)args[0], out var result))
				{
					EV.PostWarningLog(base.Module, $"Argument {args[0]}not valid");
					return false;
				}
				SetRegulationMode(result);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetTolerance", delegate(out string reason, int time, object[] param)
			{
				reason = string.Empty;
				float num = Convert.ToSingle(param[0]);
				float num2 = Convert.ToSingle(param[1]);
				SetTolerance(num, num2);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetFineTuning", delegate(out string reason, int time, object[] param)
			{
				reason = string.Empty;
				SetFineTuning(Convert.ToSingle(param[0]));
				return true;
			});
			return true;
		}

		public virtual void SetFineTuning(float fineTuning)
		{
			_currentFineTuningValue = fineTuning;
		}

		public virtual void SetTolerance(float warning, float alarm)
		{
			_currentWarningRange = warning;
			_currentAlarmRange = alarm;
			_toleranceAlarmChecker.Reset(AlarmTime);
			_toleranceWarningChecker.Reset(WarningTime);
		}

		public virtual void CheckTolerance()
		{
			if (EnableAlarm && PowerSetPoint != 0f)
			{
				_toleranceAlarmChecker.Monitor(ForwardPower, (double)PowerSetPoint * (1.0 - AlarmRange / 100.0), (double)PowerSetPoint * (1.0 + AlarmRange / 100.0), AlarmTime);
				_toleranceWarningChecker.Monitor(ForwardPower, (double)PowerSetPoint * (1.0 - WarningRange / 100.0), (double)PowerSetPoint * (1.0 + WarningRange / 100.0), WarningTime);
			}
		}

		public virtual bool CheckToleranceAlarm()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _toleranceAlarmChecker.Result;
		}

		public virtual bool CheckToleranceWarning()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _toleranceWarningChecker.Result;
		}

		public virtual void SetRegulationMode(EnumRfPowerRegulationMode enumRfPowerControlMode)
		{
		}

		public virtual void SetWorkMode(EnumRfPowerWorkMode enumRfPowerWorkMode)
		{
		}

		public virtual bool SetPowerOnOff(bool isOn, out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual void SetPower(float power)
		{
		}

		public virtual void SetFreq(float freq)
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
