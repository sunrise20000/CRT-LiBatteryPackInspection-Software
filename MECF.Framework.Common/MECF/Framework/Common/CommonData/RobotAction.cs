using System;

namespace MECF.Framework.Common.CommonData
{
	[Serializable]
	public enum RobotAction
	{
		None = 0,
		Picking = 1,
		Placing = 2,
		Moving = 3,
		Extending = 4,
		Retracting = 5
	}
}
