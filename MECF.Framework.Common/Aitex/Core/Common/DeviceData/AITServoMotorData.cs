using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITServoMotorData : NotifiableItem, IDeviceData
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
		public bool IsServoOn { get; set; }

		[DataMember]
		public bool IsStopped { get; set; }

		[DataMember]
		public bool IsError { get; set; }

		[DataMember]
		public bool IsRunning { get; set; }

		[DataMember]
		public bool IsEmo { get; set; }

		[DataMember]
		public bool IsAuto { get; set; }

		[DataMember]
		public bool IsManual { get; set; }

		[DataMember]
		public bool IsServoNormal { get; set; }

		[DataMember]
		public bool IsServoNoWarning { get; set; }

		[DataMember]
		public bool IsServoNoAlarm { get; set; }

		[DataMember]
		public bool IsNop { get; set; }

		[DataMember]
		public bool IsPositionComplete { get; set; }

		[DataMember]
		public bool IsNLimit { get; set; }

		[DataMember]
		public bool IsPLimit { get; set; }

		[DataMember]
		public bool DiPosFeedBack1 { get; set; }

		[DataMember]
		public bool DiPosFeedBack2 { get; set; }

		[DataMember]
		public bool DiPosFeedBack3 { get; set; }

		[DataMember]
		public bool DiReady { get; set; }

		[DataMember]
		public bool DiOnTarget { get; set; }

		[DataMember]
		public bool DiOnError { get; set; }

		[DataMember]
		public bool DiOnLeftLimit { get; set; }

		[DataMember]
		public bool DiOnRightLimit { get; set; }

		[DataMember]
		public bool DiOnHomeSensor { get; set; }

		[DataMember]
		public bool DoStart { get; set; }

		[DataMember]
		public bool DoPos1 { get; set; }

		[DataMember]
		public bool DoPos2 { get; set; }

		[DataMember]
		public bool DoPos3 { get; set; }

		[DataMember]
		public bool DoHomeOn { get; set; }

		[DataMember]
		public bool DoFreeOn { get; set; }

		[DataMember]
		public bool DoStop { get; set; }

		[DataMember]
		public bool DoReset { get; set; }

		[DataMember]
		public bool DoJogFwd { get; set; }

		[DataMember]
		public bool DoJogRev { get; set; }

		[DataMember]
		public int ErrorCode { get; set; }

		[DataMember]
		public float CurrentPosition { get; set; }

		[DataMember]
		public float CurrentSpeed { get; set; }

		[DataMember]
		public string CurrentStatus { get; set; }

		[DataMember]
		public float JogSpeedSetPoint { get; set; }

		[DataMember]
		public float AutoSpeedSetPoint { get; set; }

		[DataMember]
		public float AccSpeedSetPoint { get; set; }

		[DataMember]
		public ServoState State { get; set; }

		public AITServoMotorData()
		{
			DisplayName = "Undefined Servo Motor";
		}

		public void Update(IDeviceData data)
		{
		}
	}
}
