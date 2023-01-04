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
	public class AITConfigData : INotifyPropertyChanged, IDeviceData
	{
		[DataMember]
		public string SystemType { get; set; }

		[DataMember]
		public string SectionName { get; set; }

		[DataMember]
		public string EntryName { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public string Description_zh { get; set; }

		[DataMember]
		public string Description_en { get; set; }

		[DataMember]
		public string Type { get; set; }

		[DataMember]
		public string Unit { get; set; }

		[DataMember]
		public string Value { get; set; }

		[DataMember]
		public string Default { get; set; }

		[DataMember]
		public string RangeLowLimit { get; set; }

		[DataMember]
		public string RangeUpLimit { get; set; }

		[DataMember]
		public string XPath { get; set; }

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

		public void Update(IDeviceData data)
		{
			throw new NotImplementedException();
		}
	}
}
