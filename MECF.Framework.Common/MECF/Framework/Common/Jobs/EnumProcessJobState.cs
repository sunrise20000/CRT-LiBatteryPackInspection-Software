namespace MECF.Framework.Common.Jobs
{
	public enum EnumProcessJobState
	{
		Created = 0,
		Queued = 1,
		Canceled = 2,
		SetUp = 3,
		WaitingForStart = 4,
		Processing = 5,
		ProcessingComplete = 6,
		Pausing = 7,
		Paused = 8,
		Aborting = 9,
		Stopping = 10,
		Complete = 11
	}
}
