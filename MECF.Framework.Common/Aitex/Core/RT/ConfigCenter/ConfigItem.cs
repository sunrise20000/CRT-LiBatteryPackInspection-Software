namespace Aitex.Core.RT.ConfigCenter
{
	public class ConfigItem
	{
		public object this[string configName]
		{
			get
			{
				return CONFIG.Get(configName);
			}
			set
			{
				CONFIG.Set(configName, value);
			}
		}
	}
}
