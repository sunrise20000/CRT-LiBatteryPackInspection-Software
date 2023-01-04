using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.Event
{
	internal class EventLogWriter
	{
		public void WriteEvent(EventItem ev)
		{
			if (ev.Description == null)
			{
				ev.Description = "";
			}
			if (ev.Level == EventLevel.Alarm)
			{
				LOG.Error(ev.Description.Replace("'", "''"));
			}
			else if (ev.Level == EventLevel.Warning)
			{
				LOG.Warning(ev.Description.Replace("'", "''"));
			}
			else
			{
				LOG.Info(ev.Description.Replace("'", "''"), isTraceOn: false);
			}
		}
	}
}
