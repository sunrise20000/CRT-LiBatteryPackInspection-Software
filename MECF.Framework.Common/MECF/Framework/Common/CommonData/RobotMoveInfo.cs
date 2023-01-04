using System;
using System.Runtime.Serialization;

namespace MECF.Framework.Common.CommonData
{
	[Serializable]
	[DataContract]
	public class RobotMoveInfo : NotifiableItem
	{
		private string bladeTarget;

		private RobotArm armTarget;

		private RobotAction action;

		[DataMember]
		public string BladeTarget
		{
			get
			{
				return bladeTarget;
			}
			set
			{
				bladeTarget = value;
				InvokePropertyChanged("BladeTarget");
			}
		}

		[DataMember]
		public RobotArm ArmTarget
		{
			get
			{
				return armTarget;
			}
			set
			{
				armTarget = value;
				InvokePropertyChanged("ArmTarget");
			}
		}

		[DataMember]
		public RobotAction Action
		{
			get
			{
				return action;
			}
			set
			{
				action = value;
				InvokePropertyChanged("Action");
			}
		}

		public override string ToString()
		{
			return $"{bladeTarget} - {action}";
		}
	}
}
