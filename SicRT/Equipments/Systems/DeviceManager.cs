using Aitex.Core.RT.Device;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Breakers.NSXCOM;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Servo.NAIS;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temps.AE;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temps.Omron;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MachineVision.Keyence;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.HwAligner;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temps.P116PIDTC;

namespace SicRT.Instances
{
    public class DeviceEntity : DeviceEntityT<DeviceManager>
    {
        public DeviceEntity()
        {
        }
    }

    public class DeviceManager : DeviceManagerBase
    {
        public DeviceManager()
        {
        }

        public override bool Initialize()
        {
            if (SC.GetConfigItem("AETemp.EnableDevice").BoolValue)
            {
                var aetemp = new AETemp("PM1", "AETemp", "AETemp");
                aetemp.Initialize();
                QueueDevice(aetemp);

            }

            if (SC.GetConfigItem("TempOmron.EnableDevice").BoolValue)
            {
                var Omron = new TempOmron("PM1", "TempOmron", "TempOmron");
                Omron.Initialize();
                QueueDevice(Omron);
            }

            if (SC.GetConfigItem("P116PIDTC.EnableDevice").BoolValue)
            {
                var p116 = new P116PIDTC("TM", "P116PIDTC", "P116PIDTC");
                p116.Initialize();
                QueueDevice(p116);
            }

            if (SC.GetConfigItem("NSXBreakerII.EnableDevice").BoolValue)
            {
                var NSX = new NSXBreakerII("TM", "NSXBreakerII", "NSXBreakerII");
                NSX.Initialize();
            }
            

            if (SC.GetConfigItem("ITAUPSB.EnableDevice").BoolValue)
            {
                var ITA2 = new ITAUPS("PM1", "ITAUPSB", "ITAUPSB", null, null);
                ITA2.Initialize();
                QueueDevice(ITA2);
            }

            if (SC.GetConfigItem("NAISServo.EnableDevice").BoolValue)
            {
                var Servo = new NAISServo("PM1", "NAISServo", "NAISServo");
                Servo.Initialize();
                QueueDevice(Servo);
            }


            if (SC.GetConfigItem("HiWinAligner.EnableDevice").BoolValue)
            {
                var HwAligner = new HwAlignerGuide("TM", "HiWinAligner", "HiWinAligner");
                HwAligner.Initialize();
                QueueDevice(HwAligner);
            }

            if (SC.GetConfigItem("KeyenceCVX300F.EnableDevice").BoolValue)
            {
                var KCVX = new KeyenceCVX300F("TM", "KeyenceCVX300F", "KeyenceCVX300F");
                KCVX.Initialize();
                QueueDevice(KCVX);
            }

            GetDevice<SignalTowerBase>("PM1.SignalTower").CustomSignalTower();

            return true;
        }

        protected override void QueueDevice(IDevice device)
        {
            QueueDevice($"{device.Module}.{device.Name}", device);
        }

    }
}