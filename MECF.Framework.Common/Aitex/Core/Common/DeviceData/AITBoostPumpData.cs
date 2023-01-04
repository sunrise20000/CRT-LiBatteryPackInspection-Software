using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITBoostPumpData : INotifyPropertyChanged, IDeviceData
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceSchematicId { get; set; }

		[DataMember]
		public bool IsRunning { get; set; }

		[DataMember]
		public bool IsError { get; set; }

		[DataMember]
		public bool EnableFrequency { get; set; }

		[DataMember]
		public double EnableSetPoint { get; set; }

		[DataMember]
		public double PressureSetPoint { get; set; }

		[DataMember]
		public double PressureSetPointMax { get; set; }

		[DataMember]
		public double InverterFrequency { get; set; }

		public string PressureSetPointDisplay => ((int)PressureSetPoint).ToString();

		public string FrequencyDisplay => EnableFrequency ? $"{(int)InverterFrequency}%" : "";

		public string InverterFrequencyDisplay => InverterFrequency.ToString("F1");

		public event PropertyChangedEventHandler PropertyChanged;

		public void InvokePropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public void InvokePropertyChanged()
		{
			PropertyInfo[] properties = GetType().GetProperties();
			PropertyInfo[] array = properties;
			foreach (PropertyInfo propertyInfo in array)
			{
				InvokePropertyChanged(propertyInfo.Name);
				if (propertyInfo.PropertyType == typeof(ICommand) && propertyInfo.GetValue(this, null) is DelegateCommand<string> delegateCommand)
				{
					delegateCommand.RaiseCanExecuteChanged();
				}
			}
			FieldInfo[] fields = GetType().GetFields();
			FieldInfo[] array2 = fields;
			foreach (FieldInfo fieldInfo in array2)
			{
				InvokePropertyChanged(fieldInfo.Name);
				if (fieldInfo.FieldType == typeof(ICommand) && fieldInfo.GetValue(this) is DelegateCommand<string> delegateCommand2)
				{
					delegateCommand2.RaiseCanExecuteChanged();
				}
			}
		}

		public AITBoostPumpData()
		{
			DisplayName = "未定义";
		}

		public void Update(IDeviceData data)
		{
			if (data is AITBoostPumpData)
			{
				InvokePropertyChanged();
			}
		}
	}
}
