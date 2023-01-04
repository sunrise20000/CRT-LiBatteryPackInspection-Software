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
	public abstract class RfMatchBase : BaseDevice, IDevice
	{
		protected double _currentTuneFineTuningValue;

		protected double _currentLoadFineTuningValue;

		protected SCConfigItem _scFineTuningEnable;

		protected SCConfigItem _scTuneFineTuningValue;

		protected SCConfigItem _scLoadFineTuningValue;

		private float _currentTuneWarningRange;

		private float _currentLoadWarningRange;

		private float _currentTuneAlarmRange;

		private float _currentLoadAlarmRange;

		protected SCConfigItem _scEnableAlarm;

		protected SCConfigItem _scTuneAlarmTime;

		protected SCConfigItem _scTuneAlarmRange;

		protected SCConfigItem _scTuneWarningTime;

		protected SCConfigItem _scTuneWarningRange;

		protected ToleranceChecker _toleranceTuneAlarmChecker = new ToleranceChecker();

		protected ToleranceChecker _toleranceTuneWarningChecker = new ToleranceChecker();

		protected SCConfigItem _scLoadAlarmTime;

		protected SCConfigItem _scLoadAlarmRange;

		protected SCConfigItem _scLoadWarningTime;

		protected SCConfigItem _scLoadWarningRange;

		protected ToleranceChecker _toleranceLoadAlarmChecker = new ToleranceChecker();

		protected ToleranceChecker _toleranceLoadWarningChecker = new ToleranceChecker();

		public virtual EnumRfMatchTuneMode TuneMode1 { get; set; }

		public virtual EnumRfMatchTuneMode TuneMode2 { get; set; }

		public virtual EnumRfMatchTuneMode TuneMode1Setpoint { get; set; }

		public virtual EnumRfMatchTuneMode TuneMode2Setpoint { get; set; }

		public virtual float LoadPosition1 { get; set; }

		public virtual float LoadPosition2 { get; set; }

		public virtual float LoadPosition1Setpoint { get; set; }

		public virtual float LoadPosition2Setpoint { get; set; }

		public virtual float TunePosition1 { get; set; }

		public virtual float TunePosition2 { get; set; }

		public virtual float TunePosition1Setpoint { get; set; }

		public virtual float TunePosition2Setpoint { get; set; }

		public virtual float DCBias { get; set; }

		public virtual float BiasPeak { get; set; }

		public virtual AITRfMatchData DeviceData { get; set; }

		public virtual double TuneFineTuningValue
		{
			get
			{
				if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
				{
					return 1.0;
				}
				if (_currentTuneFineTuningValue != 0.0)
				{
					return 1.0 + _currentTuneFineTuningValue / 100.0;
				}
				return (_scTuneFineTuningValue != null) ? (1.0 + _scTuneFineTuningValue.DoubleValue / 100.0) : 1.0;
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
				if (_currentLoadFineTuningValue != 0.0)
				{
					return 1.0 + _currentLoadFineTuningValue / 100.0;
				}
				return (_scLoadFineTuningValue != null) ? (1.0 + _scLoadFineTuningValue.DoubleValue / 100.0) : 1.0;
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

		public virtual double TuneAlarmTime
		{
			get
			{
				if (_scTuneAlarmTime != null)
				{
					return _scTuneAlarmTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double TuneAlarmRange
		{
			get
			{
				if (_currentTuneAlarmRange > 0f)
				{
					return _currentTuneAlarmRange;
				}
				if (_scTuneAlarmRange != null)
				{
					return _scTuneAlarmRange.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double TuneWarningTime
		{
			get
			{
				if (_scTuneWarningTime != null)
				{
					return _scTuneWarningTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double TuneWarningRange
		{
			get
			{
				if (_currentTuneWarningRange > 0f)
				{
					return _currentTuneWarningRange;
				}
				if (_scTuneWarningRange != null)
				{
					return _scTuneWarningRange.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double LoadAlarmTime
		{
			get
			{
				if (_scLoadAlarmTime != null)
				{
					return _scLoadAlarmTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double LoadAlarmRange
		{
			get
			{
				if (_currentLoadAlarmRange > 0f)
				{
					return _currentLoadAlarmRange;
				}
				if (_scLoadAlarmRange != null)
				{
					return _scLoadAlarmRange.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double LoadWarningTime
		{
			get
			{
				if (_scLoadWarningTime != null)
				{
					return _scLoadWarningTime.DoubleValue;
				}
				return 0.0;
			}
		}

		public virtual double LoadWarningRange
		{
			get
			{
				if (_currentLoadWarningRange > 0f)
				{
					return _currentLoadWarningRange;
				}
				if (_scLoadWarningRange != null)
				{
					return _scLoadWarningRange.DoubleValue;
				}
				return 0.0;
			}
		}

		protected RfMatchBase()
		{
		}

		protected RfMatchBase(string module, string name, XmlElement node = null, string ioModule = "")
			: base(module, name, name, name)
		{
			if (node != null)
			{
				_scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, base.Module + "." + base.Name + ".EnableAlarm");
				_scTuneAlarmTime = ParseScNode("scTuneAlarmTime", node, ioModule, base.Module + "." + base.Name + ".TuneAlarmTime");
				_scTuneAlarmRange = ParseScNode("scTuneAlarmRange", node, ioModule, base.Module + "." + base.Name + ".TuneAlarmRange");
				_scTuneWarningTime = ParseScNode("scTuneWarningTime", node, ioModule, base.Module + "." + base.Name + ".TuneWarningTime");
				_scTuneWarningRange = ParseScNode("scTuneWarningRange", node, ioModule, base.Module + "." + base.Name + ".TuneWarningRange");
				_scLoadAlarmTime = ParseScNode("scLoadAlarmTime", node, ioModule, base.Module + "." + base.Name + ".LoadAlarmTime");
				_scLoadAlarmRange = ParseScNode("scLoadAlarmRange", node, ioModule, base.Module + "." + base.Name + ".LoadAlarmRange");
				_scLoadWarningTime = ParseScNode("scLoadWarningTime", node, ioModule, base.Module + "." + base.Name + ".LoadWarningTime");
				_scLoadWarningRange = ParseScNode("scLoadWarningRange", node, ioModule, base.Module + "." + base.Name + ".LoadWarningRange");
				_scTuneFineTuningValue = ParseScNode("scTuneFineTuningValue", node, ioModule, base.Module + ".FineTuning." + base.Name + "Tune");
				_scLoadFineTuningValue = ParseScNode("scLoadFineTuningValue", node, ioModule, base.Module + ".FineTuning." + base.Name + "Load");
				_scFineTuningEnable = ParseScNode("scFineTuningEnable", node, ioModule, base.Module + ".FineTuning.IsEnable");
			}
		}

		public virtual bool Initialize()
		{
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			DATA.Subscribe(base.Module + "." + base.Name + ".TuneMode1", () => TuneMode1.ToString());
			DATA.Subscribe(base.Module + "." + base.Name + ".TuneMode2", () => TuneMode2.ToString());
			DATA.Subscribe(base.Module + "." + base.Name + ".LoadPosition1", () => LoadPosition1);
			DATA.Subscribe(base.Module + "." + base.Name + ".LoadPosition2", () => LoadPosition2);
			DATA.Subscribe(base.Module + "." + base.Name + ".TunePosition1", () => TunePosition1);
			DATA.Subscribe(base.Module + "." + base.Name + ".TunePosition2", () => TunePosition2);
			OP.Subscribe(base.Module + "." + base.Name + ".SetTuneMode1", delegate(string function, object[] args)
			{
				if (!Enum.TryParse<EnumRfMatchTuneMode>((string)args[0], out var result2))
				{
					EV.PostWarningLog(base.Module, $"Argument {args[0]}not valid");
					return false;
				}
				SetTuneMode1(result2);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetLoad1", delegate(string function, object[] args)
			{
				SetLoad1((float)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetTune1", delegate(string function, object[] args)
			{
				SetTune1((float)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetTuneMode2", delegate(string function, object[] args)
			{
				if (!Enum.TryParse<EnumRfMatchTuneMode>((string)args[0], out var result))
				{
					EV.PostWarningLog(base.Module, $"Argument {args[0]}not valid");
					return false;
				}
				SetTuneMode2(result);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetLoad2", delegate(string function, object[] args)
			{
				SetLoad2((float)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetTune2", delegate(string function, object[] args)
			{
				SetTune2((float)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetActivePresetNo1", delegate(string function, object[] args)
			{
				SetActivePresetNo1((int)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetActivePresetNo2", delegate(string function, object[] args)
			{
				SetActivePresetNo2((int)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetPreSetsAndTrajectories1", delegate(string function, object[] args)
			{
				SetPreSetsAndTrajectories1((Presets)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetPreSetsAndTrajectories2", delegate(string function, object[] args)
			{
				SetPreSetsAndTrajectories2((Presets)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".EnablePreset1", delegate(string function, object[] args)
			{
				EnablePreset1((bool)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".EnablePreset2", delegate(string function, object[] args)
			{
				EnablePreset2((bool)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".EnableCapacitorMove1", delegate(string function, object[] args)
			{
				EnableCapacitorMove1((bool)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".EnableCapacitorMove2", delegate(string function, object[] args)
			{
				EnableCapacitorMove2((bool)args[0]);
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
			OP.Subscribe(base.Module + "." + base.Name + ".SetFineTuning", delegate(out string reason, int time, object[] param)
			{
				reason = string.Empty;
				SetFineTuning(Convert.ToSingle(param[0]), Convert.ToSingle(param[1]));
				return true;
			});
			return true;
		}

		public virtual void SetTolerance(float tuneWarning, float tuneAlarm, float loadWarning, float loadAlarm)
		{
			_currentTuneWarningRange = tuneWarning;
			_currentTuneAlarmRange = tuneAlarm;
			_toleranceTuneAlarmChecker.Reset(TuneAlarmTime);
			_toleranceTuneWarningChecker.Reset(TuneWarningTime);
			_currentLoadWarningRange = loadWarning;
			_currentLoadAlarmRange = loadAlarm;
			_toleranceLoadAlarmChecker.Reset(LoadAlarmTime);
			_toleranceLoadWarningChecker.Reset(LoadWarningTime);
		}

		public virtual void SetFineTuning(float tuneFineTuning, float loadFineTuning)
		{
			_currentTuneFineTuningValue = tuneFineTuning;
			_currentLoadFineTuningValue = loadFineTuning;
		}

		public virtual void CheckTolerance()
		{
			if (EnableAlarm && TuneMode1 == EnumRfMatchTuneMode.Auto)
			{
				if (TunePosition1Setpoint != 0f)
				{
					_toleranceTuneAlarmChecker.Monitor(TunePosition1, (double)TunePosition1Setpoint * (1.0 - TuneAlarmRange / 100.0), (double)TunePosition1Setpoint * (1.0 + TuneAlarmRange / 100.0), TuneAlarmTime);
					_toleranceTuneWarningChecker.Monitor(TunePosition1, (double)TunePosition1Setpoint * (1.0 - TuneWarningRange / 100.0), (double)TunePosition1Setpoint * (1.0 + TuneWarningRange / 100.0), TuneWarningTime);
				}
				if (LoadPosition1Setpoint != 0f)
				{
					_toleranceLoadAlarmChecker.Monitor(LoadPosition1, (double)LoadPosition1Setpoint * (1.0 - LoadAlarmRange / 100.0), (double)LoadPosition1Setpoint * (1.0 + LoadAlarmRange / 100.0), LoadAlarmTime);
					_toleranceLoadWarningChecker.Monitor(LoadPosition1, (double)LoadPosition1Setpoint * (1.0 - LoadWarningRange / 100.0), (double)LoadPosition1Setpoint * (1.0 + LoadWarningRange / 100.0), LoadWarningTime);
				}
			}
		}

		public virtual bool CheckTuneToleranceAlarm()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _toleranceTuneAlarmChecker.Result;
		}

		public virtual bool CheckLoadToleranceAlarm()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _toleranceLoadAlarmChecker.Result;
		}

		public virtual bool CheckTuneToleranceWarning()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _toleranceTuneWarningChecker.Result;
		}

		public virtual bool CheckLoadToleranceWarning()
		{
			if (!EnableAlarm)
			{
				return false;
			}
			return _toleranceLoadWarningChecker.Result;
		}

		public virtual void SetPreSetsAndTrajectories1(Presets presets)
		{
			throw new NotImplementedException();
		}

		public virtual void SetActivePresetNo2(int v)
		{
			throw new NotImplementedException();
		}

		public virtual void SetPreSetsAndTrajectories2(Presets presets)
		{
			throw new NotImplementedException();
		}

		public virtual void EnablePreset1(bool v)
		{
			throw new NotImplementedException();
		}

		public virtual void EnablePreset2(bool v)
		{
			throw new NotImplementedException();
		}

		public virtual void EnableCapacitorMove1(bool v)
		{
			throw new NotImplementedException();
		}

		public virtual void EnableCapacitorMove2(bool v)
		{
			throw new NotImplementedException();
		}

		public virtual void SetActivePresetNo1(int v)
		{
			throw new NotImplementedException();
		}

		public virtual void SetTuneMode1(EnumRfMatchTuneMode enumRfMatchTuneMode)
		{
		}

		public virtual void SetLoad1(float load)
		{
		}

		public virtual void SetTune1(float tune)
		{
		}

		public virtual void SetTuneMode2(EnumRfMatchTuneMode enumRfMatchTuneMode)
		{
		}

		public virtual void SetLoad2(float load)
		{
		}

		public virtual void SetTune2(float tune)
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
