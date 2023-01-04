using System;
using System.Windows;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Caliburn.Micro;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.Ctrlib.Controls;

namespace SicUI.Models
{
    public class SicModuleUIViewModelBase : ModuleUiViewModelBase
    {
        public ModuleInfo CassAL
        {
            get
            {
                if (ModuleManager.ModuleInfos["CassAL"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["CassAL"];
                return null;
            }
        }
        public ModuleInfo CassAR
        {
            get
            {
                if (ModuleManager.ModuleInfos["CassAR"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["CassAR"];
                return null;
            }
        }
        public ModuleInfo CassBL
        {
            get
            {
                if (ModuleManager.ModuleInfos["CassBL"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["CassBL"];
                return null;
            }
        }

        public ModuleInfo Buffer 
        {
            get
            {
                if (ModuleManager.ModuleInfos["Buffer"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["Buffer"];
                return null;
            }
        }


        public ModuleInfo Aligner { get; set; }
        public ModuleInfo TMRobot { get; set; }
        public ModuleInfo WaferRobot { get; set; }
        public ModuleInfo TrayRobot { get; set; }        
        public ModuleInfo LoadLock { get; set; }
        public ModuleInfo UnLoad { get; set; }
        public ModuleInfo PM1 { get; set; }
        public ModuleInfo PM2 { get; set; }


        #region Wafer info for machine


        public WaferInfo BufferWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["Buffer"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["Buffer"].WaferManager.Wafers[0];
                return null;
            }
        }

        public WaferInfo PM1Wafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["PM1"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["PM1"].WaferManager.Wafers[0];
                return null;
            }
        }
        public WaferInfo PM2Wafer
        {
            get
            {
                //if (ModuleManager.ModuleInfos["PM2"].WaferManager.Wafers.Count > 0)
                //    return ModuleManager.ModuleInfos["PM2"].WaferManager.Wafers[0];
                return null;
            }
        }

        public WaferInfo Wafer1
        {
            get
            {
                if (ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers[0];
                return null;
            }
        }
        public WaferInfo TrayRobotWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["TrayRobot"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["TrayRobot"].WaferManager.Wafers[0];
                return null;
            }
        }
        public WaferInfo WaferRobotWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["WaferRobot"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["WaferRobot"].WaferManager.Wafers[0];
                return null;
            }
        }

        #endregion

        #region Tray Visble
        public Visibility TrayRobotHaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["TrayRobot"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["TrayRobot"].WaferManager.Wafers[0].WaferTrayStatus > 0)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility TMRobotHaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers[0].WaferTrayStatus > 0)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility LoadLockHaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["LoadLock"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["LoadLock"].WaferManager.Wafers[0].WaferTrayStatus > 0)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility UnLoadHaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["UnLoad"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["UnLoad"].WaferManager.Wafers[0].WaferTrayStatus > 0)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility PM1HaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["PM1"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["PM1"].WaferManager.Wafers[0].WaferTrayStatus > 0)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility PM2HaveTray
        {
            get
            {
                //if (ModuleManager.ModuleInfos["PM2"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["PM2"].WaferManager.Wafers[0].WaferTrayStatus > 0)
                //{
                //    return Visibility.Visible;
                //}
                return Visibility.Hidden;
            }
        }
        #endregion


        protected void InitPM()
        {
            TMRobot = ModuleManager.ModuleInfos["TMRobot"];
            PM1 = ModuleManager.ModuleInfos["PM1"];
            //PM2 = ModuleManager.ModuleInfos["PM2"];
        }

    }

    public class SicUIViewModelBase : UiViewModelBase
    {
        public string SystemName { get; set; }

        public ModuleInfo CassAL
        {
            get
            {
                if (ModuleManager.ModuleInfos["CassAL"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["CassAL"];
                return null;
            }
        }
        public ModuleInfo CassAR
        {
            get
            {
                if (ModuleManager.ModuleInfos["CassAR"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["CassAR"];
                return null;
            }
        }
        public ModuleInfo CassBL
        {
            get
            {
                if (ModuleManager.ModuleInfos["CassBL"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["CassBL"];
                return null;
            }
        }

        public ModuleInfo Buffer
        {
            get
            {
                if (ModuleManager.ModuleInfos["Buffer"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["Buffer"];
                return null;
            }
        }

        public ModuleInfo Aligner { get; set; }
        public ModuleInfo TMRobot { get; set; }
        public ModuleInfo WaferRobot { get; set; }
        public ModuleInfo TrayRobot { get; set; }
        public ModuleInfo LoadLock { get; set; }
        public ModuleInfo UnLoad { get; set; }
        public ModuleInfo PM1 { get; set; }
        public ModuleInfo PM2 { get; set; }


        #region Wafer info for machine
        public WaferInfo LoadLockWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["LoadLock"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["LoadLock"].WaferManager.Wafers[0];

                return null;
            }
        }

        public WaferInfo UnLoadWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["UnLoad"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["UnLoad"].WaferManager.Wafers[0];

                return null;
            }
        }

        public WaferInfo Wafer1
        {
            get
            {
                if (ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers[0];
                return null;
            }
        }

        public WaferInfo AlignerWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["Aligner"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["Aligner"].WaferManager.Wafers[0];

                return null;
            }
        }
        
        [Subscription("Aligner.Status")]
        public string AlignerStatus
        {
            get;
            set;
        }

        /// <summary>
        /// 返回Aligner是否正在找缺口。
        /// </summary>
        public bool IsAlignerWaferRotary => AlignerStatus == "Aligning";
        

        [Subscription("PM1.Status")]
        public string Pm1Status
        {
            get;
            set;
        }
        
        /// <summary>
        /// 返回PM1中的Wafer是否正在旋转。
        /// </summary>
        public bool IsPm1WaferRotary => Pm1Status == "Process";
        
        public WaferInfo TMRobotWafer1
        {
            get
            {
                if (ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers[0];
                return null;
            }
        }

        public WaferInfo BufferWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["Buffer"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["Buffer"].WaferManager.Wafers[0];
                return null;
            }
        }

        public WaferInfo PM1Wafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["PM1"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["PM1"].WaferManager.Wafers[0];
                return null;
            }
        }
        public WaferInfo PM2Wafer
        {
            get
            {
                //if (ModuleManager.ModuleInfos["PM2"].WaferManager.Wafers.Count > 0)
                //    return ModuleManager.ModuleInfos["PM2"].WaferManager.Wafers[0];
                return null;
            }
        }

        public WaferInfo TrayRobotWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["TrayRobot"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["TrayRobot"].WaferManager.Wafers[0];
                return null;
            }
        }
        public WaferInfo WaferRobotWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["WaferRobot"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["WaferRobot"].WaferManager.Wafers[0];
                return null;
            }
        }

        #region Tray Visble
        public Visibility TrayRobotHaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["TrayRobot"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["TrayRobot"].WaferManager.Wafers[0].WaferTrayStatus == 1)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility TMRobotHaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["TMRobot"].WaferManager.Wafers[0].WaferTrayStatus == 1)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility LoadLockHaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["LoadLock"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["LoadLock"].WaferManager.Wafers[0].WaferTrayStatus == 1)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility UnLoadHaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["UnLoad"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["UnLoad"].WaferManager.Wafers[0].WaferTrayStatus == 1)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility PM1HaveTray
        {
            get
            {
                if (ModuleManager.ModuleInfos["PM1"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["PM1"].WaferManager.Wafers[0].WaferTrayStatus == 1)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }

        public Visibility PM2HaveTray
        {
            get
            {
                //if (ModuleManager.ModuleInfos["PM2"].WaferManager.Wafers.Count > 0 && ModuleManager.ModuleInfos["PM2"].WaferManager.Wafers[0].WaferTrayStatus == 1)
                //{
                //    return Visibility.Visible;
                //}
                return Visibility.Hidden;
            }
        }
        #endregion
        #endregion


        protected void InitTM()
        {
            TMRobot = ModuleManager.ModuleInfos["TMRobot"];
        }

        protected void InitLL()
        {
            LoadLock = ModuleManager.ModuleInfos["LoadLock"];
        }

        protected void InitEFEM()
        {
            WaferRobot = ModuleManager.ModuleInfos["WaferRobot"];
            TrayRobot = ModuleManager.ModuleInfos["TrayRobot"];
        }

        protected void InitAligner()
        {
            Aligner = ModuleManager.ModuleInfos["Aligner"];
        }

        protected void InitPM()
        {
            PM1 = ModuleManager.ModuleInfos["PM1"];
            //PM2 = ModuleManager.ModuleInfos["PM2"];

        }



        public override void OnWaferTransfer(DragDropEventArgs args)
        {
            try
            {
                TransferWafer(args.TranferFrom, args.TranferTo);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }


        public void TransferWafer(Slot p_from, Slot p_to)
        {
            try
            {
                if (p_from == null || p_to == null || !p_from.IsValidSlot() || !p_to.IsValidSlot())
                    return;

                //DialogButton btns = DialogButton.Transfer | DialogButton.Cancel;       
                string info = " from " + p_from.ModuleID + " to " + p_to.ModuleID;
                string message = "Are you sure to transfer the wafer: \n" + info;


                WindowManager wm = new WindowManager();
                WaferTransferDialogViewModel _transferVM = new WaferTransferDialogViewModel(message, false, false, false);
                _transferVM.AlignerVisibility = Visibility.Hidden;
                _transferVM.CoolingVisibility = Visibility.Hidden;
                _transferVM.BladeVisibility = Visibility.Hidden;
                bool? bret = wm.ShowDialogWithNoStyle(_transferVM);
                if ((bool)bret)
                {
                    //get and use transfer conditions
                    WaferTransferCondition conditions = _transferVM.DialogResult;

                    InvokeClient.Instance.Service.DoOperation("System.MoveWafer",
                        p_from.ModuleID, p_from.SlotID, p_to.ModuleID, p_to.SlotID,
                        conditions.IsPassAligner, conditions.AlignerAngle, conditions.IsPassCooling, conditions.CoolingTime, (int)conditions.Blade, conditions.IsVirtualTransferWaferInfo, conditions.IsVirtualTransferTrayInfo);
                }

                p_from.ClearDragDropStatus();
                p_to.ClearDragDropStatus();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

    }
}
