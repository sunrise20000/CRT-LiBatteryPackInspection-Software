using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class MfcBase : BaseDevice, IDevice
	{
		private DeviceTimer rampTimer = new DeviceTimer();

		protected float rampTarget;

		private float rampInitValue;

		private int rampTime;

		private float _currentWarningRange;

		private float _currentAlarmRange;

		protected ToleranceChecker _toleranceAlarmChecker = new ToleranceChecker();

		protected ToleranceChecker _toleranceWarningChecker = new ToleranceChecker();

		protected SCConfigItem _scGasName;

		protected SCConfigItem _scEnable;

		protected SCConfigItem _scN2Scale;

		protected SCConfigItem _scN2MinimumFlow;

		protected SCConfigItem _scScaleFactor;

		protected SCConfigItem _scAlarmRange;

		protected SCConfigItem _scEnableAlarm;

		protected SCConfigItem _scAlarmTime;

		protected SCConfigItem _scWarningTime;

		protected SCConfigItem _scWarningRange;

		protected SCConfigItem _scDefaultSetPoint;

		protected SCConfigItem _scRegulationFactor;

		private SCConfigItem _scFineTuningEnable;

		private SCConfigItem _scFineTuningValue;

		private R_TRIG _trigOffline = new R_TRIG();

		protected double _currentFineTuningValue;

		protected string _uniqueName;

		public string Unit { get; set; }

		public virtual float Scale
		{
			get
			{
				if (_scN2Scale == null || _scScaleFactor == null)
				{
					return 100f;
				}
				return (float)(_scN2Scale.DoubleValue * _scScaleFactor.DoubleValue);
			}
			set
			{
			}
		}

		public virtual float MinimumFlow { get; set; }

		public virtual float SetPoint { get; set; }

		public virtual float FeedBack { get; set; }

		public bool IsOutOfTolerance => _toleranceAlarmChecker.Result;

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
					return (_scAlarmTime.IntValue != 0) ? ((double)_scAlarmTime.IntValue) : _scAlarmTime.DoubleValue;
				}
				return 10.0;
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

		public string DisplayName
		{
			get
			{
				if (_scGasName != null)
				{
					return _scGasName.StringValue;
				}
				return base.Display;
			}
		}

		public virtual AITMfcData DeviceData => new AITMfcData
		{
			UniqueName = _uniqueName,
			Type = "MFC",
			DeviceName = base.Name,
			DeviceSchematicId = base.DeviceID,
			DisplayName = DisplayName,
			FeedBack = ((SetPoint == 0f) ? 0f : FeedBack),
			SetPoint = SetPoint,
			Scale = Scale
		};

		public MfcBase()
		{
		}

		public MfcBase(string module, XmlElement node, string ioModule = "")
		{
			Unit = node.GetAttribute("unit");
			base.Module = (string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module"));
			base.Name = node.GetAttribute("id");
			base.Display = node.GetAttribute("display");
			base.DeviceID = node.GetAttribute("schematicId");
			_scGasName = ParseScNode("scGasName", node, ioModule, base.Module + "." + base.Name + ".GasName");
			_scEnable = ParseScNode("scEnable", node);
			_scN2Scale = ParseScNode("scN2Scale", node, ioModule, base.Module + "." + base.Name + ".N2Scale");
			_scScaleFactor = ParseScNode("scScaleFactor", node, ioModule, base.Module + "." + base.Name + ".ScaleFactor");
			_scAlarmRange = ParseScNode("scAlarmRange", node, ioModule, base.Module + "." + base.Name + ".AlarmRange");
			_scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, base.Module + "." + base.Name + ".EnableAlarm");
			_scAlarmTime = ParseScNode("scAlarmTime", node, ioModule, base.Module + "." + base.Name + ".AlarmTime");
			_scWarningTime = ParseScNode("scWarningTime", node, ioModule, base.Module + "." + base.DeviceID + ".WarningTime");
			_scWarningRange = ParseScNode("scWarningRange", node, ioModule, base.Module + "." + base.DeviceID + ".WarningRange");
			_scFineTuningValue = ParseScNode("scFineTuningValue", node, ioModule, base.Module + ".FineTuning." + base.Name);
			_scFineTuningEnable = ParseScNode("scFineTuningEnable", node, ioModule, base.Module + ".FineTuning.IsEnable");
			_scDefaultSetPoint = ParseScNode("scDefaultSetPoint", node);
			_scRegulationFactor = ParseScNode("scFlowRegulationFactor", node, ioModule, base.Module + "." + base.Name + ".RegulationFactor");
			_uniqueName = base.Module + "." + base.Name;
		}

		public virtual bool Initialize()
		{
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			DATA.Subscribe(base.Module + "." + base.Name + ".FlowFeedback", () => FeedBack);
			DATA.Subscribe(base.Module + "." + base.Name + ".FlowSetPoint", () => SetPoint);
			OP.Subscribe(base.Module + "." + base.Name + ".SetMfcFlow", delegate(string function, object[] args)
			{
				SetMfcFlow(Convert.ToSingle(args[0]));
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".Ramp", delegate(string function, object[] args)
			{
				SetMfcFlow(Convert.ToSingle(args[0]));
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetFineTuning", delegate(out string reason, int time, object[] param)
			{
				reason = string.Empty;
				SetFineTuning(Convert.ToSingle(param[0]));
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
			InitSc();
			return true;
		}

		public virtual void InitSc()
		{
			_scGasName = SC.GetConfigItem(base.ScBasePath + "." + base.Name + ".GasName");
			_scEnable = SC.GetConfigItem(base.ScBasePath + "." + base.Name + ".Enable");
			_scN2Scale = SC.GetConfigItem(base.ScBasePath + "." + base.Name + ".N2Scale");
			_scScaleFactor = SC.GetConfigItem(base.ScBasePath + "." + base.Name + ".ScaleFactor");
			_scEnableAlarm = SC.GetConfigItem(base.ScBasePath + "." + base.Name + ".EnableAlarm");
			_scAlarmRange = SC.GetConfigItem(base.ScBasePath + "." + base.Name + ".AlarmRange");
			_scAlarmTime = SC.GetConfigItem(base.ScBasePath + "." + base.Name + ".AlarmTime");
			_scWarningRange = SC.GetConfigItem(base.ScBasePath + "." + base.Name + ".WarningRange");
			_scWarningTime = SC.GetConfigItem(base.ScBasePath + "." + base.Name + ".WarningTime");
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
			if (EnableAlarm && SetPoint != 0f)
			{
				_toleranceAlarmChecker.Monitor(FeedBack, (double)SetPoint * (1.0 - AlarmRange / 100.0), (double)SetPoint * (1.0 + AlarmRange / 100.0), AlarmTime);
				_toleranceWarningChecker.Monitor(FeedBack, (double)SetPoint * (1.0 - WarningRange / 100.0), (double)SetPoint * (1.0 + WarningRange / 100.0), WarningTime);
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

		public virtual void SetFineTuning(float fineTuning)
		{
			_currentFineTuningValue = fineTuning;
		}

		public virtual void SetMfcFlow(float flow)
		{
		}

		public virtual bool SetMfcFlow(int time, float flow, out string reason)
		{
			reason = "";
			return true;
		}

		private bool InvokeRamp(string method, object[] args)
		{
			float val = Convert.ToSingle(args[0].ToString());
			val = Math.Min(val, Scale);
			val = Math.Max(val, 0f);
			int num = 0;
			if (args.Length >= 2)
			{
				num = Convert.ToInt32(args[1].ToString());
			}
			Ramp(val, num);
			EV.PostInfoLog(base.Module, $"{_uniqueName} ramp to {val}{Unit} in {num} seconds");
			return true;
		}

		public virtual void Monitor()
		{
			MonitorRamping();
			MonitorTolerance();
		}

		public virtual void Reset()
		{
			_toleranceAlarmChecker.Reset(AlarmTime);
			_toleranceWarningChecker.Reset(WarningTime);
			_trigOffline.RST = true;
		}

		public virtual void Terminate()
		{
			Ramp(0f, 0);
		}

		public void Ramp(int time)
		{
			Ramp(0f, time);
		}

		public void Ramp(float target, int time)
		{
			target = Math.Max(0f, target);
			target = Math.Min(Scale, target);
			rampInitValue = SetPoint;
			rampTime = time;
			rampTarget = target;
			rampTimer.Start(rampTime);
		}

		public void StopRamp()
		{
			Ramp(SetPoint, 0);
		}

		private void MonitorRamping()
		{
			if (rampTimer.IsTimeout() || rampTime == 0)
			{
				SetPoint = rampTarget;
			}
			else
			{
				SetPoint = (float)((double)rampInitValue + (double)(rampTarget - rampInitValue) * rampTimer.GetElapseTime() / (double)rampTime);
			}
		}

		protected virtual void MonitorTolerance()
		{
			CheckTolerance();
		}
	}
}
