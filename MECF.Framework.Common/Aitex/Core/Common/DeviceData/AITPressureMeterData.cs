using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITPressureMeterData : NotifiableItem, IDeviceData
	{
		[DataMember]
		public double OpenDegree { get; set; }

		[DataMember]
		public double ActMode { get; set; }

		[DataMember]
		public double SetMode { get; set; }

		[DataMember]
		public string Module { get; set; }

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
		public double Precision { get; set; }

		[DataMember]
		public double DefaultValue { get; set; }

		[DataMember]
		public bool IsWarning { get; set; }

		[DataMember]
		public bool IsError { get; set; }

		[DataMember]
		public string ErroMessage { get; set; }

		[DataMember]
		public string Type { get; set; }

		[DataMember]
		public double Factor { get; set; }

		public string Display
		{
			get
			{
				string text = ((FeedBack > Precision && Precision > 1.0) ? Precision.ToString(FormatString) : FeedBack.ToString(FormatString));
				return DisplayWithUnit ? (text + " " + Unit) : text;
			}
		}

		[DataMember]
		public string FormatString { get; set; }

		[DataMember]
		public bool DisplayWithUnit { get; set; }

		public AITPressureMeterData()
		{
			DisplayName = "Undefined";
			Factor = 1.0;
			Unit = "";
			Type = "";
			Precision = double.MaxValue;
			FormatString = "F3";
		}

		public void Update(IDeviceData data)
		{
			throw new NotImplementedException();
		}
	}
}
