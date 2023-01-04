using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SicPM.PmDevices
{
    public class DicMode
    {
        public enum FlowMode
        {
            Purge,
            Vent,
            Run,
            Close,
        }

        public enum ArH2Switch
        {
            Ar,
            H2,
        }

        public enum HeaterControlMode
        {
            Power,
            Pyro,
            Hold
        }
    }
}
