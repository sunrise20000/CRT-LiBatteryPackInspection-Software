using System;
using System.Collections.Generic;
using System.Linq;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Jobs;
using SicRT.Equipments.Schedulers;
using SicRT.Modules.Schedulers;


namespace SicRT.Modules
{
    public partial class AutoTransfer : SchedulerModuleFactory
    {
        public class TrayInfo
        {
            public int TrayModule { get; set; }
            public int TraySlot { get; set; }
            public int TrayProcessCount { get; set; }
        }

        private List<ControlJobInfo> _lstNeedMapJob = new List<ControlJobInfo>();
        private List<ControlJobInfo> _lstControlJobs = new List<ControlJobInfo>();
        private List<ProcessJobInfo> _lstProcessJobs = new List<ProcessJobInfo>();
        private List<TrayInfo> _lstProcessedTrayInfo = new List<TrayInfo>();  //保存石墨盘的进入腔体的信息(最多4次工艺需要换盘)

        private int _maxTrayCount = 2;                      //可以同时运行的石墨盘总数
        private bool _cassProcessOnebyOne = false;          //Cassette是左右交互取片还是一个Cassette取完再取另一个
        private readonly R_TRIG _notifyTrayExhausted = new R_TRIG();
        private readonly R_TRIG _trigLoadUnLoadAllPumpAlarm = new R_TRIG();
        private readonly R_TRIG _trigUnLoadWaferLeave = new R_TRIG();

        private const string LogSource = "Scheduler";
        private SchedulerDBCallback _dbCallback;
        //private const string StatsNameTotalRunningWafer = "TotalRunningWafer";

        public bool CassetteFromAToB => SC.GetValue<bool>("System.Scheduler.CassetteFromAToB");

        private bool _isCycleMode;
        private int _cycleSetPoint = 0;
        private int _cycledCount = 0;
        private int _cycledWafer = 0;
        private string _curRecipeName = "";
        private string _curSequenceName = "";

        private bool _isModuleErrorPrevious;
        private bool[] _isPMErrorPrevious = new bool[2];

        private string _timeBuffer1 { get; set; }
        private string _timeBuffer2 { get; set; }
        private string _timeBuffer3 { get; set; }

        private Dictionary<string, DateTime> _bufferWaferInfo = new Dictionary<string, DateTime>();

        private string _sAutoTransferConditionText;
        public string AutoTransferConditionText
        {
            get
            {
                return _sAutoTransferConditionText;
            }
            set
            {
                _sAutoTransferConditionText = value;
            }
        }
        private string _sWhichCondition;
        public string WhichCondition
        {
            get
            {
                return _sWhichCondition;
            }
            set
            {
                _sWhichCondition = value;
            }
        }
        //

        public AutoTransfer()
        {
            _dbCallback = new SchedulerDBCallback();

            DATA.Subscribe("Scheduler.IsCycleMode", () => _isCycleMode);
            DATA.Subscribe("Scheduler.CycledCount", () => _cycledCount);
            DATA.Subscribe("Scheduler.CycleSetPoint", () => _cycleSetPoint);
            DATA.Subscribe("Scheduler.RecipeName", () => _curRecipeName);
            DATA.Subscribe("Scheduler.SequenceName", () => _curSequenceName);

            DATA.Subscribe("Scheduler.TimeBuffer1", () => _timeBuffer1);
            DATA.Subscribe("Scheduler.TimeBuffer2", () => _timeBuffer2);
            DATA.Subscribe("Scheduler.TimeBuffer3", () => _timeBuffer3);

            //显示所需条件信息用
            DATA.Subscribe("Scheduler.AutoTransferConditionText", () => AutoTransferConditionText);
            //要显示哪个任务的条件
            DATA.Subscribe("Scheduler.WhichCondition", () => WhichCondition);

            //DATA.Subscribe("CassAL.SlotSequenceList", () => currentSlotSequenceList["CassAL"]);
            DATA.Subscribe("CassAL.LocalJobName", () =>
            {
                var jb = _lstControlJobs.FirstOrDefault(x => x.Module == "CassAL");
                if (jb != null)
                    return jb.Name;
                return "";
            });
            DATA.Subscribe("CassAL.LocalJobStatus", () =>
            {
                var jb = _lstControlJobs.FirstOrDefault(x => x.Module == "CassAL");
                if (jb != null)
                    return jb.State.ToString();
                return "";
            });
            //DATA.Subscribe("CassAR.SlotSequenceList", () => currentSlotSequenceList["CassAR"]);
            DATA.Subscribe("CassAR.LocalJobName", () =>
            {
                var jb = _lstControlJobs.FirstOrDefault(x => x.Module == "CassAR");
                if (jb != null)
                    return jb.Name;
                return "";
            });
            DATA.Subscribe("CassAR.LocalJobStatus", () =>
            {
                var jb = _lstControlJobs.FirstOrDefault(x => x.Module == "CassAR");
                if (jb != null)
                    return jb.State.ToString();
                return "";
            });

            OP.Subscribe($"{ModuleName.TM}.ResetTask", (string cmd, object[] args) =>
            {
                
                return true;
            });

            OP.Subscribe($"{ModuleName.EFEM}.ResetTask", (string cmd, object[] args) =>
            {
               
                return true;
            });

            OP.Subscribe($"{ModuleName.WaferRobot}.ResetTask", (string cmd, object[] args) =>
            {
                
                return true;
            });

            OP.Subscribe($"{ModuleName.TrayRobot}.ResetTask", (string cmd, object[] args) =>
            {
                
                return true;
            });
            
        }

