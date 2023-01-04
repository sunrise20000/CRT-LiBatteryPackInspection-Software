using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITVaporLineData : INotifyPropertyChanged, IDeviceData
	{
		private string _title;

		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceSchematicId { get; set; }

		[DataMember]
		public string Unit { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public double Scale { get; set; }

		[DataMember]
		public double SetPoint { get; set; }

		[DataMember]
		public double FeedBack { get; set; }

		[DataMember]
		public double DefaultValue { get; set; }

		[DataMember]
		public bool IsWarning { get; set; }

		[DataMember]
		public string ErroMessage { get; set; }

		[DataMember]
		public string Type { get; set; }

		[DataMember]
		public double Factor { get; set; }

		[DataMember]
		public string DisplayTitle
		{
			get
			{
				return $"{DeviceSchematicId}({DisplayName})";
			}
			set
			{
				_title = value;
			}
		}

		[DataMember]
		public bool HasMFC { get; set; }

		public Visibility MFCVisibility => (!HasMFC) ? Visibility.Hidden : Visibility.Visible;

		[DataMember]
		public AITValveData SupplyValveData { get; set; }

		[DataMember]
		public AITValveData RunValveData { get; set; }

		[DataMember]
		public AITValveData BypassValveData { get; set; }

		[DataMember]
		public AITValveData FeedValveData { get; set; }

		[DataMember]
		public AITMfcData SupplyMFCData { get; set; }

		[DataMember]
		public AITPressureMeterData PressureMeterData { get; set; }

		[DataMember]
		public AITSensorData HighLevelSensordata { get; set; }

		[DataMember]
		public AITSensorData MiddleLevelSensordata { get; set; }

		[DataMember]
		public AITSensorData LowLevelSensordata { get; set; }

		[DataMember]
		public AITHeaterData HeaterData { get; set; }

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

		public AITVaporLineData()
		{
			DisplayName = "Undefined";
			Factor = 1.0;
			Unit = "";
			Type = "Vapor";
		}

		public void Update(IDeviceData data)
		{
			throw new NotImplementedException();
		}
	}
}
