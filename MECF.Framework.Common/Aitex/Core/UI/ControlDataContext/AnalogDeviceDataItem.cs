using System;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
	[Serializable]
	[DataContract]
	public class AnalogDeviceDataItem
	{
		private double _factor = 1.0;

		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

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
		public string Type { get; set; }

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

		[DataMember]
		public string FormatString { get; set; }

		public AnalogDeviceDataItem()
		{
			DisplayName = "未定义设备";
			FormatString = "F1";
		}
	}
}
