using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.TazmoRobot
{
    public enum TazmoRobotState
    {
        Standby=0,
        TPInUse=1,
        InOperation=2,
        Running,
        Pause=7,
        Initializing=9,
        StandbyDueToUnInit= 0X10,
        RecoverabelErrorWithoutInit = 0X20,
        ErrorRequiredInit =0xA0,
        ErrorRequiredPowerCycle = 0xC0,

    }
    public enum TazmoRobotPositionEnum
    {
        UnloadLoad,
        Mapping,
    }
    public enum TazmoRobotArmExtensionEnum
    {
        PositionToPull,
        ArmExtension,
    }
    public enum TazmoRobotWorkPresenceInfoEnum
    {
        WorkExists,
        NoWork,
    }
    public enum TazmoRobotGripPostionInfoEnum
    {
        Close,
        Oepn,
        WorkIsGripped,
        PostionUnknow = 0xF,
    }
    public enum TazmoRobotZPositionInfo
    {
        Down=1,
        MidDown,
        Mid,
        MidUp,
        Up,
        Unknow = 0xF,
    }




    public class TazmoRobot : RobotBaseDevice, IConnection
    {
        private bool isSimulatorMode;
        private string _scRoot;

        public TazmoRobotState DeviceState { get; private set; }
        

        public bool TaExecuteSuccss { get; set; }
        private TazmoRobotConnection _connection;
        public TazmoRobotConnection Connection => _connection;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private PeriodicJob _thread;
        private static Object _locker = new Object();

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private bool _enableLog = true;

        private bool _commErr = false;
        private string _address;
        

        public int CurrentStopPositionPoint { get; private set; }
        public int CurrentSlotNumber { get; private set; }

        public TazmoRobotPositionEnum CurrentPositionCate { get; private set; }
        public TazmoRobotArmExtensionEnum CurrentArmExtensionPos { get; private set; }
        public TazmoRobotWorkPresenceInfoEnum CurrentWorkPresnece { get; private set; }

        public TazmoRobotWorkPresenceInfoEnum CurrentArm1WorkPresnece { get; private set; }
        public TazmoRobotWorkPresenceInfoEnum CurrentArm2WorkPresnece { get; private set; }
        public TazmoRobotGripPostionInfoEnum CurrentArm1GripperPosition { get; private set; }
        public TazmoRobotGripPostionInfoEnum CurrentArm2GripperPosition { get; private set; }
        public TazmoRobotZPositionInfo CurrentZpositionCate { get; private set; }


        public string PortName { get; private set; }
        public TazmoRobot(string module, string name, string scRoot, string portName="") : base(module, name)
        {

            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _scRoot = scRoot; 
            //base.Initialize();
            ResetPropertiesAndResponses();
            RegisterSpecialData();

            
            int bautRate = SC.GetValue<int>($"{Name}.BaudRate");
            int dataBits = SC.GetValue<int>($"{Name}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{Name}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{Name}.StopBits"), out StopBits stopBits);
            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");
            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");            
            PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            _connection = new TazmoRobotConnection(this,PortName,bautRate,dataBits,parity,stopBits);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            //_address = SC.GetStringValue($"{_scRoot}.{Name}.DeviceAddress");




        }
        public void ParseStatus(string state)
        {
            int statecode = Convert.ToInt32(state, 16);

            if (statecode <= 0x10) DeviceState = (TazmoRobotState)statecode;
            else
                ErrorCode = statecode.ToString();

            if (statecode >= 0x20 && statecode <= 0x99) DeviceState = TazmoRobotState.RecoverabelErrorWithoutInit;
            if (statecode >= 0xA0 && statecode <= 0xBF) DeviceState = TazmoRobotState.ErrorRequiredInit;
            if (statecode > 0xC0 && statecode <= 0xFF) DeviceState = TazmoRobotState.ErrorRequiredPowerCycle;
        }

        public void ParseStatusAndPostion(string statedata)
        {
            string[] data = statedata.Split(',');
            if(data.Length !=11)
            {
                EV.PostAlarmLog("System", "Received in valid status and postion data:" + statedata);
                return;
            }
            ParseStatus(data[0]);
            CurrentStopPositionPoint = Convert.ToInt32(data[1]);
            CurrentSlotNumber = Convert.ToInt32(data[2]);
            CurrentPositionCate = (TazmoRobotPositionEnum)Convert.ToInt32(data[3]);
            CurrentArmExtensionPos = (TazmoRobotArmExtensionEnum)Convert.ToInt32(data[4]);
            CurrentWorkPresnece = (TazmoRobotWorkPresenceInfoEnum)Convert.ToInt32(data[5]);
            CurrentArm1WorkPresnece = (TazmoRobotWorkPresenceInfoEnum)Convert.ToInt32(data[6]);
            CurrentArm2WorkPresnece = (TazmoRobotWorkPresenceInfoEnum)Convert.ToInt32(data[7]);
            CurrentArm1GripperPosition = (TazmoRobotGripPostionInfoEnum)Convert.ToInt32(data[8]);
            CurrentArm2GripperPosition = (TazmoRobotGripPostionInfoEnum)Convert.ToInt32(data[9]);
            CurrentZpositionCate = (TazmoRobotZPositionInfo)Convert.ToInt32(data[10]);
        }
        private void RegisterSpecialData()
        {
            
        }

        private void ResetPropertiesAndResponses()
        {
            
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
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public string Address { get; private set; }

        public bool IsConnected { get; private set; }

        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        public override RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm)
        {
            if (arm == RobotArmEnum.Lower)
            {
                if (CurrentArm1WorkPresnece == TazmoRobotWorkPresenceInfoEnum.NoWork)
                    return RobotArmWaferStateEnum.Absent;
                if (CurrentArm1WorkPresnece == TazmoRobotWorkPresenceInfoEnum.WorkExists)
                    return RobotArmWaferStateEnum.Present;
            }
            if(arm == RobotArmEnum.Upper)
            {
                if (CurrentArm2WorkPresnece == TazmoRobotWorkPresenceInfoEnum.NoWork)
                    return RobotArmWaferStateEnum.Absent;
                if (CurrentArm2WorkPresnece == TazmoRobotWorkPresenceInfoEnum.WorkExists)
                    return RobotArmWaferStateEnum.Present;
            }
            return RobotArmWaferStateEnum.Unknown;
        }

        public override bool IsReady()
        {
            throw new NotImplementedException();
        }

        protected override bool fClear(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fError(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fReset(object[] param)
        {
            bool waferconfirm = SC.ContainsItem("Robot.WaferConfirmOnHome") &&
                (SC.GetValue<int>("Robot.WaferConfirmOnHome") == 0);  //0=auto detect,1=down move after release
            lock (_locker)
            {
                _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "RST", waferconfirm ? "0" : "1"));
            }
            return true;
        }
        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "HOM", ""));
            }
            return true;

        }

        protected override bool fResetToReady(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartExtendForPick(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartExtendForPlace(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartGoTo(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartGrip(object[] param)
        {
            RobotArmEnum arm = (RobotArmEnum)((int)param[0]);
            lock (_locker)
            {
                if (arm == RobotArmEnum.Both)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVN", "1"));
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVN", "2"));
                }
                if(arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVN", "1"));
                }
                if(arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVN", "2"));
                }
            }
            return true;            
        }

        

        protected override bool fStartMapWafer(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartPickWafer(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartPlaceWafer(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartReadData(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartRetractFromPick(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartRetractFromPlace(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartSetParameters(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartSwapWafer(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartTransferWafer(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartUnGrip(object[] param)
        {
            RobotArmEnum arm = (RobotArmEnum)((int)param[0]);
            lock (_locker)
            {
                if (arm == RobotArmEnum.Both)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVF", "1"));
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVF", "2"));
                }
                if (arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVF", "1"));
                }
                if (arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVF", "2"));
                }
            }
            return true;     
        }
        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _connection.EnableLog(_enableLog);

            _trigRetryConnect.RST = true;

            //base.Reset();
        }
    }

}
