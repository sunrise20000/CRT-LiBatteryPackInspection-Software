using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITCylinderData : NotifiableItem, IDeviceData
	{
		[DataMember]
		public string Module { get; set; }

		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceSchematicId { get; set; }

		[DataMember]
		public bool OpenFeedback { get; set; }

		[DataMember]
		public bool CloseFeedback { get; set; }

		[DataMember]
		public bool OpenSetPoint { get; set; }

		[DataMember]
		public bool CloseSetPoint { get; set; }

		[DataMember]
		public bool IsLoop { get; set; }

		public string StringStatus
		{
			get
			{
				if (OpenFeedback && !CloseFeedback)
				{
					return CylinderState.Open.ToString();
				}
				if (!OpenFeedback && CloseFeedback)
				{
					return CylinderState.Close.ToString();
				}
				if (OpenFeedback && CloseFeedback)
				{
					return CylinderState.Error.ToString();
				}
				if (!OpenFeedback && !CloseFeedback)
				{
					return CylinderState.Unknown.ToString();
				}
				return "Unknown";
			}
		}

		public string StringSetPoint
		{
			get
			{
				if (OpenSetPoint && !CloseSetPoint)
				{
					return CylinderState.Open.ToString();
				}
				if (!OpenSetPoint && CloseSetPoint)
				{
					return CylinderState.Close.ToString();
				}
				if (OpenSetPoint && CloseSetPoint)
				{
					return CylinderState.Error.ToString();
				}
				if (!OpenSetPoint && !CloseSetPoint)
				{
					return CylinderState.Unknown.ToString();
				}
				return "Unknown";
			}
		}

		public AITCylinderData()
		{
			DisplayName = "Undefined Cylinder";
		}

		public void Update(IDeviceData data)
		{
		}
	}
}
