using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.DRYVacuum
{
    public class DRYVacuumPump : PumpBase, IConnection
    {
        public string Address => Connection.Address;
        public bool IsConnected => Connection.IsConnected && !_connection.IsCommunicationError;
        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        public string PortStatus { get; set; } = "Closed";

        private DRYVacuumPumpConnection _connection;
        public DRYVacuumPumpConnection Connection
        {
            get { return _connection; }
        }

        public override AITPumpData DeviceData
        {
            get
            {
                AITPumpData data = new AITPumpData()
                {
                    IsOn = IsOn,
                    IsError = IsError,
                    OverTemp = IsOverTemperature,
                    DeviceModule = Module,
                    DeviceName = Name,
                };
                return data;
            }
        }
        public override AITPumpData DeviceDataMP
        {
            get
            {
                AITPumpData data = new AITPumpData()
                {
                    IsOn = IsMPOn,
                    IsError = IsError||IsAlarm,
                    IsWarning = IsWarning,
                    //OverTemp = IsOverTemperature,
                    DeviceModule = Module,
                    DeviceName = Name,
                };
                return data;
            }
        }
        public override AITPumpData DeviceDataBP
        {
            get
            {
                AITPumpData data = new AITPumpData()
                {
                    IsOn = IsBPOn,
                    IsError = IsError||IsAlarm,
                    IsWarning = IsWarning,
                    //OverTemp = IsOverTemperature,
                    DeviceModule = Module,
                    DeviceName = Name,
                };
                return data;
            }
        }

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        
        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        //public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();
        private bool SoftonDelay { get; set; }

        public  bool IsWarning { get; set; }

        public  bool IsAlarm { get; set; }

        private object _locker = new object();

        private bool _enableLog;
        private string _portName;
        private string _scRoot;

        private string _runMode;
        public string RunMode
        {
            get { return _runMode; }
            set { _runMode = value; }
        }
        private string _MPOn;
        public string MPOn
        {
            get { return _MPOn; }
            set { _MPOn = value; }
        }
        private string _BPOn;
        public string BPOn
        {
            get { return _BPOn; }
            set { _BPOn = value; }
        }
        private bool _isMPOn;
        public bool IsMPOn
        {
            get { return _isMPOn; }
            set { _isMPOn = value; }
        }

        private bool _isBPOn;
        public bool IsBPOn
        {
            get { return _isBPOn; }
            set { _isBPOn = value; }
        }
        public int delaytimeout { get; set; }
        public DRYVacuumPump(string module, string name, string scRoot,string port) : base(module, name)
        {
            _scRoot = scRoot;
            _portName = port;
        }
        
        public override bool Initialize()
        {
            base.Initialize();

            if (_connection != null && _connection.IsConnected)
                return true;

            if (_connection != null && _connection.IsConnected)
                _connection.Disconnect();

            delaytimeout = SC.GetValue<int>($"{Name}.DelayTime");
            //_portName = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.DeviceAddress");
            //_enableLog = SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");
            //_portName = SC.GetStringValue($"{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");
            _connection = new DRYVacuumPumpConnection(_portName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                SoftonDelay = true;
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            
            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            //lock (_locker)
            //{
            //    _lstHandler.AddLast(new EdwardsGetControlHandler(this));
            //    _lstHandler.AddLast(new EdwardsFormatModeHandler(this, true));
            //}
           // _lstMonitorHandler.AddLast(new DRYVacuumPumpQueryStatusHandler(this));
            DATA.Subscribe($"{Module}.{Name}.RunMode", () => _runMode);
            DATA.Subscribe($"{Module}.{Name}.MPOn", () => _MPOn);
            DATA.Subscribe($"{Module}.{Name}.BPOn", () => _BPOn);
            DATA.Subscribe($"{Module}.{Name}.IsWarning", () => IsWarning);
            DATA.Subscribe($"{Module}.{Name}.IsAlarm", () => IsAlarm);
            DATA.Subscribe($"{Module}.{Name}.IsMPOn", () => IsMPOn);
            DATA.Subscribe($"{Module}.{Name}.IsBPOn", () => IsBPOn);
            return true;
        }
        internal void NoteSetParaCompleted()
        {
        }

        private bool OnTimer()
        {
            try
            {
                //_connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(_portName);
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //lock (_locker)
                            //{
                            //    _lstHandler.AddLast(new EdwardsGetControlHandler(this));
                            //    _lstHandler.AddLast(new EdwardsFormatModeHandler(this, true));
                            //}
                        }
                    }
                    return true;
                }
                if (SoftonDelay)
                {   
                    Thread.Sleep(delaytimeout);
                    SoftonDelay = false;
                }
                QueryStatus();
                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0)
                        {
                            foreach (var monitorHandler in _lstMonitorHandler)
                            {
                                _lstHandler.AddLast(monitorHandler);
                            }

                        }

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
        public void QueryStatus()
        {

            _lstHandler.AddLast(new DRYVacuumPumpQueryStatusHandler(this));      
        }

        public override void Monitor()
        {
            try
            {
                //_connection.EnableLog(_enableLog);


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

        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            base.Reset();
        }

        #region Command Functions

        internal void NoteSwitchCompleted()
        {
            ActionCommandCompleted = true;
        }
        public override void SetPumpOnOff(bool isOn)
        {
            //lock (_locker)
            //{
            //    if (isOn)
            //        _lstHandler.AddLast(new EdwardsSwitchOnPumpHandler(this));
            //    else
            //        _lstHandler.AddLast(new EdwardsSwitchOffPumpHandler(this));
            //}
        }

        #endregion

        #region Properties
        public string Error { get; private set; }

        public bool ActionCommandCompleted { get; private set; }

        private string _systemType;
        //private Dictionary<string, Dictionary<string, ParaResponse>> AlarmStatusDict = new Dictionary<string, Dictionary<string, ParaResponse>>();
        private Dictionary<int, ParaResponse> AlarmStatusDict = new Dictionary<int, ParaResponse>();
        private Dictionary<int, ParaResponse> BitfieldStatusDict = new Dictionary<int, ParaResponse>();
        private Dictionary<int, ParaResponse> ParaValueDict = new Dictionary<int, ParaResponse>();

        public ParaResponse GetAlarmStatus(int parameter)
        {
            if (AlarmStatusDict.ContainsKey(parameter))
                return AlarmStatusDict[parameter];
            else
                return null;
        }

        internal void NoteAlarmStatus(string parameter, string data)
        {
            try
            {
                int iPara = int.Parse(parameter);
                if (AlarmStatusDict.ContainsKey(iPara))
                {
                    AlarmStatusDict[iPara] = new ParaResponse(parameter);
                }
                else
                {
                    AlarmStatusDict.Add(iPara, new ParaResponse(data));
                }
            }
            catch (Exception ex)
            {
                NoteError("Invalid Alarm Status");
                LOG.Error($"Invalid Alarm Status: {data}, Exception info: {ex}");
            }

        }

        private int Str2Int(string sValue)
        {
            if (string.IsNullOrEmpty(sValue))
                throw new Exception("empty int value");
            if (sValue.Length > 1)
            {
                sValue = sValue.TrimStart('0');
            }
            return int.Parse(sValue);
        }


        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();



        public void NoteError(string reason)
        {
            if (reason != null)
            {
                _trigWarningMessage.CLK = true;
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
                }
                Error = reason;
            }
            else
            {
                Error = null;
            }
        }
        public void NoteWarning(ErrorInfo info)
        {
            if (info.ErrorName != null)
            {
                if (info.IsError&&info._trigError.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} warn, {info.ErrorName}");
                }
                IsWarning = true; 
                Error = info.ErrorName;
            }
            else
            {
                Error = null;
            }
        }
        public void NoteAlarm(ErrorInfo info)
        {
            if (info.ErrorName != null)
            {
                if (info.IsError&&info._trigError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} alarm, {info.ErrorName}");
                }
                Error = info.ErrorName;
            }
            else
            {
                Error = null;
            }
        }

        internal void NoteRawCommandInfo(string command, string data)
        {
            //var curIOResponse = IOResponseList.Find(res => res.SourceCommand == command);
            //if (curIOResponse != null)
            //{
            //    IOResponseList.Remove(curIOResponse);
            //}
            //IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        #endregion
        #region Errorcode
        public List<ErrorInfo> errorInfo = new List<ErrorInfo>()
        {
            new ErrorInfo(){Index=0,ErrorName="Water flow low",Level=2},
            new ErrorInfo(){Index=5,ErrorName="Casing temp. high",Level=2},
            new ErrorInfo(){Index=6,ErrorName="BP-G oil level low",Level=2},
            new ErrorInfo(){Index=7,ErrorName="BP-M oil level low",Level=2},
            new ErrorInfo(){Index=8,ErrorName="MP-G oil level low",Level=2},
            new ErrorInfo(){Index=9,ErrorName="MP-M oil level low",Level=2},
            new ErrorInfo(){Index=10,ErrorName="Drv brg temp. high",Level=2},
            new ErrorInfo(){Index=11,ErrorName="Drvn brg temp. high",Level=2},
            new ErrorInfo(){Index=12,ErrorName="Oil level low",Level=2},
            new ErrorInfo(){Index=13,ErrorName="BOX temp. high",Level=2},
            new ErrorInfo(){Index=14,ErrorName="N2 valve open",Level=2},
            new ErrorInfo(){Index=15,ErrorName="Cooler 1 temp. high",Level=2},
            new ErrorInfo(){Index=16,ErrorName="Cooler 2 temp. high",Level=2},
            new ErrorInfo(){Index=17,ErrorName="Cooler 3 temp. high",Level=2},
            new ErrorInfo(){Index=18,ErrorName="Pump N2 flow low",Level=2},
            new ErrorInfo(){Index=19,ErrorName="Ehx. N2 flow low",Level=2},
            new ErrorInfo(){Index=20,ErrorName="Exh. trap temp. high",Level=2},
            new ErrorInfo(){Index=21,ErrorName="Back press. high",Level=2},
            new ErrorInfo(){Index=22,ErrorName="Heater error",Level=2},
            new ErrorInfo(){Index=23,ErrorName="BP motor temp. high",Level=2},
            new ErrorInfo(){Index=24,ErrorName="MP motor temp. high",Level=2},
            new ErrorInfo(){Index=25,ErrorName="Driver temp. high",Level=2},
            new ErrorInfo(){Index=26,ErrorName="Communication error",Level=2},
            new ErrorInfo(){Index=27,ErrorName="Valve error",Level=2},
            new ErrorInfo(){Index=31,ErrorName="Other warnings",Level=2},
            new ErrorInfo(){Index=50,ErrorName="Casing temp. high",Level=3},
            new ErrorInfo(){Index=51,ErrorName="BP motor temp. high",Level=3},
            new ErrorInfo(){Index=52,ErrorName="MP motor temp. high",Level=3},
            new ErrorInfo(){Index=53,ErrorName="Water leakage",Level=3},
            new ErrorInfo(){Index=54,ErrorName="BP thermal",Level=3},
            new ErrorInfo(){Index=55,ErrorName="MP thermal",Level=3},
            new ErrorInfo(){Index=60,ErrorName="MP no current",Level=3},
            new ErrorInfo(){Index=63,ErrorName="Back press. high",Level=3},
            new ErrorInfo(){Index=64,ErrorName="Power failure",Level=3},
            new ErrorInfo(){Index=65,ErrorName="MP driver protection",Level=3},
            new ErrorInfo(){Index=66,ErrorName="BP driver protection",Level=3},
            new ErrorInfo(){Index=67,ErrorName="BP overload 2",Level=3},
            new ErrorInfo(){Index=68,ErrorName="MP overload 2",Level=3},
            new ErrorInfo(){Index=69,ErrorName="BP step out",Level=3},
            new ErrorInfo(){Index=70,ErrorName="MP step out",Level=3},
            new ErrorInfo(){Index=71,ErrorName="Emergency off (EMO)",Level=3},
            new ErrorInfo(){Index=72,ErrorName="Exh. N2 flow low",Level=3},
            new ErrorInfo(){Index=73,ErrorName="Water flow low continued",Level=3},
            new ErrorInfo(){Index=74,ErrorName="External interlock",Level=3},
            new ErrorInfo(){Index=81,ErrorName="Other alarms",Level=3},
        };
        #endregion
    }


    public class ParaResponse
    {
        public ParaResponse(string paraRes)
        {
            var paraArray = paraRes.Split(',');
            priorityLevel = int.Parse(paraArray[0]);
            alarmType = int.Parse(paraArray[1]);
            bitfiledStatus = int.Parse(paraArray[2]);
        }
        public int priorityLevel;
        public int alarmType;
        public int bitfiledStatus;
    }
}
