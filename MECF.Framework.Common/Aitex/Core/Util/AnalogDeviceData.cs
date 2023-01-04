using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Util
{
	[Serializable]
	[DataContract]
	public class AnalogDeviceData
	{
		private double _factor = 1.0;

		[DataMember]
		public string TechnicalName { get; set; }

		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DeviceId { get; set; }

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
		public double Factor
		{
			get
			{
				return _factor;
			}
			set
			{
				_factor = value;
			}
		}

		public override string ToString()
		{
			return FeedBack.ToString();
		}

		public AnalogDeviceData()
		{
		}

		public AnalogDeviceData(AnalogDeviceData deviceData)
		{
			TechnicalName = deviceData.TechnicalName;
			DeviceName = deviceData.DeviceName;
			DeviceId = deviceData.DeviceId;
			Unit = deviceData.Unit;
			Description = deviceData.Description;
			Scale = deviceData.Scale;
			SetPoint = deviceData.SetPoint;
			FeedBack = deviceData.FeedBack;
			DefaultValue = deviceData.DefaultValue;
			IsWarning = deviceData.IsWarning;
			ErroMessage = deviceData.ErroMessage;
		}
	}
}
