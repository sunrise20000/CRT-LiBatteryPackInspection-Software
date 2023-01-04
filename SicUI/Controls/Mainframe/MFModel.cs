using Aitex.Core.Common;
using Aitex.Core.UI.MVVM;
using Aitex.Sorter.Common;
using SicUI.Controls.Common;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SicUI.Client;

namespace SicUI.Controls.Parts
{
	public class MFModel : ViewModelControl
	{
		//public FoupDoorState PM1DoorState
		//{
		//	get;
		//	set;
		//}

		//public FoupDoorState PM2DoorState
		//{
		//	get;
		//	set;
		//}

		//public FoupDoorState PM3DoorState
		//{
		//	get;
		//	set;
		//}

		//public FoupDoorState LL1ATMDoorState
		//{
		//	get;
		//	set;
		//}

		//public FoupDoorState LL1VTMDoorState
		//{
		//	get;
		//	set;
		//}

        //public WaferInfo PM1Wafer
        //{
        //    get;
        //    set;
        //}

        //public WaferInfo PM2Wafer
        //{
        //    get;
        //    set;
        //}

        //public WaferInfo PM3Wafer
        //{
        //    get;
        //    set;
        //}

        //public WaferInfo LL1Wafer
        //{
        //    get;
        //    set;
        //}

        //public WaferInfo[] RobotWafers
        //{
        //    get;
        //    set;
        //}


        // PM1.A LL1.A PM2.B
        public ModuleName TMRobotBladeTarget
		{
			get;
			set;
		}

		public string ArmAExtended
		{
			get;
			set;
		}

		public string ArmBExtended
		{
			get;
			set;
		}

		public ICommand DeviceOperationCommand
		{
			get;
			private set;
		}
     
        public MFModel()
		{
			DeviceOperationCommand = new DelegateCommand<object>(DeviceOperation);

      //      if (ModuleManager.ModuleInfos.ContainsKey("PM1"))
      //          PM1Wafer = ModuleManager.ModuleInfos["PM1"].WaferManager.AitexWafers[0];

		    //if (ModuleManager.ModuleInfos.ContainsKey("PM2"))
		    //    PM2Wafer = ModuleManager.ModuleInfos["PM2"].WaferManager.AitexWafers[0];

		    //if (ModuleManager.ModuleInfos.ContainsKey("PM3"))
      //          PM3Wafer = ModuleManager.ModuleInfos["PM3"].WaferManager.AitexWafers[0];

		    //if (ModuleManager.ModuleInfos.ContainsKey("TMRobot"))
      //          RobotWafers = ModuleManager.ModuleInfos["TMRobot"].WaferManager.AitexWafers;
        }

        void DeviceOperation(object param)
		{
			//InvokeClient.Instance.Service.DoOperation(OperationName.DeviceOperation.ToString(), (object[])param);
		}
	}
}
