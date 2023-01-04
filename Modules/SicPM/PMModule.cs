using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;
using SicPM.RecipeExecutions;
using SicPM.RecipeRoutine;
using SicPM.Routines;

namespace SicPM
{
    public partial class PMModule : PMModuleBase, IRecipeExecutor
    {
        public enum STATE
        {
            NotInstall,
            NotConnected,
            Init,
            Idle,
            //Safety,
            ProcessIdle,
            Homing,
            ToAtmIdle,
            ToProcessIdle,
            Pump,
            Vent,
            Purge,
            OpenSlitValve,
            CloseSlitValve,
            Error,
            PrepareTransfer,
            PostTransfer,
            PreProcess,
            PostProcess,
            Process,
            ProcessAborting,
            Aborting,
            LeakCheck,
            MFCCali,
            Paused,
            LidMoving,
            LidMoving0,
            BottomMoving,
            OpenLid,
            Clean,
            Isolation,
            ExchangeMO,
            ServopUp,
            ServoDown,
            ServoHome,
            ServoReset,
            ToDefault,
            PcCalibration,
            ServoReset0,
            ServoReset1,
            ServoDown0,
            ServoDown1,
            ServopUp0,
            ServopUp1,
            ServoHome0,
            ServoHome1,
            PcCalibration0,
            PreTransfer,
            VacIdle,
            ToProcessIdle1,
            ToVacIdle1,
            ToVacIdle,
            ToProcessIdle2,
            PrePMMacro,
            PMMacro,
            PostPMMacro,
            PMMacroPause,
            ServiceIdle,
            StopHeat,
            CleanProcess,

        }
        public enum MSG
        {
            Home,
            Reset,
            Abort,
            ProcessAbort,
            Error,
            Error1,
            Connected,
            Disconnected,
            Transfer,
            PrepareTransfer,
            PostTransfer,
            Pump,
            Vent,
            Purge,
            OpenSlitValve,
            CloseSlitValve,
            SelectRecipe,
            RunRecipe,
            RecipeSkipStep,
            Pause,
            Continue,

            PostProcess,
            Process,
            LeakCheck,
            StartMFCCalibration,
            ChamberMoveBody,
            ChamberMoveBottom,
            ShowMessage,
            ToInit,
            ToSafety,
            AtmIdle,
            ToProcessIdle,
            OpenLid,
            Clean,
            Isolation,
            ExchangeMO,
            ServopUp,
            ServoDown,
            ServoHome,
            ServoReset,
            ToDefault,
            PreTransfer,
            PcCalibration,
            VacIdle,

            RunPMMacro,
            PMMacro,
            PostPMMacro,
            ToServiceIdle,
            StopHeat,
            CleanProcess

        }
        public enum ServoStates
        {
            Home,
            Homing,
            Up,
            Uping,
            Down,
            Downing
        }

        public bool IsServiceIdle { get; set; }

        public ServoStates ServoState { get; set; }
        public bool IsMoRoutineRuning
        {
            get { return FsmState == (int)STATE.ExchangeMO; }
        }

        public bool IsServiceMode
        {
            get { return IsServiceIdle; }// !IsOnline; }
        }

        public override bool IsInit
        {
            get { return FsmState == (int)STATE.Init; }
        }

        public bool IsSafety
        {
            get { return false; }
            //get { return FsmState == (int)STATE.Safety; }
        }

        public bool IsBusy
        {
            get { return !IsInit && !IsSafety && !IsError && !IsIdle && !IsProcessIdle; }
        }
        public bool IsProcessing
        {
            get { return FsmState == (int)STATE.Process; }
        }

        public override bool IsIdle
        {
            get { return FsmState == (int)STATE.Idle && CheckAllMessageProcessed(); }
        }

        public override bool IsProcessIdle
        {
            get { return FsmState == (int)STATE.ProcessIdle && CheckAllMessageProcessed(); }
        }

        public RecipeRunningInfo RecipeRunningInfo
        {
            get
            {
                return _recipeRunningInfo;
            }
        }

        public override bool IsReady
        {
            get { return (FsmState == (int)STATE.Idle || FsmState == (int)STATE.ProcessIdle || FsmState == (int)STATE.VacIdle) && CheckAllMessageProcessed(); }
        }

        public override bool IsError
        {
            get { return FsmState == (int)STATE.Error; }
        }

        public override bool IsService
        {
            get { return IsServiceIdle; }
        }


        public bool IsAlarm
        {
            get
            {
                return FsmState == (int)STATE.Error;
            }
        }


        public bool IsWarning
        {
            get
            {
                int count = 0;
                var warnings = EV.GetAlarmEvent();
                foreach (var warm in warnings)
                {
                    if (warm.Level == EventLevel.Warning && warm.Source == Name)
                        count++;
                }

                return !IsAlarm && count > 0;
            }
        }

        public bool IsPaused { get; set; }

        public int CurrentRoutineLoop
        {
            get
            {
                if (FsmState == (int)STATE.Purge)
                {
                    return _purgeRoutine.LoopCounter + 1; // CurrentLoop从0开始
                }
                else if (FsmState == (int)STATE.Clean)
                {
                    return _cleanRoutine.LoopCounter + 1;
                }

                return 0;
            }
        }

        public int CurrentRoutineLoopTotal
        {
            get
            {
                if (FsmState == (int)STATE.Purge)
                {
                    return _purgeRoutine.LoopTotalTime;
                }
                else if (FsmState == (int)STATE.Clean)
                {
                    return _cleanRoutine.LoopTotalTime;
                }

                return 0;
            }
        }

        public void ResetToleranceChecker()
        {

        }

        public void OnProcessStart(string v1, string recipeName, bool v2)
        {

        }

        public void PauseRecipe(out string reason)
        {
            reason = string.Empty;
            StopRamp();
        }

        public bool CheckEndPoint()
        {
            return true;
        }

