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
	public class AITSignalTowerData : INotifyPropertyChanged, IDeviceData
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceSchematicId { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public bool IsRedLightOn { get; set; }

		[DataMember]
		public bool IsYellowLightOn { get; set; }

		[DataMember]
		public bool IsGreenLightOn { get; set; }

		[DataMember]
		public bool IsBlueLightOn { get; set; }

		[DataMember]
		public bool IsWhiteLightOn { get; set; }

		[DataMember]
		public bool IsBuzzerOn { get; set; }

		[DataMember]
		public bool IsBuzzer1On { get; set; }

		[DataMember]
		public bool IsBuzzer2On { get; set; }

		[DataMember]
		public bool IsBuzzer3On { get; set; }

		[DataMember]
		public bool IsBuzzer4On { get; set; }

		[DataMember]
		public bool IsBuzzer5On { get; set; }

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

		public AITSignalTowerData()
		{
			DisplayName = "Undefined";
		}

		public void Update(IDeviceData data)
		{
			throw new NotImplementedException();
		}
	}
}
