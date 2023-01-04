using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class HeaterBase : BaseDevice, IDevice
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

		public virtual bool IsPowerOn { get; set; }

		public virtual float TempSetPoint { get; set; }

		public virtual float TempFeedback { get; set; }

		public virtual AITHeaterData DeviceData { get; set; }

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

		protected HeaterBase()
		{
		}

		protected HeaterBase(string module, string name, XmlElement node = null, string ioModule = "")
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
			DATA.Subscribe(base.Module + "." + base.Name + ".TempFeedback", () => TempFeedback);
			DATA.Subscribe(base.Module + "." + base.Name + ".TempSetPoint", () => TempSetPoint);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsPowerOn", () => IsPowerOn);
			OP.Subscribe(base.Module + "." + base.Name + ".SetPowerOn", delegate
			{
				SetPowerOnOff(isOn: true);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetPowerOff", delegate
			{
				SetPowerOnOff(isOn: false);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetPowerOnOff", delegate(string function, object[] args)
			{
				bool.TryParse(args[0].ToString(), out var result);
				SetPowerOnOff(result);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetTemperature", delegate(string function, object[] args)
			{
				SetTemperature((float)args[0]);
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
			if (EnableAlarm && TempSetPoint != 0f)
			{
				_toleranceAlarmChecker.Monitor(TempFeedback, (double)TempSetPoint * (1.0 - AlarmRange / 100.0), (double)TempSetPoint * (1.0 + AlarmRange / 100.0), AlarmTime);
				_toleranceWarningChecker.Monitor(TempFeedback, (double)TempSetPoint * (1.0 - WarningRange / 100.0), (double)TempSetPoint * (1.0 + WarningRange / 100.0), WarningTime);
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

		public virtual void SetPowerOnOff(bool isOn)
		{
		}

		public virtual void SetTemperature(float temperature)
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
			_toleranceAlarmChecker.Reset(AlarmTime);
			_toleranceWarningChecker.Reset(WarningTime);
		}
	}
}
