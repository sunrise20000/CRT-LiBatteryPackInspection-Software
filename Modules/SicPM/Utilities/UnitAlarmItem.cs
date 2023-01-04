using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SicPM.Utilities
{
    public class UnitAlarmItem
    {
        public string DI { get; set; }//IO name
        public string ID { get; set; }
        public string Description { get; set; }
        public string GlobalDescription_en { get; set; }
        public string GlobalDescription_zh { get; set; }
        public string Level { get; set; }
        public bool AlarmValue { get; set; }
    }
}
