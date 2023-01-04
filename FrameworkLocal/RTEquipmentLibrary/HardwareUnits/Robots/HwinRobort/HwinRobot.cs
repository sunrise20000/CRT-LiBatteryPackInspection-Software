using System;
using System.Collections.Generic;
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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Errors;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobot
{
    public class HwinRobot : RobotBaseDevice, IConnection
    {
        #region Variables

        private readonly object _locker = new object();

        private PeriodicJob _thread;

        private readonly Dictionary<string, string> _errorCodeReferenceDic = new Dictionary<string, string>();
        private readonly LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private readonly SCConfigItem _scHomeTimeout;
        private readonly SCConfigItem _scMotionTimeout;
        private readonly R_TRIG _trigCommunicationError = new R_TRIG();
        private readonly R_TRIG _trigRetryConnect = new R_TRIG();

        private List<ModuleName> _disabledIntlkModules = new List<ModuleName>();

        private readonly bool _enableLog;
        private string _scRoot;

        private readonly R_TRIG _trigError = new R_TRIG();
        private bool _isAlarm;

        #endregion

        #region Constructors

        public HwinRobot(string module, string name, string endof = "\r\n") : base(module, name)
        {
            DATA.Subscribe($"{Module}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.Address", () => Address);
            DATA.Subscribe($"{Module}.IsBobotReady", () => IsReady());
            OP.Subscribe($"{Module}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
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

            OP.Subscribe($"{Module}.PUTSP", (string cmd, object[] args) =>
            {
                RobotPlaceSP(args);
                return true;
            });
            OP.Subscribe($"{Module}.GETSP", (string cmd, object[] args) =>
            {
                RobotPickSP(args);
                return true;
            });
            OP.Subscribe($"{Module}.InputA", (string cmd, object[] args) =>
            {
                InputA();
                return true;
            });

            OP.Subscribe($"{Module}.GETA", (string cmd, object[] args) =>
            {
                RobotPickA(args);
                return true;
            });
            OP.Subscribe($"{Module}.PUTA", (string cmd, object[] args) =>
            {
                RobotPlaceA(args);
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

            ModuleAssociateStationDic = new Dictionary<string, string>()
            {
                {$"{ModuleName.LoadLock }", "D" },
                {$"{ModuleName.UnLoad}", "E" },
                {$"{ModuleName.CassAL}", "A"},
                {$"{ModuleName.CassAR}", "B"},
                {$"{ModuleName.Aligner}", "C"},
            };


            try
            {
                string deviceIP = "";
                deviceIP = SC.GetStringValue($"{Module}.Address");
                _enableLog = SC.GetValue<bool>($"{Module}.EnableLogMessage");
                _scHomeTimeout = SC.GetConfigItem($"{Module}.HomeTimeout");
                _scMotionTimeout = SC.GetConfigItem($"{Module}.MotionTimeout");


                WaferManager.Instance.SubscribeLocation(Name, 1, true, false);
                Connection = new HwinRobotTCPConnection(deviceIP, endof);
                Connection.EnableLog(_enableLog);

                Status = new HiwinRobotStatus();

                if (Connection.Connect())
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

                _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            }
            catch (Exception ex)
            {
                EV.PostAlarmLog(Module, ex.ToString());
            }
        }

        #endregion

        #region Properties

        public string Address => Connection.Address;

        public bool IsConnected => Connection.IsConnected && !Connection.IsCommunicationError;
        
        public string PortStatus { get; set; } = "Closed";
        
        public bool Connect() { return Connection.Connect(); }

        public bool Disconnect() { return Connection.Disconnect(); }

        public HwinRobotTCPConnection Connection { get; }

        public Dictionary<string, string> ModuleAssociateStationDic { get; set; }

        public R_TRIG _trigConnection = new R_TRIG();

        public HiwinRobotStatus Status { get; private set; }

        public HiwinRobotAggregatedErrors Errors { get; private set; }

        #endregion

        #region Methods

        public override bool IsReady()
        {
            return !Connection.IsBusy && _lstHandler.Count == 0 && !Connection.IsCommunicationError && !IsBusy;// && !_isAlarm;//&& (RobotState == RobotStateEnum.Idle)
        }

        protected override bool Init()
        {
            return true;
        }
        
        private bool OnTimer()
        {
            try
            {
                Connection.MonitorTimeout();

                if (!Connection.IsConnected || Connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !Connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        if (!Connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {Connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            _trigConnection.CLK = true;
                            EV.PostInfoLog(Module, $"Retry connect succee with {Connection.Address}, {Module}.{Name}");

                        }
                    }
                    return true;
                }

                HandlerBase handler = null;
                if (!Connection.IsBusy)
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
                        Connection.Execute(handler);
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
                _trigCommunicationError.CLK = Connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {Connection.LastCommunicationError}");
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

            Connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            _trigWarningMessage.RST = true;
            _trigRetryConnect.RST = true;

            base.Reset();
        }

        #endregion

        #region Command Functions
        internal bool PraseGetWaferData()
        {
            if (CurrentParamter != null && CurrentParamter.Length >= 3)
            {
                ModuleName sourceModule;
                int sourceSlot;
                if (!int.TryParse(CurrentParamter[2].ToString(), out sourceSlot) || !Enum.TryParse(CurrentParamter[1].ToString(), out sourceModule))
                {
                    return false;
                }

                if (!WaferManager.Instance.CheckHasTray(RobotModuleName, 0) )
                {
                    WaferManager.Instance.WaferMoved(sourceModule, sourceSlot, RobotModuleName, 0);
                    return true;
                }

            }
            return false;

        }

        /// <summary>
        /// 解析STAT指令返回值。
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        internal bool ParseStatusData(int status)
        {
            var stat = status.ToBitTypeClass<HiwinRobotStatus>(16);
            Status = stat;
            List<string> sInfo = Status.AggregateErrorMessages();
            if (sInfo != null)
            {
                foreach (string s in sInfo)
                {
                    EV.PostWarningLog(Module, s);
                }
            }
            return true;
        }

        /// <summary>
        /// 从控制器获取错误代码。
        /// <para>注意：如果控制器返回的字符串格式错误，则解析失败，Errors设置为null。</para>
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        internal virtual bool ParseErrData(string response)
        {
            try
            {
                Errors = null;
                Errors = new HiwinRobotAggregatedErrors(response);
                if(Errors.AggregateErrorMessages.Count > 0)
                {
                    foreach (string errMsg in Errors.AggregateErrorMessages)
                    {
                        EV.PostAlarmLog(Module, errMsg);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return true;
            }
        }

        internal bool PrasePutWaferData()
        {
            if (CurrentParamter != null && CurrentParamter.Length >= 3)
            {
                ModuleName sourceModule;
                int sourceSlot;
                if (!int.TryParse(CurrentParamter[2].ToString(), out sourceSlot) || !Enum.TryParse(CurrentParamter[1].ToString(), out sourceModule))
                {
                    return false;
                }

                if (!WaferManager.Instance.CheckHasTray(sourceModule, sourceSlot))
                {
                    WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourceModule, sourceSlot);
                    return true;
                }
            }
            return false;
        }

        public void RobotPickB(object[] args)
        {
            if (args.Length >= 2)
            {
                _lstHandler.Clear();
                lock (_locker)
                {
                    _lstHandler.AddLast(new HwinRobotGETBHandler(this, args[0].ToString(), Convert.ToInt32(args[1])));
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
                    _lstHandler.AddLast(new HwinRobotPUTBHandler(this, args[0].ToString(), Convert.ToInt32(args[1])));
                }
            }
        }

        public void RobotPickA(object[] args)
        {
            if (args.Length >= 2)
            {
                _lstHandler.Clear();
                lock (_locker)
                {
                    _lstHandler.AddLast(new HwinRobotGETAHandler(this, args[0].ToString(), Convert.ToInt32(args[1])));
                }
            }
        }

        public void RobotPlaceA(object[] args)
        {
            if (args.Length >= 2)
            {
                _lstHandler.Clear();
                lock (_locker)
                {
                    _lstHandler.AddLast(new HwinRobotPUTAHandler(this, args[0].ToString(), Convert.ToInt32(args[1])));
                }
            }
        }

        public void RobotPickSP(object[] args)
        {
            if (args.Length >= 3)
            {
                _lstHandler.Clear();
                lock (_locker)
                {
                    _lstHandler.AddLast(new HwinRobotGETSPHandler(this, args[0].ToString(), Convert.ToInt32(args[1]), Convert.ToInt32(args[2])));
                }
            }
        }

        public void RobotPlaceSP(object[] args)
        {
            if (args.Length >= 3)
            {
                _lstHandler.Clear();
                lock (_locker)
                {
                    _lstHandler.AddLast(new HwinRobotPUTSPHandler(this, args[0].ToString(), Convert.ToInt32(args[1]), Convert.ToInt32(args[2])));
                }
            }
        }
        
        internal void SetWaferData(string data)
        {
            //0:未扫片 1:有 2:没有  3:斜放 4:两片 5:薄片
            string errorMessage = "";
            int waferCount = 1;
            if (CurrentInteractModule == ModuleName.CassAL || CurrentInteractModule == ModuleName.CassAR)
            {
                waferCount = 25;
            }
            if (data.Length==25)//Wafer Cassette
            {
                for (int i = 0; i < waferCount; i++)
                {
                    string waferStatue = data.Substring(i, 1);
                    if (waferStatue == "1")
                    {
                        if (!WaferManager.Instance.CheckHasWafer(CurrentInteractModule, i))
                        {
                            WaferManager.Instance.CreateWafer(CurrentInteractModule, i, Aitex.Core.Common.WaferStatus.Normal);
                        }
                    }
                    else if (waferStatue == "0" || waferStatue == "X")
                    {
                        if (WaferManager.Instance.CheckHasWafer(CurrentInteractModule, i))
                        {
                            WaferManager.Instance.DeleteWafer(CurrentInteractModule, i);
                        }
                    }
                    else
                    {
                        string msg = "";
                        if (!WaferManager.Instance.CheckHasWafer(CurrentInteractModule, i))
                        {
                            WaferManager.Instance.CreateWafer(CurrentInteractModule, i, GetWaferStarusByNum(waferStatue,out msg));
                        }
                        else
                        {
                            WaferManager.Instance.GetWafer(CurrentInteractModule, i).Status = GetWaferStarusByNum(waferStatue, out msg);// Aitex.Core.Common.WaferStatus.Crossed;
                        }
                        errorMessage += $"slot {i + 1} has {msg}!" + "\r\n";
                    }
                }
            }
            else if (data.Length == 1)
            {
                if (data == "0")
                {
                    if (!WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(Module), 0))
                    {
                        WaferManager.Instance.CreateWafer(ModuleHelper.Converter(Module), 0, Aitex.Core.Common.WaferStatus.Normal);
                    }
                }
                else if (data == "1")
                {
                    if (WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(Module), 0))
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

        private Aitex.Core.Common.WaferStatus GetWaferStarusByNum(string num,out string msg)
        {
            switch (num)
            {
                case "2":
                    msg = "crossed wafer";
                    return Aitex.Core.Common.WaferStatus.Crossed;
                case "3":
                    msg = "double wafer";
                    return Aitex.Core.Common.WaferStatus.Double;                    
                case "4":
                    msg = "thin wafer";
                    return Aitex.Core.Common.WaferStatus.Double;
                case "5":
                    msg = "flimsy wafer";
                    return Aitex.Core.Common.WaferStatus.Double;
                default:
                    msg = "wafer";
                    return Aitex.Core.Common.WaferStatus.Crossed;
            }                
        }

        public void RobotCheckStat()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotSTATHandler(this));
            }
        }

        public virtual void RobotCheckError()
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new HwinRobotERRHandler(this));
            }
        }

        public void RobotHome()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotHomeHandler(this));
            }
        }

        public void RobotRsr()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotRSRHandler(this));
            }
        }

        public void RobotMap(object[] args)
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotMapHandler(this, args[0].ToString()));
            }
        }

        public void InputA()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotOutpOpenHandler(this));
                _lstHandler.AddLast(new HwinRobotINPUTHandler(this));
                _lstHandler.AddLast(new HwinRobotOutpCloseHandler(this));
            }
        }


        public void RobotConnectHomeAll()
        {
            IsBusy = false;
            Connection.ForceClear();
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotRespHandler(this));
                _lstHandler.AddLast(new HwinRobotRemsHandler(this));
                _lstHandler.AddLast(new HwinRobotSVONHandler(this));
                _lstHandler.AddLast(new HwinRobotHomeHandler(this, _scMotionTimeout.IntValue));
                _lstHandler.AddLast(new HwinRobotOutpOpenHandler(this));
                _lstHandler.AddLast(new HwinRobotINPUTHandler(this));
                _lstHandler.AddLast(new HwinRobotOutpCloseHandler(this));
            }
        }

        public void RobotRESP()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotRespHandler(this));
            }
        }

        public void RobotREMS()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotRemsHandler(this));
            }
        }

        public void RobotSVON()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotSVONHandler(this));
            }
        }
        #endregion

        #region Note Functions
        private readonly R_TRIG _trigWarningMessage = new R_TRIG();

        public void NoteError(string reason)
        {
            if(reason != null)
            {
                fAbort(null);
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
            }
//             if (reason != null)
//             {
//                 fAbort(null);
// 
//                 _trigWarningMessage.CLK = true;
//                 var content = reason;
//                 if (_errorCodeReferenceDic.ContainsKey(reason))
//                 {
//                     content = _errorCodeReferenceDic[reason];
//                 }
//                 if (_trigWarningMessage.Q)
//                 {
//                     EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason} {content}");
//                 }
//                 _isAlarm = true;
//                 ErrorCode = reason;
//             }
//             else
//             {
//                 _isAlarm = false;
//             }
        }

        internal void NoteActionCompleted()
        {
            //Blade1Target = ModuleName.System;
            //Blade2Target = ModuleName.System;
            IsBusy = false;
            CheckToPostMessage((int)RobotMsg.ActionDone);
        }

        internal void NotePickCompleted()
        {
            PostMessageWithoutCheck((int)RobotMsg.PickComplete, CurrentParamter);
        }

        internal void NotePlaceCompleted()
        {
            PostMessageWithoutCheck((int)RobotMsg.PlaceComplete, CurrentParamter);
        }

        protected override bool fStartHome(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HwinRobotHomeHandler(this, _scHomeTimeout.IntValue));
            }
            return true;
        }

        protected override bool fClear(object[] param)
        {
            return true;
        }

        public override bool ReadParameter(object[] param)
        {
            return CheckToPostMessage((int)RobotMsg.ReadData, param);
        }

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
                        _lstHandler.AddLast(new HwinRobotMapHandler(this, param[0].ToString(), _scMotionTimeout.IntValue)); 
                        _lstHandler.AddLast(new HwinRobotRSRHandler(this, _scMotionTimeout.IntValue)); 
                        _lstHandler.AddLast(new HwinRobotHomeHandler(this, _scMotionTimeout.IntValue));
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
                    ModuleName sourceModule;
                    int sourceSlot;
                    if (!int.TryParse(CurrentParamter[2].ToString(), out sourceSlot) || !Enum.TryParse(CurrentParamter[1].ToString(), out sourceModule))
                    {
                        return false;
                    }

                    if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                    {
                        WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourceModule, sourceSlot);
                    }
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HwinRobotPUTSPHandler(this, param[1].ToString(), Convert.ToInt32(param[2]), 4, _scMotionTimeout.IntValue)); //收回
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
                //arm, station, slot
                if (param.Length >= 3)
                {
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HwinRobotGETSPHandler(this, param[1].ToString(), Convert.ToInt32(param[2]), 3, _scMotionTimeout.IntValue)); //吸住
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
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;

            try
            {

                ////arm, station, slot
                //if (param.Length >= 3)
                //{
                //    ModuleName sourceModule;
                //    int sourceSlot;
                //    if (!int.TryParse(CurrentParamter[2].ToString(), out sourceSlot) || !Enum.TryParse(CurrentParamter[1].ToString(), out sourceModule))
                //    {                        
                //        return false;
                //    }

                //    if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                //    {
                //        WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourceModule, sourceSlot);
                //    }

                //    //lock (_locker)
                //    //{
                //    //    _lstHandler.AddLast(new HwinRobotPUTSPHandler(this, CurrentParamter[1].ToString(), Convert.ToInt32(CurrentParamter[2]), 4, _scMotionTimeout.IntValue)); //缩回
                //    //}
                //}
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return base.fPlaceComplete(param);
        }

        protected override bool fPickComplete(object[] param)
        {
            //arm, station, slot

            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;

            if (CurrentParamter.Length >= 3)
            {
                ModuleName sourceModule;
                int sourceSlot;
                if (!int.TryParse(CurrentParamter[2].ToString(), out sourceSlot) || !Enum.TryParse(CurrentParamter[1].ToString(), out sourceModule))
                {
                    return false;
                }

                if (WaferManager.Instance.CheckHasWafer(sourceModule, sourceSlot))
                {
                    WaferManager.Instance.WaferMoved(sourceModule, sourceSlot, RobotModuleName, 0);
                }

                lock (_locker)
                {
                    _lstHandler.AddLast(new HwinRobotGETSPHandler(this, CurrentParamter[1].ToString(), Convert.ToInt32(CurrentParamter[2]), 4, _scMotionTimeout.IntValue)); //缩回
                }
            }

            return base.fPickComplete(param);
        }

        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fReset(object[] param)
        {
            _trigError.RST = true;

            Connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _trigRetryConnect.RST = true;
            if (_isAlarm)
            {
                lock (_locker)
                {
                    //_lstHandler.AddLast(new HwinRobotResetHandler(this));
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

        //[module][slot]  伸出手臂
        protected override bool fStartExtendForPick(object[] param)
        {
            try
            {
                if (param.Length >= 3)
                {

                    lock (_locker)
                    {
                        //初始化和伸出
                        _lstHandler.AddLast(new HwinRobotGETSPHandler(this, param[1].ToString(), Convert.ToInt32(param[2]), 1,_scMotionTimeout.IntValue));
                        _lstHandler.AddLast(new HwinRobotGETSPHandler(this, param[1].ToString(), Convert.ToInt32(param[2]), 2, _scMotionTimeout.IntValue));
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

        protected override bool fStartExtendForPlace(object[] param)
        {
            try
            {
                if (param.Length >= 3)
                {
                    lock (_locker)
                    {
                        //初始化和伸出
                        _lstHandler.AddLast(new HwinRobotPUTSPHandler(this, param[1].ToString(), Convert.ToInt32(param[2]), 1, _scMotionTimeout.IntValue));
                        _lstHandler.AddLast(new HwinRobotPUTSPHandler(this, param[1].ToString(), Convert.ToInt32(param[2]), 2, _scMotionTimeout.IntValue));
                        _lstHandler.AddLast(new HwinRobotPUTSPHandler(this, param[1].ToString(), Convert.ToInt32(param[2]), 3, _scMotionTimeout.IntValue));
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

        /// <summary>
        /// 开始从Pick处缩回
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        protected override bool fStartRetractFromPick(object[] param)
        {
            try
            {
                if (param.Length >= 2)
                {

                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HwinRobotGETSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 3, _scMotionTimeout.IntValue));
                        _lstHandler.AddLast(new HwinRobotGETSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 4, _scMotionTimeout.IntValue));
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

        /// <summary>
        /// 开始从Place处缩回
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        protected override bool fStartRetractFromPlace(object[] param)
        {
            try
            {
                if (param.Length >= 2)
                {
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HwinRobotPUTSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 3, _scMotionTimeout.IntValue));
                        _lstHandler.AddLast(new HwinRobotPUTSPHandler(this, param[0].ToString(), Convert.ToInt32(param[1]), 4, _scMotionTimeout.IntValue));
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
                Connection.ForceClear();
                _lstHandler.Clear();
            }
            return true;
        }

        protected override bool fAbort(object[] param)
        {
            lock (_locker)
            {
                IsBusy = false;
                Connection.ForceClear();
                _lstHandler.Clear();
            }
            return true;
        }

        #endregion

    }
}
