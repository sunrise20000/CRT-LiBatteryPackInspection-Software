using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.HirataII
{
    public class HirataIILoadPort:LoadPortBaseDevice,IConnection
    {
        public HirataIILoadPort(string module,string name,string scRoot, IoTrigger[] dos,bool IsTCPconnection = false,RobotBaseDevice robot =null) :base(module,name,robot)
        {
            _scRoot = scRoot;
            _isTcpConnection = IsTCPconnection;
            LoadPortType = "TDKLoadPort";
            if (dos != null && dos.Length >= 1)
            {
                _doLoadPortOK = dos[0];

            }
            InitializeLP();
            SubscribeLPData();
        }

        private void SubscribeLPData()
        {
            DATA.Subscribe($"{Module}.{Name}.SystemStatus", () => SystemStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.Mode", () => Mode.ToString());
            DATA.Subscribe($"{Module}.{Name}.InitPosMovement", () => InitPosMovement.ToString());
            DATA.Subscribe($"{Module}.{Name}.OperationStatus", () => OperationStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => ErrorCode.ToString());
            DATA.Subscribe($"{Module}.{Name}.ContainerStatus", () => ContainerStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.ClampPosition", () => ClampPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.LPDoorLatchPosition", () => LPDoorLatchPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.VacuumStatus", () => VacuumStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.LPDoorState", () => LPDoorState.ToString());
            DATA.Subscribe($"{Module}.{Name}.WaferProtrusion", () => WaferProtrusion.ToString());
            DATA.Subscribe($"{Module}.{Name}.ElevatorAxisPosition", () => ElevatorAxisPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.MapperPostion", () => MapperPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingStatus", () => MappingStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.Model", () => Model.ToString());
            DATA.Subscribe($"{Module}.{Name}.IsFosbModeActual", () => IsFosbModeActual.ToString());
            DATA.Subscribe($"{Module}.{Name}.DockPosition", () => DockPosition.ToString());
        }
        public override string SpecCarrierType
        {
            get
            {
                if (_isPlaced)
                    return SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadCarrierIndex}");
                return "";
            }
            set => base.SpecCarrierType = value;
        }

        public override bool ReadCarrierID(int offset = 0, int length = 16)
        {
            if (SC.ContainsItem($"{_scRoot}.{Name}.NeedMoveToReadCarrierID") &&
                SC.GetValue<bool>($"{_scRoot}.{Name}.NeedMoveToReadCarrierID"))
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new HirataIIMoveToReadCarrierIDHandler(this));
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "FCOTB", null));
                }
                return true;
            }
            else
                return base.ReadCarrierID(offset, length);

        }
        public override bool IsEnableDualTransfer(out string reason)
        {
            reason = "";
            if (SC.ContainsItem($"CarrierInfo.EnableDualTransfer{InfoPadCarrierIndex}") &&
                SC.GetValue<bool>($"CarrierInfo.EnableDualTransfer{InfoPadCarrierIndex}"))
            {

                return true;
            }
            return base.IsEnableDualTransfer(out reason);
        }
        private void InitializeLP()
        {
            IsMapWaferByLoadPort = true;
            if (_doLoadPortOK != null)
                _doLoadPortOK.SetTrigger(true, out _);

            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"LoadPort.{Name}.EnableLogMessage");
            if (_isTcpConnection)
            {
                Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                _tcpConnection = new HirataIILoadPortTCPConnection(this, Address);
                
                _tcpConnection.EnableLog(_enableLog);
                if (_tcpConnection.Connect())
                {
                    //LOG.Write($"Connected with {Module}.{Name} .");
                    EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");                    
                }
                else
                {
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                }
            }
            else
            {
                string portName = SC.GetStringValue($"LoadPort.{Name}.PortName");
                int bautRate = SC.GetValue<int>($"LoadPort.{Name}.BaudRate");
                int dataBits = SC.GetValue<int>($"LoadPort.{Name}.DataBits");
                Enum.TryParse(SC.GetStringValue($"LoadPort.{Name}.Parity"), out Parity parity);
                Enum.TryParse(SC.GetStringValue($"LoadPort.{Name}.StopBits"), out StopBits stopBits);

                _connection = new HirataIILoadPortConnection(this, portName, bautRate, dataBits, parity, stopBits);
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
                        //LOG.Write($"Connected with {Module}.{Name} .");
                        EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
                        break;
                    }
                    if (count > 0 && retry++ > count)
                    {
                        EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                        break;
                    }
                } while (true);
            }
           ConnectionManager.Instance.Subscribe($"{Name}", this);
           
            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            



        }

        private bool OnTimer()
        {
            try
            {
                if (_isTcpConnection)
                {
                    _tcpConnection.MonitorTimeout();
                    if(!_tcpConnection.IsConnected || _tcpConnection.IsCommunicationError)
                    {
                        lock (_locker)
                        {
                            _lstHandler.Clear();
                        }
                        _trigRetryConnect.CLK = !_tcpConnection.IsConnected;
                        if (_trigRetryConnect.Q)
                        {

                            Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                            _tcpConnection = new HirataIILoadPortTCPConnection(this, Address);
                            _tcpConnection.EnableLog(_enableLog);
                            if (!_tcpConnection.Connect())
                            {
                                EV.PostAlarmLog(Module, $"Can not connect with {_tcpConnection.Address}, {Module}.{Name}");
                            }
                        }
                        return true;
                    }
                    _trigActionDone.CLK = (_lstHandler.Count == 0 && !_tcpConnection.IsBusy);
                    if (_trigActionDone.Q)
                        OnActionDone(null);

                    HandlerBase handler = null;
                    if (!_tcpConnection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                            }

                            if (_lstHandler.Count > 0)
                            {
                                handler = _lstHandler.First.Value;
                                if (handler != null) _tcpConnection.Execute(handler);
                                _lstHandler.RemoveFirst();
                            }
                        }


                    }

                }
                else
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
                    _trigActionDone.CLK = (_lstHandler.Count == 0 && !_connection.IsBusy);
                    if (_trigActionDone.Q)
                        OnActionDone(null);

                    HandlerBase handler = null;
                    if (!_connection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                            }

                            if (_lstHandler.Count > 0)
                            {
                                handler = _lstHandler.First.Value;
                                if (handler != null) _connection.Execute(handler);
                                _lstHandler.RemoveFirst();
                            }
                        }
                    }
                }


                if (InfoPadType == 2)   // Fixed by SC
                {
                    InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
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
            base.Monitor();
            try
            {
                if (_isTcpConnection)
                {
                    _tcpConnection.EnableLog(_enableLog);

                    _trigCommunicationError.CLK = _tcpConnection.IsCommunicationError;
                    if (_trigCommunicationError.Q)
                    {
                        EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_tcpConnection.LastCommunicationError}");
                        OnError("Communicartion Error");
                    }
                }
                else
                {
                    _connection.EnableLog(_enableLog);

                    _trigCommunicationError.CLK = _connection.IsCommunicationError;
                    if (_trigCommunicationError.Q)
                    {
                        EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                        OnError("Communicartion Error");
                    }
                }
            
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
        private R_TRIG _trigActionDone = new R_TRIG();
        private string _scRoot;
        private bool _isTcpConnection;
        private HirataIILoadPortConnection _connection;
        private HirataIILoadPortTCPConnection _tcpConnection;
        private IoTrigger _doLoadPortOK;
        private IoSensor _diInfoPadA = null;
        private IoSensor _diInfoPadB = null;
        private IoSensor _diInfoPadC = null;
        private IoSensor _diInfoPadD = null;

        //InfoPadType,0=OnLP,1=Ext,2=FixedbySC
        public int InfoPadType
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{Name}.InfoPadType") ? SC.GetValue<int>($"LoadPort.{Name}.InfoPadType") : 2;
                
            }
        }
        public HirataIILoadPortConnection Connection
        {
            get => _connection;
        }
        public HirataIILoadPortTCPConnection TCPConnection => _tcpConnection;

        private PeriodicJob _thread;
        private static Object _locker = new Object();
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private bool _enableLog = true;
        //private bool _commErr = false;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        public TDKSystemStatus SystemStatus { get; set; }
        public TDKMode Mode { get; set; }
        public TDKInitPosMovement InitPosMovement { get; set; }
        public TDKOperationStatus OperationStatus { get; set; } 
        public TDKContainerStatus ContainerStatus { get; set; }
        public TDKPosition ClampPosition { get; set; }
        public TDKPosition LPDoorLatchPosition { get; set; }
        public TDKVacummStatus VacuumStatus { get; set; }
        public TDKPosition LPDoorState { get; set; }
        public TDKWaferProtrusion WaferProtrusion { get; set; }
        public TDKElevatorAxisPosition ElevatorAxisPosition { get; set; }
        public TDKDockPosition DockPosition { get; set; }
        public TDKMapPosition MapperPostion { get; set; }
        public TDKMappingStatus MappingStatus { get; set; }

        public TDKModel Model { get; set; }
        public string Address { get; set; }
       
        public bool IsConnected => this.Connect();// _connection.IsConnected;

        public bool Disconnect()
        {
            if (_isTcpConnection)
                return _tcpConnection.Disconnect();
            return _connection.Disconnect();
        }
        public void OnCarrierNotPlaced()
        {
            _isPlaced = false;
            ConfirmRemoveCarrier();
        }


        public void OnCarrierNotPresent()
        {
            _isPresent = false;
            //ConfirmRemoveCarrier();
        }

        public void OnCarrierPlaced()
        {
            _isPlaced = true;
            ConfirmAddCarrier();
            
        }


        public void OnCarrierPresent()
        {
            _isPresent = true;
            //ConfirmAddCarrier();
        }

        public void OnSwitchKey1()
        {
            _isAccessSwPressed = true;
        }

        public void OnSwitchKey2()
        {

        }

        public void OffSwitchKey1()
        {
            _isAccessSwPressed = false;
        }

        public void OffSwitchKey2()
        {

        }
        public bool OnEvent(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new HirataIIGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
            }
            return true;
        }
        private LoadportCassetteState _cassetteState = LoadportCassetteState.None;
        public override LoadportCassetteState CassetteState
        {
            get { return _cassetteState; }
            set
            {
                _cassetteState = value;
            }
        }
        public void SetCassetteState(LoadportCassetteState state)
        {
            _cassetteState = state;

            if (state == LoadportCassetteState.Normal)
            {
                if (!_isPlaced)
                {
                    OnCarrierPlaced();
                }

                if (!_isPresent)
                {
                    OnCarrierPresent();
                }
            }
        }



        protected override bool fStartWrite(object[] param)
        {
            return false;
        }

        protected override bool fStartRead(object[] param)
        {
            return false;
        }       
        protected override bool fStartExecute(object[] param)
        {
            try
            {
                switch(param[0].ToString())
                {
                    case "Move":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, param[1].ToString(), null));
                        }
                        break;
                    case "Set":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIISetHandler(this, param[1].ToString(), null));
                        }
                        break;
                    case "Get":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIGetHandler(this, param[1].ToString(), null));
                        }
                        break;
                    case "SetIndicator":
                        Indicator light = (Indicator)param[1];
                        IndicatorState state = (IndicatorState)param[2];
                        string[] statestr = new string[] {"","LP","BL","LO" }; //{ "","LON", "LBL", "LOF" };
                        string strLight = "";
                        if (light == Indicator.LOAD)
                            strLight = "LOD";
                        if (light == Indicator.UNLOAD)
                            strLight = "ULD";
                        if (light == Indicator.ACCESSMANUL)
                            strLight = "ST2";
                        if (light == Indicator.ACCESSAUTO)
                            strLight = "ST3";
                        if (string.IsNullOrEmpty(strLight))
                            return false;
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIISetHandler(this, statestr[(int)state], strLight));
                            _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));

                        }
                        break;
                    case "QueryIndicator":
                        lock (_locker)
                        {                            
                            _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
                        }
                        break;
                    case "QueryState":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
                        }
                        break;
                    case "Undock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "YWAIT", null));
                        }
                        break;
                    case "Dock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "YDOOR", null));
                        }
                        break;
                    case "CloseDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "DORFW", null));
                        }
                        break;
                    case "OpenDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "DORBK", null));
                        }
                        break;
                    case "Unclamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "PODOP", null));
                        }
                        break;
                    case "Clamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "PODCL", null));
                        }
                        break;
                    case "DoorUp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "ZDRUP", null));
                        }
                        break;
                    case "DoorDown":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "ZDRDW", null));
                        }
                        break;


                    case "OpenDoorNoMap":
                        lock (_locker)
                        {
                            //_lstHandler.AddLast(new TDKMoveHandler(this, "YDOOR", null));
                        }
                        break;
                    case "OpenDoorAndMap":
                        lock (_locker)
                        {
                            //_lstHandler.AddLast(new TDKMoveHandler(this, "YDOOR", null));
                        }
                        break;
                    case "MapWafer":
                        lock(_locker)
                        {
                            if(DockPosition != TDKDockPosition.Dock)
                                _lstHandler.AddLast(new HirataIIMoveHandler(this, "YDOOR", null));
                            if(DoorState != FoupDoorState.Open)
                                _lstHandler.AddLast(new HirataIIMoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "MAPDO", null));
                            _lstHandler.AddLast(new HirataIIGetHandler(this, "MAPRD", null));
                        }                        
                        break;

                    case "ReadCarrierID":
                        lock(_locker)
                        {

                            _lstHandler.AddLast(new HirataIIMoveToReadCarrierIDHandler(this));
                        }
                        break;
                }
                return true;



            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                EV.PostAlarmLog(Name, $"Parameter invalid");
                return false;
                
            }
        }

        protected override bool fStartUnload(object[] param)
        {
            if (!_isPlaced)
            {
                EV.PostAlarmLog(Name, $"No carrier on {Name},can't unload.");
                return false;
            }
            if (!_isDocked)
            {
                EV.PostAlarmLog(Name, $"Carrier is not docked on {Name},can't unload.");
                return false;
            }
            if (IsRequestFOSBMode)
            {
                lock (_locker)
                {
                    if (!IsFosbModeActual)
                        _lstHandler.AddLast(new HirataIISetHandler(this, "FSB", "ON"));

                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "ZDRUP", null));
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "DORFW", null));
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "YWAIT", null));
                }
            }
            else
            {
                if (IsFosbModeActual)
                    _lstHandler.AddLast(new HirataIISetHandler(this, "FSB", "OF"));
                if (param.Length >= 1 && param[0].ToString() == "UnloadWithMap")
                {
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "CUDMP", null));
                    _lstHandler.AddLast(new HirataIIGetHandler(this, "MAPRD", null));
                }
                if (param.Length >= 1 && param[0].ToString() == "UnloadWithoutMap")
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "CULOD", null));
                if (param == null || param.Length == 0)
                {
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "CUDMP", null));
                    _lstHandler.AddLast(new HirataIIGetHandler(this, "MAPRD", null));
                }
            }
            return true;



        }

        protected override bool fStartLoad(object[] param)
        {
            if(!_isPlaced)
            {
                EV.PostAlarmLog(Name, $"No carrier on {Name},can't load.");
                return false;
            }
            if (_isDocked)
            {
                EV.PostAlarmLog(Name, $"Carrier is docked on {Name},can't load.");
                return false;
            }
            if (IsRequestFOSBMode) 
            { 
                lock (_locker)
                {   
                    if(!IsFosbModeActual)
                        _lstHandler.AddLast(new HirataIISetHandler(this, "FSB", "ON"));
                    if(ClampPosition == TDKPosition.Open)
                        _lstHandler.AddLast(new HirataIIMoveHandler(this, "PODCL", null));
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "DORBK", null));
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "YDOOR", null));
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "ZDRDW", null));
                }               
            }
            else
            {
                lock (_locker)
                {

                    if (IsFosbModeActual)
                        _lstHandler.AddLast(new HirataIISetHandler(this, "FSB", "OF"));
                    if (param.Length >= 1 && param[0].ToString() == "LoadWithMap")
                    {
                        _lstHandler.AddLast(new HirataIIMoveHandler(this, "CLDMP", null));
                        _lstHandler.AddLast(new HirataIIGetHandler(this, "MAPRD", null));
                    }
                    if (param.Length >= 1 && param[0].ToString() == "LoadWithoutMap")
                        _lstHandler.AddLast(new HirataIIMoveHandler(this, "CLOAD", null));
                    if (param == null || param.Length == 0)
                    {
                        _lstHandler.AddLast(new HirataIIMoveHandler(this, "CLDMP", null));
                        _lstHandler.AddLast(new HirataIIGetHandler(this, "MAPRD", null));
                    }
                }
            }
            return true;
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                if(param.Length >=1 && param[0].ToString() == "ForceHome")
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "ABORG", null));
                else
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "ORGSH", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new HirataIIGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
            }
            return true;
        }
        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            return fStartExecute(new object[] { "SetIndicator", light, state });
        }

        protected override bool fStartReset(object[] param)
        {
            _lstHandler.Clear();
            if (_isTcpConnection)
            {
                if (!_tcpConnection.IsConnected)
                    _tcpConnection.Connect();
                _tcpConnection.ForceClear();
            }
            else
            {
                if (!_connection.IsConnected)
                    _connection.Connect();
                _connection.ForceClear();
            }
            
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataIISetHandler(this, "RESET", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new HirataIIGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
            }
            return true;
        }
        public override void OnError(string error = "")
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                if(_isTcpConnection)
                    _tcpConnection.ForceClear();
                else
                    _connection.ForceClear();
                _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
               // _lstHandler.AddLast(new HirataIIGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
            }

            base.OnError(error);
        }

        public override WaferSize GetCurrentWaferSize()
        {
            int intwz = SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}");
            switch (intwz)
            {
                case 0:
                    return WaferSize.WS0;
                case 1:
                    return WaferSize.WS0;
                case 2:
                    return WaferSize.WS2;
                case 3:
                    return WaferSize.WS3;
                case 4:
                    return WaferSize.WS4;
                case 5:
                    return WaferSize.WS5;
                case 6:
                    return WaferSize.WS6;
                case 7:
                case 8:
                    return WaferSize.WS8;
                case 12:
                    return WaferSize.WS12;
                default:
                    return WaferSize.WS0;
            }
        }



    }
    public enum TDKSystemStatus
    {
        Normal = 0x30,
        RecoverableError = 0x41,
        UnrecoverableError = 0x45,
    }
    public enum TDKMode
    {
        Online = 0x30,
        Teaching = 0x31,
        Maintenance = 0x32,
    }
    public enum TDKInitPosMovement
    {
        OperationStatus = 0x30,
        HomePosStatus = 0x31,
        LoadStatus = 0x32,
    }
    public enum TDKOperationStatus
    {
        DuringStop = 0x30,
        DuringOperation = 0x31,
    }
    public enum TDKContainerStatus
    {
        Absence = 0x30,
        NormalMount = 0x31,
        MountError = 0x32,
    }
    public enum TDKPosition
    {
        Open = 0x30,
        Close = 0x31,
        TBD = 0x3F
    }
    public enum TDKVacummStatus
    {
        OFF = 0x30,
        ON = 0x31,
    }
    public enum TDKWaferProtrusion
    {
        ShadingStatus = 0x30,
        LightIncidentStatus = 0x31,
    }
    public enum TDKElevatorAxisPosition
    {
        UP = 0x30,
        Down = 0x31,
        MappingStartPos = 0x32,
        MappingEndPos = 0x33,
        TBD = 0x3F,
    }
    public enum TDKDockPosition
    {
        Undock = 0x30,
        Dock = 0x31,
        TBD = 0x3F,
    }
    public enum TDKMapPosition
    {
        MeasurementPos = 0x30,
        WaitingPost = 0x31,
        TBD = 0x3F,
    }
    public enum TDKMappingStatus
    {
        NotPerformed = 0x30,
        NormalEnd = 0x31,
        ErrorStop = 0x32,
    }
    public enum TDKModel
    {
        Type1 = 0x30,
        Type2 = 0x31,
        Type3 = 0x32,
        Type4 = 0x33,
        Type5 = 0x34,
    }
}
