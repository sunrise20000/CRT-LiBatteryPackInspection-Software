namespace MECF.Framework.Common.Account
{
	public enum AuthorizeResult
	{
		None = -1,
		WrongPwd = 1,
		HasLogin = 2,
		NoMatchRole = 3,
		NoSession = 4,
		NoMatchUser = 5
	}
}
