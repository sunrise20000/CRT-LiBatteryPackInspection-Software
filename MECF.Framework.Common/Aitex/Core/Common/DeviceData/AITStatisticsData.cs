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
	public class AITStatisticsData : INotifyPropertyChanged, IDeviceData
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
		public string LastPMTime { get; set; }

		[DataMember]
		public double TimeFromLastPM { get; set; }

		[DataMember]
		public double TimeTotal { get; set; }

		[DataMember]
		public double PMInterval { get; set; }

		public string TimeFromLastPMDisplay
		{
			get
			{
				TimeSpan timeSpan = TimeSpan.FromSeconds(TimeFromLastPM);
				return $"{timeSpan.Days} Days {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
			}
		}

		public string TimeTotalDisplay
		{
			get
			{
				TimeSpan timeSpan = TimeSpan.FromSeconds(TimeTotal);
				return $"{timeSpan.Days} Days {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
			}
		}

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

		public AITStatisticsData()
		{
			DisplayName = "Undefined";
		}

		public void Update(IDeviceData data)
		{
			throw new NotImplementedException();
		}
	}
}
