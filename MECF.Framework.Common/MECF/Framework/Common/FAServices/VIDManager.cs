using Aitex.Common.Util;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.Util;
using MECF.Framework.Common.SCCore;

namespace MECF.Framework.Common.FAServices
{
	public class VIDManager : Singleton<VIDManager>
	{
		public void Initialize()
		{
			VIDGenerator vIDGenerator = new VIDGenerator("SVID", PathManager.GetCfgDir() + ".\\..\\..\\..\\config\\VIDs\\_SVID.xml");
			vIDGenerator.Initialize();
			vIDGenerator.GenerateId(Singleton<DataManager>.Instance.VidDataList);
			VIDGenerator vIDGenerator2 = new VIDGenerator("ECID", PathManager.GetCfgDir() + ".\\..\\..\\..\\config\\VIDs\\_ECID.xml");
			vIDGenerator2.Initialize();
			vIDGenerator2.GenerateId(Singleton<SystemConfigManager>.Instance.VidConfigList);
			VIDGenerator vIDGenerator3 = new VIDGenerator("CEID", PathManager.GetCfgDir() + ".\\..\\..\\..\\config\\VIDs\\_CEID.xml");
			vIDGenerator3.Initialize();
			vIDGenerator3.GenerateId(Singleton<EventManager>.Instance.VidEventList);
			VIDGenerator vIDGenerator4 = new VIDGenerator("ALID", PathManager.GetCfgDir() + ".\\..\\..\\..\\config\\VIDs\\_ALID.xml");
			vIDGenerator4.Initialize();
			vIDGenerator4.GenerateId(Singleton<EventManager>.Instance.VidAlarmList);
			VIDGenerator vIDGenerator5 = new VIDGenerator("DVID", PathManager.GetCfgDir() + ".\\..\\..\\..\\config\\VIDs\\_DVID.xml");
		}
	}
}
