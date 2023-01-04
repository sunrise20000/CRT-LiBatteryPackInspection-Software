namespace MECF.Framework.Common.FAServices.E40s
{
	public enum ProcessJobState
	{
		NONE = 0,
		POOLED = 1,
		SETTING_UP = 2,
		WAITING_FOR_START = 3,
		PROCESSING = 4,
		PROCESS_COMPLETE = 5,
		STOPPING = 6,
		PAUSING = 7,
		PAUSED = 8,
		ABORTING = 9
	}
}