        public bool CheckAllDevicesStable(float v1, float v2, float v3, float v4, float v5, float v6, float v7, float v8, float v9)
        {
            return true;
        }

        public bool CheckEnableRunProcess(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void AbortRunProcess(out string reason)
        {
            _pmInterLock.SetPMProcessRunning(false, out reason);
            _pmInterLock.SetPMPreProcessRunning(false, out reason);
            reason = string.Empty;
            StopRamp();
            SetRotationServo(0, 0);
        }

        public event Action<string> OnEnterError;


        //private PMPumpRoutine _pumpRoutine;
        //private PMVentRoutine _ventRoutine;
        private PMChamberMoveBodyRoutine _chamberMoveBodyRoutine;
        private PMButttomSectionMoveRoutine pMButttomSectionMoveRoutine;
        private PMLeakCheckRoutine _leakCheckRoutine;
        private PMPumpRoutine _pumpRoutine;
        private PMPurgeRoutine _purgeRoutine;
        private PMToAtmIdleRoutine _toAtmIdleRoutine;
        private PMVacIdleRoutine _vacIdleRoutine;
        private PMVentRoutine _ventRoutine;
        private PMToIsolationRoutine _toIsolationRoutine;
        private PMCleanRoutine _cleanRoutine;
        private PMExchangeMoRoutine _changeMoRoutine;

        private PreProcess _preprocessRoutine;
        private Process _processRoutine;
        private PostProcess _postprocessRoutine;
        private PMToProcessIdleRoutine _pmtoProcessIdle;
        private PMProcessAbortRoutine _pmProcessAbort;
        private ModuleName _module;

        private RecipeRunningInfo _recipeRunningInfo = new RecipeRunningInfo();

        private PMServoToPressure _pmServoToPressure;

        private PMPcCalibrationRoutine _pmPcCalibrationRoutine;

        private PMPostTransferRoutine _pmPostTransfer;
        private PMPrepareTransferRoutine _pmPreTransfer;

        private PMMacroRoutine _pMMacroRoutine;
        private PMStopHeatEnableRoutine _stopHeatEnableRoutine;
        private CleanRecipe _cleanRecipe;

        private bool _isInitFlag = false;

        public PMModule(ModuleName module) : base(1)
        {
            _module = module;

            Module = module.ToString();
            Name = module.ToString();

            IsOnline = false;

            EnumLoop<STATE>.ForEach((item) =>
            {
                MapState((int)item, item.ToString());
            });

            EnumLoop<MSG>.ForEach((item) =>
            {
                MapMessage((int)item, item.ToString());
            });

            EnableFsm(50, IsInstalled ? STATE.Init : STATE.NotInstall);
        }

        public override bool Initialize()
        {
            InitRoutine();

            InitDevice();

            InitInterlock();

            InitFsm();

            InitOp();

            InitData();

            InitalDeviceFunc();

            return base.Initialize();
        }

        private void InitRoutine()
        {
            _purgeRoutine = new PMPurgeRoutine(ModuleHelper.Converter(Module), this);
            _leakCheckRoutine = new PMLeakCheckRoutine(ModuleHelper.Converter(Module), this);
            _chamberMoveBodyRoutine = new PMChamberMoveBodyRoutine(ModuleHelper.Converter(Module), this);
            pMButttomSectionMoveRoutine = new PMButttomSectionMoveRoutine(ModuleHelper.Converter(Module), this);

            _preprocessRoutine = new PreProcess(ModuleHelper.Converter(Module), this);
            _processRoutine = new Process(ModuleHelper.Converter(Module), this);
            _postprocessRoutine = new PostProcess(ModuleHelper.Converter(Module), this);

            _toAtmIdleRoutine = new PMToAtmIdleRoutine(ModuleHelper.Converter(Module), this);
            _vacIdleRoutine = new PMVacIdleRoutine(ModuleHelper.Converter(Module), this);
            _ventRoutine = new PMVentRoutine(ModuleHelper.Converter(Module), this);
            _pumpRoutine = new PMPumpRoutine(ModuleHelper.Converter(Module), this);
            _cleanRoutine = new PMCleanRoutine(ModuleHelper.Converter(Module), this);
            _purgeRoutine = new PMPurgeRoutine(ModuleHelper.Converter(Module), this);
            _toIsolationRoutine = new PMToIsolationRoutine(ModuleHelper.Converter(Module), this);
            _changeMoRoutine = new PMExchangeMoRoutine(ModuleHelper.Converter(Module), this);

            _pmServoToPressure = new PMServoToPressure(ModuleHelper.Converter(Module), this);
            _pmtoProcessIdle = new PMToProcessIdleRoutine(ModuleHelper.Converter(Module), this);

            _pmPcCalibrationRoutine = new PMPcCalibrationRoutine(ModuleHelper.Converter(Module), this);
            _pmPostTransfer = new PMPostTransferRoutine(ModuleHelper.Converter(Module), this);
            _pmPreTransfer = new PMPrepareTransferRoutine(ModuleHelper.Converter(Module), this);

            _pMMacroRoutine = new PMMacroRoutine(ModuleHelper.Converter(Module), this);
            _stopHeatEnableRoutine = new PMStopHeatEnableRoutine(ModuleHelper.Converter(Module), this);
            _cleanRecipe = new CleanRecipe(ModuleHelper.Converter(Module), this);

            _pmProcessAbort = new PMProcessAbortRoutine(ModuleHelper.Converter(Module), this);
        }

        private void InitData()
        {
            DATA.Subscribe($"{Module}.Status", () => StringFsmStatus.Replace("0", "").Replace("1", "").Replace("2", ""));
            DATA.Subscribe($"{Module}.IsOnline", () => IsOnline);
            DATA.Subscribe($"{Module}.IsService", () => IsServiceIdle);

            DATA.Subscribe($"{Module}.IsAlarm", () => IsAlarm);
            DATA.Subscribe($"{Module}.IsWarning", () => IsWarning);
            DATA.Subscribe($"{Module}.IsIdle", () => !IsWarning && (IsIdle || IsInit || IsSafety));

            DATA.Subscribe($"{Module}.WaferSize", () => WaferManager.Instance.GetWaferSize(_module, 0).ToString());


            DATA.Subscribe($"{Name}.LeakCheckElapseTime", () =>
            {
                if (FsmState == (int)STATE.LeakCheck)
                    return _leakCheckRoutine.ElapsedTime;
                return 0;
            });


            DATA.Subscribe($"{Name}.ChamberPressure", () => ChamberPressure);

            DATA.Subscribe($"{Name}.ServoStates", () => ServoState.ToString());

            DATA.Subscribe($"{Name}.SelectedRecipeName", () => _recipeRunningInfo.RecipeName);
            DATA.Subscribe($"{Name}.RecipeStepNumber", () => _recipeRunningInfo.StepNumber);
            DATA.Subscribe($"{Name}.RecipeStepName", () => _recipeRunningInfo.StepName);
            DATA.Subscribe($"{Name}.RecipeStepElapseTime", () => _recipeRunningInfo.StepElapseTime);
            DATA.Subscribe($"{Name}.RecipeStepTime", () => _recipeRunningInfo.StepTime);
            DATA.Subscribe($"{Name}.RecipeTotalElapseTime", () => _recipeRunningInfo.TotalElapseTime);
            DATA.Subscribe($"{Name}.RecipeTotalTime", () => _recipeRunningInfo.TotalTime);
            DATA.Subscribe($"{Name}.CurrentRoutineLoop", () => CurrentRoutineLoop);
            DATA.Subscribe($"{Name}.CurrentRoutineLoopTotal", () => CurrentRoutineLoopTotal);

            DATA.Subscribe($"{Name}.RecipeStepElapseTime2", () => _recipeRunningInfo.StepElapseTime2);
            DATA.Subscribe($"{Name}.RecipeTotalElapseTime2", () => _recipeRunningInfo.TotalElapseTime2);

            //DATA.Subscribe($"{Name}.ArH2Switch", () => _recipeRunningInfo.ArH2Switch);
            //DATA.Subscribe($"{Name}.N2FlowMode", () => _recipeRunningInfo.N2FlowMode);

            //
            DATA.Subscribe($"{Name}.Recipe.EditPassword", () => EPassword);
        }

        private void InitOp()
        {
            OP.Subscribe($"{Module}.Home", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Home));
            OP.Subscribe($"{Module}.Reset", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Reset));
            OP.Subscribe($"{Module}.Abort", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Abort));

