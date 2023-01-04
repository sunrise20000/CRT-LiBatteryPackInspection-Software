using Aitex.Core.RT.Device;
using MECF.Framework.Common.Equipment;
using Mainframe.EFEMs.Routines;

namespace Mainframe.EFEMs
{
    public class WaferRobotModule : RobotModuleBase
    {

        public WaferRobotModule(ModuleName module) : base(module)
        {

        }

        protected override void InitRoutine()
        {
            HomeRoutine = new WaferRobotHomeRoutine();
            MapRoutine = new WaferRobotMapRoutine();
            PickRoutine = new WaferRobotPickRoutine();
            PlaceRoutine = new WaferRobotPlaceRoutine();

            base.InitRoutine();
        }

        protected override void InitDevice()
        {
            RobotDevice = DEVICE.GetDevice<SicWaferRobot>($"{ModuleName.WaferRobot}.{ModuleName.WaferRobot}");
            base.InitDevice();
        }
    }
}
