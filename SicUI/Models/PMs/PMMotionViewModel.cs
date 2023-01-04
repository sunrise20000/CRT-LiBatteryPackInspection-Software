using Aitex.Core.Util;
using Caliburn.Micro;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using SicUI.Controls;
using System;
using System.Collections.Generic;
using System.Windows;

namespace SicUI.Models.PMs
{
    public class PMMotionViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        public PMMotionViewModel()
        {
            this.DisplayName = "Motion";


            SHLocation = 100;
            MiddleLocation = 290;
            BottomLocation = 480;
            RingLocation = 0;

            SelectedMoveBodyGroup = MoveBodyGroup[0];
            GetScTime();
        }

        public string Module => SystemName;

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            CheckHeatConditionOK(out var _);
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            NotifyOfPropertyChange("IsChamMoveBodyUp");
            NotifyOfPropertyChange("IsChamMoveBodyDown");
            NotifyOfPropertyChange("IsChamMoveBodyFront");
            NotifyOfPropertyChange("IsChamMoveBodyEnd");
        }

        [Subscription("Status")]
        public string Status { get; set; }

        [Subscription("IsOnline")]
        public bool IsOnline { get; set; }

        [Subscription("IsService")]
        public bool IsService { get; set; }

        public bool PMIsIdle =>
            (Status == "Idle" || Status == "ProcessIdle" || Status == "Safety" || Status == "VacIdle" ||
             Status == "Error" || Status == "ServiceIdle") && !IsOnline;

        //public string NextTipsContent { get; set; }


        #region Properties


        public float SHLocation { get; set; }
        public float MiddleLocation { get; set; }
        public float BottomLocation { get; set; }
        public float RingLocation { get; set; }

        private List<string> _moveBodyGroup = new List<string>() { "ShowHead", "Middle" };
        public List<string> MoveBodyGroup
        {
            get { return _moveBodyGroup; }
            set { _moveBodyGroup = value; NotifyOfPropertyChange("MoveBodyGroup"); }
        }
        private string _selectedMoveBodyGroup;
        public string SelectedMoveBodyGroup
        {
            get { return _selectedMoveBodyGroup; }
            set
            {
                _selectedMoveBodyGroup = value; NotifyOfPropertyChange("SelectedMoveBodyGroup");
            }
        }

        private int _moveUpDownTime = 3000;
        private int _moveEndForwardTime = 5000;
        private int _moveSwingTime = 1000;
        private int _moveSectionUpDown = 1000;
        private int _moveRingUp = 3000;

        private void GetScTime()
        {
        }

        public int TimeMoveUp => _moveUpDownTime;
        public int TimeMoveEnd => _moveEndForwardTime;
        public int TimeSectionUp => _moveSectionUpDown;
        public int TimeSwing => _moveSwingTime;
        public int TimeRingUp => _moveRingUp;

        #endregion Properties

        #region Properties

        [Subscription("ChamberMoveBody.ForwardLatchFaceback")]
        public bool FowardLatchFeedBack { get; set; }
        [Subscription("ChamberMoveBody.BackwardLatchFaceback")]
        public bool BackwardLatchFeedBack { get; set; }

        [Subscription("ChamberMoveBody.UpFaceback")]
        public bool IsChamMoveBodyUp { get; set; }
        [Subscription("ChamberMoveBody.DownFaceback")]
        public bool IsChamMoveBodyDown { get; set; }
        [Subscription("ChamberMoveBody.UpLatchFaceback")]
        public bool IsChamMoveBodyUpLatch { get; set; }

        [Subscription("ChamberMoveBody.FrontFaceback")]
        public bool IsChamMoveBodyFront { get; set; }
        [Subscription("ChamberMoveBody.EndFaceback")]
        public bool IsChamMoveBodyEnd { get; set; }

        [Subscription("ChamberMoveBody.UpSetpoint")]
        public bool UpSetpoint { get; set; }
        [Subscription("ChamberMoveBody.DownSetpoint")]
        public bool DownSetpoint { get; set; }
        [Subscription("ChamberMoveBody.ForwardSetpoint")]
        public bool ForwardSetpoint { get; set; }
        [Subscription("ChamberMoveBody.BackwardSetpoint")]
        public bool BackwardSetpoint { get; set; }

        [Subscription("ChamberMoveBody.IsRemoteFeceback")]
        public bool IsRemoteFeceback { get; set; }
        [Subscription("ChamberMoveBody.IsLockedFeceback")]
        public bool IsLockedFeceback { get; set; }
        [Subscription("ChamberMoveBody.UpDownEnableFaceback")]
        public bool UpDownEnableFaceback { get; set; }



        public bool IsBottomLidSwingUnlock { get; set; }

        [Subscription("GasConnector.GasConnectorLoosenFeedback")]
        public bool IsGasConnectorLoosen { get; set; }
        [Subscription("GasConnector.GasConnectorTightenFeedback")]
        public bool IsGasConnectorTighten { get; set; }


        [Subscription("SHLid.LoosenFaceback")]
        public bool IsSHLidLoosen { get; set; }
        [Subscription("SHLid.TightenFaceback")]
        public bool IsSHLidTighten { get; set; }
        [Subscription("SHLid.ClosedFaceback")]
        public bool IsSHLidClosed { get; set; }
        [Subscription("SHLidSwing.LidLockFaceback")]
        public bool IsSHLidSwingLock { get; set; }
        [Subscription("SHLidSwing.LidUnlockFaceback")]
        public bool IsSHLidSwingUnlock { get; set; }


        //[Subscription("ConfinementRing.RingUpSensor")]
        //public bool RingUpSensor { get; set; }

        //[Subscription("ConfinementRing.RingDownSensor")]
        //public bool RingDownSensor { get; set; }


        [Subscription("Trigger.LidMotionEnable")]
        public bool IsLidMotionEnable { get; set; }
        [Subscription("Trigger.RotationMotorEnable")]
        public bool IsRotationMotorEnable { get; set; }

        [Subscription("MiddleLid.LoosenFaceback")]
        public bool IsMiddleLidLoosen { get; set; }
        [Subscription("MiddleLid.TightenFaceback")]
        public bool IsMiddleLidTighten { get; set; }
        [Subscription("MiddleLid.ClosedFaceback")]
        public bool IsMiddleLidClosed { get; set; }
        [Subscription("MiddleLidSwing.LidLockFaceback")]
        public bool IsMiddleLidSwingLock { get; set; }
        [Subscription("MiddleLidSwing.LidUnlockFaceback")]
        public bool IsMiddleLidSwingUnlock { get; set; }
        #endregion Properties

        #region OP
        public void ChamMoveBodyUp()
        {
            if ((IsSHLidLoosen && IsSHLidSwingUnlock) || (IsMiddleLidLoosen && IsMiddleLidSwingUnlock) )
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBody.SetUpSetpoint");
            }
            else
            {
                ShowMessage("Can not set Body Up，you must set the Lid Loosen and the Swing Unlock first!");
            }
        }
        public void ChamMoveBodyDown()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBody.SetDownSetpoint");
        }

        public void ChamMoveBodyLatch()
        {
            bool setValue = true;
            if (IsChamMoveBodyUpLatch)
            {
                setValue = false;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBody.SetUpLatch", new object[] { setValue });
        }
        public void ChamMoveBodyForward()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBody.SetForward");
        }
        public void ChamMoveBodyBackward()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBody.SetBackward");
        }

        public void SetForwardLatch()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBody.SetForwardLatch");
        }
        public void SetBackwardLatch()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBody.SetBackwardLatch");
        }
        public void SetUpDownEnable()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBody.SetUpDownEnable", !UpDownEnableFaceback);
        }



        public void GasConnectorLoosen()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.GasConnector.SetGasConnectorLoosen");
        }
        public void GasConnectorTighten()
        {
            if ((IsSHLidTighten && IsSHLidSwingLock) && (IsMiddleLidTighten && IsMiddleLidSwingLock))
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.GasConnector.SetGasConnectorTighten");
            }
            else
            {
                ShowMessage("Can not set GasConnectorTighten，you must set all Lid Tighten and all Swing Lock first!");
                return;
            }
        }
        public void SHLidLoosen()
        {
            if (IsSHLidSwingUnlock)
            {
                ShowMessage("Can not set SHLid while the SHLidSwing is Unlock!");
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.SHLid.Loosen");
        }
        public void SHLidTighten()
        {
            if (IsSHLidSwingUnlock)
            {
                ShowMessage("Can not set SHLid while the SHLidSwing is Unlock!");
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.SHLid.Tighten");
        }
        public void SHLidLock()
        {
            if (IsSHLidTighten)
            {
                ShowMessage("Can not set Swing to Lock while the SH Lid is tighten!");
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.SHLidSwing.SetLidLockSetpoint");
        }
        public void SHLidUnlock()
        {
            if (IsSHLidTighten)
            {
                ShowMessage("Can not set Swing to UnLock while the SH Lid is tighten!");
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.SHLidSwing.SetLidUnlockSetpoint");
        }


        
        public void LidMotionEnable()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.Trigger.SetLidMotionEnable");
        }
        public void RotationMotorEnable()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.Trigger.SetRotationMotorEnable");
        }





        public void MiddleLidLoosen()
        {
            if (IsMiddleLidSwingUnlock)
            {
                ShowMessage("Can not set MiddleLid while the MiddleLidSwing is Unlock!");
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.MiddleLid.Loosen");
        }
        public void MiddleLidTighten()
        {
            if (IsMiddleLidSwingUnlock)
            {
                ShowMessage("Can not set MiddleLid while the MiddleLidSwing is Unlock!");
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.MiddleLid.Tighten");
        }
        public void MiddleLidLock()
        {
            if (IsMiddleLidTighten)
            {
                ShowMessage("Can not set MiddleLidSwing to Lock while the Middle Lid is tighten!");
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.MiddleLidSwing.SetLidLockSetpoint");
        }
        public void MiddleLidUnlock()
        {
            if (IsMiddleLidTighten)
            {
                ShowMessage("Can not set MiddleLidSwing to UnLock while the Middle Lid is tighten!");
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.MiddleLidSwing.SetLidUnlockSetpoint");
        }


        public void ChamberMoveBodyOpen()
        {
            if (!CheckHeatConditionOK(out var tips))
            {
                //string strTips = $"Are you sure want to open the chamber lid? ";
                //if (HeatEnableTips == Visibility.Visible)
                //{
                //    strTips = $"Are you sure want to open the chamber lid? \r\nBe Careful of High Temperature!";
                //}
               
                if (ShowChoosenDialog(tips))
                {

                }
            }
            else
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBodyOpen", SelectedMoveBodyGroup, true);
            }

            
        }
        public void ChamberMoveBodyClose()
        {
            if (ShowChoosenDialog("Are you sure want to close the chamber lid ? "))
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.ChamberMoveBodyOpen", SelectedMoveBodyGroup, false);
            }
        }

        #endregion OP

        #region EnableProperties
        public bool IsActionEnable => false;
        private bool OpenSH => SelectedMoveBodyGroup.Contains("ShowHead");
        private bool OpenMiddle => SelectedMoveBodyGroup.Contains("Middle");

        public bool EnableSelectLid => IsRemoteFeceback && IsChamMoveBodyFront && IsChamMoveBodyDown && IsSHLidClosed && IsMiddleLidClosed;
        public bool EnableOpen => IsService && !IsOnline && IsRemoteFeceback && IsChamMoveBodyFront && IsChamMoveBodyDown && (IsSHLidClosed  && IsMiddleLidClosed); 
        public bool EnableClose => IsService && !IsOnline && IsRemoteFeceback && IsChamMoveBodyFront && IsChamMoveBodyDown && (IsSHLidSwingUnlock  || IsMiddleLidSwingUnlock) ;

        public bool EnableUpDownEnable => ((!UpDownEnableFaceback && !IsChamMoveBodyDown) || IsChamMoveBodyDown) && (IsChamMoveBodyFront && IsGasConnectorLoosen && PMIsIdle && !IsRemoteFeceback && (IsSHLidSwingUnlock || IsMiddleLidSwingUnlock));
        public bool EnableUpLatch => IsChamMoveBodyFront && PMIsIdle && IsRemoteFeceback && IsGasConnectorLoosen && (IsSHLidSwingUnlock  || IsMiddleLidSwingUnlock);
        public bool EnableForwardLatch => IsChamMoveBodyUp && IsGasConnectorLoosen && !FowardLatchFeedBack && IsRemoteFeceback;
        public bool EnableBackwardLatch => IsChamMoveBodyUp && IsChamMoveBodyFront && IsGasConnectorLoosen && !BackwardLatchFeedBack && IsRemoteFeceback;

        public bool EnableSHTighten => OpenSH && IsSHLidSwingLock && IsSHLidLoosen && PMIsIdle && IsRemoteFeceback;
        public bool EnableSHLoosen => OpenSH && IsSHLidTighten && IsChamMoveBodyDown && IsChamMoveBodyFront && PMIsIdle && !IsMiddleLidLoosen && !IsSHLidLoosen  && IsGasConnectorLoosen && IsRemoteFeceback;
        public bool EnableSHSwingLock => OpenSH && IsSHLidLoosen && IsChamMoveBodyDown && IsChamMoveBodyFront && PMIsIdle && IsRemoteFeceback;
        public bool EnableSHSwingUnlock => OpenSH && IsSHLidLoosen && PMIsIdle && IsChamMoveBodyDown && IsChamMoveBodyFront && IsRemoteFeceback;

       

        public bool EnableMiddleTighten => OpenMiddle && IsMiddleLidSwingLock && IsMiddleLidLoosen && PMIsIdle && IsRemoteFeceback;
        public bool EnableMiddleLoosen => OpenMiddle && IsMiddleLidTighten && IsChamMoveBodyDown && IsChamMoveBodyFront && PMIsIdle && !IsMiddleLidLoosen && !IsSHLidLoosen  && IsGasConnectorLoosen && IsRemoteFeceback;
        public bool EnableMiddleSwingLock => OpenMiddle && IsMiddleLidLoosen && IsChamMoveBodyDown && IsChamMoveBodyFront && PMIsIdle && IsRemoteFeceback;
        public bool EnableMiddleSwingUnlock => OpenMiddle && IsMiddleLidLoosen && PMIsIdle && IsRemoteFeceback && IsChamMoveBodyDown && IsChamMoveBodyFront;

        public bool EnableGasConnectorLoosen => IsChamMoveBodyFront && IsChamMoveBodyDown && PMIsIdle && IsGasConnectorTighten;
        public bool EnableGasConnectorTighten => IsGasConnectorLoosen && PMIsIdle && IsChamMoveBodyFront && IsChamMoveBodyDown && IsSHLidTighten && IsMiddleLidTighten;

        public bool EnableMoveBodyOpen => IsChamMoveBodyDown && PMIsIdle && !IsMiddleLidLoosen && !IsSHLidLoosen ;
        public bool EnableMoveBodyClose => !IsChamMoveBodyDown && PMIsIdle;
        public Visibility UpDownISFalse => !UpDownEnableFaceback && !IsChamMoveBodyDown ? Visibility.Visible : Visibility.Hidden; 

        #endregion EnableProperties

        private void ShowMessage(string msg)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.ShowMessage", msg);
        }

        public bool ShowChoosenDialog(string strInfo)
        {
            ChooseDialogBoxViewModel dialog = new ChooseDialogBoxViewModel();
            dialog.DisplayName = "Tips";
            dialog.InfoStr = strInfo;


            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if (!bret.HasValue || !bret.Value)
            {
                return false;
            }
            return true;
        }


        #region Rotation Servo
        [Subscription("PMServo.ServoEnable")]
        public bool IsServoEnable { get; set; }
        [Subscription("PMServo.ServoReady")]
        public bool IsServoReady { get; set; }

        [Subscription("PMServo.ServoError")]
        public bool IsServoError { get; set; }

        [Subscription("PMServo.ActualSpeedFeedback")]
        public float ActualSpeedFeedback { get; set; }
        [Subscription("PMServo.ActualCurrentFeedback")]
        public float ActualCurrentFeedback { get; set; }

        public void SetServoEnable()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetServoEnable", !IsServoEnable);
        }
        public void SetServoInital()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetServoInital");
        }
        public void SetServoReset()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetServoReset");
        }

        public void SetActualSpeed(object data)
        {
            int speed = 0;
            if (!Int32.TryParse(data.ToString(), out speed))
            {
                return;
            }
            if (speed > 1000)
            {
                speed = 1000;
                return;
            }
            if (speed < 0)
            {
                speed = 0;
            }

            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetActualSpeed", speed);
        }
        #endregion

        [Subscription("OmronTemp.Enable")]
        public bool OmronTempEnable { get; set; }

        public void LineHeaterEnable()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.OmronTemp.SetEnable", !OmronTempEnable);
        }



        #region Ring
        [Subscription("IsOnline")]
        public bool PM1IsOnline { get; set; }
        [Subscription("Status")]
        public string PM1Status { get; set; }

        [Subscription("ConfinementRing.RingCurPos")]
        public float RingCurPos { get; set; }

        [Subscription("ConfinementRing.RingUpPos")]
        public float RingUpPos { get; set; }

        [Subscription("ConfinementRing.RingDownPos")]
        public float RingDownPos { get; set; }

        [Subscription("ConfinementRing.RingUpSensor")]
        public bool RingUpSensor { get; set; }

        [Subscription("ConfinementRing.RingDownSensor")]
        public bool RingDownSensor { get; set; }

        [Subscription("ConfinementRing.RingDone")]
        public bool RingDone { get; set; }

        [Subscription("ConfinementRing.RingIsServoOn")]
        public bool RingIsServoOn { get; set; }

        [Subscription("ConfinementRing.RingIsBusy")]
        public bool RingIsBusy { get; set; }

        [Subscription("ConfinementRing.RingIsAlarm")]
        public bool RingIsAlarm { get; set; }

        public bool ConfinementRingBtnEnable => !RingIsBusy && !RingIsAlarm && !PM1IsOnline && PM1Status != "Process";

        public void RingServoStop()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ConfinementRing.ServoStop");
        }

        public void RingServoOn()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ConfinementRing.ServoOn");
        }

        public void RingServoReset()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ConfinementRing.ServoReset");
        }

        public void RingMoveUpPos()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ConfinementRing.MoveUpPos");
        }

        public void RingMoveDownPos()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ConfinementRing.MoveDownPos");
        }

        public void RingJogUp(float distance)
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ConfinementRing.JogUp", distance);
        }

        public void RingJogDown(float distance)
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ConfinementRing.JogDown", distance);
        }
        #endregion


        public Visibility HeatEnableTips { get; set; }
        private bool CheckHeatConditionOK(out string tips)
        {
            tips = "";
            var lidOpenAfterHeatDisableMinutes = (int)QueryDataClient.Instance.Service.GetConfig("PM.LidEnableOpenAfterHeatDisbaleMinuts");
            var dateTimeStr = QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.OpenLidCountDownTime").ToString();
            if (string.IsNullOrEmpty(dateTimeStr))
            {
                tips = $"It seems that the heater is enabled, the chamber might be at a high temperature, please check the status of the PSU!";
            }
            else if (DateTime.TryParse(dateTimeStr, out var dtStartTime))
            {
                var minutesToWait = lidOpenAfterHeatDisableMinutes - (DateTime.Now - dtStartTime).TotalMinutes;
                if (dtStartTime.AddMinutes(lidOpenAfterHeatDisableMinutes) < DateTime.Now)
                {
                    HeatEnableTips = Visibility.Collapsed;
                    return true;
                }

                tips = $"The temperature is too high to open the Lid. \r\nWait for {minutesToWait:0.0} minutes before opening it";
            }

            HeatEnableTips = Visibility.Visible;
            
            return false;
        }
    }
}
