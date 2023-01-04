using Aitex.Core.RT.DBCore;
using MECF.Framework.Common.DBCore;

namespace MECF.Framework.RT.Core
{
    public class WaferDataRecorderEx : WaferDataRecorder
    {
        public static void ChangeWaferId(string guid, string waferId)
        {
            DB.Insert(
                $"UPDATE \"wafer_data\" SET \"wafer_id\"='{waferId}' WHERE \"guid\"='{guid}';");
        }
    }
}
