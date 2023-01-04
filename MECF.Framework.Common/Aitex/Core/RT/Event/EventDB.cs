namespace Aitex.Core.RT.Event
{
	public class EventDB
	{
		private const string Delete_Event = "Delete from \"EventManager\" where \"OccurTime\" < '{0}' ";
	}
}