        public bool HasJobRunning => _lstControlJobs.Count > 0;


        public void Clear()
        {
            foreach (var sch in LstAllSchedulers)
            {
                sch.ResetTask();
            }

            _lstControlJobs.Clear();
            _lstProcessJobs.Clear();
            _bufferWaferInfo.Clear();
        }

        public void ResetTask()
        {
            foreach (var sch in LstAllSchedulers.Where(sch => !sch.IsOnline))
            {
                sch.ResetTask();
            }
        }

        public void GetConfig()
        {
            _cycledCount = 0;

            _isCycleMode = SC.GetValue<bool>("System.IsCycleMode");
            _cycleSetPoint = _isCycleMode ? SC.GetValue<int>("System.CycleCount") : 0;
            
        }

        /// <summary>
        /// Start Auto Transfer
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        public Result Start(params object[] objs)
        {
            GetConfig();

            return Result.RUN;
        }


        public Result Monitor()
        {
           
            ControlJobInfo cjActive = _lstControlJobs.Find(x => x.State == EnumControlJobState.Executing);
            if (cjActive != null)
            {
                MonitorModuleTasks();
            }

            MonitorModuleError();

            MonitorModuleState();
            
            MonitorCleanTasks();

            // 如果没有Module发生错误，则将信号灯设置为绿色
            if (!LstAllSchedulers.Any(x => x.IsError)) ;
                //_signalTower..CreateLight(LightType.Green);

            return Result.RUN;
        }


        #region Module task

        public Result MonitorModuleTasks()
        {
            MonitorAlignerTask();

            return Result.RUN;
        }
        
        private void MonitorAlignerTask()
        {
            if (!SchAligner.IsAvailable)
            {
                return;
            }

            
            if (SchAligner.FirstDetectWaferArrive(0) || SchAligner.FirstDetectWaferLeave(0))
            {
                SchAligner.ResetAlignedStatus();
            }

            if (!SchAligner.IsAvailable)
            {
                return;
            }

            if (SchAligner.HasWafer(0) && SchAligner.CheckWaferNextStepIsThis(ModuleName.Aligner, 0))
            {
                SchAligner.Aligning();
                SchAligner.GetWaferInfo(0).NextSequenceStep++;
            }
        }


        #endregion
        

        #region Module error

        public Result MonitorModuleError()
        {
            return Result.RUN;
        }

        public Result MonitorModuleState()
        {
            bool controlJobExecuting = false;
            bool isModuleBusy = false;

            foreach (var controlJob in _lstControlJobs)
            {
                if (controlJob.State == EnumControlJobState.Executing)
                    controlJobExecuting = true;
            }

            //if (!controlJobExecuting)
            //    return Result.RUN;

            //if (_tmRobot.IsOnline && _tmRobot.Entity.IsBusy)
            //    isModuleBusy = true;

            //if (_efemRobot.IsOnline && _efemRobot.Entity.IsBusy)
            //    isModuleBusy = true;

            //foreach (var pm in _lstPms) // PM出错，不影响其他腔体的传片
            //{
            //    if (pm.IsOnline && pm.Entity.IsBusy)
            //        isModuleBusy = true;
            //}

            //foreach (var ll in _lstLls)
            //{
            //    if (ll.IsOnline && ll.Entity.IsBusy)
            //        isModuleBusy = true;
            //}

            //foreach (var lp in _lstLps)
            //{
            //    if (lp.IsOnline && lp.Entity.IsBusy)
            //        isModuleBusy = true;
            //}

            //if (SchAligner.IsOnline && SchAligner.Entity.IsBusy)
            //    isModuleBusy = true;

            //if (isModuleBusy)
            //{
            //    _timer.Stop();
            //    _started = false;
            //}
            //else
            //{
            //    if (!_started)
            //    {
            //        _timer.Start(_deadLockTimeout * 1000);
            //        _started = true;
            //    }
            //}

            //_trigDeadLock.CLK = _timer.IsTimeout();

            //if (_trigDeadLock.Q)
            //{
            //    EV.PostWarningLog("System", "A deadlock has occurred");
            //}

            return Result.RUN;
        }

        public Result MonitorCleanTasks()
        {
            //if (!_isInited)
            //{
            //    _isInited = true;
            //    InitClean();
            //}
            //foreach (var pm in _lstPms)
            //{
            //    pm.MonitorCleanTasks();
            //}

            return Result.RUN;
        }
        
        #endregion
    }
}