            OP.Subscribe($"{Module}.Pump", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Pump));
            OP.Subscribe($"{Module}.Vent", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Vent));
            OP.Subscribe($"{Module}.Purge", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Purge));

            OP.Subscribe($"{Module}.OpenLid", (string cmd, object[] args) => CheckToPostMessage((int)MSG.OpenLid));
            OP.Subscribe($"{Module}.Isolation", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Isolation));
            OP.Subscribe($"{Module}.Clean", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Clean));
            OP.Subscribe($"{Module}.ExchangeMO", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ExchangeMO, (string)args[0]));
            OP.Subscribe($"{Module}.ToDefault", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ToDefault));

            OP.Subscribe($"{Name}.SelectRecipe", (string cmd, object[] args) => CheckToPostMessage((int)MSG.SelectRecipe, args[0]));
            OP.Subscribe($"{Name}.RunRecipe", (string cmd, object[] args) => CheckToPostMessage((int)MSG.RunRecipe, (string)args[0], (bool)args[1], (bool)args[2]));
            OP.Subscribe($"{Name}.RecipeSkipStep", (string cmd, object[] args) => CheckToPostMessage((int)MSG.RecipeSkipStep));
            OP.Subscribe($"{Name}.Pause", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Pause));
            OP.Subscribe($"{Name}.Continue", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Continue, args[0].ToString()));

            OP.Subscribe($"{Name}.LeakCheck", (string cmd, object[] args) => CheckToPostMessage((int)MSG.LeakCheck, args));

            OP.Subscribe($"{Module}.ChamberMoveBodyOpen", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ChamberMoveBody, args));
            OP.Subscribe($"{Module}.ChamberBottomOpen", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ChamberMoveBottom, args));
            OP.Subscribe($"{Module}.ShowMessage", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ShowMessage, args));

            OP.Subscribe($"{Module}.ToAtmIdle", (string cmd, object[] args) => CheckToPostMessage((int)MSG.AtmIdle));
            OP.Subscribe($"{Module}.ToVacIdle", (string cmd, object[] args) => CheckToPostMessage((int)MSG.VacIdle));
            OP.Subscribe($"{Module}.ToProcessIdle", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ToProcessIdle));

            OP.Subscribe($"{Module}.ServoHome", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ServoHome));
            OP.Subscribe($"{Module}.ServoUp", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ServopUp));
            OP.Subscribe($"{Module}.ServoDown", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ServoDown));
            OP.Subscribe($"{Module}.ServoReset", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ServoReset));


            OP.Subscribe($"{Module}.PostTransfer", (string cmd, object[] args) => CheckToPostMessage((int)MSG.PostTransfer));
            OP.Subscribe($"{Module}.PreTransfer", (string cmd, object[] args) => CheckToPostMessage((int)MSG.PreTransfer));
            OP.Subscribe($"{Module}.SetToServiceIdle", (string cmd, object[] args) => CheckToPostMessage((int)MSG.ToServiceIdle));

            OP.Subscribe($"{Module}.PcCalibration", (string cmd, object[] args) => CheckToPostMessage((int)MSG.PcCalibration));
            OP.Subscribe($"{Module}.RunPMMacro", (string cmd, object[] args) => CheckToPostMessage((int)MSG.RunPMMacro, (string)args[0]));

            OP.Subscribe($"{Module}.CleanRecipe", (string cmd, object[] args) => CheckToPostMessage((int)MSG.CleanProcess, (string)args[0]));
            OP.Subscribe($"{Module}.ReloadRecipe", (string cmd, object[] args) => ReloadRecipe((string)args[0]));
            OP.Subscribe($"{Module}.Recipe.EditorChangePassword", (string cmd, object[] args) => WriteEPassword((string)args[0]));
            OP.Subscribe($"{Module}.SignalTower.SwitchOffBuzzerEx", (string cmd, object[] args) => SwitchOffBuzzerEx(args));
        }

        private bool ReloadRecipe(string reloadedRecipe)
        {
            _recipeRunningInfo.XmlRecipeToReload = reloadedRecipe;
            _recipeRunningInfo.NeedReloadRecipe = true;
            return true;
        }

        private void InitFsm()
        {
            //Error
            Transition(STATE.Error, MSG.Reset, FsmReset, STATE.Idle);
            AnyStateTransition(MSG.Error, FsmOnError, STATE.Error);        //在Service模式下,不成功
            AnyStateTransition(FSM_MSG.ALARM, FsmOnError, STATE.Error);
            AnyStateTransition(MSG.Error1, FsmOnError1, STATE.Error);      //在Routine失败时生效
            AnyStateTransition(MSG.ToInit, FsmToInit, STATE.Init);

            EnterExitTransition<STATE, FSM_MSG>(STATE.Error, FsmEnterError, FSM_MSG.NONE, FsmExitError);

            Transition(STATE.Init, MSG.Disconnected, null, STATE.NotConnected);
            Transition(STATE.Init, MSG.Home, FsmStartHome, STATE.Homing);

            //not connected
            Transition(STATE.NotConnected, MSG.Connected, null, STATE.Init);

            //Home(ToSafety过程)
            EnterExitTransition((int)STATE.Homing, FsmEnterHome, (int)FSM_MSG.NONE, FsmExitHome);
            Transition(STATE.Error, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Homing, FSM_MSG.TIMER, FsmMonitorHomeTask, STATE.Idle);
            Transition(STATE.Homing, MSG.Error, null, STATE.Idle);
            Transition(STATE.Homing, MSG.Abort, FsmAbortTask, STATE.Idle);

            //AtmIdle
            Transition(STATE.Idle, MSG.AtmIdle, FsmToAtmIdle, STATE.ToAtmIdle);
            Transition(STATE.VacIdle, MSG.AtmIdle, FsmToAtmIdle, STATE.ToAtmIdle);
            Transition(STATE.ProcessIdle, MSG.AtmIdle, FsmToAtmIdle, STATE.ToAtmIdle);
            Transition(STATE.ToAtmIdle, FSM_MSG.TIMER, FsmMonitorSafetyTask, STATE.Idle);
            Transition(STATE.ToAtmIdle, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Idle
            Transition(STATE.Idle, MSG.ToProcessIdle, FsmToProcessIdle, STATE.ToProcessIdle); //ToProcessIdle
            Transition(STATE.ToProcessIdle, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.ToProcessIdle, MSG.Abort, FsmAbortTask, STATE.Idle);
            Transition(STATE.Idle, MSG.Home, FsmStartHome, STATE.Homing);                   //ToSafety (Homing过程)


            //VacIdle
            Transition(STATE.Idle, MSG.VacIdle, FsmVacIdle, STATE.ToVacIdle);

            Transition(STATE.ToVacIdle, FSM_MSG.TIMER, FsmMonitorTask, STATE.VacIdle);
            Transition(STATE.ToVacIdle, MSG.Abort, FsmAbortTask, STATE.Idle);

            Transition(STATE.ProcessIdle, MSG.VacIdle, FsmVacIdle, STATE.ToVacIdle1);
            Transition(STATE.VacIdle, MSG.ToProcessIdle, FsmToProcessIdle, STATE.ToProcessIdle1);
            Transition(STATE.ToProcessIdle1, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.ToProcessIdle1, MSG.Abort, FsmAbortTask, STATE.VacIdle);
            Transition(STATE.ToVacIdle1, FSM_MSG.TIMER, FsmMonitorTask, STATE.VacIdle);
            Transition(STATE.ToVacIdle1, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);

            //ToServiceIdle
            Transition(STATE.Idle, MSG.ToServiceIdle, FsmToService, STATE.Idle);
            Transition(STATE.VacIdle, MSG.ToServiceIdle, FsmToService, STATE.VacIdle);
            Transition(STATE.ProcessIdle, MSG.ToServiceIdle, FsmToService, STATE.ProcessIdle);
            Transition(STATE.Error, MSG.ToServiceIdle, FsmToService, STATE.Error);


            //ProcessIdle
            Transition(STATE.ProcessIdle, MSG.ToProcessIdle, FsmToProcessIdle, STATE.ToProcessIdle);

            Transition(STATE.ProcessIdle, MSG.PrepareTransfer, FsmStartPrepareTransfer, STATE.PrepareTransfer);
            Transition(STATE.PrepareTransfer, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.PrepareTransfer, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);

            Transition(STATE.ProcessIdle, MSG.PreTransfer, FsmStartPreTransfer2, STATE.PreTransfer);
            Transition(STATE.PreTransfer, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.PreTransfer, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);

            Transition(STATE.ProcessIdle, MSG.PostTransfer, FsmStartPostTransfer, STATE.PostTransfer);
            Transition(STATE.PostTransfer, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.PostTransfer, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);

            Transition(STATE.ProcessIdle, MSG.OpenSlitValve, FsmStartOpenSlitValve, STATE.OpenSlitValve);
            Transition(STATE.OpenSlitValve, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.OpenSlitValve, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);

            Transition(STATE.ProcessIdle, MSG.CloseSlitValve, FsmStartCloseSlitValve, STATE.CloseSlitValve);
            Transition(STATE.CloseSlitValve, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.CloseSlitValve, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);

            Transition(STATE.ProcessIdle, MSG.CleanProcess, FsmStartCleanRecipe, STATE.CleanProcess);
            Transition(STATE.CleanProcess, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.CleanProcess, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);

            //MoveBody
            Transition(STATE.Idle, MSG.ChamberMoveBody, FsmStartMoveBody, STATE.LidMoving);
            Transition(STATE.LidMoving, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.LidMoving, MSG.Abort, FsmAbortTask, STATE.Idle);


            //PcCalibration
            Transition(STATE.Idle, MSG.PcCalibration, FsmStartPcCalibration, STATE.PcCalibration);
            Transition(STATE.PcCalibration, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.PcCalibration, MSG.Abort, FsmAbortTask, STATE.Idle);



            //ShowMessage
            Transition(STATE.Idle, MSG.ShowMessage, ShowMessage, STATE.Idle);
            Transition(STATE.Init, MSG.ShowMessage, ShowMessage, STATE.Init);  //测试时防报错

            //Pump
            Transition(STATE.Idle, MSG.Pump, FsmStartPump, STATE.Pump);
            Transition(STATE.Pump, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Pump, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Vent
            Transition(STATE.Idle, MSG.Vent, FsmStartVent, STATE.Vent);
            Transition(STATE.Vent, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Vent, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Purge
            Transition(STATE.Idle, MSG.Purge, FsmStartPurge, STATE.Purge);
            Transition(STATE.Purge, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Purge, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Leak check
            Transition(STATE.Idle, MSG.LeakCheck, FsmStartLeakCheck, STATE.LeakCheck);
            Transition(STATE.LeakCheck, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.LeakCheck, MSG.Abort, FsmAbortTask, STATE.Idle);

            //OpenLid
            Transition(STATE.Idle, MSG.OpenLid, FsmStartPurge, STATE.OpenLid);
            Transition(STATE.OpenLid, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.OpenLid, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Clean
            Transition(STATE.Idle, MSG.Clean, FsmStartClean, STATE.Clean);
            Transition(STATE.Clean, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Clean, MSG.Abort, FsmAbortTask, STATE.Idle);

            //ExchangeMO
            Transition(STATE.Idle, MSG.ExchangeMO, FsmStartExchangeMO, STATE.ExchangeMO);
            Transition(STATE.ExchangeMO, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.ExchangeMO, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Pump
            Transition(STATE.VacIdle, MSG.Pump, FsmStartPump, STATE.Pump);
            //Vent
            Transition(STATE.VacIdle, MSG.Vent, FsmStartVent, STATE.Vent);
            //Purge
            Transition(STATE.VacIdle, MSG.Purge, FsmStartPurge, STATE.Purge);
            //Leak check
            Transition(STATE.VacIdle, MSG.LeakCheck, FsmStartLeakCheck, STATE.LeakCheck);
            //OpenLid
            Transition(STATE.VacIdle, MSG.OpenLid, FsmStartPurge, STATE.OpenLid);
            //Clean
            Transition(STATE.VacIdle, MSG.Clean, FsmStartClean, STATE.Clean);
            //ExchangeMO
            Transition(STATE.VacIdle, MSG.ExchangeMO, FsmStartExchangeMO, STATE.ExchangeMO);

            //FsmStartToIsolation
            Transition(STATE.Idle, MSG.Isolation, FsmStartToIsolation, STATE.Isolation);
            Transition(STATE.Isolation, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Isolation, MSG.Abort, FsmAbortTask, STATE.Idle);

            //ProcessIdle
            Transition(STATE.ProcessIdle, MSG.SelectRecipe, FsmSelectRecipe, STATE.ProcessIdle);

            Transition(STATE.ProcessIdle, MSG.RunRecipe, FsmStartPreProcess, STATE.PreProcess);

            //PreProcess
            Transition(STATE.PreProcess, FSM_MSG.TIMER, FsmMonitorTask, STATE.PreProcess);
            Transition(STATE.PreProcess, MSG.ProcessAbort, null, STATE.Idle);
            Transition(STATE.PreProcess, MSG.Abort, null, STATE.Idle);

            //Process
            Transition(STATE.PreProcess, MSG.Process, FsmStartProcess, STATE.Process);
            Transition(STATE.Process, FSM_MSG.TIMER, FsmMonitorTask, STATE.Process);
            Transition(STATE.Process, MSG.ProcessAbort, FsmStartProcessAbort, STATE.ProcessAborting);
            Transition(STATE.Process, MSG.Abort, FsmStartProcessAbort, STATE.Aborting);
            Transition(STATE.ProcessAborting, FSM_MSG.TIMER, FsmMonitorProcessAbortingTask, STATE.Error);
            Transition(STATE.Aborting, FSM_MSG.TIMER, FsmMonitorProcessAbortingTask, STATE.Idle);
            //Transition(STATE.ProcessAborting, MSG.Abort, FsmAbortTask, STATE.Error);
            EnterExitTransition<STATE, FSM_MSG>(STATE.Process, FsmEnterProcess, FSM_MSG.NONE, FsmExitProcess);

            Transition(STATE.Process, MSG.Pause, FsmRecipePause, STATE.Paused);

            Transition(STATE.Paused, FSM_MSG.TIMER, FsmMonitorTask, STATE.Paused);
            Transition(STATE.Paused, MSG.Continue, FsmStartContinue, STATE.Process);
            Transition(STATE.Paused, MSG.Error, null, STATE.Paused);

            Transition(STATE.Process, MSG.RecipeSkipStep, FsmSkipStep, STATE.Process);

            //PostProcess
            Transition(STATE.Process, MSG.PostProcess, FsmStartPostProcess, STATE.PostProcess);
            Transition(STATE.PostProcess, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.PostProcess, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);
            EnterExitTransition<STATE, FSM_MSG>(STATE.PostProcess, null, FSM_MSG.NONE, ExitPostProcess);

            //MFC Calibration
            Transition(STATE.ProcessIdle, MSG.StartMFCCalibration, FsmStartMFCCalibration, STATE.MFCCali);
            Transition(STATE.MFCCali, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.MFCCali, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);

            Transition(STATE.Idle, MSG.SelectRecipe, FsmSelectRecipe, STATE.Idle);

            //PMMacro相关状态
            Transition(STATE.Idle, MSG.RunPMMacro, FsmStartPMMacro, STATE.PMMacro);
            Transition(STATE.VacIdle, MSG.RunPMMacro, FsmStartPMMacro, STATE.PMMacro);
            Transition(STATE.ProcessIdle, MSG.RunPMMacro, FsmStartPMMacro, STATE.PMMacro);
            Transition(STATE.PMMacro, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.PMMacro, MSG.Abort, FsmAbortTask, STATE.Idle);

            Transition(STATE.PMMacro, MSG.Pause, FsmPMMacroPause, STATE.PMMacroPause);
            Transition(STATE.PMMacroPause, FSM_MSG.TIMER, FsmMonitorTask, STATE.PMMacroPause);
            Transition(STATE.PMMacroPause, MSG.Continue, FsmPMMacroContinue, STATE.PMMacro);

            EnterExitTransition<STATE, FSM_MSG>(STATE.PMMacro, null, FSM_MSG.NONE, FsmExitPMMacro);

            //StopHeat
            Transition(STATE.ProcessIdle, MSG.StopHeat, FsmStartStopHeat, STATE.StopHeat);
            Transition(STATE.StopHeat, FSM_MSG.TIMER, FsmMonitorTask, STATE.ProcessIdle);
            Transition(STATE.StopHeat, MSG.Abort, FsmAbortTask, STATE.ProcessIdle);
        }

        private bool FsmToInit(object[] param)
        {
            return true;
        }

        private bool FsmExitError(object[] param)
        {
            return true;
        }

        private bool FsmEnterError(object[] param)
        {
            if (OnEnterError != null)
                OnEnterError(Module);

            if (IsOnline)
            {
                EV.PostWarningLog(Module, $"{Module}");
            }

            return true;
        }

        private bool FsmStartPostTransfer(object[] param)
        {
            bool needEnableHeat = false;
            if (param.Length > 0 && bool.TryParse(param[0].ToString(), out needEnableHeat))
            {
                _pmPostTransfer.Init(needEnableHeat);
            }
            Result ret = StartRoutine(_pmPostTransfer);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;

            return ret == Result.RUN;
        }

        private bool FsmStartPrepareTransfer(object[] param)
        {
            QueueRoutine.Clear();

            double targetPressure = SC.GetValue<double>($"TM.PressureBalance.BalancePressure");
            _pmServoToPressure.Init(targetPressure);
            QueueRoutine.Enqueue(_pmServoToPressure);

            Result ret = StartRoutine();
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartPreTransfer2(object[] param)
        {
            Result ret = StartRoutine(_pmPreTransfer);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;

            return ret == Result.RUN;
        }

        private bool FsmStartSetOffline(object[] param)
        {
            IsOnline = false;
            return true;
        }

        private bool FsmStartSetOnline(object[] param)
        {
            if (IsServiceIdle)
            {
                EV.PostWarningLog(Module, $"can not set {Module} online while in service mode");
                return false;
            }
            else
            {
                IsOnline = true;
                return true;
            }
        }

        private bool FsmToAtmIdle(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>("PM.IsAtmIdleRoutine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.AtmIdleFileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.AtmRoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                ret = StartRoutine(_toAtmIdleRoutine);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;

            return ret == Result.RUN;
        }

        private bool FsmVacIdle(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>("PM.IsVacIdleRoutine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.VacIdleFileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.VacRoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                ret = StartRoutine(_vacIdleRoutine);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;

            return ret == Result.RUN;
        }

        private bool FsmToProcessIdle(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>("PM.IsProcessIdleRoutine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.ProcessIdleFileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.ProcessIdleRoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                ret = StartRoutine(_pmtoProcessIdle);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;

            return ret == Result.RUN;
        }

        private bool FsmStartPump(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>("PM.IsPumpRoutine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.PumpFileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.PumpRoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                ret = StartRoutine(_pumpRoutine);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartVent(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>("PM.IsVentRoutine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.VentFileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.VentRoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                ret = StartRoutine(_ventRoutine);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;

            return ret == Result.RUN;
        }

        private bool FsmStartPurge(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>("PM.IsPurgeRoutine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.PurgeFileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.PurgeRoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                ret = StartRoutine(_purgeRoutine);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartClean(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>("PM.IsCleanRoutine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.CleanFileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.CleanRoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                ret = StartRoutine(_cleanRoutine);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartExchangeMO(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>($"PM.IsExchangeMO{param[0]}Routine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.ExchangeMO{param[0]}FileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.ExchangeMORoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                _changeMoRoutine.Init(param[0].ToString());
                ret = StartRoutine(_changeMoRoutine);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartToIsolation(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>("PM.IsIsolationRoutine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.IsolationFileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.IsolationRoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                ret = StartRoutine(_toIsolationRoutine);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartLeakCheck(object[] param)
        {
            Result ret;
            if (SC.GetValue<bool>("PM.IsLeakCheckRoutine"))
            {
                string RoutineFileName = SC.GetStringValue($"PM.{Module}.LeakCheckFileName");

                _pMMacroRoutine.Init(RoutineFileName, RoutineType.LeakCheckRoutine);
                ret = StartRoutine(_pMMacroRoutine);
            }
            else
            {
                if (param != null && param.Length >= 2)
                {
                    _leakCheckRoutine.Init((int)param[0], (int)param[1]);
                }
                else
                {
                    _leakCheckRoutine.Init();
                }

                ret = StartRoutine(_leakCheckRoutine);
            }

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmAbortTask(object[] param)
        {
            Result ret = Result.RUN;
            AbortRoutine();

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmToService(object[] param)
        {
            if (IsOnline)
            {
                return false;
            }
            else
            {
                IsServiceIdle = !IsServiceIdle;
            }
            return true;
        }

        private bool FsmStartStopHeat(object[] param)
        {
            Result ret = StartRoutine(_stopHeatEnableRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartMoveBody(object[] param)
        {
            _chamberMoveBodyRoutine.Init(param);
            Result ret = StartRoutine(_chamberMoveBodyRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartPcCalibration(object[] param)
        {
            Result ret = StartRoutine(_pmPcCalibrationRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartCleanRecipe(object[] param)
        {
            _cleanRecipe.Init(param[0].ToString());

            Result ret = StartRoutine(_cleanRecipe);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool ShowMessage(object[] param)
        {
            EV.PostWarningLog(Module, $"{param[0]}");
            return true;
        }

        private bool FsmStartOpenSlitValve(object[] param)
        {
            return true;
        }

        private bool FsmStartCloseSlitValve(object[] param)
        {
            return true;
        }

        private bool FsmSelectRecipe(object[] param)
        {
            _recipeRunningInfo.RecipeName = (string)param[0];
            return true;
        }

        private bool FsmStartPreProcess(object[] param)
        {
            if(IsServiceIdle)
            {
                EV.PostWarningLog(Module, $"can not set {Module} process while in service mode");
                return false;
            }

            _preprocessRoutine.Init((string)param[0], (bool)param[1], (bool)param[2]);
            Result ret = StartRoutine(_preprocessRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartProcess(object[] param)
        {
            Result ret = StartRoutine(_processRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmEnterProcess(object[] param)
        {
            WaferManager.Instance.UpdateWaferProcessStatus(ModuleHelper.Converter(Module), 0, EnumWaferProcessStatus.InProcess);

            return true;
        }

        private bool FsmExitProcess(object[] param)
        {
            if (!_processRoutine.IsPaused)
            {
                WaferManager.Instance.UpdateWaferProcessStatus(ModuleHelper.Converter(Module), 0, EnumWaferProcessStatus.Completed);
                _processRoutine.ExitProcess();
                if (!RecipeRunningInfo.IsRoutineAbort)
                {
                    EV.PostWarningLog(Module, $"Exit Process: Recipe Step:{_recipeRunningInfo.StepNumber}, " +
                                              $"Elapsed:{_recipeRunningInfo.StepElapseTime:F1}s");
                    //StopProcess();
                }
            }
            return true;
        }

        private bool FsmRecipePause(object[] param)
        {
            _processRoutine.PauseRecipe();
            return true;
        }

        private bool FsmStartContinue(object[] param)
        {
            _processRoutine.SetContinue((string)param[0]);
            return true;
        }

        private bool FsmSkipStep(object[] param)
        {
            _processRoutine.SkipCurrentRecipeStep();
            return true;
        }

        private bool FsmStartPostProcess(object[] param)
        {
            Result ret = StartRoutine(_postprocessRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool ExitPostProcess(object[] param)
        {
            _pmInterLock.SetPMProcessRunning(false, out string reason);
            _pmInterLock.SetPMPreProcessRunning(false, out reason);

            return true;
        }

        private bool FsmStartMFCCalibration(object[] param)
        {
            return true;
        }

        private bool FsmStartPMMacro(object[] param)
        {
            _pMMacroRoutine.Init((string)param[0]);
            Result ret = StartRoutine(_pMMacroRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmPMMacroPause(object[] param)
        {
            _pMMacroRoutine.PauseRecipe();
            return true;
        }

        private bool FsmPMMacroContinue(object[] param)
        {
            _pMMacroRoutine.SetContinue((string)param[0]);
            return true;
        }

        private bool FsmExitPMMacro(object[] param)
        {
            _pMMacroRoutine.ExitProcess();

            return true;
        }

        private bool FsmMonitorProcessAbortingTask(object[] param)
        {
            Result ret = MonitorRoutine();
            if (ret == Result.FAIL)
            {
                return false;
            }

            if (ret == Result.DONE)
            {
                return true;
            }

            return false;
        }

        private bool FsmStartProcessAbort(object[] param)
        {
            Result ret = StartRoutine(_pmProcessAbort);

            if (ret == Result.FAIL || ret == Result.DONE)
                return false;

            return ret == Result.RUN;
        }

        private bool FsmOnError(object[] param)
        {
            if (IsServiceIdle)
            {
                return false;
            }

            IsOnline = false;

            StopRamp();
            _pmInterLock.SetPMProcessRunning(false, out string reason);
            _pmInterLock.SetPMPreProcessRunning(false, out reason);

            if (FsmState.Equals((int)STATE.Process))
            {
                PostMsg(MSG.ProcessAbort);
                return false;
            }

            if (FsmState.Equals((int)STATE.ProcessAborting))
            {
                return false;
            }

            return true;
        }

        private bool FsmOnError1(object[] param)
        {
            IsOnline = false;

            if (FsmState.Equals((int)STATE.Error))
            {
                return false;
            }
            else
            {
                StopRamp();
                _pmInterLock.SetPMProcessRunning(false, out string reason);
                _pmInterLock.SetPMPreProcessRunning(false, out reason);
            }

            return true;
        }

        private bool FsmReset(object[] param)
        {
            var alarms = EV.GetAlarmEvent();
            int alarmCount = alarms != null ? alarms.FindAll(a => a.Level == EventLevel.Alarm && a.Source == Module).Count : 0;
            if (alarmCount > 0)
            {
                EV.ClearAlarmEvent();
            }

            _pmInterLock.Reset();
            TC1.Reset();
            TC2.Reset();

            if (!_isInitFlag)
            {
                PostMsg(MSG.ToInit);
                return false;
            }
            return true;
        }

        private bool FsmStartHome(object[] param)
        {
            _isInitFlag = false;
            
            _pmInterLock.Reset();
            TC1.Reset();
            TC2.Reset();
            
            return true;
        }

        private bool FsmExitHome(object[] param)
        {
            return true;
        }

        private bool FsmEnterHome(object[] param)
        {
            return true;
        }

        private bool FsmMonitorHomeTask(object[] param)
        {
            Result ret = MonitorRoutine();
            if (ret == Result.FAIL)
            {
                PostMsg(MSG.Error1);
                return false;
            }

            if (ret == Result.DONE)
            {
                _isInitFlag = true;
                return true;
            }

            return false;
        }

        private bool FsmMonitorSafetyTask(object[] param)
        {
            Result ret = MonitorRoutine();
            if (ret == Result.FAIL)
            {
                PostMsg(MSG.Error1);
                return false;
            }

            if (ret == Result.DONE)
            {
                return true;
            }

            return false;
        }

        private FixSizeQueue<double> InnerTempRecordQueue = new FixSizeQueue<double>(16);
        private FixSizeQueue<double> MiddleTempRecordQueue = new FixSizeQueue<double>(16);
        private FixSizeQueue<double> OuterTempRecordQueue = new FixSizeQueue<double>(16);
        private void MonitorAETempRasingFastAlarm()
        {
            var AETempInnerRasingRate = SC.GetValue<double>($"PM.{Module}.Heater.SCRTempRasingRate");
            var AETempMiddleRasingRate = SC.GetValue<double>($"PM.{Module}.Heater.AETempMiddleRasingRate");
            var AETempOuterRasingRate = SC.GetValue<double>($"PM.{Module}.Heater.AETempOuterRasingRate");
            var AETempRasingFastIsAlarm = SC.GetValue<bool>($"PM.{Module}.Heater.AETempRasingFastIsAlarm");

            if (FsmState == (int)STATE.Process)
            {

                //if (InnerTempRecordQueue.Count == 16)
                //{
                //    var InnerHalfBeforeAverage = InnerTempRecordQueue.ToList().Take(8).ToList().Average();
                //    var InnerHalfAfterAverage = InnerTempRecordQueue.ToList().Skip(8).ToList().Average();

                //    if (Math.Abs(InnerHalfAfterAverage - InnerHalfBeforeAverage) > AETempInnerRasingRate)
                //    {
                //        if (AETempRasingFastIsAlarm)
                //        {
                //            EV.PostAlarmLog(Module, $"AETemp Inner rasing fast");
                //        }
                //        else
                //        {
                //            EV.PostWarningLog(Module, $"AETemp Inner rasing fast");
                //        }
                //    }

                //}

                if (MiddleTempRecordQueue.Count == 16)
                {
                    var MiddleHalfBeforeAverage = MiddleTempRecordQueue.ToList().Take(8).ToList().Average();
                    var MiddleHalfAfterAverage = MiddleTempRecordQueue.ToList().Skip(8).ToList().Average();

                    if (Math.Abs(MiddleHalfAfterAverage - MiddleHalfBeforeAverage) > AETempMiddleRasingRate)
                    {
                        if (AETempRasingFastIsAlarm)
                        {
                            EV.PostAlarmLog(Module, $"AETemp Middle rasing fast");
                            TC1.ActivateAlarmDoAeTempRaisingFast();
                        }
                        else
                        {
                            EV.PostWarningLog(Module, $"AETemp Middle rasing fast");
                        }
                    }
                }

                if (OuterTempRecordQueue.Count == 16)
                {
                    var OuterHalfBeforeAverage = MiddleTempRecordQueue.ToList().Take(8).ToList().Average();
                    var OuterHalfAfterAverage = MiddleTempRecordQueue.ToList().Skip(8).ToList().Average();
                    if (Math.Abs(OuterHalfAfterAverage - OuterHalfBeforeAverage) > AETempOuterRasingRate)
                    {
                        if (AETempRasingFastIsAlarm)
                        {
                            EV.PostAlarmLog(Module, $"AETemp Outer rasing fast");
                        }
                        else
                        {
                            EV.PostWarningLog(Module, $"AETemp Outer rasing fast");
                        }
                    }
                }

            }

            InnerTempRecordQueue.Enqueue(TC1.InnerTemp);
            MiddleTempRecordQueue.Enqueue(TC1.MiddleTemp);
            OuterTempRecordQueue.Enqueue(TC1.OuterTemp);


            //tempMonitorDT.Start(0);
        }

        private bool FsmMonitorTask(object[] param)
        {

            MonitorAETempRasingFastAlarm();

            Result ret = MonitorRoutine();
            if (ret == Result.FAIL)
            {
                PostMsg(MSG.Error1);
                return false;
            }

            if (ret == Result.DONE && FsmState == (int)STATE.PreProcess)
            {
                PostMsg(MSG.Process);
                return true;
            }

            if (ret == Result.DONE && FsmState == (int)STATE.Process)
            {
                PostMsg(MSG.PostProcess);
                return true;
            }

            return ret == Result.DONE;
        }

        public override bool Home(out string reason)
        {
            CheckToPostMessage((int)MSG.Home);
            reason = string.Empty;
            return true;
        }

        public override bool TransferHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override bool PostTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override bool CheckReadyForTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType,
            out string reason)
        {

            reason = string.Empty;
            return true;
        }

        public override void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType)
        {

        }

        public override void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType)
        {

        }

        public override bool CheckLidClosed()
        {
            throw new System.NotImplementedException();
        }

        public void InvokeOffline()
        {
            PutOffline();
            //PostMsg((int)MSG.SetOffline);
        }

        public void InvokeOnline()
        {
            PutOnline();
            //PostMsg((int)MSG.SetOnline);
        }

        #region RecipeEditorPassword
        public string EPassword
        {
            get
            {
                return SC.GetConfigItem("System.Recipe.EditPassword").StringValue;
            }

            set
            {
                ;
            }
        }

        public bool WriteEPassword(string sEPassword)
        {
            try
            {
                SC.SetItemValue("System.Recipe.EditPassword", sEPassword);
                return false;
            }
            catch
            {
                EV.PostWarningLog(Module, "Change EditorPassword error.");
            }
            return true;
        }



        #endregion

    }
}
