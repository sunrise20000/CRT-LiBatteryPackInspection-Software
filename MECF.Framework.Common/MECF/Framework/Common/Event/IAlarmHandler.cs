namespace MECF.Framework.Common.Event
{
	public interface IAlarmHandler
	{
		void AlarmStateChanged(AlarmEventItem item);
	}
}
