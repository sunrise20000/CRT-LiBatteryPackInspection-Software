namespace MECF.Framework.Common.FAServices.E40s
{
	public enum PJOperation
	{
		ABORT = 0,
		CANCEL = 1,
		PAUSE = 2,
		RESUME = 3,
		START_PROCESS = 4,
		STOP = 5,
		CREATEENH = 6,
		DUPLICATE_CREATE = 7,
		MULTI_CREATE = 8,
		DEQUEUE = 9,
		PR_GETALLJOBS = 10,
		PR_GETSPACE = 11,
		PR_SET_RECIPE_VARIABLE = 12,
		PR_SET_START_METHOD = 13
	}
}
