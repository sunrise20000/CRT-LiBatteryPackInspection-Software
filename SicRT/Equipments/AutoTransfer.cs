using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Jobs;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using SicRT.Equipments.Schedulers;
using SicRT.Modules.Schedulers;
using SicRT.Scheduler;


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

        private SicPM.Devices.IoSignalTower _signalTower;

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
                SchTmRobot.ResetTask();
                return true;
            });

            OP.Subscribe($"{ModuleName.EFEM}.ResetTask", (string cmd, object[] args) =>
            {
                SchWaferRobot.ResetTask();
                SchTrayRobot.ResetTask();
                return true;
            });

            OP.Subscribe($"{ModuleName.WaferRobot}.ResetTask", (string cmd, object[] args) =>
            {
                SchWaferRobot.ResetTask();
                return true;
            });

            OP.Subscribe($"{ModuleName.TrayRobot}.ResetTask", (string cmd, object[] args) =>
            {
                SchTrayRobot.ResetTask();
                return true;
            });

            _signalTower = DEVICE.GetDevice<SicPM.Devices.IoSignalTower>("PM1.SignalTower");
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
            _maxTrayCount = LstPmSchedulers.Count * 2;
        }

        #region Job Management

        public bool CreateJob(Dictionary<string, object> param)
        {
            string reason = "";
            string[] slotSequence = (string[])param["SlotSequence"];
            string jobId = (string)param["JobId"];
            string module = (string)param["Module"];
            bool autoStart = (bool)param["AutoStart"];
            string lotId = jobId;
            if (param.ContainsKey("LotId"))
                lotId = (string)param["LotId"];

            if (CheckModuleHaveWaferWithNoJob(out reason))
            {
                EV.PostWarningLog(LogSource, $"{reason}");
                return false;
            }

            if (!ValidateSequence(slotSequence, out reason))
            {
                EV.PostWarningLog(LogSource, $"{reason}");
                return false;
            }

            if (slotSequence.Length != 25)
            {
                reason = $"slot sequence parameter not valid, length is {slotSequence.Length}, should be 25";
                EV.PostWarningLog(LogSource, reason);
                return false;
            }

            if (string.IsNullOrEmpty(jobId))
            {
                jobId = "CJ_Local_" + module + DateTime.Now;
            }

            if (_lstControlJobs.Exists(x => x.Name == jobId))
            {
                reason = $"LotID : {jobId} already created";
                EV.PostWarningLog(LogSource, reason);
                return false;
            }


            ControlJobInfo cj = new ControlJobInfo();
            cj.Name = jobId;
            cj.Module = module;
            cj.LotName = jobId;
            cj.LotInnerId = Guid.NewGuid();
            cj.LotWafers = new List<WaferInfo>();
            cj.SetState(EnumControlJobState.WaitingForStart);

            Dictionary<string, bool[]> seqSlot = new Dictionary<string, bool[]>();
            Dictionary<string, List<Tuple<ModuleName, int>>> seqSlotWafers = new Dictionary<string, List<Tuple<ModuleName, int>>>();
            Dictionary<string, string> indexSequence = new Dictionary<string, string>();

            bool enableGroupBySequence = SC.GetValue<bool>("System.Scheduler.GroupWaferBySequence");
            string WaferAssociationInfo = $"WaferAssociationInfo({module}):";
            for (int i = 0; i < 25; i++)
            {
                WaferAssociationInfo = WaferAssociationInfo + string.Format(" slot{0} -- {1};", i + 1, slotSequence[i]);
                if (string.IsNullOrEmpty(slotSequence[i]) || string.IsNullOrEmpty(slotSequence[i].Trim()))
                    continue;

                string groupName = enableGroupBySequence ? slotSequence[i].Trim() : i.ToString();
                indexSequence[groupName] = slotSequence[i];
                if (!seqSlot.ContainsKey(groupName))
                {
                    seqSlot[groupName] = new bool[25];
                }
                if (!seqSlotWafers.ContainsKey(groupName))
                {
                    seqSlotWafers[groupName] = new List<Tuple<ModuleName, int>>();
                }
                seqSlot[groupName][i] = true;

                if (!WaferManager.Instance.CheckHasWafer(module, i))
                {
                    reason = $"job wafer: {module} slot {i + 1} not in the carrier";
                    EV.PostWarningLog(LogSource, reason);
                    return false;
                }
                if (!WaferManager.Instance.CheckWafer(ModuleHelper.Converter(module), i, WaferStatus.Normal))
                {
                    reason = $"job wafer: {module} slot {i + 1} status is {WaferManager.Instance.GetWafer(ModuleHelper.Converter(module), i).Status}";
                    EV.PostWarningLog(LogSource, reason);
                    return false;
                }

                var wafer = WaferManager.Instance.GetWafer(ModuleHelper.Converter(module), i);
                if (wafer == null || wafer.IsEmpty)
                {
                    reason = $"specifies wafer: {module} slot {i + 1} not in the carrier";
                    EV.PostWarningLog(LogSource, reason);
                    return false;
                }
                if (wafer.ProcessState != EnumWaferProcessStatus.Idle)
                {
                    reason = $"specifies wafer: {module} slot {i + 1} process state is not idle";
                    EV.PostWarningLog(LogSource, reason);
                    return false;
                }
                if (wafer.ProcessJob != null)
                {
                    reason = $"specifies wafer: {module} slot {i + 1} ProcessJob is not null";
                    EV.PostWarningLog(LogSource, reason);
                    return false;
                }
                if (wafer.SubstE90Status != EnumE90Status.NeedProcessing)
                {
                    reason = $"specifies wafer: {module} slot {i + 1} SubstE90Status is not NeedProcessing";
                    EV.PostWarningLog(LogSource, reason);
                    return false;
                }

                seqSlotWafers[groupName].Add(Tuple.Create(ModuleHelper.Converter(module), i));
                cj.LotWafers.Add(WaferManager.Instance.GetWafer(ModuleHelper.Converter(module), i));
                WaferManager.Instance.GetWafer(ModuleHelper.Converter(module), i).PPID = slotSequence[i];
            }

            //currentSlotSequenceList[module] = slotSequence.Reverse().ToList();
            EV.PostInfoLog(LogSource, WaferAssociationInfo);

            if (seqSlotWafers.Count == 0)
            {
                reason = $"Can not create job, no wafer assigned";
                EV.PostWarningLog(LogSource, reason);
                return false;
            }

            List<ProcessJobInfo> pjs = new List<ProcessJobInfo>();
            string[] seqs = seqSlot.Keys.ToArray();
            for (int i = 0; i < seqs.Length; i++)
            {
                ProcessJobInfo pj = new ProcessJobInfo();
                pj.Name = jobId + "_" + (i + 1);
                pj.Sequence = SequenceInfoHelper.GetInfo(indexSequence[seqs[i]]);
                pj.ControlJobName = cj.Name;
                //pj.LotName = lotId;
                pj.SlotWafers = seqSlotWafers[seqs[i]];
                pj.SetState(EnumProcessJobState.Queued);

                //if (!CheckSequencePmReady(pj.Sequence, null, out _, out string innerReason))
                //{
                //    reason = $"no valid chamber for the {innerReason}";
                //    EV.PostWarningLog(LogSource, reason);
                //    return false;
                //}
                //if (!CheckSequenceRecipeFileValid(pj.Sequence, out reason))
                //{
                //    reason = $"recipe file not valid in the sequence, {reason}";
                //    EV.PostWarningLog(LogSource, reason);
                //    return false;
                //}

                //if (!CheckSequenceOrderOk(pj.Sequence, out reason))
                //{
                //    reason = $"sequence path not valid, {reason}";
                //    EV.PostWarningLog(LogSource, reason);
                //    return false;
                //}

                pjs.Add(pj);
            }

            foreach (var pj in pjs)
            {
                cj.ProcessJobNameList.Add(pj.Name);
                _lstProcessJobs.Add(pj);
            }

            _lstControlJobs.Add(cj);

            int totalWafer = 0;
            foreach (var pj in pjs)
            {
                foreach (var pjSlotWafer in pj.SlotWafers)
                {
                    WaferInfo wafer = WaferManager.Instance.GetWafer(pjSlotWafer.Item1, pjSlotWafer.Item2);
                    wafer.ProcessJob = pj;
                    WaferDataRecorder.SetCjInfo(wafer.InnerId.ToString(), cj.InnerId.ToString());
                    WaferDataRecorder.SetWaferSequence(wafer.InnerId.ToString(), pj.Sequence.Name);
                    WaferDataRecorder.SetWaferLotId(wafer.InnerId.ToString(), jobId);
                    WaferManager.Instance.UpdateWaferLotId(pjSlotWafer.Item1, pjSlotWafer.Item2, jobId);

                    totalWafer++;
                }
            }

            CarrierManager.Instance.DeleteCarrier(cj.Module);
            CarrierManager.Instance.CreateCarrier(cj.Module);
            CarrierManager.Instance.UpdateCarrierId(cj.Module, $"{cj.Module} {DateTime.Now}");
            CarrierInfo carrier = CarrierManager.Instance.GetCarrier(cj.Module);
            JobDataRecorder.StartCJ(cj.InnerId.ToString(), carrier.InnerId.ToString(), cj.Name, cj.Module, cj.Module, totalWafer);
            return true;
        }

        public bool CreateJobEx(Dictionary<string, object> param)
        {
            string reason = "";
            string[] slotSequence = (string[])param["SlotSequence"];
            string jobId = (string)param["JobId"];
            string module = (string)param["Module"];
            bool autoStart = (bool)param["AutoStart"];

            if (slotSequence.Length != 25)
            {
                reason = $"slot sequence parameter not valid, slot count is {slotSequence.Length}, should be 25";
                EV.PostWarningLog(LogSource, reason);
                return false;
            }


            if (!ValidateSequence(slotSequence, out reason))
            {
                EV.PostWarningLog(LogSource, $"{reason}");
                return false;
            }


            if (string.IsNullOrEmpty(jobId))
            {
                jobId = "CJ_Local_" + module;
            }

            for (int i = 0; i < 25; i++)
            {
                if (string.IsNullOrEmpty(slotSequence[i]) || string.IsNullOrEmpty(slotSequence[i].Trim()))
                {
                    continue;
                }
            }

            if (CheckModuleHaveWaferWithNoJob(out reason))
            {
                return false;
            }




            //bool cassAHasWafer = false;
            //bool cassBHasWafer = false;
            //for (int i = 0; i < 25; i++)
            //{
            //    if (SchCassAL.HasWafer(i))
            //    {
            //        cassAHasWafer = true;
            //        break;
            //    }
            //}
            //for (int i = 0; i < 25; i++)
            //{
            //    if (SchCassAR.HasWafer(i))
            //    {
            //        cassBHasWafer = true;
            //        break;
            //    }
            //}

            ////从CassetteA出发，运行流程后回到另一个Cassette,需要一个Cassette有片子，一个Cassette没有片子
            //if (_cassetteFromAToB)
            //{
            //    if (cassAHasWafer && cassBHasWafer)
            //    {
            //        EV.PostWarningLog(LogSource, $"Cassette AL and Cassette AR both have wafer,Can not Run Sequence From CassetteAL to CassetteAR!");
            //        return;
            //    }
            //}

            //for (int i = 0; i < 25; i++)
            //{
            //    if (SchCassAL.HasWafer(i))
            //    {
            //        WaferInfo wafer = SchCassAL.GetWaferInfo(i);
            //        if (wafer.ProcessState == EnumWaferProcessStatus.Idle)
            //        {
            //            wafer.NextSequenceStep = 0;
            //        }
            //    }
            //    if (SchCassAR.HasWafer(i))
            //    {
            //        WaferInfo wafer = SchCassAR.GetWaferInfo(i);
            //        if (wafer.ProcessState == EnumWaferProcessStatus.Idle)
            //        {
            //           wafer.NextSequenceStep = 0;
            //        }
            //    }
            //}

            Dictionary<string, bool[]> seqSlot = new Dictionary<string, bool[]>();
            Dictionary<string, List<int>> seqSlotWafers = new Dictionary<string, List<int>>();
            Dictionary<string, string> indexSequence = new Dictionary<string, string>();

            bool enableGroupBySequence = false;
            for (int i = 0; i < 25; i++)
            {
                if (string.IsNullOrEmpty(slotSequence[i]) || string.IsNullOrEmpty(slotSequence[i].Trim()))
                    continue;

                string groupName = enableGroupBySequence ? slotSequence[i].Trim() : i.ToString();
                indexSequence[groupName] = slotSequence[i];

                if (!seqSlot.ContainsKey(groupName))
                {
                    seqSlot[groupName] = new bool[25];
                }

                if (!seqSlotWafers.ContainsKey(groupName))
                {
                    seqSlotWafers[groupName] = new List<int>();
                }

                seqSlot[groupName][i] = true;
                seqSlotWafers[groupName].Add(i);
            }

            if (seqSlotWafers.Count == 0)
            {
                reason = $"Can not create job, no wafer assigned";
                EV.PostWarningLog(LogSource, reason);
                return false;
            }

            List<ProcessJobInfo> pjs = new List<ProcessJobInfo>();
            string[] seqs = seqSlot.Keys.ToArray();

            List<string> pjIDs = new List<string>();
            for (int i = 0; i < seqs.Length; i++)
            {
                var name = jobId + "_" + (i + 1);

                if (!CreateProcessJob(name, indexSequence[seqs[i]], seqSlotWafers[seqs[i]], autoStart)) //ProcessJob创建失败直接返回，不再创建ControlJob
                {
                    return false;

                }

                pjIDs.Add(name);
            }

            if (!CreateControlJob(jobId, module, pjIDs, autoStart))
            {
                foreach (var pjName in pjIDs)
                {
                    var pj = _lstProcessJobs.FirstOrDefault(x => x.Name == pjName);
                    if (pj != null)
                        _lstProcessJobs.Remove(pj);
                }
            }

            return true;
        }

        private bool ValidateSequence(string[] seqs, out string reason)
        {
            reason = string.Empty;
            bool isAllSequenceNull = true;
            for (int i = 0; i < seqs.Length; i++)
            {
                if (string.IsNullOrEmpty(seqs[i]))
                    continue;
                var sequence = SequenceInfoHelper.GetInfo(seqs[i]);
                if ((sequence == null || sequence.Steps == null || sequence.Steps.Count == 0) && !string.IsNullOrEmpty(sequence.Name))
                {
                    reason = $"Invalid sequence {seqs[i]}";
                    return false;
                }

                isAllSequenceNull = false;

                if (sequence.Steps != null)
                {
                    int currentIndex = 0;
                    if (sequence.Steps[currentIndex] == null || sequence.Steps[currentIndex].StepModules.Count <= 0 || sequence.Steps[currentIndex].StepModules[0] != ModuleName.Aligner)
                    {
                        reason = $"Invalid sequence {seqs[i]}, {currentIndex + 1}st step should be Aligner";
                        return false;
                    }
                    else
                    {
                        if (sequence.Steps[currentIndex].AlignAngle < 0 || sequence.Steps[currentIndex].AlignAngle > 360)
                        {
                            reason = $"Invalid sequence {seqs[i]},{currentIndex + 1}st step Aligner angle parameter is not valid";
                            return false;
                        }
                    }

                    currentIndex++;
                    if (sequence.Steps[currentIndex] == null || sequence.Steps[currentIndex].StepModules.Count <= 0 || sequence.Steps[currentIndex].StepModules[0] != ModuleName.Load)
                    {
                        reason = $"Invalid sequence {seqs[i]}, {currentIndex + 1}st step should be Load";
                        return false;
                    }

                    currentIndex++;
                    if (sequence.Steps[currentIndex] == null || sequence.Steps[currentIndex].StepModules.Count <= 0 || sequence.Steps[currentIndex].StepModules[0] != ModuleName.Buffer)
                    {
                        reason = $"Invalid sequence {seqs[i]}, {currentIndex + 1}st step should be Buffer";
                        return false;
                    }
                    else
                    {
                        if (!sequence.Steps[currentIndex].StepParameter.ContainsKey("SlotSelection"))
                        {
                            reason = $"Invalid sequence {seqs[i]},{currentIndex + 1}st step the parameter 'SlotSelection' of Buffer cannot be found.";
                            return false;
                        }

                        if (string.IsNullOrEmpty(sequence.Steps[currentIndex].StepParameter["SlotSelection"]
                                .ToString()))
                        {
                            reason = $"Invalid sequence {seqs[i]},{currentIndex + 1}st step the parameter 'SlotSelection' of Buffer is not specified.";
                            return false;
                        } 
                        
                        /*if(sequence.Steps[currentIndex].StepParameter["SlotSelection"].ToString().Contains("3"))
                        {
                            reason = $"Invalid sequence {seqs[i]},{currentIndex + 1}st step the option '3' cannot be selected in parameter 'SlotSelection' of Buffer.";
                            return false;
                        }*/

                        if (!sequence.Steps[currentIndex].StepParameter.ContainsKey("BufferType") || !sequence.Steps[currentIndex].StepParameter["BufferType"].ToString().Contains("Heat"))
                        {
                            reason = $"Invalid sequence {seqs[i]},{currentIndex + 1}st step Buffer BufferType parameter is not valid";
                            return false;
                        }
                    }


                    currentIndex++;
                    if (sequence.Steps[currentIndex] == null || sequence.Steps[currentIndex].StepModules.Count <= 0 || sequence.Steps[currentIndex].StepModules.Any(pm => !ModuleHelper.IsPm(pm)))
                    {
                        reason = $"Invalid sequence {seqs[i]}, {currentIndex + 1}st step should be PM";
                        return false;
                    }

                    if (sequence.Steps.Count == 7)
                    {
                        currentIndex++;
                        if (sequence.Steps[currentIndex] == null || sequence.Steps[currentIndex].StepModules.Count <= 0 || sequence.Steps[currentIndex].StepModules[0] != ModuleName.Buffer)
                        {
                            reason = $"Invalid sequence {seqs[i]}, {currentIndex + 1}st step should be Buffer";
                            return false;
                        }
                        else
                        {
                            if (!sequence.Steps[currentIndex].StepParameter.ContainsKey("SlotSelection"))
                            {
                                reason = $"Invalid sequence {seqs[i]},{currentIndex + 1}st step the parameter 'SlotSelection' of Buffer cannot be found.";
                                return false;
                            }
                            
                            if(sequence.Steps[currentIndex].StepParameter["SlotSelection"].ToString() != "3")
                            {
                                reason = $"Invalid sequence {seqs[i]},{currentIndex + 1}st step only the '3' option must be selected in the parameter 'SlotSelection' of Buffer. ";
                                return false;
                            }

                            if (!sequence.Steps[currentIndex].StepParameter.ContainsKey("BufferType") || !sequence.Steps[currentIndex].StepParameter["BufferType"].ToString().Contains("Cooling"))
                            {
                                reason = $"Invalid sequence {seqs[i]},{currentIndex + 1}st step the parameter 'BufferType' of Buffer must be 'Cooling'.";
                                return false;
                            }
                        }
                    }

                    currentIndex++;
                    if (sequence.Steps[currentIndex] == null || sequence.Steps[currentIndex].StepModules.Count <= 0 || sequence.Steps[currentIndex].StepModules[0] != ModuleName.UnLoad)
                    {
                        reason = $"Invalid sequence {seqs[i]}, {currentIndex + 1}st step should be Unload";
                        return false;
                    }

                    currentIndex++;
                    if (sequence.Steps[currentIndex] == null || sequence.Steps[currentIndex].StepModules.Count <= 0 || sequence.Steps[currentIndex].StepModules[0] != ModuleName.Aligner)
                    {
                        reason = $"Invalid sequence {seqs[i]}, {currentIndex + 1}st step should be Aligner";
                        return false;
                    }
                    else
                    {
                        if (sequence.Steps[0].AlignAngle < 0 || sequence.Steps[0].AlignAngle > 360)
                        {
                            reason = $"Invalid sequence {seqs[i]}, Aligner angle parameter is not valid";
                            return false;
                        }
                    }


                }


            }

            if (isAllSequenceNull)
            {
                reason = $"Invalid sequence, sequence are all null ";
                return false;
            }
            return true;
        }

        public bool CreateControlJob(string jobId, string module, List<string> pjIDs, bool isAutoStart)
        {
            if (_lstControlJobs.Exists(x => x.Name == jobId))
            {
                EV.PostWarningLog(LogSource, $"{jobId} is already created");
                return false;
            }

            if (!ModuleHelper.IsCassette(ModuleHelper.Converter(module)))
            {
                EV.PostWarningLog(LogSource, $"{module} should be Cassete");
                return false;
            }


            if (_lstProcessJobs.Count <= 0) //判断上一步ProcessJob是否创建成功
            {
                EV.PostWarningLog(LogSource, $"process job is not exist");
                return false;
            }

            ControlJobInfo cj = new ControlJobInfo();
            cj.Name = jobId;
            cj.Module = module;
            cj.LotName = jobId;
            cj.LotInnerId = Guid.NewGuid();
            cj.LotWafers = new List<WaferInfo>();
            cj.SetState(EnumControlJobState.WaitingForStart);


            int totalWafer = 0;
            foreach (var pjName in pjIDs)
            {
                var pj = _lstProcessJobs.FirstOrDefault(x => x.Name == pjName);
                if (pj == null)
                {
                    LOG.Info($"not find {pjName} while create control job");
                    continue;
                }


                var slotWafers = new List<Tuple<ModuleName, int>>();
                foreach (var slotWafer in pj.SlotWafers)
                {
                    var _module = GetModule(module);
                    if (!_module.HasWafer(slotWafer.Item2))
                    {
                        EV.PostWarningLog(LogSource, $"job wafer: {module} slot {slotWafer.Item2 + 1} not in the carrier");
                        return false;
                    }
                    if (!_module.CheckWaferStatus(slotWafer.Item2, WaferStatus.Normal))
                    {
                        EV.PostWarningLog(LogSource, $"job wafer: {module} slot {slotWafer.Item2 + 1} status is {_module.GetWaferInfo(slotWafer.Item2).Status}");
                        return false;
                    }
                    if (_module.GetWaferInfo(slotWafer.Item2).ProcessState != EnumWaferProcessStatus.Idle)
                    {
                        EV.PostWarningLog(LogSource, $"job wafer: {module} slot {slotWafer.Item2 + 1} process status is {_module.GetWaferInfo(slotWafer.Item2).ProcessState}");
                        return false;
                    }

                    slotWafers.Add(Tuple.Create(ModuleHelper.Converter(module), slotWafer.Item2));



                    totalWafer++;
                }

                pj.ControlJobName = cj.Name;

                cj.ProcessJobNameList.Add(pj.Name);
                pj.SlotWafers = slotWafers;
                foreach (var pjSlotWafer in pj.SlotWafers)
                {
                    WaferInfo wafer = GetModule(pjSlotWafer.Item1).GetWaferInfo(pjSlotWafer.Item2);
                    cj.LotWafers.Add(wafer);
                    wafer.ProcessJob = pj;
                    wafer.ProcessJobID = pj.Sequence.Name;
                    WaferDataRecorder.SetCjInfo(wafer.InnerId.ToString(), cj.InnerId.ToString());
                    WaferDataRecorder.SetWaferSequence(wafer.InnerId.ToString(), pj.Sequence.Name);
                    WaferDataRecorder.SetWaferLotId(wafer.InnerId.ToString(), jobId);
                    WaferManager.Instance.UpdateWaferLotId(pjSlotWafer.Item1, pjSlotWafer.Item2, jobId);
                }
            }

            _lstControlJobs.Add(cj);

            CarrierManager.Instance.DeleteCarrier(cj.Module);
            CarrierManager.Instance.CreateCarrier(cj.Module);
            CarrierManager.Instance.UpdateCarrierId(cj.Module, $"{cj.Module} {DateTime.Now}");
            CarrierInfo carrier = CarrierManager.Instance.GetCarrier(cj.Module);
            JobDataRecorder.StartCJ(cj.InnerId.ToString(), carrier.InnerId.ToString(), cj.Name, cj.Module, cj.Module, totalWafer);

            return true;
        }

        public bool CreateProcessJob(string jobId, string sequenceName, List<int> slotNumbers/*0 based*/, bool isAutoStart)
        {
            var sequenceInfo = SequenceInfoHelper.GetInfo(sequenceName);

            //模块顺序检查
            List<int> lstLoad = new List<int>();
            List<int> lstBuffer = new List<int>();
            List<int> lstUnLoad = new List<int>();
            List<int> lstPM = new List<int>();


            for (int i = 0; i < sequenceInfo.Steps.Count; i++)
            {
                if (sequenceInfo.Steps[i].StepModules.Count == 0)
                {
                    return false;
                }
                if (sequenceInfo.Steps[i].StepModules.Any(pm => ModuleHelper.IsPm(pm)))
                {
                    lstPM.Add(i);
                }
                if (sequenceInfo.Steps[i].StepModules.Any(load => load == ModuleName.Load || load == ModuleName.LoadLock))
                {
                    lstLoad.Add(i);
                }
                if (sequenceInfo.Steps[i].StepModules.Any(unload => unload == ModuleName.UnLoad))
                {
                    lstUnLoad.Add(i);
                }
                if (sequenceInfo.Steps[i].StepModules.Any(buff => buff == ModuleName.Buffer))
                {
                    lstBuffer.Add(i);
                }
            }

            if (lstPM.Count <= 0)
            {
                EV.PostWarningLog(LogSource, $"Sequence must have pm step info!");
                return false;
            }
            if (lstLoad.Count > 1)
            {
                EV.PostWarningLog(LogSource, $"Sequence can not contain {lstLoad.Count} Load Step!");
                return false;
            }
            if (lstUnLoad.Count > 1)
            {
                EV.PostWarningLog(LogSource, $"Sequence can not contain {lstUnLoad.Count} UnLoad Step!");
                return false;
            }
            if (lstBuffer.Count > 0)
            {
                foreach (int bufferId in lstBuffer)
                {
                    if (lstLoad.Count == 1)
                    {
                        if (bufferId < lstLoad[0])
                        {
                            EV.PostWarningLog(LogSource, $"Sequence Load Step Must Before Buffer Step!");
                            return false;
                        }
                    }
                    if (lstUnLoad.Count == 1)
                    {
                        if (bufferId > lstUnLoad[0])
                        {
                            EV.PostWarningLog(LogSource, $"Sequence Buffer Step Must Before UnLoad Step!");
                            return false;
                        }
                    }
                }
            }



            var slotWafers = new List<Tuple<ModuleName, int>>();
            foreach (var slot in slotNumbers)
            {
                slotWafers.Add(Tuple.Create(ModuleName.System, slot));
            }

            ProcessJobInfo pj = new ProcessJobInfo();
            pj.Name = jobId;
            pj.Sequence = sequenceInfo;
            pj.SlotWafers = slotWafers;
            pj.SetState(EnumProcessJobState.Queued);

            _lstProcessJobs.Add(pj);

            return true;
        }


        internal void StopJob(string jobName)
        {
            //ControlJobInfo cj = _lstControlJobs.Find(x => x.Name == jobName);
            //if (cj == null)
            //{
            //    EV.PostWarningLog(LogSource, $"stop job rejected, not found job with id {jobName}");
            //    return;
            //}
            foreach (var cj in _lstControlJobs)
            {
                foreach (var pj in _lstProcessJobs)
                {
                    if (pj.ControlJobName == cj.Name)
                    {
                        pj.SetState(EnumProcessJobState.Stopping);
                    }
                }

                if (_cycleSetPoint > 0)
                {
                    _cycleSetPoint = _cycledCount;
                }
            }
        }

        internal void AbortJob(string jobName)
        {
            ControlJobInfo cj = _lstControlJobs.Find(x => x.Name == jobName);
            if (cj == null)
            {
                EV.PostWarningLog(LogSource, $"abort job rejected, not found job with id {jobName}");
                return;
            }

            int unprocessed_cj = 0;
            int aborted_cj = 0;

            List<ProcessJobInfo> pjAbortList = new List<ProcessJobInfo>();
            foreach (var pj in _lstProcessJobs)
            {
                if (pj.ControlJobName == cj.Name)
                {
                    pj.SetState(EnumProcessJobState.Aborting);
                    pjAbortList.Add(pj);

                    int unprocessed = 0;
                    int aborted = 0;
                    WaferInfo[] wafers = WaferManager.Instance.GetWaferByProcessJob(pj.Name);
                    foreach (var waferInfo in wafers)
                    {
                        waferInfo.ProcessJob = null;
                        waferInfo.NextSequenceStep = 0;
                        if (waferInfo.ProcessState != EnumWaferProcessStatus.Completed)
                        {
                            unprocessed++;
                            unprocessed_cj++;
                        }
                    }
                    JobDataRecorder.EndPJ(pj.InnerId.ToString(), aborted, unprocessed);
                }
            }

            foreach (var pj in pjAbortList)
            {
                _lstProcessJobs.Remove(pj);
            }

            _lstControlJobs.Remove(cj);

            _dbCallback.LotFinished(cj);
            JobDataRecorder.EndCJ(cj.InnerId.ToString(), aborted_cj, unprocessed_cj);

            Clear();
        }

        public void ResumeJob(string jobName)
        {
            //ControlJobInfo cj = _lstControlJobs.Find(x => x.Name == jobName);
            //if (cj == null)
            //{
            //    EV.PostWarningLog(LogSource, $"resume job rejected, not found job with id {jobName}");
            //    return;
            //}

            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Paused)
                {
                    cj.SetState(EnumControlJobState.Executing);
                }
            }
        }

        internal void PauseJob(string jobName)
        {
            //ControlJobInfo cj = _lstControlJobs.Find(x => x.Name == jobName);
            //if (cj == null)
            //{
            //    EV.PostWarningLog(LogSource, $"pause job rejected, not found job with id {jobName}");
            //    return;
            //}

            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Executing)
                {
                    cj.SetState(EnumControlJobState.Paused);
                }
            }
        }

        internal void StartJob(string jobName)
        {
            GetConfig();

            //ControlJobInfo cj = _lstControlJobs.Find(x => x.Name == jobName);
            //if (cj == null)
            //{
            //    EV.PostWarningLog(LogSource, $"start job rejected, not found job with id {jobName}");
            //    return;
            //}
            ////StartPreprocess();
            //if (_isCycleMode)
            //{
            //    if (CheckAllJobDone())
            //    {
            //        _cycledWafer = 0;
            //        _cycledCount = 0;
            //    }
            //}

            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.WaitingForStart)
                {
                    cj.BeginTime = DateTime.Now;
                    cj.LotInnerId = Guid.NewGuid();
                    _dbCallback.LotCreated(cj);

                    cj.SetState(EnumControlJobState.Executing);
                }
            }
        }
        #endregion

        /// <summary>
        /// Start Auto Transfer
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        public Result Start(params object[] objs)
        {
            GetConfig();
            _cycledWafer = 0;
            _cycledCount = 0;
            //_totalWaferWhenStart = StatsDataManager.Instance.GetValue(StatsNameTotalRunningWafer);

            bool hasPmOnline = false;

            foreach (var schedulerPm in LstPmSchedulers)
            {
                if (schedulerPm.IsOnline && !schedulerPm.IsError)
                {
                    hasPmOnline = true;
                    break;
                }
            }

            if (SchPm1.IsError)
            {
                EV.PostWarningLog("Scheduler", "can not change to auto mode, at least one process chamber be online and no error");
                return Result.FAIL;
            }

            if (SchPm1.IsService)
            {
                EV.PostWarningLog("Scheduler", "can not change to auto mode, PM  is Service Mode");
                return Result.FAIL;
            }

            if (!hasPmOnline)
            {
                EV.PostWarningLog("Scheduler", "can not change to auto mode, at least one process chamber be online and no error");
                return Result.FAIL;
            }

            if (!SchTmRobot.IsOnline || SchTmRobot.IsError)
            {
                EV.PostWarningLog("Scheduler", "can not change to auto mode, TM robot should be online and no error");
                return Result.FAIL;
            }

            return Result.RUN;
        }


        public Result Monitor()
        {
            _notifyTrayExhausted.CLK = CheckTrayExhausted();
            if (_notifyTrayExhausted.Q)
            {
                EV.PostAlarmLog("System", "Tray ProcessCount Exhausted , No Tray for next Process !");
            }

            ControlJobInfo cjActive = _lstControlJobs.Find(x => x.State == EnumControlJobState.Executing);
            if (cjActive != null)
            {
                MonitorModuleTasks();
            }

            MonitorModuleError();

            MonitorModuleState();

            MonitorJobTasks();

            MonitorCleanTasks();

            // 如果没有Module发生错误，则将信号灯设置为绿色
            if (!LstAllSchedulers.Any(x => x.IsError)) ;
                //_signalTower..CreateLight(LightType.Green);

            return Result.RUN;
        }

        #region Job task
        public Result MonitorJobTasks()
        {
            UpdateProcessJobStatus();

            UpdateControlJobStatus();

            StartNewJob();

            return Result.RUN;
        }

        protected bool CheckAllWaferReturned(ProcessJobInfo pj, bool checkAllProcessed)
        {
            for (int i = 0; i < pj.SlotWafers.Count; ++i)
            {
                WaferInfo wafer = GetModule(GetWaferReturnedCassette(pj.SlotWafers[i].Item1)).GetWaferInfo(pj.SlotWafers[i].Item2);
                if (wafer.IsEmpty)
                    return false;
                if (checkAllProcessed && GetModule(GetWaferReturnedCassette(pj.SlotWafers[i].Item1)).CheckWaferNeedProcess(pj.SlotWafers[i].Item2))
                    return false;
                if (wafer.ProcessJob == null || wafer.ProcessJob.InnerId != pj.InnerId)
                {
                    return false;
                }
            }

            return true;
        }

        private ModuleName GetWaferReturnedCassette(ModuleName moduleFrom)
        {
            if (CassetteFromAToB)
            {
                if (moduleFrom == ModuleName.CassAL)
                {
                    return ModuleName.CassAR;
                }
                else if (moduleFrom == ModuleName.CassAR)
                {
                    return ModuleName.CassAL;
                }

            }
            return moduleFrom;
        }

        protected bool CheckAllDummyWaferReturned()
        {
            foreach (var schedulerPm in LstPmSchedulers)
            {
                if (WaferManager.Instance.CheckWaferIsDummy(schedulerPm.Module, 0))
                    return false;
            }

            if (WaferManager.Instance.CheckWaferIsDummy(SchTmRobot.Module, 0))
                return false;

            if (WaferManager.Instance.CheckWaferIsDummy(SchTmRobot.Module, 1))
                return false;

            return true;
        }

        protected bool CheckAllPmCleaned(ProcessJobInfo pj)
        {
            foreach (var schedulerPm in LstPmSchedulers)
            {
                if (GetModule(schedulerPm.Module).CheckNeedRunClean(out _, out _))
                {
                    foreach (var sequenceStepInfo in pj.Sequence.Steps)
                    {
                        if (sequenceStepInfo.StepModules.Contains(schedulerPm.Module))
                            return false;
                    }
                }
            }


            return true;
        }

        private void UpdateProcessJobStatus()
        {
            foreach (var pj in _lstProcessJobs)
            {
                if (pj.State == EnumProcessJobState.Processing)
                {
                    if (CheckAllWaferReturned(pj, true) && CheckAllPmCleaned(pj) && CheckAllDummyWaferReturned())
                    {
                        //if (CheckWaferSequenceStepDone(ModuleName.LoadLock, 0))
                        if (SchUnLoad.IsAvailable && SchBuffer.IsAvailable && SchTmRobot.IsAvailable && SchLoadLock.IsAvailable
                            && SchWaferRobot.IsAvailable && SchUnLoad.NoTray(0)
                            && SchWaferRobot.IsAvailable && SchTmRobot.NoTray(0)
                            && SchWaferRobot.IsAvailable && SchLoadLock.NoTray(0))
                        {
                            pj.SetState(EnumProcessJobState.ProcessingComplete);
                            JobDataRecorder.EndPJ(pj.InnerId.ToString(), 0, 0);
                        }
                    }
                }
                else if (pj.State == EnumProcessJobState.Stopping)
                {
                    if (CheckAllWaferReturned(pj, false) && CheckAllPmCleaned(pj) && CheckAllDummyWaferReturned())
                    {
                        //if (CheckWaferSequenceStepDone(ModuleName.LoadLock, 0))
                        if (SchUnLoad.IsAvailable && SchBuffer.IsAvailable && SchTmRobot.IsAvailable && SchLoadLock.IsAvailable
                            && SchWaferRobot.IsAvailable && SchUnLoad.NoTray(0)
                            && SchWaferRobot.IsAvailable && SchTmRobot.NoTray(0)
                            && SchWaferRobot.IsAvailable && SchLoadLock.NoTray(0))
                        {
                            pj.SetState(EnumProcessJobState.ProcessingComplete);
                            JobDataRecorder.EndPJ(pj.InnerId.ToString(), 0, 0);
                        }
                    }
                }
            }
        }

        private void UpdateControlJobStatus()
        {
            if (_lstControlJobs.Count == 0)
                return;

            bool allControlJobComplete = true;
            List<ControlJobInfo> cjRemoveList = new List<ControlJobInfo>();
            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Executing)
                {
                    bool allPjCompleted = true;
                    foreach (var pjName in cj.ProcessJobNameList)
                    {
                        var pj = _lstProcessJobs.Find(x => x.Name == pjName);
                        if (pj == null)
                        {
                            LOG.Error($"Not find pj named {pjName} in {cj.Name}");
                            continue;
                        }
                        if (pj.State != EnumProcessJobState.Complete && pj.State != EnumProcessJobState.ProcessingComplete)
                        {
                            allPjCompleted = false;
                            break;
                        }
                    }

                    if (allPjCompleted)
                    {
                        cj.SetState(EnumControlJobState.Completed);
                        int unprocessed_cj = 0;
                        int aborted_cj = 0;

                        foreach (var pj in _lstProcessJobs)
                        {
                            if (pj.ControlJobName == cj.Name)
                            {
                                int unprocessed = 0;
                                int aborted = 0;
                                WaferInfo[] wafers = WaferManager.Instance.GetWaferByProcessJob(pj.Name);
                                foreach (var waferInfo in wafers)
                                {
                                    waferInfo.ProcessJob = null;
                                    waferInfo.NextSequenceStep = 0;
                                    if (waferInfo.ProcessState != EnumWaferProcessStatus.Completed)
                                    {
                                        unprocessed++;
                                        unprocessed_cj++;
                                    }
                                }
                                JobDataRecorder.EndPJ(pj.InnerId.ToString(), aborted, unprocessed);
                            }
                        }

                        JobDataRecorder.EndCJ(cj.InnerId.ToString(), aborted_cj, unprocessed_cj);
                        _dbCallback.LotFinished(cj);
                    }
                }

                if (cj.State == EnumControlJobState.Completed)
                {
                    cjRemoveList.Add(cj);
                }

                allControlJobComplete = allControlJobComplete && cj.State == EnumControlJobState.Completed;
            }

            if (_isCycleMode && _cycledCount < _cycleSetPoint)
            {
                int countPerCycle = 0;
                int countProcessed = 0;
                foreach (var pj in _lstProcessJobs)
                {
                    foreach (var pjSlotWafer in pj.SlotWafers)
                    {
                        countPerCycle++;
                        WaferInfo wafer = GetModule(pjSlotWafer.Item1).GetWaferInfo(pjSlotWafer.Item2);
                        if (!wafer.IsEmpty && !GetModule(pjSlotWafer.Item1).CheckWaferNeedProcess(pjSlotWafer.Item2))
                            countProcessed++;
                    }
                }

                _cycledWafer = _cycledCount * countPerCycle + countProcessed;
                //StatsDataManager.Instance.SetValue(StatsNameTotalRunningWafer, _totalWaferWhenStart + _cycledWafer);


                if (allControlJobComplete)
                {
                    _cycledCount++;

                    if (_cycledCount < _cycleSetPoint)
                    {
                        foreach (var cj in _lstControlJobs)
                        {
                            cj.SetState(EnumControlJobState.Executing);
                        }
                        foreach (var pj in _lstProcessJobs)
                        {
                            pj.SetState(EnumProcessJobState.Queued);
                            pj.InnerId = Guid.NewGuid();
                            foreach (var pjSlotWafer in pj.SlotWafers)
                            {
                                WaferInfo wafer = GetModule(pjSlotWafer.Item1).GetWaferInfo(pjSlotWafer.Item2);

                                wafer.ProcessJob = null;
                                wafer.NextSequenceStep = 0;
                                wafer.ProcessState = EnumWaferProcessStatus.Idle;
                            }
                        }
                    }
                }

            }

            foreach (var cj in cjRemoveList)
            {
                List<ProcessJobInfo> pjRemoveList = new List<ProcessJobInfo>();
                foreach (var pj in _lstProcessJobs)
                {
                    if (pj.ControlJobName == cj.Name)
                        pjRemoveList.Add(pj);
                }

                foreach (var pj in pjRemoveList)
                {
                    _lstProcessJobs.Remove(pj);
                }

                _lstControlJobs.Remove(cj);
            }

            ControlJobInfo cjActived = null;
            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Executing)
                {
                    cjActived = cj;
                    break;
                }
            }
        }

        private void StartNewJob()
        {
            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Executing)
                {
                    foreach (var pjName in cj.ProcessJobNameList)
                    {
                        var pj = _lstProcessJobs.Find(x => x.Name == pjName);
                        if (pj == null)
                        {
                            LOG.Error($"Not find pj named {pjName} in {cj.Name}");
                            continue;
                        }

                        if (pj.State == EnumProcessJobState.Queued)
                        {
                            ActiveProcessJob(pj);
                        }
                    }
                }
            }
        }

        public bool CheckAllJobDone()
        {
            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Executing || cj.State == EnumControlJobState.Paused)
                    return false;
            }
            if (_lstControlJobs.Count == 0)
            {
                return true;
            }
            return false;
        }

        private bool ActiveProcessJob(ProcessJobInfo pj)
        {
            //_lstPms.Clear();
            //for (int i = 0; i < pj.Sequence.Steps.Count; i++)
            //{
            //    SequenceStepInfo stepInfo = pj.Sequence.Steps[i];
            //    foreach (var module in stepInfo.StepModules)
            //    {
            //        if (ModuleHelper.IsPm(module) && _lstPms.Exists(x => x.Module == module))
            //        {
            //            _lstPms.Add(_lstPms.Find(x => x.Module == module));
            //        }
            //    }
            //}

            foreach (var pjSlotWafer in pj.SlotWafers)
            {
                WaferInfo wafer = GetModule(pjSlotWafer.Item1).GetWaferInfo(pjSlotWafer.Item2);
                wafer.ProcessJob = pj;
                wafer.NextSequenceStep = 0;

                WaferDataRecorder.SetPjInfo(wafer.InnerId.ToString(), pj.InnerId.ToString());
            }

            ControlJobInfo cj = _lstControlJobs.Find(x => x.Name == pj.ControlJobName);

            CarrierInfo carrier = CarrierManager.Instance.GetCarrier(cj.Module);
            JobDataRecorder.StartPJ(pj.InnerId.ToString(), null, cj.InnerId.ToString(), pj.Name, cj.Module, cj.Module, pj.SlotWafers.Count);
            pj.SetState(EnumProcessJobState.Processing);

            return true;
        }
        #endregion

        #region Module task

        public Result MonitorModuleTasks()
        {
            //Load和UnLoad不能同时抽气
            //System.Diagnostics.Debug.Assert(!((_tmRobot.IsInPumping && SchLoadLock.IsInPumping) || (_tmRobot.IsInPumping && SchUnLoad.IsInPumping) || (SchUnLoad.IsInPumping && SchLoadLock.IsInPumping)),$"检测到 SchLoadLock.IsInPumping :{SchLoadLock.IsInPumping}, SchUnLoad.IsInPumping :{SchUnLoad.IsInPumping}, _tmRobot.IsInPumping :{_tmRobot.IsInPumping}");
            _trigLoadUnLoadAllPumpAlarm.CLK = (SchUnLoad.IsInPumping && SchLoadLock.IsInPumping);
            if (_trigLoadUnLoadAllPumpAlarm.Q)
                EV.PostAlarmLog("Scheduler",
                    $"检测到Load和UnLoad同时抽气，Load Task: {SchLoadLock.GetTaskRunning()}, UnLoad Task: {SchUnLoad.GetTaskRunning()}");
            
            MonitorPMTask();
            MonitorBufferTask();
            MonitorAlignerTask();

            MonitorTmRobotTask();
            MonitorWaferRobotTask();
            MonitorTrayRobotTask();

            MonitorLoadTask();
            MonitorUnLoadTask();

            return Result.RUN;
        }

        private void MonitorBufferTask()
        {
            if (!SchBuffer.IsAvailable)
            {
                return;
            }

            if (SchBuffer.FirstDetectTrayArrive(0)
                || SchBuffer.FirstDetectTrayArrive(1)
                || SchBuffer.FirstDetectTrayArrive(2))
            {
                SchBuffer.ResetPurgeStatus();
            }


            for (int i = 0; i < 3; i++)
            {
                bool canExcute = SchBuffer.HasWafer(i) && SchBuffer.CheckWaferNextStepIsThis(SchBuffer.Module, i);
                if (canExcute)
                {
                    WaferInfo bufferWafer = SchBuffer.GetWaferInfo(i);

                    #region 获取Sequence中的参数

                    if (!GetWaferSequenceNextValue(ModuleName.Buffer, i, "BufferType", out var heatCoolingMode))
                    {
                        continue;
                    }
                    if (!GetWaferSequenceNextValue(ModuleName.Buffer, i, "SetValue", out var strSetPoint))
                    {
                        continue;
                    }
                    if (!GetWaferSequenceNextValue(ModuleName.Buffer, i, "PurgeCount", out var strPurgeLoopCount))
                    {
                        continue;
                    }
                    if (!GetWaferSequenceNextValue(ModuleName.Buffer, i, "PumpDelayTime", out var strPurgePumpDelay))
                    {
                        continue;
                    }
                    if (!int.TryParse(strSetPoint, out var bufferSetValue) 
                        || !int.TryParse(strPurgeLoopCount, out var purgeLoopCount)
                        || !int.TryParse(strPurgePumpDelay, out var purgePumpDelay))
                    {
                        continue;
                    }

                    #endregion
                   
                    WaferInfo wafer = SchBuffer.GetWaferInfo(i);
                    //分别判断冷却和加热方式温度是否达到
                    if (heatCoolingMode == "HeatByTemp")
                    {
                        if (_bufferWaferInfo.ContainsKey(bufferWafer.InnerId.ToString()))
                        {
                            DateTime dtStartTime = _bufferWaferInfo[bufferWafer.InnerId.ToString()];
                            double pastTime = (DateTime.Now - dtStartTime).TotalSeconds;

                            //选择By温度5秒后再判断温度
                            if (pastTime > 5)
                            {
                                if (SchBuffer.GetTemperature() >= bufferSetValue)
                                    wafer.NextSequenceStep++;
                                _bufferWaferInfo.Remove(bufferWafer.InnerId.ToString());
                            }
                        }
                        else
                        {
                            _bufferWaferInfo.Add(bufferWafer.InnerId.ToString(), DateTime.Now);
                        }
                    }
                    else if (heatCoolingMode == "HeatByTime")
                    {
                        if (_bufferWaferInfo.ContainsKey(bufferWafer.InnerId.ToString()))
                        {
                            DateTime dtStartTime = _bufferWaferInfo[bufferWafer.InnerId.ToString()];
                            double pastTime = (DateTime.Now - dtStartTime).TotalSeconds;

                            if (i == 0)
                            {
                                _timeBuffer1 = pastTime > bufferSetValue ? "0" : (bufferSetValue - pastTime).ToString();
                            }
                            else if (i == 1)
                            {
                                _timeBuffer2 = pastTime > bufferSetValue ? "0" : (bufferSetValue - pastTime).ToString();
                            }
                            if (i == 2)
                            {
                                _timeBuffer3 = pastTime > bufferSetValue ? "0" : (bufferSetValue - pastTime).ToString();
                            }

                            if (pastTime > bufferSetValue)
                            {
                                wafer.NextSequenceStep++;
                                _bufferWaferInfo.Remove(bufferWafer.InnerId.ToString());
                            }
                        }
                        else
                        {
                            _bufferWaferInfo.Add(bufferWafer.InnerId.ToString(), DateTime.Now);
                        }
                    }
                    else if (heatCoolingMode == "CoolingByTime")
                    {
                        if (_bufferWaferInfo.ContainsKey(bufferWafer.InnerId.ToString()))
                        {
                            DateTime dtStartTime = _bufferWaferInfo[bufferWafer.InnerId.ToString()];
                            double pastTime = (DateTime.Now - dtStartTime).TotalSeconds;

                            if (i == 0)
                            {
                                _timeBuffer1 = pastTime > bufferSetValue ? "0" : (bufferSetValue - pastTime).ToString();
                            }
                            else if (i == 1)
                            {
                                _timeBuffer2 = pastTime > bufferSetValue ? "0" : (bufferSetValue - pastTime).ToString();
                            }
                            if (i == 2)
                            {
                                _timeBuffer3 = pastTime > bufferSetValue ? "0" : (bufferSetValue - pastTime).ToString();
                            }

                            if (pastTime > bufferSetValue)
                            {
                                wafer.NextSequenceStep++;
                                _bufferWaferInfo.Remove(bufferWafer.InnerId.ToString());
                            }
                        }
                        else
                        {
                            _bufferWaferInfo.Add(bufferWafer.InnerId.ToString(), DateTime.Now);
                        }
                    }

                    // 如果TMRobot空闲，并且Buffer收到新盘，则根据Sequence配置进行Purge。
                    if (SchTmRobot.IsAvailable && !SchBuffer.HasPurged)
                    {
                        SchTmRobot.Purge(purgeLoopCount, purgePumpDelay);
                        SchBuffer.HasPurged = true;
                    }

                    return;
                }
            }
 
            //Place和Pick条件都一样
            if (!SchBuffer.IsReadyForPlace(ModuleName.TMRobot, 0))
            {
                SchBuffer.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Place, 0);
            }
        }

        private void MonitorLoadTask()
        {
            //TODO LL中Wafer和Tray的情况还需要优化，大体分三种条件：Wafer+Tray, WaferOnly, TrayOnly，然后再各自进一步细化

            // 如果是第一次喂Wafer进来，或第一次检测到Wafer被取走，则重置Load腔状态。
            if (SchLoadLock.FirstDetectWaferArrive(0) || SchLoadLock.FirstDetectWaferLeave(0))
            {
                Debug.WriteLine("Reset Purge Status!");
                SchLoadLock.ResetPurgedAndGroupedStatus();
            }

            if (!SchLoadLock.IsAvailable)
            {
                return;
            }

            // 如果Load腔有Wafer和Tray，并且Tray的工艺次数没有超过限制
            if (SchLoadLock.HasWafer(0) && SchLoadLock.HasTrayAndNotExceedProcessCount(0))
            {
                // 如果Wafer和Tray还没有组合，则先进行组合操作
                if (!SchLoadLock.CheckWaferTrayGrouped())
                {
                    SchLoadLock.GroupWaferTray();
                    return;
                }
                
                // 如果没Purge过，啥也不干
                if (SchLoadLock.HasPurged == false)
                    return;
                
                // 有Wafer和石墨盘，等待TM来取
                //! 此时Unload不能抽气
                if (!SchLoadLock.IsReadyForPick(ModuleName.TMRobot, 0) 
                    && !SchUnLoad.IsInPumping
                    && (SchTmRobot.NoWafer(0) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0)))
                {
                        SchLoadLock.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Pick, 0);
                        return;
                }
            }
            // 如果Load里有超过工艺次数限制的Tray，则准备让TrayRobot来拿。
            else if (SchLoadLock.HasTrayAndExceedProcessCount(0))
            {
                if (!SchLoadLock.IsReadyForPick(ModuleName.TrayRobot, 0) 
                    && !SchUnLoad.IsInPumping
                    && (SchTmRobot.NoWafer(0) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0)))
                    SchLoadLock.PrepareTransfer(ModuleName.TrayRobot, EnumTransferType.Pick, 0);
            }
            // 如果TMRobot或Buffer没Wafer但有超过工艺次数限制的Tray，准备将Tray传出去
            else if ((SchTmRobot.NoWafer(0) && SchTmRobot.HasTrayAndExceedProcessCount(0))
                    || (SchBuffer.NoWafer(0) && SchBuffer.HasTrayAndExceedProcessCount(0))
                    || (SchBuffer.NoWafer(1) && SchBuffer.HasTrayAndExceedProcessCount(1))
                    || (SchBuffer.NoWafer(2) && SchBuffer.HasTrayAndExceedProcessCount(2)))
            {
                if (!SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0) 
                    && !SchUnLoad.IsInPumping
                    && (SchTmRobot.NoWafer(0) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0)))
                {
                    SchLoadLock.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Place, 0);
                    return;
                }
            }
            //Load只有石墨盘,要么是WaferRobot来放,要么是TrayRobot来取
            else if (SchLoadLock.NoWafer(0)) 
            {
                if (!SchLoadLock.IsReadyForPlace(ModuleName.WaferRobot, 0) 
                    && !SchUnLoad.IsInPumping
                    && (SchTmRobot.NoWafer(0) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0)))
                {
                    SchLoadLock.PrepareTransfer(ModuleName.WaferRobot, EnumTransferType.Place, 0);
                    return;
                }
            }
            else // Load有盘准备做工艺
            {
                // 预测下个Tray从内部来
                if(GetTraySlot(0) == null || GetCurrentTrayCount() >= 2)
                /*if ((SchTmRobot.HasTray(0) && SchTmRobot.NoWafer(0) // TMRobot有可用Tray
                    || (SchBuffer.HasTrayAndNotExceedProcessCount(0) && SchBuffer.NoWafer(0)) // Buffer 1有可用Tray
                    || (SchBuffer.HasTrayAndNotExceedProcessCount(1) && SchBuffer.NoWafer(1)) // Buffer 2有可用Tray
                    || (SchBuffer.HasTrayAndNotExceedProcessCount(2) && SchBuffer.NoWafer(2)) // Buffer 3有可用Tray
                    || SchPm1.HasTrayAndNotExceedProcessCount(0)    // PM1有可用Tray
                    || SchUnLoad.HasTrayAndNotExceedProcessCount(0)) // UnLoad有可用Tray
                    && (GetTraySlot(0) == null || GetCurrentTrayCount() >= 2)) */
                {
                    // 如果还没Purge，阻止TMRobot放Tray
                    if (!SchLoadLock.HasPurged)
                        return;

                    if (!SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0) 
                        && !SchUnLoad.IsInPumping
                        && (SchTmRobot.NoWafer(0) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0)))
                    {
                        SchLoadLock.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Place, 0);
                        return;
                    }
                }
                else // 从Cass来Tray
                {
                    if (!SchLoadLock.IsReadyForPlace(ModuleName.TrayRobot, 0) 
                        && !SchUnLoad.IsInPumping
                        && (SchTmRobot.NoWafer(0) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0)))
                    {
                        SchLoadLock.PrepareTransfer(ModuleName.TrayRobot, EnumTransferType.Place, 0);
                        return;
                    }
                }
            }
        }

        private void MonitorUnLoadTask()
        {
            if (!SchUnLoad.IsAvailable)
                return;

            if (!SchWaferRobot.IsAvailable && SchWaferRobot.Target == ModuleName.UnLoad) // 如果WaferRobot正在操作UnLoad
                return;

            if (!SchTmRobot.IsAvailable && SchTmRobot.Target == ModuleName.UnLoad) // 如果TMRobot正在操作UnLoad
                return;

            // 重置UnLoad Purge和分离状态
            if (SchUnLoad.FirstDetectWaferArrive(0))
            {
                EV.PostInfoLog(SchUnLoad.Module.ToString(), "Wafer arrived.");
                SchUnLoad.ResetPurgedAndSeparatedStatus();
            }

            // 如果TMRobot拿着耗尽的Tray，则先处理LL。
            if (SchTmRobot.HasTrayAndExceedProcessCount(0))
                return;

            // 当Load, Aligner, WaferRobot有Wafer时，可能需要优先保障Wafer进PM
            if ((SchLoadLock.CheckWaferNeedProcess(0)
                 || SchAligner.CheckWaferNeedProcess(0)
                 || SchWaferRobot.CheckWaferNeedProcess(0)))
            {
                // 如果LL、TM或Buffer有可用的Tray，则不要抢Pump。
                // 其它情况下，继续UnLoad动作，因为当系统中只有一个可用Tray时，UnLoad不完成，LL也无法走下一步，所有Module停止工作
                if (SchBuffer.HasAvailableTray() || SchTmRobot.HasTray(0) ||
                    SchLoadLock.HasTray(0))
                    return;
            }
                

            if (!SchUnLoad.IsAvailable)
            {
                return;
            }

            // TM把Wafer和Tray放如UnLoad后，先冷却，再分离，然后取Tray前Purge
            if (SchUnLoad.HasWafer(0) && SchUnLoad.HasTray(0))
            {
                // 如果没冷却，先冷却
                if (!SchUnLoad.CheckCoolingCompleted())
                {
                    GetWaferSequenceCoolingTime(SchUnLoad.Module, 0, out var coolingTime);
                    SchUnLoad.CoolingAndPurge(
                        true,
                        coolingTime, 
                        SchUnLoad.GetWaferPurgeCount(0),
                        SchUnLoad.GetWaferPumpDelayTime(0));
                    return;
                }

                // 如果没分离，先分离
                if (!SchUnLoad.CheckWaferTraySeparated())
                {
                    SchUnLoad.SeparateWaferTray();
                    return;
                }
                
                if (!SchUnLoad.IsAvailable)
                {
                    return;
                }

                // 如果Sequence下一步是UnLoad,并且W&T已冷却和分离
                if (!SchUnLoad.CheckPurgedBeforeTrayPicking()
                    && !SchLoadLock.IsInPumping)
                {
                    SchUnLoad.PurgeBeforeTrayPicking(
                        SchUnLoad.GetWaferPurgeCount(0),
                        SchUnLoad.GetWaferPumpDelayTime(0));
                    return;
                }

                if (!SchUnLoad.IsAvailable)
                {
                    return;
                }

                // 如果Tray拿走以前还没Purge，先Purge，在让TMRobot取Tray
                if (!SchUnLoad.CheckPurgedBeforeTrayPicking())
                    return;

                if (!SchUnLoad.IsAvailable)
                {
                    return;
                }

                if (!SchUnLoad.IsReadyForPick(ModuleName.TMRobot, 0) 
                    && !SchLoadLock.IsInPumping)
                {
                    // UnLoad中有Tray，则准备好让TMRobot来取
                    SchUnLoad.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Pick, 0);
                    return;
                }
            }
            else if (SchUnLoad.HasWafer(0)) // 如果没Tray，下一步让WaferRobot取Wafer
            {
                if (!SchUnLoad.IsAvailable)
                {
                    return;
                }


                // 如果取Wafer前还未Purge，先Purge
                if (!SchUnLoad.CheckPurgedBeforeWaferPicking() &&
                    SchUnLoad.CheckWaferNextStepIsThis(ModuleName.UnLoad, 0)
                    && !SchLoadLock.IsInPumping)
                {
                    var cycle = SchUnLoad.GetWaferPurgeCount(0, "PurgeCountBeforeWaferPicking");
                    var pumpDelay = SchUnLoad.GetWaferPumpDelayTime(0, "PumpDelayTimeBeforeWaferPicking");

                    SchUnLoad.LastAfAtmPurgeCount = SchUnLoad.GetWaferPurgeCount(0, "PurgeCountAfterWaferPicking");
                    SchUnLoad.LastAfAtmPurgeDelay =
                        SchUnLoad.GetWaferPumpDelayTime(0, "PumpDelayTimeAfterWaferPicking");


                    SchUnLoad.PurgeBeforeWaferPicking(cycle, pumpDelay);
                    SchUnLoad.GetWaferInfo(0).NextSequenceStep++;
                    return;
                }

                if (!SchUnLoad.IsAvailable)
                {
                    return;
                }

                if (SchUnLoad.CheckPurgedBeforeWaferPicking()
                    && !SchUnLoad.IsReadyForPick(ModuleName.WaferRobot, 0)
                    && !SchLoadLock.IsInPumping)
                {
                    // 准备好让WaferRobot来取
                    SchUnLoad.PrepareTransfer(ModuleName.WaferRobot, EnumTransferType.Pick, 0);
                }
            }
            else // 如果UnLoad里啥也没有，则准备好下次TMRobot喂 W&T
            {
                if (!SchUnLoad.IsAvailable)
                {
                    return;
                }

                // 如果Wafer取完后还没有Purge，则终止后去操作。
                if (!SchUnLoad.CheckPurgedAfterWaferPicked())
                    return;

                if (!SchUnLoad.IsAvailable)
                {
                    return;
                }

                if (!SchUnLoad.IsReadyForPlace(ModuleName.TMRobot, 0)
                    && !SchLoadLock.IsInPumping)
                {
                    SchUnLoad.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Place, 0);
                    return;
                }
            }
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

        private void MonitorPMTask()
        {
            foreach (var pm in LstPmSchedulers)
            {
                if (!pm.IsAvailable)
                    continue;

                if (SchPm1.HasWafer(0))
                {
                    if (SchPm1.CheckNeedRunClean(out bool withWafer, out string recipe) && withWafer
                          && GetModule(pm.Module).GetWaferInfo(0).Status == WaferStatus.Dummy
                          && GetModule(pm.Module).GetWaferInfo(0).ProcessState == EnumWaferProcessStatus.Wait)
                    {
                        pm.Process(recipe, true, withWafer);
                        continue;
                    }

                    if (GetModule(pm.Module).CheckWaferNeedProcess(0, pm.Module))
                    {
                        WaferInfo wafer = GetModule(pm.Module).GetWaferInfo(0);
                        if (pm.Process(wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].RecipeName, false, true))
                        {
                            GetModule(pm.Module).GetWaferInfo(0).NextSequenceStep++;
                            continue;
                        }
                    }
                    else
                    {
                        if (!pm.IsReadyForPick(ModuleName.TMRobot, 0))
                        {
                            pm.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Pick, 0);
                        }
                    }
                }
                else
                {
                    if (GetModule(pm.Module).CheckNeedRunClean(out bool withWafer, out string recipe) && !withWafer)
                    {
                        pm.Process(recipe, true, withWafer);
                        continue;
                    }

                    if (!pm.IsReadyForPlace(ModuleName.TMRobot, 0))
                    {
                        pm.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Place, 0);
                    }
                }
            }
        }

        private void MonitorTmRobotTask()
        {
            if (!SchTmRobot.IsAvailable)
                return;

            foreach (var pm in LstPmSchedulers)
            {
                if (pm.IsWaitTransfer(ModuleName.TMRobot))
                    pm.StopWaitTransfer(ModuleName.TMRobot);
            }

            if (SchBuffer.IsWaitTransfer(ModuleName.TMRobot))
                SchBuffer.StopWaitTransfer(ModuleName.TMRobot);

            if (SchLoadLock.IsWaitTransfer(ModuleName.TMRobot))
                SchLoadLock.StopWaitTransfer(ModuleName.TMRobot);

            if (SchUnLoad.IsWaitTransfer(ModuleName.TMRobot))
                SchUnLoad.StopWaitTransfer(ModuleName.TMRobot);


            MonitorTmRobotPMTask();

            MonitorTmRobotBufferTask();

            MonitorTmRobotLoadTask();

            MonitorTmRobotUnLoadTask();
        }

        private void MonitorWaferRobotTask()
        {
            foreach (var cass in LstCassetteSchedulers)
            {
                if (cass.IsWaitTransfer(ModuleName.WaferRobot))
                    cass.StopWaitTransfer(ModuleName.WaferRobot);
            }

            if (SchAligner.IsWaitTransfer(ModuleName.WaferRobot))
                SchAligner.StopWaitTransfer(ModuleName.WaferRobot);

            if (SchLoadLock.IsWaitTransfer(ModuleName.WaferRobot))
                SchLoadLock.StopWaitTransfer(ModuleName.WaferRobot);

            if (SchUnLoad.IsWaitTransfer(ModuleName.WaferRobot))
                SchUnLoad.StopWaitTransfer(ModuleName.WaferRobot);

            MonitorWaferRobotCassetteTask();
            MonitorWaferRobotUnLoadTask();
            MonitorWaferRobotAlignerTask();
            MonitorWaferRobotLoadTask();
        }

        private void MonitorTrayRobotTask()
        {
            if (SchCassBL.IsWaitTransfer(ModuleName.TrayRobot))
                SchCassBL.StopWaitTransfer(ModuleName.TrayRobot);

            if (SchLoadLock.IsWaitTransfer(ModuleName.TrayRobot))
                SchLoadLock.StopWaitTransfer(ModuleName.TrayRobot);

            MonitorTrayRobotLoadTask();

            MonitorTrayRobotCassetteTask();
        }

        private void MonitorTmRobotLoadTask()
        {
            if (!SchLoadLock.IsAvailable)
                return;
            if (!SchTmRobot.IsAvailable)
                return;
            
            // TMRobot拿到失效的Tray，并且Load没Tray没Wafer，则可以将Tray放入Load准备回收
            var canPlace = 
                SchTmRobot.HasTrayAndExceedProcessCount(0)
                && SchTmRobot.NoWafer(0) 
                && SchLoadLock.NoTray(0)
                && SchLoadLock.NoWafer(0);
            if (canPlace)
            {
                // Unload和Load公用一个真空泵，Unload抽真空时Load不能抽真空。
                if (SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0) && !SchUnLoad.IsInPumping)
                {
                    if (SchTmRobot.Place(SchLoadLock.Module, 0, Hand.Blade1))
                    {
                        SchLoadLock.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
            }

            if (!SchLoadLock.IsAvailable)
                return;
            if (!SchTmRobot.IsAvailable)
                return;

            // TMRobot拿着可用的空Tray，并且Load有Wafer没Tray，则可以放Tray
            canPlace = 
                (SchBuffer.HasAvailableTray() 
                 || (SchTmRobot.HasTray(0) && SchTmRobot.NoWafer(0))
                 || SchPm1.HasTrayAndNotExceedProcessCount(0) 
                 || SchUnLoad.HasTrayAndNotExceedProcessCount(0))
                && SchLoadLock.NoTray(0)  // Load没Tray
                && SchLoadLock.HasWafer(0); // Load有Wafer
            if (canPlace)
            {
                // 如果 Wafer还没Purge，先Purge Wafer
                if (!SchLoadLock.HasPurged)
                {
                    if (SchLoadLock.CheckWaferNextStepIsThis(ModuleName.LoadLock, 0) 
                        && !SchUnLoad.IsInPumping
                        && (SchTmRobot.NoWafer(0) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0))
                        && SchWaferRobot.CheckTaskDone()) // Wafer Place完成，否则还在Vent，会触发Interlock
                    {
                        // Group以后执行吹扫动作
                        SchLoadLock.Purge(SchLoadLock.GetWaferPurgeCount(0), SchLoadLock.GetWaferPumpDelayTime(0));
                        SchLoadLock.GetWaferInfo(0).NextSequenceStep++;
                        return;
                    }

                    return;
                }
                
                
                if (SchTmRobot.HasTray(0) 
                    && SchTmRobot.NoWafer(0) 
                    && SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0) 
                    && !SchUnLoad.IsInPumping)
                {
                    if (SchTmRobot.Place(SchLoadLock.Module, 0, Hand.Blade1))
                    {
                        SchLoadLock.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
            }


            if (!SchLoadLock.IsAvailable)
                return;
            if (!SchTmRobot.IsAvailable)
                return;

            //pick TM无Tray,需要Process,下一个位置没有Tray
            bool canPick = SchTmRobot.NoTray(0)
                           && SchTmRobot.NoWafer(0)
                           && SchLoadLock.CheckWaferNeedProcess(0);
            if (canPick)
            {
                // 没组合先组合
                if (!SchLoadLock.CheckWaferTrayGrouped())
                    return;
                
                // 没Purge先Purge
                if (!SchLoadLock.HasPurged)
                {
                    if (SchLoadLock.CheckWaferNextStepIsThis(ModuleName.LoadLock, 0) 
                        && !SchUnLoad.IsInPumping
                        && (SchTmRobot.NoWafer(0) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0)))
                    {
                        // Group以后执行吹扫动作
                        SchLoadLock.Purge(SchLoadLock.GetWaferPurgeCount(0), SchLoadLock.GetWaferPumpDelayTime(0));
                        SchLoadLock.GetWaferInfo(0).NextSequenceStep++;
                        return;
                    }

                    return;
                }
                

                if (SchLoadLock.IsReadyForPick(ModuleName.TMRobot, 0) 
                    && !SchUnLoad.IsInPumping)
                {
                    if (SchTmRobot.Pick(SchLoadLock.Module, 0, Hand.Blade1))
                    {
                        SchLoadLock.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
            }

        }

        private void MonitorTmRobotUnLoadTask()
        {
            if (!SchUnLoad.IsAvailable)
                return;
            if (!SchTmRobot.IsAvailable)
                return;

            //UnLoad取盘只关心LoadLock是否有空位

            //place Robot有Wafer,UnLoad无Tray无Wafer
            bool canPlaceUnLoad = SchTmRobot.HasWafer(0)
                && SchUnLoad.NoTray(0)
                && SchUnLoad.NoWafer(0)
                && (SchAligner.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0) || SchTmRobot.CheckWaferSequenceStepDone(0))
                ;//&& _tmRobot.GetWaferInfo(0).ProcessState == EnumWaferProcessStatus.Completed;
            if (canPlaceUnLoad)
            {
                if (SchUnLoad.IsReadyForPlace(ModuleName.TMRobot, 0) && !SchLoadLock.IsInPumping)
                {
                    if (SchTmRobot.Place(SchUnLoad.Module, 0, Hand.Blade1))
                    {
                        SchUnLoad.ResetPurgedAndSeparatedStatus();
                        SchUnLoad.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
            }


            if (!SchUnLoad.IsAvailable)
                return;
            if (!SchTmRobot.IsAvailable)
                return;

            //pick UnLoad有Tray,TM无Tray,LoadLock没有Tray,UnLoad分离完成
            bool canPickUnLoad = SchTmRobot.NoTray(0)
                                 && SchTmRobot.NoWafer(0)
                                 && SchUnLoad.HasTray(0)
                                 && SchLoadLock.NoTray(0);
            //&& SchUnLoad.GetWaferInfo(0).ProcessState == EnumWaferProcessStatus.Completed;
            if (canPickUnLoad)
            {
                //需要UnLoad把石墨盘和Wafer分离完成
                if (!SchUnLoad.CheckWaferTraySeparated())
                {
                    return;
                }

                if (SchUnLoad.IsReadyForPick(ModuleName.TMRobot, 0) 
                    && SchBuffer.HasEmptySlot()
                    && !SchLoadLock.IsInPumping)
                {
                    if (SchTmRobot.Pick(SchUnLoad.Module, 0, Hand.Blade1))
                    {
                        SchUnLoad.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
            }
        }


        private void MonitorTmRobotBufferTask()
        {
            if (!SchTmRobot.IsAvailable)
                return;
            if (!SchBuffer.IsAvailable)
                return;

            //place Buffer位置没有Tray,Robot有Wafer,下一步骤是Buffer
            bool canPlace = SchTmRobot.HasWafer(0) && SchBuffer.CheckWaferNextStepIsThis(ModuleName.TMRobot, 0);
            if (canPlace)
            {
                SlotItem bufferEmptySlot = GetEmptyBufferSlot(SchTmRobot.GetWaferInfo(0));
                if (bufferEmptySlot == null)
                {
                    return;
                }

                if (SchBuffer.IsReadyForPlace(ModuleName.TMRobot, bufferEmptySlot.Slot))
                {
                    if (SchTmRobot.Place(SchBuffer.Module, bufferEmptySlot.Slot, Hand.Blade1))
                    {
                        SchBuffer.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
                return;
            }

            if (!SchTmRobot.IsAvailable)
                return;
            if (!SchBuffer.IsAvailable)
                return;

            // TMRobot有Tray没wafer
            var canPlaceTrayToBuffer = 
                SchTmRobot.HasTray(0)  
                && SchTmRobot.NoWafer(0) 
                && (SchLoadLock.HasTray(0)  // Load已经有盘
                    || !SchLoadLock.CheckWaferNeedProcess(0)  // Load有Wafer但不需要做工艺
                    || (SchLoadLock.CheckWaferNeedProcess(0)  && !SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0))); // Load有Wafer需要做工艺但还没主备好放盘
            if (canPlaceTrayToBuffer)
            {
                // TMRobot有Tray，并且Tray已消耗完，Load空，则准备将Tray返回Cassette
                if (SchTmRobot.HasTrayAndExceedProcessCount(0) && SchLoadLock.NoTray(0) && SchLoadLock.NoWafer(0))
                    return;

                // 如果Load已经准备好接收Tray，则不要将Tray放如Buffer
                if (SchLoadLock.HasWafer(0) && SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0))
                    return;


                SlotItem bufferEmptySlot = null;
                for (int i = 0; i < 3; i++)
                {
                    if (SchBuffer.NoTray(i) && SchBuffer.NoWafer(i))
                    {
                        bufferEmptySlot = new SlotItem(ModuleName.Buffer, i);
                        break;
                    }
                }

                if (bufferEmptySlot == null)
                {
                    return;
                }

                if (SchBuffer.IsReadyForPlace(ModuleName.TMRobot, bufferEmptySlot.Slot))
                {
                    if (SchTmRobot.Place(SchBuffer.Module, bufferEmptySlot.Slot, Hand.Blade1))
                    {
                        SchBuffer.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
            }


            if (!SchTmRobot.IsAvailable)
                return;
            if (!SchBuffer.IsAvailable)
                return;

            //pick 当前步(UnLoad没有Wafer),Buffer有Tray,机械手没有Tray
            bool canPick = SchTmRobot.NoTray(0);
            if (canPick)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (SchBuffer.HasTray(i) && !SchBuffer.CheckWaferNextStepIsThis(ModuleName.Buffer, i))
                    {
                        int bufferSetValue = 0;
                        if (!GetWaferSequenceCurrentValue(ModuleName.Buffer, i, "BufferType", out string strBufferType))
                        {
                            continue;
                        }
                        if (!GetWaferSequenceCurrentValue(ModuleName.Buffer, i, "SetValue", out string strBufferSetValue))
                        {
                            continue;
                        }
                        if (!Int32.TryParse(strBufferSetValue, out bufferSetValue))
                        {
                            continue;
                        }

                        //分别判断冷却和加热方式温度是否达到
                        if (strBufferType == "HeatByTemp" && SchBuffer.GetTemperature() < bufferSetValue)
                        {
                            continue;
                        }
                        else if (strBufferType == "CoolingByTemp" && SchBuffer.GetTemperature() > bufferSetValue)
                        {
                            continue;
                        }

                        //如果已经完成所有步骤或者下一步需要经过UnLoad,UnLoad不允许有盘
                        if (SchBuffer.CheckWaferSequenceStepDone(i) || SchUnLoad.CheckWaferNextStepIsThis(ModuleName.Buffer, i) || SchAligner.CheckWaferNextStepIsThis(ModuleName.Buffer, i))
                        {
                            if (SchUnLoad.HasWafer(0) || SchUnLoad.HasTray(0))
                            {
                                continue;
                            }
                        }

                        //下一步如果去PM1，判断PM1是否准备好
                        if (SchPm1.CheckWaferNextStepIsThis(ModuleName.Buffer, i))
                        {
                            if (!SchPm1.IsAvailable || SchPm1.HasTray(0) || SchPm1.HasWafer(0) || !SchPm1.IsReadyForPlace(ModuleName.TMRobot, 0) ||
                                !SchPm1.CheckBufferToPMTemp())
                                continue;
                        }

                        // 取Wafer，送入PM或UnLoad
                        if (SchBuffer.CheckWaferNextStepModuleNoTray(i))
                        {
                            // 如果Buffer[i]中的盘的下一站是PM，但PM没准备好，则啥也不干
                            if(SchPm1.CheckWaferNextStepIsThis(ModuleName.Buffer, i) 
                               && !SchPm1.IsReadyForPlace(ModuleName.TMRobot, 0))
                                continue;
                            
                            // 如果Buffer[i]中的盘的下一站是UnLoad，但UnLoad没准备好，则啥也不干
                            if(SchUnLoad.CheckWaferNextStepIsThis(ModuleName.Buffer, i) 
                               && (!SchUnLoad.IsAvailable || !SchUnLoad.IsReadyForPlace(ModuleName.TMRobot, 0) || SchLoadLock.IsInPumping))
                                continue;

                            // Buffer准备好取盘
                            if (SchBuffer.IsReadyForPick(ModuleName.TMRobot, i))
                            {
                                if (SchTmRobot.Pick(SchBuffer.Module, i, Hand.Blade1))
                                {
                                    SchBuffer.WaitTransfer(ModuleName.TMRobot);
                                    return;
                                }
                            }
                        }
                    }
                }
            }


            if (!SchTmRobot.IsAvailable)
                return;
            if (!SchBuffer.IsAvailable)
                return;

            //pick 工艺开始时,如果Buffer中有Tray时,优先取Buffer中的Tray，而不是Cst中Tray
            canPick = SchTmRobot.NoTray(0)
                      && SchLoadLock.NoTray(0)
                      && SchTrayRobot.NoTray(0) && SchTrayRobot.CheckTaskDone()
                      && ((GetWaferInJobQueue() != null || SchAligner.CheckWaferNeedProcess(0) || SchWaferRobot.CheckWaferNeedProcess(0)) || SchLoadLock.CheckWaferNeedProcess(0));
            if (canPick)
            {
                //优先取出超过ProcessCount的Tray
                for (int i = 0; i < 3; i++)
                {
                    if (SchBuffer.HasTray(i) && SchBuffer.NoWafer(i))
                    {
                        if (SchBuffer.IsReadyForPick(ModuleName.TMRobot, i) 
                            && SchBuffer.HasTrayAndExceedProcessCount(i)
                            && SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0))
                        {
                            if (SchTmRobot.Pick(SchBuffer.Module, i, Hand.Blade1))
                            {
                                SchBuffer.WaitTransfer(ModuleName.TMRobot);
                                return;
                            }
                        }
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    if (SchBuffer.HasTray(i) && SchBuffer.NoWafer(i))
                    {
                        // 仅当Load准备好放Tray后，才从Buffer拿Tray
                        if (SchBuffer.IsReadyForPick(ModuleName.TMRobot, i)
                            && SchLoadLock.IsAvailable
                            && SchLoadLock.CheckWaferNeedProcess(0)
                            && SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0)
                            && !SchUnLoad.IsInPumping)
                        {
                            if (SchTmRobot.Pick(SchBuffer.Module, i, Hand.Blade1))
                            {
                                SchBuffer.WaitTransfer(ModuleName.TMRobot);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void MonitorTmRobotPMTask()
        {
            if (!SchTmRobot.IsAvailable)
                return;


            // 是否需要将Wafer传入PM
            bool blade0HasWaferAndNeedProcess = SchTmRobot.HasWafer(0) && SchTmRobot.CheckWaferNeedProcess(0);

            if (blade0HasWaferAndNeedProcess)
            {
                foreach (var pm in LstPmSchedulers)
                {
                    if (!pm.IsAvailable
                        || !GetModule(pm.Module).NoWafer(0)
                        || !pm.IsReadyForPlace(ModuleName.TMRobot, 0)
                        || !pm.CheckTempBelow900())
                        continue;
                    

                    bool blade0Place = SchTmRobot.HasWafer(0) && SchTmRobot.CheckWaferNeedProcess(0, pm.Module);
                    if (blade0Place)
                    {
                        Hand placeBlade = Hand.Blade1;

                        if (SchTmRobot.Place(pm.Module, 0, placeBlade))
                        {
                            pm.WaitTransfer(ModuleName.TMRobot);
                            return;
                        }
                    }
                }
            }

            if (!SchTmRobot.IsAvailable)
                return;

            //pick from pm
            if (SchTmRobot.NoWafer(0) && SchTmRobot.NoTray(0))
            {
                Hand pickBlade = Hand.Blade1;

                ModuleName pickPm = ModuleName.System;
                foreach (var schedulerPm in LstPmSchedulers)
                {
                    //增加温度低于900才能Pick的限制
                    if (!schedulerPm.IsAvailable
                        || GetModule(schedulerPm.Module).NoWafer(0)
                        || GetModule(schedulerPm.Module).CheckWaferNeedProcess(0, schedulerPm.Module)
                        || SchTmRobot.HasWafer((int)pickBlade)
                        || !schedulerPm.CheckTempBelow900())
                        continue;

                    //如果下一步是Buffer
                    if (SchBuffer.CheckWaferNextStepIsThis(schedulerPm.Module, 0))
                    {
                        SlotItem bufferEmptySlot = GetEmptyBufferSlot(GetModule(schedulerPm.Module).GetWaferInfo(0));
                        if (bufferEmptySlot == null)
                        {
                            return;
                        }
                    }
                    else if (SchUnLoad.HasWafer(0) || SchUnLoad.HasTray(0))
                    {
                        return;
                    }

                    pickPm = schedulerPm.Module;
                    break;
                }

                if (pickPm != ModuleName.System)
                {
                    SchedulerModule pm = GetModule(pickPm.ToString());

                    // 是否下一站是Buffer，并且Buffer里有空槽位
                    var nextToBufferAndAvailable =
                        (SchBuffer.CheckWaferNextStepIsThis(pm.Module, 0)
                         || SchBuffer.CheckWaferNextStepIsThis(pm.Module, 1)
                         || SchBuffer.CheckWaferNextStepIsThis(pm.Module, 2))
                        && SchBuffer.HasEmptySlot();

                    // 下一站是UnLoad并且UnLoad准备好放盘
                    var nextToUnloadAndAvailable = 
                        SchUnLoad.IsAvailable
                            &&SchUnLoad.CheckWaferNextStepIsThis(pm.Module, 0) 
                        && SchUnLoad.IsReadyForPlace(ModuleName.TMRobot, 0);

                    if (pm.IsReadyForPick(ModuleName.TMRobot, 0)) // PM准备好取盘
                    {
                        // Buffer和UnLoad都没准备好，先不要从PM拿盘
                        if (!nextToBufferAndAvailable && !nextToUnloadAndAvailable)
                            return;

                        if (SchTmRobot.Pick(pm.Module, 0, pickBlade))
                        {
                            pm.WaitTransfer(ModuleName.TMRobot);

                            return;
                        }
                    }
                }
            }
        }

        private void MonitorWaferRobotLoadTask()
        {
            if (!SchWaferRobot.IsAvailable)
                return;
            if (!SchLoadLock.IsAvailable)
                return;


            //place Load没有Wafer有Tray,Robot有Wafer,步骤没有完成,下一步不是Aligner (其它:SlitValve都关了,大气)
            bool canPlace = SchWaferRobot.HasWafer(0)
                && SchLoadLock.NoWafer(0)
                         && SchLoadLock.NoTray(0)
                         && SchWaferRobot.GetWaferInfo(0).Status != WaferStatus.Dummy
                         && !SchAligner.CheckWaferNextStepIsThis(ModuleName.WaferRobot, 0)
                         && SchWaferRobot.CheckWaferNeedProcess(0);

            if (canPlace)
            {
                if (SchLoadLock.IsReadyForPlace(ModuleName.WaferRobot, 0))
                {
                    if (SchWaferRobot.Place(SchLoadLock.Module, 0, Hand.Blade1))
                    {
                        SchLoadLock.WaitTransfer(ModuleName.WaferRobot);
                        return;
                    }
                }
            }
        }

        private void MonitorWaferRobotUnLoadTask()
        {
            if (!SchWaferRobot.IsAvailable)
                return;
            if (!SchUnLoad.IsAvailable)
                return;

            //pick UnLoad有Wafer没有Tray,Robot没有Wafer,步骤都完成了或者下一步的模块没有片子  (其它:UnLoad 夹爪夹住,顶针下降,SlitValve都关了,大气)
            bool canPick = SchWaferRobot.NoWafer(0)
                && SchUnLoad.HasWafer(0)
                && SchUnLoad.NoTray(0)
                && (SchUnLoad.CheckWaferNextStepModuleNoWafer(0) || SchUnLoad.CheckWaferSequenceStepDone(0))
                && SchUnLoad.GetWaferInfo(0).Status != WaferStatus.Dummy;

            if (canPick)
            {
                if (!SchAligner.IsAvailable)
                    return;

                //下一步是Aligner并且Aligner上不允许有片子
                if (SchAligner.CheckWaferNextStepIsThis(ModuleName.UnLoad, 0))
                {
                    if (SchAligner.HasWafer(0))
                    {
                        return;
                    }
                }

                if (!SchUnLoad.IsAvailable)
                    return;

                if (SchUnLoad.IsReadyForPick(ModuleName.WaferRobot, 0))
                {
                    if (SchWaferRobot.Pick(SchUnLoad.Module, 0, Hand.Blade1))
                    {
                        SchUnLoad.WaitTransfer(ModuleName.WaferRobot);
                        return;
                    }
                }
            }


            _trigUnLoadWaferLeave.CLK = SchUnLoad.HasWafer(0) == false;
            if (_trigUnLoadWaferLeave.Q)
                SchUnLoad.HasWaferPickedByWaferRobot = true;

            if (SchUnLoad.HasWaferPickedByWaferRobot && !SchUnLoad.CheckPurgedAfterWaferPicked())
            {
                if (!SchUnLoad.IsAvailable)
                    return;

                // 如果Wafer取完后还没有Purge，先Purge
                if (!SchLoadLock.IsInPumping
                    && SchWaferRobot.IsAvailable) // WaferRobot刚取完Wafer，关闭闸板阀后，还在Vent，此时先不要Purge
                {
                    SchUnLoad.PurgeAfterWaferPicked(SchUnLoad.LastAfAtmPurgeCount, SchUnLoad.LastAfAtmPurgeDelay);
                }
            }
        }

        private void MonitorWaferRobotAlignerTask()
        {
            if (!SchWaferRobot.IsAvailable || !SchAligner.IsAvailable)
            {
                return;
            }

            //place 任务还没完成,下一步是Aligner,Robot有片子,Aligner没片子
            var canPlaceAligner =
                SchAligner.NoWafer(0)
                && SchWaferRobot.HasWafer(0)
                && SchAligner.CheckWaferNextStepIsThis(ModuleName.WaferRobot, 0)
                && SchWaferRobot.GetWaferInfo(0).Status != WaferStatus.Dummy;
            if (canPlaceAligner)
            {
                if (SchAligner.IsReadyForPlace(ModuleName.WaferRobot, 0))
                {
                    if (SchWaferRobot.Place(SchAligner.Module, 0, Hand.Blade1))
                    {
                        SchAligner.WaitTransfer(ModuleName.WaferRobot);
                        return;
                    }
                }
            }

            if (!SchWaferRobot.IsAvailable || !SchAligner.IsAvailable)
            {
                return;
            }

            // 检查WaferRobot是否可以从Aligner取片
            if (SchAligner.HasAligned && SchAligner.HasWafer(0) && SchWaferRobot.NoWafer(0)
                && SchAligner.GetWaferInfo(0).Status != WaferStatus.Dummy 
                && !SchAligner.CheckWaferNextStepIsThis(ModuleName.Aligner, 0))
            {
                // 取片准备送入LoadLock
                var canPickForLoadLock = 
                    SchLoadLock.CheckWaferNextStepIsThis(ModuleName.Aligner, 0)
                    && SchLoadLock.IsReadyForPlace(ModuleName.WaferRobot, 0);

                //pick Aligner wafer to load
                if (canPickForLoadLock)
                {
                    //需要运行工艺,Load不能有片子，Load不能有Tray
                    if (SchAligner.CheckWaferNeedProcess(0))
                    {
                        if (SchLoadLock.HasWafer(0)
                            || SchLoadLock.HasTray(0)
                            || SchTmRobot.HasTrayAndExceedProcessCount(0)
                            || SchBuffer.HasTrayAndExceedProcessCount(0)
                            || SchBuffer.HasTrayAndExceedProcessCount(1)
                            || SchBuffer.HasTrayAndExceedProcessCount(2))
                        {
                            return;
                        }
                    }

                    if (SchAligner.IsReadyForPick(ModuleName.WaferRobot, 0))
                    {
                        if (SchWaferRobot.Pick(SchAligner.Module, 0, Hand.Blade1))
                        {
                            SchAligner.WaitTransfer(ModuleName.WaferRobot);
                            return;
                        }
                    }
                }

                //pick Aligner wafer to cst
                var canPickForCass = SchAligner.CheckWaferSequenceStepDone(0);

                //WaferRobot从Aligner Pick Wafer不应检查Load腔条件

                if (canPickForCass)
                {
                    if (SchAligner.IsReadyForPick(ModuleName.WaferRobot, 0))
                    {
                        if (SchWaferRobot.Pick(SchAligner.Module, 0, Hand.Blade1))
                        {
                            SchAligner.WaitTransfer(ModuleName.WaferRobot);
                            return;
                        }
                    }
                }
            }
        }


        private void MonitorWaferRobotCassetteMapTask()
        {
            //if (!SchWaferRobot.IsAvailable)
            //    return;

            //if (SchWaferRobot.HasWafer(0))
            //{
            //    return;
            //}
            //else
            //{
            //    ControlJobInfo cjActive = _lstNeedMapJob.Find(x => x.State == EnumControlJobState.WaitingForStart);
            //    if (cjActive != null)
            //    {
            //        if (SchWaferRobot.Map(ModuleHelper.Converter(cjActive.Module)))
            //        { 

            //        }
            //    }
            //}
        }

        /// <summary>
        /// 检查是否Buffer和Cassette中已经没有可用的Tray
        /// </summary>
        /// <returns></returns>
        public bool NoAvailableTray()
        {
         
            return GetTraySlot(8) == null // Cass 里没Tray
                   && (SchBuffer.NoTray(0) || SchBuffer.HasTrayAndExceedProcessCount(0) || SchBuffer.HasWafer(0))  // Buffer1没Tray，或Tray用完，或有Wafer
                   && (SchBuffer.NoTray(1) || SchBuffer.HasTrayAndExceedProcessCount(1) || SchBuffer.HasWafer(1))  // Buffer2没Tray，或Tray用完，或有Wafer
                   && (SchBuffer.NoTray(2) || SchBuffer.HasTrayAndExceedProcessCount(2) || SchBuffer.HasWafer(2))  // Buffer3没Tray，或Tray用完，或有Wafer
                   && (SchPm1.NoTray(0) || SchPm1.HasTrayAndExceedProcessCount(0)) // PM1 没Tray，或者有Tray但次数不够
                   && (SchUnLoad.NoTray(0) || SchUnLoad.HasTrayAndExceedProcessCount(0));
        }

        private void MonitorWaferRobotCassetteTask()
        {
            if (!SchWaferRobot.IsAvailable)
                return;

            //place 任务完成了,Robot有片子,Cassette没有片子
            bool canPlaceCassette = SchWaferRobot.HasWafer(0)
                && SchWaferRobot.GetWaferInfo(0).Status != WaferStatus.Dummy
                && SchWaferRobot.CheckWaferSequenceStepDone(0);
            if (canPlaceCassette)
            {
                WaferInfo wafer = SchWaferRobot.GetWaferInfo(0);
                if (GetWaferReturnedCassette((ModuleName)wafer.OriginStation) == ModuleName.CassAL)
                {
                    if (SchCassAL.IsAvailable && SchCassAL.IsReadyForPlace(ModuleName.WaferRobot, wafer.OriginSlot))
                    {
                        if (SchWaferRobot.Place(SchCassAL.Module, wafer.OriginSlot, Hand.Blade1))
                        {
                            SchCassAL.WaitTransfer(ModuleName.WaferRobot);
                            return;
                        }
                    }
                }
                else if (GetWaferReturnedCassette((ModuleName)wafer.OriginStation) == ModuleName.CassAR)
                {
                    if (SchCassAR.IsAvailable && SchCassAR.IsReadyForPlace(ModuleName.WaferRobot, wafer.OriginSlot))
                    {
                        if (SchWaferRobot.Place(SchCassAR.Module, wafer.OriginSlot, Hand.Blade1))
                        {
                            SchCassAR.WaitTransfer(ModuleName.WaferRobot);
                            return;
                        }
                    }
                }
            }

            if (!SchWaferRobot.IsAvailable)
                return;

            //pick Robot没有片子,下一步的模块没有片子
            bool canPick = SchWaferRobot.NoWafer(0);
            SlotItem position = GetWaferInJobQueue();
            if (canPick && position != null)
            {
                // 没有可用的Tray，不要取Wafer
                if (NoAvailableTray())
                    return; 

                // 如果Buffer、TMRobot、Load中有工艺次数用完的Tray，先不要拿Wafer，等Tray传出去
                if (!SchLoadLock.IsReadyForPlace(ModuleName.WaferRobot, 0)
                    || SchLoadLock.HasWafer(0)
                    || SchLoadLock.HasTrayAndExceedProcessCount(0)
                    || SchLoadLock.HasTrayAndExceedProcessCount(0)
                    || SchTmRobot.HasTrayAndExceedProcessCount(0)
                    || SchBuffer.HasTrayAndExceedProcessCount(0)
                    || SchBuffer.HasTrayAndExceedProcessCount(1)
                    || SchBuffer.HasTrayAndExceedProcessCount(2)) 
                {
                    return;
                }
                
                //下一步是Aligner并且Aligner上不允许有片子
                if (SchAligner.CheckWaferNextStepIsThis(position.Module, position.Slot))
                {
                    if (SchAligner.HasWafer(0))
                    {
                        return;
                    }
                    ////如果UnLoad有Wafer,而且下一步是Aligner,则不能取
                    //if (CheckWaferNextStepIsAligner(ModuleName.UnLoad, 0))
                    //{
                    //    return;
                    //}
                }

              

                if (GetRunWaferCount() >= _maxTrayCount)//超过可以运行的石墨盘最大数
                {
                    return;
                }

                if (position.Module == ModuleName.CassAR)
                {
                    if (SchCassAR.IsAvailable && SchCassAR.IsReadyForPick(ModuleName.WaferRobot, 0))
                    {
                        if (SchWaferRobot.Pick(position.Module, position.Slot, Hand.Blade1))
                        {
                            SchCassAR.WaitTransfer(ModuleName.WaferRobot);
                            return;
                        }
                    }
                }
                else if (position.Module == ModuleName.CassAL)
                {
                    if (SchCassAL.IsAvailable && SchCassAL.IsReadyForPick(ModuleName.WaferRobot, 0))
                    {
                        if (SchWaferRobot.Pick(position.Module, position.Slot, Hand.Blade1))
                        {
                            SchCassAL.WaitTransfer(ModuleName.WaferRobot);
                            return;
                        }
                    }
                }

            }
        }


        private void MonitorTrayRobotLoadTask()
        {
            //----------------------------------
            string s = "";
            if (WhichCondition == "TrayRobotLoad")
            {
                s += AddC("SchTrayRobot.IsAvailable", SchTrayRobot.IsAvailable.ToString(), "True");
                s += AddC("SchLoadLock.IsAvailable", SchLoadLock.IsAvailable.ToString(), "True");
                s += "canPlace:\r\n";
                s += AddC("SchLoadLock.NoTray(0)", SchLoadLock.NoTray(0).ToString(), "True");
                s += AddC("SchTrayRobot.HasTray(0)", SchTrayRobot.HasTray(0).ToString(), "True");
            }
            AutoTransferConditionText = s;
            //----------------------------------
            //
            if (!SchTrayRobot.IsAvailable)
                return;
            if (!SchLoadLock.IsAvailable)
                return;

            //place UnLoad没有Tray,Load没有Tray,Robot有Tray,Cassette或Aligner有任务未完成
            bool canPlace = SchLoadLock.NoTray(0)
                && SchTrayRobot.HasTrayAndNotExceedProcessCount(0)
                && SchLoadLock.HasWafer(0)
                && (GetWaferInJobQueue() != null || (SchAligner.HasWafer(0) && SchAligner.CheckWaferNeedProcess(0)) || (SchWaferRobot.HasWafer(0) && SchWaferRobot.CheckWaferNeedProcess(0)) || (SchLoadLock.HasWafer(0) && SchLoadLock.CheckWaferNeedProcess(0)));
            if (canPlace)
            {
                //TMRobot有Tray无Wafer也不能放
                if (SchTmRobot.NoWafer(0) && SchTmRobot.HasTray(0))
                {
                    return;
                }

                if (SchLoadLock.IsReadyForPlace(ModuleName.TrayRobot, 0))
                {
                    if (SchTrayRobot.Place(SchLoadLock.Module, 0, Hand.Blade1))
                    {
                        SchLoadLock.WaitTransfer(ModuleName.TrayRobot);
                        return;
                    }
                }
            }
            
            if (!SchTrayRobot.IsAvailable)
                return;
            if (!SchLoadLock.IsAvailable)
                return;


            //pick Load有Tray,判断CassatteA和Aligner还有片子没有跑PM工艺就不需要Pick
            bool needPick = SchLoadLock.HasTrayAndExceedProcessCount(0) && SchTrayRobot.NoTray(0);
            if (needPick)
            {
                SlotItem emptyTraySlot = GetTrayOrignSlot(ModuleName.LoadLock, 0);// GetEmptyTraySlot(13); //先取Wafer再取石墨盘

                // 打印警告信息。
                _rTrigLoadHasExhaustedTrayButCorrespondingCassBSlotHasTray.CLK = emptyTraySlot == null;
                if (_rTrigLoadHasExhaustedTrayButCorrespondingCassBSlotHasTray.Q)
                    EV.PostWarningLog(ModuleName.LoadLock.ToString(),
                        $"Tray in Load belongs to slot {SchLoadLock.GetWaferInfo(0).TrayOriginSlot} but the slot of CassB is not empty.");

                if (emptyTraySlot != null)
                {
                    if (SchLoadLock.IsReadyForPick(ModuleName.TrayRobot, 0))
                    {
                        if (SchTrayRobot.Pick(SchLoadLock.Module, 0, Hand.Blade1))
                        {
                            SchLoadLock.WaitTransfer(ModuleName.TrayRobot);
                            return;
                        }
                    }
                }

                
                
            }

        }

        private readonly R_TRIG _rTrigLoadHasExhaustedTrayButCorrespondingCassBSlotHasTray = new R_TRIG();

        private void MonitorTrayRobotCassetteTask()
        {
            if (!SchTrayRobot.IsAvailable)
                return;

            if (!SchCassBL.IsAvailable)
                return;

            // 判断是否将Tray还回Cassette，分以下三种条件：
            bool isReturnTrayToCass =
                SchTrayRobot.HasTrayAndExceedProcessCount(0) // TrayRobot上有Tray并且达到使用次数上限
                || SchLoadLock.HasTray(0) // Load腔已经有Tray
                || (GetWaferInJobQueue() == null && SchAligner.NoWafer(0) && SchWaferRobot.NoWafer(0) &&
                    SchLoadLock.NoWafer(0)); // 后续没有Job可做

            // Load有失效的Tray准备取出，但手上有Tray，先把Tray还回去
            bool isReturnTrayToCass1 = SchLoadLock.HasTrayAndExceedProcessCount(0) && SchTrayRobot.HasTray(0);

            if (isReturnTrayToCass || isReturnTrayToCass1)
            {
                SlotItem slotItem = GetTrayOrignSlot(ModuleName.TrayRobot, 0); //GetEmptyTraySlot(13);
                if (slotItem != null)
                {
                    if (SchCassBL.IsReadyForPlace(ModuleName.TrayRobot, slotItem.Slot))
                    {
                        if (SchTrayRobot.Place(slotItem.Module, slotItem.Slot, Hand.Blade1))
                        {
                            SchCassBL.WaitTransfer(ModuleName.TrayRobot);
                            return;
                        }
                    }
                }
            }

            //pick Casstte有Tray,UnLoad没有Tray,Load没有Tray(各个腔体运行的石墨盘总数小于设定值)
            if (GetCurrentTrayCount() >= _maxTrayCount)
            {
                return;
            }

            if (!SchTrayRobot.IsAvailable)
                return;

            if (!SchCassBL.IsAvailable)
                return;

            // 如果下述腔体有Tray没Wafer，则等待TMRobot从内部送盘到LL
            if (CheckHasTrayAndNoWafer(new List<Tuple<ModuleName, int>>()
            {
                new Tuple<ModuleName, int>(ModuleName.Buffer,0),
                new Tuple<ModuleName, int>(ModuleName.Buffer,1),
                new Tuple<ModuleName, int>(ModuleName.Buffer,2),
                new Tuple<ModuleName, int>(ModuleName.TMRobot,0),
                new Tuple<ModuleName, int>(ModuleName.LoadLock,0),
            })) 
                return;

            bool canPick = SchLoadLock.HasWafer(0) 
                           && SchLoadLock.NoTray(0)
                           && SchLoadLock.CheckWaferNeedProcess(0)
                           && SchLoadLock.IsAvailable
                           && SchLoadLock.IsReadyForPlace(ModuleName.TrayRobot, 0)
                           && GetAvailableTrayInBuffer() == null // Buffer里有可用的Tray，则不要从Cass拿Tray
                           && SchTmRobot.NoTray(0);
            if (canPick)
            {
                var slotItem = GetTraySlot(8);

                if (slotItem != null)
                {
                    if (SchCassBL.IsReadyForPick(ModuleName.TrayRobot, slotItem.Slot))
                    {
                        if (SchTrayRobot.Pick(slotItem.Module, slotItem.Slot, Hand.Blade1))
                        {
                            SchCassBL.WaitTransfer(ModuleName.TrayRobot);
                            return;
                        }
                    }
                }
            }
        }

        #endregion

        #region Logic Check


        /// <summary>
        /// 获得Buffer的方式和数值
        /// </summary>
        /// <param name="module"></param>
        /// <param name="slot"></param>
        /// <param name="coolingType"></param>
        /// <param name="setValue"></param>
        /// <returns></returns>
        private bool GetWaferSequenceNextValue(ModuleName module, int slot, string nodeName, out string nodeValue)
        {
            nodeValue = "";

            if (!GetModule(module).HasWafer(slot))
                return false;

            WaferInfo wafer = GetModule(module).GetWaferInfo(slot);

            if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                return false;

            if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                return false;

            if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules.Contains(module))
                return false;

            nodeValue = wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter[nodeName].ToString();
            if (String.IsNullOrEmpty(nodeValue))
            {
                return false;
            }
            return true;
        }

        private bool GetWaferSequenceCurrentValue(ModuleName module, int slot, string nodeName, out string nodeValue)
        {
            nodeValue = "";

            if (!GetModule(module).HasWafer(slot))
                return false;

            WaferInfo wafer = GetModule(module).GetWaferInfo(slot);

            if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                return false;

            if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                return false;

            if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep - 1].StepModules.Contains(module))
                return false;

            nodeValue = wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep - 1].StepParameter[nodeName].ToString();
            if (String.IsNullOrEmpty(nodeValue))
            {
                return false;
            }
            return true;
        }

        private bool GetWaferSequenceCoolingTime(ModuleName module, int slot, out int coolingTime)
        {
            coolingTime = 0;

            if (!WaferManager.Instance.CheckHasWafer(module, slot))
                return false;

            WaferInfo wafer = WaferManager.Instance.GetWafer(module, slot);

            if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                return false;

            if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                return false;

            if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules.Contains(module))
                return false;

            if (!int.TryParse(wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter["CoolingTime"].ToString(),
                    out coolingTime))
            {
                coolingTime = 0;
                //coolingTime = SC.GetValue<int>("Unload.DefaultCoolingTime");
                //EV.PostWarningLog("Scheduler", $"Sequence step Unload cooling time is not valid, instead with the SC default value {coolingTime} seconds");

                return false;
            }

            return true;
        }

        private bool CheckHasTrayAndNoWafer(List<Tuple<ModuleName, int>> needCheckPositions)
        {
            foreach (var positionTuple in needCheckPositions)
            {
                var module = GetModule(positionTuple.Item1);
                if (module.HasTray(positionTuple.Item2) && module.NoWafer(positionTuple.Item2))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckTrayExhausted()
        {
            return !(SchBuffer.HasTrayAndNotExceedProcessCount(0)
                     | SchBuffer.HasTrayAndNotExceedProcessCount(1)
                     | SchBuffer.HasTrayAndNotExceedProcessCount(2)
                     | SchCassBL.HasTrayAndNotExceedProcessCount(0)
                     | SchCassBL.HasTrayAndNotExceedProcessCount(1)
                     | SchCassBL.HasTrayAndNotExceedProcessCount(2)
                     | SchCassBL.HasTrayAndNotExceedProcessCount(3)
                     | SchCassBL.HasTrayAndNotExceedProcessCount(4)
                     | SchCassBL.HasTrayAndNotExceedProcessCount(5)
                     | SchCassBL.HasTrayAndNotExceedProcessCount(6)
                     | SchCassBL.HasTrayAndNotExceedProcessCount(7)
                     | SchLoadLock.HasTrayAndNotExceedProcessCount(0)
                     | SchUnLoad.HasTrayAndNotExceedProcessCount(0)
                     | SchTmRobot.HasTrayAndNotExceedProcessCount(0)
                     | SchTrayRobot.HasTrayAndNotExceedProcessCount(0)
                     | SchPm1.HasTrayAndNotExceedProcessCount(0));
        }

        public bool CheckBufferWaferHasJob()
        {
            WaferInfo wafer = SchBuffer.GetWaferInfo(0);

            if (wafer.IsEmpty)
            {
                return false;
            }
            if (wafer.ProcessJob == null)
            {
                return false;
            }

            ProcessJobInfo pj = _lstProcessJobs.Find(x => x.InnerId == wafer.ProcessJob.InnerId);
            if (pj == null)
            {
                return false;
            }

            return true;

        }

        public bool CheckWaferProcessModuleIsAvailable(ModuleName waferModule, int waferSlot)
        {
            WaferInfo wafer = GetModule(waferModule).GetWaferInfo(waferSlot);

            if (wafer.IsEmpty)
                return false;

            if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                return false;

            if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                return false;

            //foreach (var module in wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules)
            //{
            //if (GetModule(module).NoWafer(0)
            //        && _lstPms.Find(x => x.Module == module).IsAvailable
            //        && !CheckNeedRunClean(module, out bool _, out string _))
            //        return true;
            //}

            foreach (var step in wafer.ProcessJob.Sequence.Steps)
            {
                foreach (var module in step.StepModules)
                {
                    if (module.ToString().StartsWith("PM")
                        && GetModule(module).NoWafer(0)
                        && LstPmSchedulers.Find(x => x.Module == module).IsAvailable
                        && !GetModule(module).CheckNeedRunClean(out bool _, out string _))
                        return true;
                }
            }

            return false;
        }


        private SlotItem GetWaferInJobQueue()
        {
            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Executing)
                {
                    foreach (var pj in _lstProcessJobs)
                    {
                        if (pj.ControlJobName == cj.Name && pj.State == EnumProcessJobState.Processing)
                        {
                            foreach (var pjSlotWafer in pj.SlotWafers)
                            {
                                if (pjSlotWafer.Item1 == ModuleName.CassAL || pjSlotWafer.Item1 == ModuleName.CassAR)
                                {
                                    if (GetModule(pjSlotWafer.Item1).CheckWaferNeedProcess(pjSlotWafer.Item2))
                                        return new SlotItem(pjSlotWafer.Item1, pjSlotWafer.Item2);
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }


        private SlotItem GetAvailableTrayInBuffer(int slotCount = 3)
        {
            for (var i = 0; i < slotCount; i++)
            {
                if (SchBuffer.NoWafer(i) && SchBuffer.HasTrayAndNotExceedProcessCount(i))
                    return new SlotItem(ModuleName.Buffer, i);
            }

            return null;
        }

        private SlotItem GetTraySlot(int slotCount)
        {
            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Executing)
                {
                    for (int i = 0; i < slotCount; i++)
                    {
                        if (SchCassBL.HasTrayAndNotExceedProcessCount(i))
                        {
                            return new SlotItem(ModuleName.CassBL, i);
                        }
                    }
                }
            }
            return null;
        }

        private SlotItem GetTrayOrignSlot(ModuleName module, int slot)
        {
            WaferInfo wafer = GetModule(module).GetWaferInfo(slot);
            if (wafer != null && GetModule(module).HasTray(slot) && SchCassBL.NoTray(wafer.TrayOriginSlot))
            {
                return new SlotItem(ModuleName.CassBL, wafer.TrayOriginSlot);
            }
            return null;
        }

        //private SlotItem GetEmptyTraySlot(int slotCount)
        //{
        //    foreach (var cj in _lstControlJobs)
        //    {
        //        if (cj.State == EnumControlJobState.Executing)
        //        {
        //            for (int i = 0; i < slotCount; i++)
        //            {
        //if (SchCassBL.NoTray(i))
        //                {
        //                    return new SlotItem(ModuleName.CassBL, i);
        //                }
        //            }
        //        }
        //    }
        //    return null;
        //}

        /// <summary>
        /// 获取空位置
        /// </summary>
        /// <param name="isHeat"></param>
        /// <returns></returns>
        private SlotItem GetEmptyBufferSlot(WaferInfo wafer)
        {
            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Executing)
                {
                    string strSots = wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter["SlotSelection"].ToString();
                    if (strSots == null)
                    {
                        return null;
                    }

                    string[] slots = strSots.Split(',');
                    for (int i = 0; i < slots.Length; i++)
                    {
                        int slot = 0;
                        if (Int32.TryParse(slots[i], out slot))
                        {
                            if (SchBuffer.NoTray(slot - 1))
                            {
                                return new SlotItem(ModuleName.Buffer, slot - 1);
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取各个模块的Wafer总数量
        /// </summary>
        /// <returns></returns>
        private int GetRunWaferCount()
        {
            int waferCount = 0;
            if (SchLoadLock.HasWafer(0))
            {
                waferCount++;
            }
            if (SchUnLoad.HasWafer(0))
            {
                waferCount++;
            }
            if (SchTmRobot.HasWafer(0))
            {
                waferCount++;
            }
            foreach (SchedulerPM pm in LstPmSchedulers)
            {
                if (GetModule(pm.Module).HasWafer(0))
                {
                    waferCount++;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                if (SchBuffer.HasWafer(i))
                {
                    waferCount++;
                }
            }
            return waferCount;
        }

        /// <summary>
        /// 获取各个模块的石墨盘总数量
        /// </summary>
        /// <returns></returns>
        private int GetCurrentTrayCount()
        {
            int trayCount = 0;
            if (SchLoadLock.HasTray(0))
            {
                trayCount++;
            }
            if (SchUnLoad.HasTray(0))
            {
                trayCount++;
            }
            if (SchTmRobot.HasTray(0))
            {
                trayCount++;
            }
            foreach (SchedulerPM pm in LstPmSchedulers)
            {
                if (GetModule(pm.Module).HasTray(0))
                {
                    trayCount++;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                if (SchBuffer.HasTray(i))
                {
                    trayCount++;
                }
            }
            return trayCount;
        }

        /// <summary>
        /// 获取ProcessJob可以同时运行工艺的Wafer总数
        /// </summary>
        /// <returns></returns>
        private int GetCurrentWaferCount()
        {
            int waferCountDiv = 0;
            int cassWaferCount = 0;
            foreach (var cj in _lstControlJobs)
            {
                if (cj.State == EnumControlJobState.Executing)
                {
                    foreach (var pj in _lstProcessJobs)
                    {
                        if (pj.ControlJobName == cj.Name && pj.State == EnumProcessJobState.Processing)
                        {
                            cassWaferCount += pj.SlotWafers.Count;
                            foreach (var pjSlotWafer in pj.SlotWafers)
                            {
                                var module = GetModule(pjSlotWafer.Item1);
                                if (module.HasWafer(pjSlotWafer.Item2) && !module.CheckWaferNeedProcess(pjSlotWafer.Item2))
                                {
                                    waferCountDiv++;
                                }
                            }
                        }
                    }
                }
            }

            return cassWaferCount - waferCountDiv;

            //int waferCount = 0;
            //if (SchLoadLock.HasWafer(0))
            //{
            //    waferCount++;
            //}
            //for (int i = 0; i < 3; i++)
            //{
            //if (SchBuffer.HasWafer(i))
            //    {
            //        waferCount++;
            //    }
            //}

            //if (SchWaferRobot.CheckWaferNeedProcess(0) )
            //{
            //    waferCount++;
            //}
            //if (SchAligner.CheckWaferNeedProcess(0) )
            //{
            //    waferCount++;
            //}

            //for (int i=0;i<25;i++)
            //{
            //    if (CheckWaferNeedProcess(ModuleName.CassAL, i))
            //    {
            //        waferCount++;
            //    }
            //    if (CheckWaferNeedProcess(ModuleName.CassAR, i))
            //    {
            //        waferCount++;
            //    }
            //}
            //return waferCount;
        }
        #endregion



        #region Module error

        public Result MonitorModuleError()
        {
            bool isModuleError = false;
            bool[] isPMError = new bool[2];

            for (int i = 0; i < isPMError.Length; i++)
            {
                isPMError[i] = false;
            }

            if (SchTmRobot.IsError)
                isModuleError = true;

            for (int i = 0; i < LstPmSchedulers.Count; i++) // PM出错，不影响其他腔体的传片
            {
                if (LstPmSchedulers[i].IsError)
                    isPMError[i] = true;
            }

            if (isModuleError && !_isModuleErrorPrevious)
            {
                //var jb1 = _lstControlJobs.Find(x => x.Module == "LP1");
                //if (jb1 != null)
                //    PauseJob(jb1.Name);

                //var jb2 = _lstControlJobs.Find(x => x.Module == "LP2");
                //if (jb2 != null)
                //    PauseJob(jb2.Name);
            }
            else if (!isModuleError && _isModuleErrorPrevious)
            {
                Reset();
                //CloseDoor();
            }

            for (int i = 0; i < LstPmSchedulers.Count; i++)
            {
                if (!isPMError[i] && _isPMErrorPrevious[i])
                {
                    LstPmSchedulers[i].ResetTask();
                }

                _isPMErrorPrevious[i] = isPMError[i];
            }

            _isModuleErrorPrevious = isModuleError;

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

        private bool CheckModuleHaveWaferWithNoJob(out string reason)
        {
            reason = "";
            if (_lstControlJobs.Count > 0)
            {
                reason = "lstControlJobs.Count > 0";
                return false;
            }
            else
            {
                if (SchBuffer.HasWafer(0) || SchBuffer.HasWafer(1) || SchBuffer.HasWafer(2))
                {
                    reason = $"Buffer have wafer!";
                    EV.PostWarningLog(LogSource, reason);
                    return true;
                }

                if (SchTmRobot.HasWafer(0))
                {
                    reason = $"TmRobot have wafer!";
                    EV.PostWarningLog(LogSource, reason);
                    return true;

                }
                if (SchAligner.HasWafer(0))
                {
                    reason = $"Aligner have wafer!";
                    EV.PostWarningLog(LogSource, reason);
                    return true;

                }
                if (SchWaferRobot.HasWafer(0))
                {
                    reason = $"WaferRobot have wafer!";
                    EV.PostWarningLog(LogSource, reason);
                    return true;
                }
                if (SchLoadLock.HasWafer(0))
                {
                    reason = $"Load have wafer!";
                    EV.PostWarningLog(LogSource, reason);
                    return true;

                }
                if (SchLoadLock.HasTray(0))
                {
                    reason = $"Load have Tray!";
                    EV.PostWarningLog(LogSource, reason);
                    return true;

                }
                if (SchUnLoad.HasWafer(0))
                {
                    reason = $"UnLoad have wafer!";
                    EV.PostWarningLog(LogSource, reason);
                    return true;

                }
                if (SchUnLoad.HasTray(0))
                {
                    reason = $"UnLoad have Tray!";
                    EV.PostWarningLog(LogSource, reason);
                    return true;

                }

                return false;
            }
        }
        #endregion

        /// <summary>
        /// Add condition string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private string AddC(string s, string v, string sShould)
        {
            return (s + ":" + v + ", Should be:" + sShould + "\r\n");
        }

    }
}