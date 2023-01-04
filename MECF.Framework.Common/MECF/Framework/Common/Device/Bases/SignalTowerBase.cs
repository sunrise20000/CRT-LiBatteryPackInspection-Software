using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Aitex.Common.Util;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class SignalTowerBase : BaseDevice, IDevice
	{
		private Dictionary<string, Dictionary<LightType, TowerLightStatus>> _config = new Dictionary<string, Dictionary<LightType, TowerLightStatus>>();

		private Dictionary<LightType, SignalLightBase> _lights = new Dictionary<LightType, SignalLightBase>();

		private bool _isBuzzerOff;

		public bool IsAutoSetLight { get; set; }

		public AITSignalTowerData DeviceData => new AITSignalTowerData
		{
			DeviceName = base.Name,
			DeviceSchematicId = base.DeviceID,
			DisplayName = base.Display,
			IsGreenLightOn = (_lights.ContainsKey(LightType.Green) && _lights[LightType.Green] != null && _lights[LightType.Green].Value),
			IsRedLightOn = (_lights.ContainsKey(LightType.Red) && _lights[LightType.Red] != null && _lights[LightType.Red].Value),
			IsYellowLightOn = (_lights.ContainsKey(LightType.Yellow) && _lights[LightType.Yellow] != null && _lights[LightType.Yellow].Value),
			IsWhiteLightOn = (_lights.ContainsKey(LightType.White) && _lights[LightType.White] != null && _lights[LightType.White].Value),
			IsBlueLightOn = (_lights.ContainsKey(LightType.Blue) && _lights[LightType.Blue] != null && _lights[LightType.Blue].Value),
			IsBuzzerOn = (_lights.ContainsKey(LightType.Buzzer) && _lights[LightType.Buzzer] != null && _lights[LightType.Buzzer].Value),
			IsBuzzer1On = (_lights.ContainsKey(LightType.Buzzer1) && _lights[LightType.Buzzer1] != null && _lights[LightType.Buzzer1].Value),
			IsBuzzer2On = (_lights.ContainsKey(LightType.Buzzer2) && _lights[LightType.Buzzer2] != null && _lights[LightType.Buzzer2].Value),
			IsBuzzer3On = (_lights.ContainsKey(LightType.Buzzer3) && _lights[LightType.Buzzer3] != null && _lights[LightType.Buzzer3].Value),
			IsBuzzer4On = (_lights.ContainsKey(LightType.Buzzer4) && _lights[LightType.Buzzer4] != null && _lights[LightType.Buzzer4].Value),
			IsBuzzer5On = (_lights.ContainsKey(LightType.Buzzer5) && _lights[LightType.Buzzer5] != null && _lights[LightType.Buzzer5].Value)
		};

		public SignalTowerBase()
		{
			IsAutoSetLight = true;
		}

		public SignalTowerBase(string module, string name)
			: base(module, name, name, name)
		{
			IsAutoSetLight = true;
		}

		public virtual bool Initialize()
		{
			OP.Subscribe($"{base.Module}.{base.Name}.{AITSignalTowerOperation.SwitchOffBuzzer}", SwitchOffBuzzer);
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			return true;
		}

		public void SwitchOffBuzzer(bool isOff)
		{
			_isBuzzerOff = isOff;
		}

		private bool SwitchOffBuzzer(string arg1, object[] arg2)
		{
			_isBuzzerOff = true;
			return true;
		}

		public void Monitor()
		{
			try
			{
				if (IsAutoSetLight)
				{
					MonitorSignalTowerAuto();
				}
				foreach (KeyValuePair<LightType, SignalLightBase> light in _lights)
				{
					if (light.Value != null)
					{
						light.Value.Monitor();
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		public void Terminate()
		{
		}

		public void Reset()
		{
			foreach (KeyValuePair<LightType, SignalLightBase> light in _lights)
			{
				if (light.Value != null)
				{
					light.Value.Reset();
				}
			}
			_isBuzzerOff = false;
		}

		public abstract SignalLightBase CreateLight(LightType type);

		public bool CustomSignalTower()
		{
			string text = PathManager.GetCfgDir() + "_SignalTower.xml";
			if (!File.Exists(text))
			{
				text = PathManager.GetCfgDir() + "SignalTower.xml";
			}
			return CustomSignalTower(text);
		}

		public bool CustomSignalTower(string configPathFile)
		{
			try
			{
				if (!File.Exists(configPathFile))
				{
					MessageBox.Show("Signal tower config file not exist, " + configPathFile, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
					return false;
				}
				foreach (int value in Enum.GetValues(typeof(LightType)))
				{
					_lights[(LightType)value] = CreateLight((LightType)value);
				}
				STEvents sTEvents = CustomXmlSerializer.Deserialize<STEvents>(new FileInfo(configPathFile));
				List<LightType> list = _lights.Keys.ToList();
				foreach (STEvent @event in sTEvents.Events)
				{
					if (!_config.ContainsKey(@event.Name))
					{
						_config[@event.Name] = new Dictionary<LightType, TowerLightStatus>();
					}
					foreach (LightType key in _lights.Keys)
					{
						PropertyInfo[] properties = @event.GetType().GetProperties();
						PropertyInfo[] array = properties;
						foreach (PropertyInfo propertyInfo in array)
						{
							if (!(propertyInfo.Name.ToLower() == key.ToString().ToLower()))
							{
								continue;
							}
							string text = (string)propertyInfo.GetValue(@event);
							if (!string.IsNullOrEmpty(text))
							{
								text = text.ToLower();
								if (text == "on")
								{
									_config[@event.Name][key] = TowerLightStatus.On;
								}
								else if (text == "off")
								{
									_config[@event.Name][key] = TowerLightStatus.Off;
								}
								if (text.Contains("blink"))
								{
									_config[@event.Name][key] = TowerLightStatus.Blinking;
								}
								if (list.Contains(key))
								{
									list.Remove(key);
								}
							}
						}
					}
				}
				foreach (LightType item in list)
				{
					_lights.Remove(item);
				}
			}
			catch (Exception ex)
			{
				EV.PostWarningLog(base.Module, "Signal tower config file invalid, " + ex.Message);
				return false;
			}
			return true;
		}

		public void MonitorSignalTowerAuto()
		{
			Dictionary<LightType, TowerLightStatus> dictionary = new Dictionary<LightType, TowerLightStatus>();
			foreach (int value in Enum.GetValues(typeof(LightType)))
			{
				dictionary[(LightType)value] = TowerLightStatus.Off;
			}
			foreach (KeyValuePair<string, Dictionary<LightType, TowerLightStatus>> item in _config)
			{
				object obj = DATA.Poll(item.Key);
				if (obj == null || !(obj is bool) || !(bool)obj)
				{
					continue;
				}
				foreach (LightType key in item.Value.Keys)
				{
					if (IsBuzzer(key) && _isBuzzerOff)
					{
						dictionary[key] = TowerLightStatus.Off;
					}
					else
					{
						dictionary[key] = MergeCondition(dictionary[key], item.Value[key]);
					}
				}
				if (dictionary.ContainsKey(LightType.Red) && (dictionary[LightType.Red] == TowerLightStatus.Blinking || dictionary[LightType.Red] == TowerLightStatus.On))
				{
					if (dictionary.ContainsKey(LightType.Green))
					{
						dictionary[LightType.Green] = TowerLightStatus.Off;
					}
					if (dictionary.ContainsKey(LightType.Yellow))
					{
						dictionary[LightType.Yellow] = TowerLightStatus.Off;
					}
					if (dictionary.ContainsKey(LightType.Blue))
					{
						dictionary[LightType.Blue] = TowerLightStatus.Off;
					}
				}
			}
			foreach (KeyValuePair<LightType, SignalLightBase> light in _lights)
			{
				if (light.Value != null)
				{
					light.Value.StateSetPoint = dictionary[light.Key];
				}
			}
		}

		private bool IsBuzzer(LightType type)
		{
			return type == LightType.Buzzer || type == LightType.Buzzer1 || type == LightType.Buzzer2 || type == LightType.Buzzer3 || type == LightType.Buzzer4 || type == LightType.Buzzer5;
		}

		private TowerLightStatus MergeCondition(TowerLightStatus newValue, TowerLightStatus oldValue)
		{
			if (newValue == TowerLightStatus.Blinking || oldValue == TowerLightStatus.Blinking)
			{
				return TowerLightStatus.Blinking;
			}
			if (newValue == TowerLightStatus.On || oldValue == TowerLightStatus.On)
			{
				return TowerLightStatus.On;
			}
			return TowerLightStatus.Off;
		}
	}
}
