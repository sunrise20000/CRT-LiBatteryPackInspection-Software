using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITGasSplitterData : NotifiableItem, IDeviceData
	{
		private string _title;

		[DataMember]
		public string Module { get; set; }

		[DataMember]
		public string UniqueName { get; set; }

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
		public string Range { get; set; }

		[DataMember]
		public double SetPoint { get; set; }

		[DataMember]
		public double FeedBack { get; set; }

		[DataMember]
		public double SwitchFeedBack { get; set; }

		[DataMember]
		public double DefaultValue { get; set; }

		[DataMember]
		public bool IsWarning { get; set; }

		[DataMember]
		public bool IsOffline { get; set; }

		[DataMember]
		public int Status { get; set; }

		[DataMember]
		public string ErroMessage { get; set; }

		[DataMember]
		public string Type { get; set; }

		[DataMember]
		public string FormatString { get; set; }

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

		public AITGasSplitterData()
		{
			DisplayName = "Undefined";
			Unit = "%";
			Type = "Splitter";
			DeviceSchematicId = "Undefined";
			UniqueName = "";
		}

		public void Update(IDeviceData data)
		{
			throw new NotImplementedException();
		}
	}
}
