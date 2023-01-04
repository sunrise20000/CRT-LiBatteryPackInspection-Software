using System;
using System.Windows;
using Aitex.Core.RT.Log;
using Caliburn.Micro;
using MECF.Framework.Common.OperationCenter;
using OpenSEMI.Ctrlib.Controls;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferMoveManager
    {
        #region single Instance

        public bool ShowAligner { get; set; }
        public bool ShowCooling { get; set; }
        public bool ShowBlade { get; set; }

        private WaferMoveManager()
        {
            ShowAligner = true;
            ShowCooling = true;
            ShowBlade = false;
        }

        private static WaferMoveManager m_Instance = null;
        public static WaferMoveManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new WaferMoveManager();
                }
                return m_Instance;
            }
        }
        #endregion

        public void TransferWafer(Slot p_from, Slot p_to)
        {
            try
            {
                if (p_from == null || p_to == null || !p_from.IsValidSlot() || !p_to.IsValidSlot())
                    return;

                //DialogButton btns = DialogButton.Transfer | DialogButton.Cancel;       
                string info = " from " + p_from.ModuleID + " slot " + (p_from.SlotID + 1).ToString() + " to " + p_to.ModuleID + " slot " + (p_to.SlotID + 1).ToString();
                string message = "Are you sure to transfer the wafer: \n" + info;
                //DialogButton m_dResult = DialogBox.ShowDialog(btns, DialogType.CONFIRM, message);

                bool displayAlignerCondition = (p_from.ModuleID == "LP1" || p_from.ModuleID == "LP2" || p_from.ModuleID == "LP3" || p_from.ModuleID == "EfemRobot")
                    || ((p_from.ModuleID == "LLA" || p_from.ModuleID == "LLB" || p_from.ModuleID == "LLC" || p_from.ModuleID == "LLD") && (p_to.ModuleID == "LP1" || p_to.ModuleID == "LP2" || p_to.ModuleID == "LP3" || p_to.ModuleID == "EfemRobot"));

                displayAlignerCondition = displayAlignerCondition && (p_from.ModuleID != "Aligner" && p_to.ModuleID != "Aligner");

                bool displayPassCoolingCondition = (p_from.ModuleID.Contains("PM") || p_from.ModuleID == "TMRobot");

                displayPassCoolingCondition = displayPassCoolingCondition && (!p_to.ModuleID.Contains("PM")) && (p_to.ModuleID != "TMRobot")
                    && (p_to.ModuleID != "LLA") && (p_to.ModuleID != "LLB") && (p_to.ModuleID != "LLC") && (p_to.ModuleID != "LLD");

                bool displayBladeCondition = ShowBlade;
                WindowManager wm = new WindowManager();
                WaferTransferDialogViewModel _transferVM = new WaferTransferDialogViewModel(message, displayAlignerCondition, displayPassCoolingCondition, displayBladeCondition);
                _transferVM.AlignerVisibility = ShowAligner ? Visibility.Visible : Visibility.Hidden;
                _transferVM.CoolingVisibility = ShowCooling ? Visibility.Visible : Visibility.Hidden;
                _transferVM.BladeVisibility = ShowBlade ? Visibility.Visible : Visibility.Hidden;
                bool? bret = wm.ShowDialogWithNoStyle(_transferVM);
                if ((bool)bret)
                {
                    //get and use transfer conditions
                    WaferTransferCondition conditions = _transferVM.DialogResult;

                    InvokeClient.Instance.Service.DoOperation("System.MoveWafer",
                        p_from.ModuleID, p_from.SlotID, p_to.ModuleID, p_to.SlotID,
                        conditions.IsPassAligner, conditions.AlignerAngle, conditions.IsPassCooling, conditions.CoolingTime, (int)conditions.Blade);
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
