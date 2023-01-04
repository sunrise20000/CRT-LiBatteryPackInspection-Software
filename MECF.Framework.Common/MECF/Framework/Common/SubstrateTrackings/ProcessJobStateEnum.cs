namespace MECF.Framework.Common.SubstrateTrackings
{
	public enum ProcessJobStateEnum
	{
		pjCREATED = -1,
		pjQUEUED = 0,
		pjSETTING_UP = 1,
		pjWAITING_FOR_START = 2,
		pjPROCESSING = 3,
		pjPROCESS_COMPLETED = 4,
		pjRESERVED5 = 5,
		pjPAUSING = 6,
		pjPAUSED = 7,
		pjSTOPPING = 8,
		pjABORTING = 9,
		pjSTOPPED = 10,
		pjABORTED = 11,
		pjPROCESSJOB_COMPLETED = 12
	}
}
