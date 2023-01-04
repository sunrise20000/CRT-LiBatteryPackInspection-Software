using Aitex.Core.RT.Device;
using MECF.Framework.Common.Equipment;
using Mainframe.EFEMs.Routines;

namespace Mainframe.EFEMs
{
    public class TrayRobotModule : RobotModuleBase
    {
        public TrayRobotModule(ModuleName module) : base(module)
        {
            
        }

        protected override void InitRoutine()
        {
            HomeRoutine = new TrayRobotHomeRoutine();
            MapRoutine = new TrayRobotMapRoutine();
            PickRoutine = new TrayRobotPickRoutine();
            PlaceRoutine = new TrayRobotPlaceRoutine();
            
            base.InitRoutine();
        }

        protected override void InitDevice()
        {
            RobotDevice = DEVICE.GetDevice<SicTrayRobot>($"{ModuleName.TrayRobot}.{ModuleName.TrayRobot}");
            base.InitDevice();
        }
    }
}
