using System;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.Util;

using Aitex.Sorter.Common;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.SubstrateTrackings;
using System.Collections.Generic;
using Aitex.Core.RT.SCCore;
using System.IO.Ports;
using System.Threading;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase;
using Aitex.Core.Common;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.HonghuAligners
{
    public class FuqiAligner : AlignerBaseDevice, IConnection
    {
        public enum AlignerType
        {
            Mechnical = 0,
            Vaccum,
        }
        public string Address
        {
            get
            {
                return "";

            }
        }

        public virtual bool IsConnected
        {
            get { return true; }
        }

        public virtual bool Disconnect()
        {
            return true;
        }
        public virtual bool Connect()
        {
            return true;
        }
        public const string delimiter = "\r";

        public int LastErrorCode { get; set; }
        public int Status { get; set; }
 
        public int ElapseTime { get; set; }

        public int Notch { get; set; }

        public bool Initalized { get; set; }


        private AlignerType _tazmotype = AlignerType.Vaccum;
        public AlignerType TazmoType { get => _tazmotype; }







        public bool Communication
        {
            get
            {
                return !_commErr;
            }
        }

        public virtual bool Error
        {
            get; set;

        }

        public bool Busy { get { return _connection.IsBusy || _lstHandler.Count != 0; } }


      

        public bool TaExecuteSuccss
        {
            get; set;
        }



        public void OnWaferPresent(bool iswaferon)
        {
            if(iswaferon)
            {
                if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
                    WaferManager.Instance.CreateWafer(RobotModuleName, 0, WaferStatus.Normal);
            }
            else
            {
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                {
                    var wafer = WaferManager.Instance.GetWafer(RobotModuleName, 0);
                    WaferManager.Instance.UpdateWaferE90State(wafer.WaferID, EnumE90Status.Aborted);
                }
            }    
        }

        //private int _deviceAddress;
        public override bool IsNeedRelease
        {
            get
            {
                return true;
            }
        }

        public FuqiAlignerConnection Connection
        {
            get => _connection;
        }
        private FuqiAlignerConnection _connection;

        //private int _presetNumber;

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;
        private static Object _locker = new Object();

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private bool _enableLog = true;

        private bool _commErr = false;

        private int _defaultChuckPosition;
        //private bool _exceuteErr = false;
        //private string _addr;


        private DeviceTimer _timerQuery = new DeviceTimer();
        private string _scRoot;


        private string AlarmMechanicalAlignmentError = "MechanicalAlignmentError";
        public FuqiAligner(string module, string name,string scRoot,int alignertype=0)
            : base(module,name)
        {
            Name = name;
            Module = module;
            _scRoot = scRoot;

            InitializeFuqi();
            //WaferManager.Instance.SubscribeLocation(name, 1);
        }


        public bool InitializeFuqi()
        {
            string portName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            int bautRate = SC.GetValue<int>($"{_scRoot}.{Name}.BaudRate");
            int dataBits = SC.GetValue<int>($"{_scRoot}.{Name}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.{Name}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.{Name}.StopBits"), out StopBits stopBits);            
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");

            _connection = new FuqiAlignerConnection(portName, bautRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
            int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
            if (sleep <= 0 || sleep > 10)
                sleep = 2;

            int retry = 0;
            do
            {
                _connection.Disconnect();
                Thread.Sleep(sleep * 1000);
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                    break;
                }

                if (count > 0 && retry++ > count)
                {
                    LOG.Write($"Retry connect {Module}.{Name} stop retry.");
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                    break;
                }

                Thread.Sleep(sleep * 1000);
                LOG.Write($"Retry connect {Module}.{Name} for the {retry + 1} time.");

            } while (true);

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);           

            //            string str = string.Empty;
            EV.Subscribe(new EventItem(0, "Event", AlarmMechanicalAlignmentError, "Aligner error", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));

            //            _timerQuery.Start(_queryPeriod);

            return true;
        }

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
                        _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }

                    }
                    return true;
                }

                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0)
                        {
                            //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null)); 
                            //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null)); 
                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;
                            _lstHandler.RemoveFirst();
                            if (handler != null) _connection.Execute(handler);
                            
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }


            try
            {
                _connection.EnableLog(_enableLog);

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



            return true;
        }


        #region Command

        



       
        public virtual bool MoveToReady(out string reason)
        {
            reason = "";
            return true;
        }


        
        public bool RequestPlace(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestPlace));

            }

            reason = "";
            return true;
        }


        public override bool IsReady()
        {
            return AlignerState == AlignerStateEnum.Idle && !IsBusy;
        }

        public override bool IsWaferPresent(int slotindex)
        {
            return WaferManager.Instance.CheckHasWafer(RobotModuleName, 0);
        }

        protected override bool fStartLiftup(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestPlace));
            }           
            return true;
        }
        protected override bool fStartPrepareAccept(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestPlace));
            }
            return true;
        }

        protected override bool fStartLiftdown(object[] param)
        {
            return true;
        }

        protected override bool fStartAlign(object[] param)
        {
            double aligneangle = (double)param[0];           
            lock (_locker)
            {
                
                _lstHandler.AddLast(new FuqiRequestHandler(this, $"B{aligneangle.ToString("f1")}"));
                //_lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetVacuumOffAfterAlign));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestFinishPlace));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestVacuumOff));
            }
            return true;
        }

        protected override bool fStop(object[] param)
        {
            return true;
        }

        protected override bool FsmAbort(object[] param)
        {
            return true;
        }

        protected override bool fClear(object[] param)
        {
            return true;
        }

        protected override bool fStartReadData(object[] param)
        {
            return true;
        }

        protected override bool fStartSetParameters(object[] param)
        {
            return true;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestVacuumOff));
            }            
            return true;
        }

        protected override bool fStartGrip(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestVacuumOn));

            }
            return true;
        }

        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fReset(object[] param)
        {
            _trigError.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            if(!_connection.IsConnected)
            {
                string portName = SC.GetStringValue($"{Module}.{Name}.Address");
                int bautRate = SC.GetValue<int>($"{Module}.{Name}.BaudRate");
                int dataBits = SC.GetValue<int>($"{Module}.{Name}.DataBits");
                Enum.TryParse(SC.GetStringValue($"{Module}.{Name}.Parity"), out Parity parity);
                Enum.TryParse(SC.GetStringValue($"{Module}.{Name}.StopBits"), out StopBits stopBits);
                _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

                _connection = new FuqiAlignerConnection(portName, bautRate, dataBits, parity, stopBits);
                _connection.EnableLog(_enableLog);
                _connection.Connect();
            }

            

            _trigRetryConnect.RST = true;


            lock (_locker)
            {
                _lstHandler.Clear();                
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetUseNewCommand));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.Reset));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetVacuumOffAfterAlign));
                if(SC.ContainsItem($"{_scRoot}.{Name}.CenterAndNotch") && SC.GetValue<bool>($"{_scRoot}.{Name}.CenterAndNotch"))
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetCenterAndNotch));
                else
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetOnlyNotch));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetWIDReaderOff));
            }            
            return true;
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetUseNewCommand));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.Reset));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetWIDReaderOff));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetVacuumOnAfterAlign));
                if (SC.ContainsItem($"{_scRoot}.{Name}.CenterAndNotch") && SC.GetValue<bool>($"{_scRoot}.{Name}.CenterAndNotch"))
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetCenterAndNotch));
                else
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetOnlyNotch));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestPlace));
            }
            return true;
        }
        protected override bool fStartHome(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.Reset));
                
            }
            return true;
        }

        protected override bool fError(object[] param)
        {
            return true;
        }
        
        public void OnActionDone()
        {
            IsBusy = false;
            if (_lstHandler.Count == 0)
                OnActionDone(null);
        }
        #endregion

        public override bool IsNeedPrepareBeforePlaceWafer()
        {
            return true;
        }

        public override double CompensationAngleValue
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{Name}.CompensationAngle"))
                    return SC.GetValue<double>($"Aligner.{Name}.CompensationAngle");
                return 0;
            }
        }

    }
}


