using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.Core.Extensions;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Errors;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobotB;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobot
{
    public class HwinRobotB : RobotBaseDevice, IConnection
    {
        public string Address => Connection.Address;
        public bool IsConnected => Connection.IsConnected && !_connection.IsCommunicationError;
        public string PortStatus { get; set; } = "Closed";
        public bool Connect() { return _connection.Connect(); }

        public bool Disconnect() { return _connection.Disconnect(); }


        private Dictionary<string, string> _moduleAssociateStationDic;
        public Dictionary<string, string> ModuleAssociateStationDic
        {
            get => _moduleAssociateStationDic;
            set
            {
                _moduleAssociateStationDic = value;
            }
        }

        private HwinRobotBTCPConnection _connection;
        public HwinRobotBTCPConnection Connection
        {
            get { return _connection; }
        }

        private R_TRIG _trigError = new R_TRIG();
        private bool _isAlarm;

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private Dictionary<string, string> _errorCodeReferenceDic = new Dictionary<string, string>();

        private object _locker = new object();
        private SCConfigItem _scHomeTimeout;
        private SCConfigItem _scMotionTimeout;
        private List<ModuleName> _disabledIntlkModules = new List<ModuleName>();

        private bool _enableLog;
        private string _scRoot;
        public HwinRobotB(string module, string name, string endof = "\r\n") : base(module, name)
        {
            DATA.Subscribe($"{Module}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.Address", () => Address);
            OP.Subscribe($"{Module}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
                return true;
            });
            OP.Subscribe($"{Module}.SetPosition", (string cmd, object[] args) =>
            {
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Picking,
                    ArmTarget = RobotArm.ArmA,
                    BladeTarget = "ArmA." + args[0].ToString(),
                };
                return true;
            });

            OP.Subscribe($"{Module}.MAP", (string cmd, object[] args) =>
            {
                RobotMap(args);
                return true;
            });
            OP.Subscribe($"{Module}.RSR", (string cmd, object[] args) =>
            {
                RobotRsr();
                return true;
            });
            OP.Subscribe($"{Module}.HOME", (string cmd, object[] args) =>
            {
                RobotHome();
                return true;
            }); 
            OP.Subscribe($"{Module}.InputA", (string cmd, object[] args) =>
            {
                InputA();
                return true;
            });
            OP.Subscribe($"{Module}.STAT", (string cmd, object[] args) =>
            {
                RobotCheckStat();
                return true;
            });
            OP.Subscribe($"{Module}.ERR", (string cmd, object[] args) =>
            {
                RobotCheckError();
                return true;
            });
            OP.Subscribe($"{Module}.GETB", (string cmd, object[] args) =>
            {
                RobotPickB(args);
                return true;
            });
            OP.Subscribe($"{Module}.PUTB", (string cmd, object[] args) =>
            {
                RobotPlaceB(args);
                return true;
            });
            OP.Subscribe($"{Module}.RobotConnectHomeAll", (string cmd, object[] args) =>
            {
                RobotConnectHomeAll();
                return true;
            });
            OP.Subscribe($"{Module}.RESP", (string cmd, object[] args) =>
            {
                RobotRESP();
                return true;
            });
            OP.Subscribe($"{Module}.REMS", (string cmd, object[] args) =>
            {
                RobotREMS();
                return true;
            });
            OP.Subscribe($"{Module}.SVON", (string cmd, object[] args) =>
            {
                RobotSVON();
                return true;
            });
            OP.Subscribe($"{Module}.AbortAction", (string cmd, object[] args) =>
            {
                fAbort(args);
                OnActionDone(args);
                return true;
            });

            _moduleAssociateStationDic = new Dictionary<string, string>()
            {
                {$"{ModuleName.LoadLock }", "B" },
                {$"{ModuleName.CassBL}", "A"},
                {$"{ModuleName.LoadLock }.Pick", "C" },
                {$"{ModuleName.LoadLock }.Place", "B" },
            };



            try
            {
                string deviceIP = "";
                deviceIP = SC.GetStringValue($"{Module}.Address");
                _enableLog = SC.GetValue<bool>($"{Module}.EnableLogMessage");
                _scHomeTimeout = SC.GetConfigItem($"{Module}.HomeTimeout");
                _scMotionTimeout = SC.GetConfigItem($"{Module}.MotionTimeout");

                WaferManager.Instance.SubscribeLocation(Name, 1, false, true);
                _connection = new HwinRobotBTCPConnection(deviceIP, endof);
                _connection.EnableLog(_enableLog);

                if (_connection.Connect())
                {
                    _trigConnection.CLK = true;
                    PortStatus = "Open";
                    EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                }
                else
                {
                    PortStatus = "Close";
                    EV.PostInfoLog(Module, $"{Module}.{Name} connect failed");
                }

                _thread = new PeriodicJob(200, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            }
            catch (Exception ex)
            {
                EV.PostAlarmLog(Module, ex.ToString());
            }
        }


        public override bool IsReady()
        {
            return !_connection.IsBusy && _lstHandler.Count == 0 && !_connection.IsCommunicationError && !IsBusy;//&& !_isAlarm;
        }

        protected override bool Init()
        {
            return true;
        }
        public R_TRIG _trigConnection = new R_TRIG();
        private bool OnTimer()
        {
            try
            {
                _connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            _trigConnection.CLK = true;
                            EV.PostInfoLog(Module, $"Retry connect succee with {_connection.Address}, {Module}.{Name}");

                        }
                    }
                    return true;
                }

                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;

                            _lstHandler.RemoveFirst();
                        }
                    }

                    if (handler != null)
                    {
                        _connection.Execute(handler);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public override void Monitor()
        {
            try
            {
                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        protected override void SubscribeWaferLocation()
        {
            //do nothing
        }

        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            _trigWarningMessage.RST = true;
            _trigRetryConnect.RST = true;

            base.Reset();
        }

        #region Command Functions
        internal void PutWaferData()
        {
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            IsBusy = false;
            if (CurrentParamter.Length >= 3)
            {
                ModuleName sourceModule;
                int sourceSlot = 0;
                if (!int.TryParse(CurrentParamter[2].ToString(), out sourceSlot) || !Enum.TryParse(CurrentParamter[1].ToString(), out sourceModule))
                {
                    return;
                }
                if (WaferManager.Instance.CheckHasTray(RobotModuleName, 0) && WaferManager.Instance.CheckNoTray(sourceModule, sourceSlot))
                {
                    WaferManager.Instance.TrayMoved(RobotModuleName, 0, sourceModule, sourceSlot);
                }
            }
        }

        internal void GetWaferData()
        {
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            IsBusy = false;
            if (CurrentParamter.Length >= 3)
            {
                ModuleName sourceModule;
                int sourceSlot = 0;
                if (!int.TryParse(CurrentParamter[2].ToString(), out sourceSlot) || !Enum.TryParse(CurrentParamter[1].ToString(), out sourceModule))
                {
                    return;
                }
                if (WaferManager.Instance.CheckNoTray(RobotModuleName, 0) && WaferManager.Instance.CheckHasTray(sourceModule, sourceSlot))
                {
                    WaferManager.Instance.TrayMoved(sourceModule, sourceSlot, RobotModuleName, 0);
                }
            }
        }

        public void RobotPickB(object[] args)
        {
            if (args.Length >= 2)
            {
                _lstHandler.Clear();
                lock (_locker)
                {
                    _lstHandler.AddLast(new HwinRobotBGETBHandler(this, args[0].ToString(), Convert.ToInt32(args[1])));
                }
            }
        }

        public void RobotPlaceB(object[] args)
        {
            if (args.Length >= 2)
            {
                _lstHandler.Clear();
                lock (_locker)
                {
                    _lstHandler.AddLast(new HwinRobotBPUTBHandler(this, args[0].ToString(), Convert.ToInt32(args[1])));
                }
            }
        }

        /// <summary>
        /// 解析STAT指令返回值。
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        internal bool ParseStatusData(int status)
        {
            var stat = status.ToBitTypeClass<HiwinRobotBStatus>(16);
            List<string> sInfo = stat.AggregateErrorMessages();
            if (sInfo != null)
            {
                foreach (string s in sInfo)
                {
                    EV.PostWarningLog(Module, s);
                }
            }
            return true;
        }

        internal virtual bool ParseErrData(string response)
        {
            try
            {
                var errs = new HiwinRobotAggregatedErrors(response).AggregateErrorMessages;

                if (errs != null)
                {
                    foreach (var err in errs)
                    {
                        EV.PostWarningLog(Module, err);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return true;
            }
        }


        //public void RobotPickA(object[] args)
        //{
        //    if (args.Length >= 2)
        //    {
        //        IsBusy = true;
        //        _lstHandler.Clear();
        //        lock (_locker)
        //        {
        //            _lstHandler.AddLast(new HwinRobotBGETAHandler(this, args[0].ToString(), Convert.ToInt32(args[1])));
        //        }
        //    }
        //}

        //public void RobotPlaceA(object[] args)
        //{
        //    if (args.Length >= 2)
        //    {
        //        IsBusy = true;
        //        _lstHandler.Clear();
        //        lock (_locker)
        //        {
        //            _lstHandler.AddLast(new HwinRobotBPUTAHandler(this, args[0].ToString(), Convert.ToInt32(args[1])));
        //        }
        //    }
        //}

        //public void RobotPickSP(object[] args)
        //{
        //    if (args.Length >= 3)
        //    {
        //        IsBusy = true;
        //        _lstHandler.Clear();
        //        lock (_locker)
        //        {
        //            _lstHandler.AddLast(new HwinRobotBGETSPHandler(this, args[0].ToString(), Convert.ToInt32(args[1]), Convert.ToInt32(args[2])));
        //        }
        //    }
        //}

        //public void RobotPlaceSP(object[] args)
        //{
        //    if (args.Length >= 3)
        //    {
        //        IsBusy = true;
        //        _lstHandler.Clear();
        //        lock (_locker)
        //        {
        //            _lstHandler.AddLast(new HwinRobotBPUTSPHandler(this, args[0].ToString(), Convert.ToInt32(args[1]), Convert.ToInt32(args[2])));
        //        }
        //    }
        //}

        internal void SetWaferData(string data)
        {
            //0:未扫片 1:有 2:没有  3:斜放 4:两片 5:薄片
            string errorMessage = "";
            int waferCount = 1;
            if (CurrentInteractModule == ModuleName.CassBL)
            {
                waferCount = 8;
            }

            if (data.Length == 25)//Wafer Cassette
            {
                for (int i = 0; i < waferCount; i++)
                {
                    string waferStatue = data.Substring(i, 1);
                    if (waferStatue == "1")
                    {
                        if (!WaferManager.Instance.CheckHasTray(CurrentInteractModule, i))
                        {
                            WaferManager.Instance.CreateTray(CurrentInteractModule, i);
                        }
                    }
                    else if (waferStatue == "0" || waferStatue == "X")
                    {
                        if (WaferManager.Instance.CheckHasTray(CurrentInteractModule, i))
                        {
                            WaferManager.Instance.DeleteWafer(CurrentInteractModule, i);
                        }
                    }
                    else
                    {
                        if (!WaferManager.Instance.CheckHasTray(CurrentInteractModule, i))
                        {
                            WaferManager.Instance.CreateTray(CurrentInteractModule, i,Aitex.Core.Common.WaferTrayStatus.Crossed);
                        }
                        errorMessage += $"slot {i + 1} has crossed wafer!" + "\r\n";
                    }
                }
            }
            else if (data.Length == 1)
            {
                if (data == "0")
                {
                    if (!WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(Module), 0))
                    {
                        WaferManager.Instance.CreateTray(ModuleHelper.Converter(Module), 0);
                    }
                }
                else if (data == "1")
                {
                    if (WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(Module), 0))
                    {
                        WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Module), 0);
                    }
                }
            }
            if (!String.IsNullOrEmpty(errorMessage))
            {
                EV.PostWarningLog(Module, $"Module {CurrentInteractModule} {errorMessage}");
            }
        }

        public void RobotHome()
        {
            _connection.ForceClear();
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBHomeHandler(this));
            }
        }

        public void InputA()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBINPUTHandler(this));
            }
        }

        public void RobotRsr()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBRSRHandler(this));
            }
        }

        public void RobotMap(object[] args)
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBMapHandler(this, args[0].ToString()));
            }
        }

        public void RobotConnectHomeAll()
        {
            IsBusy = false;
            Connection.ForceClear();
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBRespHandler(this));
                _lstHandler.AddLast(new HwinRobotBRemsHandler(this));
                _lstHandler.AddLast(new HwinRobotBSVONHandler(this));
                _lstHandler.AddLast(new HwinRobotBHomeHandler(this, _scMotionTimeout.IntValue));
                _lstHandler.AddLast(new HwinRobotBINPUTHandler(this));
            }
        }


        public void RobotRESP()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBRespHandler(this));
            }
        }

        public void RobotREMS()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBRemsHandler(this));
            }
        }

        public void RobotSVON()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBSVONHandler(this));
            }
        }

        public void RobotCheckStat()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBSTATHandler(this));
            }
        }


        public virtual void RobotCheckError()
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new HwinRobotBERRHandler(this));
            }
        }


        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();

        public void NoteError(string reason)
        {
            if (reason != null)
            {
                fAbort(null);

                _trigWarningMessage.CLK = true;
                var content = reason;
                if (_errorCodeReferenceDic.ContainsKey(reason))
                {
                    content = _errorCodeReferenceDic[reason];
                }
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason} {content}");
                }
                _isAlarm = true;
                ErrorCode = reason;
            }
            else
            {
                _isAlarm = false;
            }
        }

        internal void NoteActionCompleted()
        {
            CheckToPostMessage((int)RobotMsg.ActionDone);
        }

        public void ContineHome()
        {
            fAbort(null);
            _lstHandler.AddLast(new HwinRobotBHomeHandler(this, _scHomeTimeout.IntValue));

        }

        protected override bool fStartHome(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotBHomeHandler(this, _scHomeTimeout.IntValue));
            }
            return true;
        }

        protected override bool fClear(object[] param)
        {
            return true;
        }

        public override bool ReadParameter(object[] param)
        {
            IsBusy = true;
            return CheckToPostMessage((int)RobotMsg.ReadData, param);
        }

        internal void NotePickCompleted()
        {
            PostMessageWithoutCheck((int)RobotMsg.PickComplete, CurrentParamter);
        }

        internal void NotePlaceCompleted()
        {
            PostMessageWithoutCheck((int)RobotMsg.PlaceComplete, CurrentParamter);
        }

        //public override bool GoTo(object[] param)
        //{
        //    if (param.Length < 1) return false;
        //    string command = param[0].ToString();
        //    var length = param.Length;
        //    var args = param.Skip(1).Take(length - 1).ToArray();
        //    if (!IsReady())
        //        return false;
        //    switch (command)
        //    {
        //        case "GoTo":
        //            if (PostMessageWithoutCheck((int)RobotMsg.GoToPosition, args))
        //            {
        //                IsBusy = true;
        //                string log = "";
        //                foreach (var obj in args)
        //                {
        //                    log += obj.ToString() + ",";
        //                }
        //                LOG.Write($"{RobotModuleName} start go to {log}");
        //                return true;
        //            }
        //            break;
        //        case "ExtendForPick":
        //            if (PostMessageWithoutCheck((int)RobotMsg.ExtendForPick, args))
        //            {
        //                IsBusy = true;
        //                string log = "";
        //                foreach (var obj in args)
        //                {
        //                    log += obj.ToString() + ",";
        //                }
        //                LOG.Write($"{RobotModuleName} start extend for pick {log}");
        //                return true;
        //            }
        //            break;
        //        case "ExtendForPlace":
        //            if (PostMessageWithoutCheck((int)RobotMsg.ExtendForPlace, args))
        //            {
        //                IsBusy = true;
        //                string log = "";
        //                foreach (var obj in args)
        //                {
        //                    log += obj.ToString() + ",";
        //                }
        //                LOG.Write($"{RobotModuleName} start extend for place {log}");
        //                return true;
        //            }
        //            break;
        //        case "RetractFromPick":
        //            if (PostMessageWithoutCheck((int)RobotMsg.RetractFromPick, args))
        //            {
        //                IsBusy = true;
        //                string log = "";
        //                foreach (var obj in args)
        //                {
        //                    log += obj.ToString() + ",";
        //                }
        //                LOG.Write($"{RobotModuleName} start retract from pick {log}");
        //                return true;
        //            }
        //            break;
        //        case "RetractFromPlace":
        //            if (PostMessageWithoutCheck((int)RobotMsg.RetractFromPlace, args))
        //            {
        //                IsBusy = true;
        //                string log = "";
        //                foreach (var obj in args)
        //                {
        //                    log += obj.ToString() + ",";
        //                }
        //                LOG.Write($"{RobotModuleName} start retract from place {log}");
        //                return true;
        //            }
        //            break;
        //        default:
        //            break;
        //    }
        //    return false;
        //}

        protected override bool fStartReadData(object[] param)
        {

            return true;
        }

        protected override bool fStartSetParameters(object[] param)
        {
            return true;
        }

        protected override bool fStartTransferWafer(object[] param)
        {
            return true;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            return true;
        }

        protected override bool fStartGrip(object[] param)
        {
            return true;
        }

        protected override bool fStartGoTo(object[] param)
        {
            return true;
        }

        protected override bool fStartMapWafer(object[] param)
        {
            try
            {
                if (param.Length >= 1)
                {

                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HwinRobotBMapHandler(this, param[0].ToString(), _scMotionTimeout.IntValue));
                        _lstHandler.AddLast(new HwinRobotBRSRHandler(this, _scMotionTimeout.IntValue));
                        _lstHandler.AddLast(new HwinRobotBHomeHandler(this, _scMotionTimeout.IntValue));
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fStartSwapWafer(object[] param)
        {
            return false;
        }

        //(module, slot)
        protected override bool fStartPlaceWafer(object[] param)
        {
            try
            {
                //arm, station, slot
                if (param.Length >= 3)
                {

                    lock (_locker)
                    {
                        if (param[1].ToString() == ModuleName.LoadLock.ToString())
                        {
                            /* 1.打开夹爪
                             * 2.取盘位置低位
                             * 3.抬升托盘
                             */
                            int _rDistance = SC.GetValue<int>("TrayRobot.PlaceRMoveDistance");

                            _lstHandler.AddLast(new HwinRobotBOPTPHandler(this, _scMotionTimeout.IntValue));
                            _lstHandler.AddLast(new HwinRobotMTCSPHandler(this, $"{param[1].ToString()}", "Place", _scMotionTimeout.IntValue));
                            _lstHandler.AddLast(new HwinRobotMOVRPHandler(this, _rDistance, _scMotionTimeout.IntValue));
                            _lstHandler.AddLast(new HwinRobotRETHHandler(this, _scMotionTimeout.IntValue));
                            _lstHandler.AddLast(new HwinRobotBOPTClosePHandler(this, _scMotionTimeout.IntValue));
                            

                        }
                        else
                        {
                            _lstHandler.AddLast(new HwinRobotBPUTBHandler(this, param[1].ToString(), Convert.ToInt32(param[2]), _scMotionTimeout.IntValue));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        //(module, slot)
        protected override bool fStartPickWafer(object[] param)
        {
            try
            {
                //Load 高位B,Load 低位C

                //arm, station, slot
                if (param.Length >= 3)
                {
                    lock (_locker)
                    {
                        if (param[1].ToString() == ModuleName.LoadLock.ToString())
                        {
                            /* 1.打开夹爪
                             * 2.取盘位置低位
                             * 3.抬升托盘
                             * 4.收回
                             */
                            int _rDistance = SC.GetValue<int>("TrayRobot.PickRMoveDistance");

                            _lstHandler.AddLast(new HwinRobotBOPTPHandler(this,  _scMotionTimeout.IntValue));
                            _lstHandler.AddLast(new HwinRobotMTCSPHandler(this, $"{param[1].ToString()}", "Pick", _scMotionTimeout.IntValue));
                            _lstHandler.AddLast(new HwinRobotMOVRGHandler(this, _rDistance, _scMotionTimeout.IntValue)); 
                            _lstHandler.AddLast(new HwinRobotRETHHandler(this, _scMotionTimeout.IntValue));
                            _lstHandler.AddLast(new HwinRobotBOPTClosePHandler(this, _scMotionTimeout.IntValue));

                        }
                        else
                        {
                            _lstHandler.AddLast(new HwinRobotBGETBHandler(this, param[1].ToString(), Convert.ToInt32(param[2]), _scMotionTimeout.IntValue));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fPlaceComplete(object[] param)
        {
            return base.fPlaceComplete(param);
        }

        protected override bool fPickComplete(object[] param)
        {
            return base.fPickComplete(param);
        }

        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fReset(object[] param)
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _trigRetryConnect.RST = true;
            if (_isAlarm)
            {
                lock (_locker)
                {
                    //_lstHandler.AddLast(new HwinRobotBResetHandler(this));
                }

                Thread.Sleep(100);
                CheckToPostMessage((int)RobotMsg.ActionDone);
            }

            IsBusy = false;

            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartInit(object[] param)
        {
            RobotConnectHomeAll();
            return true;
        }

        protected override bool fError(object[] param)
        {
            return true;
        }

        //[module][slot]
        protected override bool fStartExtendForPick(object[] param)
        {
            try
            {
                //if (param.Length >= 2)
                //{

                //    lock (_locker)
                //    {
                //        _lstHandler.AddLast(new HwinRobotBGETSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 1, _scMotionTimeout.IntValue));
                //        _lstHandler.AddLast(new HwinRobotBGETSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 2, _scMotionTimeout.IntValue));
                //    }
                //}
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fStartExtendForPlace(object[] param)
        {
            try
            {
                //if (param.Length >= 2)
                //{

                //    lock (_locker)
                //    {
                //        _lstHandler.AddLast(new HwinRobotBPUTSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 1, _scMotionTimeout.IntValue));
                //        _lstHandler.AddLast(new HwinRobotBPUTSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 2, _scMotionTimeout.IntValue));
                //    }
                //}
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 开始从Pick处缩回
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        protected override bool fStartRetractFromPick(object[] param)
        {
            try
            {
                //if (param.Length >= 2)
                //{

                //    lock (_locker)
                //    {
                //        _lstHandler.AddLast(new HwinRobotBGETSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 3, _scMotionTimeout.IntValue));
                //        _lstHandler.AddLast(new HwinRobotBGETSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 4, _scMotionTimeout.IntValue));
                //    }
                //}
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 开始从Place处缩回
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        protected override bool fStartRetractFromPlace(object[] param)
        {
            try
            {
                //if (param.Length >= 2)
                //{
                //    lock (_locker)
                //    {
                //        _lstHandler.AddLast(new HwinRobotBPUTSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 3, _scMotionTimeout.IntValue));
                //        _lstHandler.AddLast(new HwinRobotBPUTSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 4, _scMotionTimeout.IntValue));
                //    }
                //}
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fStartExecuteCommand(object[] param)
        {
            try
            {
                InputA();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }


        public override RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm)
        {
            if (arm == RobotArmEnum.Blade1)
            {
                return IsWaferPresenceOnBlade1 ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
            }

            return RobotArmWaferStateEnum.Unknown;
        }

        protected override bool fStop(object[] param)
        {
            lock (_locker)
            {
                IsBusy = false;
                _connection.ForceClear();
                _lstHandler.Clear();
            }
            return true;
        }

        protected override bool fAbort(object[] param)
        {
            lock (_locker)
            {
                IsBusy = false;
                _connection.ForceClear();
                _lstHandler.Clear();
            }
            return true;
        }
        #endregion
    }
}
