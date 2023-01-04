using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.PMs;

namespace SicPM
{
    public partial class PMModule
    {

        private void InitInterlock()
        {
            OP.AddCheck($"{Module}.{Name}.Disconnect", new CheckPlcConnect(this));

            //OP.AddCheck($"{Module}.{Name}.Pump", new CheckPump(this));

        }

        private class CheckPlcConnect : IInterlockChecker
        {
            private PMModule _pm;
            public CheckPlcConnect(PMModule pm)
            {
                _pm = pm;
            }

            public bool CanDo(out string reason, object[] args)
            {
                if (_pm.IsBusy)
                {
                    reason = $"{_pm.Module} is in {_pm.StringFsmStatus} status, can not disconnect, should be idle";
                    return false;
                }

                reason = string.Empty;
                return true;
            }
        }
 
    }
}
