using Aitex.Common.Util;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SicPM.Utilities
{
    public static class XmlConfig
    {
        
        public static Dictionary<string, UnitAlarmItem> LoadUnitAlarmDefineConfig(string moduleName, string target)
        {
            var path = PathManager.GetCfgDir() + $"UnitAlarmDefine/{moduleName}UnitAlarmDefine.xml";
            var spinUnitAlarmDic = new Dictionary<string, UnitAlarmItem>();
            var doc = XDocument.Load(path);

            var items = doc.Root.Elements();
            if (items == null)
                return null;

            items.ToList().ForEach(x => spinUnitAlarmDic[$"{target}.{x.Attribute("DI").Value}"] = new UnitAlarmItem()
            {
                DI = x.Attribute("DI").Value,
                ID = x.Attribute("ID").Value,
                Description = x.Attribute("Description").Value,
                GlobalDescription_en = x.Attribute("GlobalDescription_en").Value,
                GlobalDescription_zh = x.Attribute("GlobalDescription_zh").Value,
                Level = x.Attribute("Level").Value,
                AlarmValue = x.Attribute("AlarmValue").Value == "1"?true:false,
            });

            return spinUnitAlarmDic;
        }

        
    }
}
