using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.PLC;
using Aitex.Core.Util;
using SicPM.Devices;
using Aitex.Core.RT.IOCore;
using static SicPM.PmDevices.DicMode;
using Aitex.Sorter.Common;

namespace SicPM
{

    public enum IoGroupName
    {
        A, B, C, D, E, F, G, H, I, J, K, Final1, Final2, EPV2, GasIn1, GasIn2, VentPump, ArSupply, GasSupply, All,
        V27, V888990, V76, V75, V70, V69, V25, V94, GroupIWithoutV94, V92V93V95, V96, V31, V32, V35V36, V65, V68
    };

    public enum MfcGroupName
    {
        Final1, Final2, M1to16, M2toM40, M2toM40NoFinal1MFC, M27toM38, M2toM26, M27toM40, M19toM33, M32toM38, M36, M28293140,
        M19toM26, All, M32M35M37, M38, AllWithoutM28M29M31M40
    };

    public partial class PMModule
    {
        #region InterLock
        [Tag("PMInterLock")]
        public Devices.IoInterLock _pmInterLock { get; set; }

        #endregion

        #region Servo & SignalTower && TV
        [Tag("SignalTower")]
        public Devices.IoSignalTower _signalTower { get; set; }

        [Tag("PMServo")]
        public Devices.SicServo _sicServo { get; set; }

        [Tag("PMAETemp")]
        public Devices.SicAETemp _sicAETemp { get; set; }



        [Tag("PMHeatEnable")]
        public Devices.IoHeat _IoHeat { get; set; }

        [Tag("TV")]
        public IoThrottleValve2 _ioThrottleValve { get; set; }
        #endregion

        #region Valve
        [Tag("V25")]
        public IoValve V25 { get; set; }
        [Tag("V27")]
        public IoValve V27 { get; set; }
        [Tag("V31")]
        public IoValve V31 { get; set; }

        [Tag("V32")]
        public IoValve V32 { get; set; }

        [Tag("V33")]
        public IoValve V33 { get; set; }

        [Tag("V33s")]
        public IoValve V33s { get; set; }

        [Tag("V35")]
        public IoValve V35 { get; set; }

        [Tag("V36")]
        public IoValve V36 { get; set; }

        [Tag("V37")]
        public IoValve V37 { get; set; }

        [Tag("V37s")]
        public IoValve V37s { get; set; }

        [Tag("V39")]
        public IoValve V39 { get; set; }

        [Tag("V39s")]
        public IoValve V39s { get; set; }

        [Tag("V40")]
        public IoValve V40 { get; set; }

        [Tag("V40s")]
        public IoValve V40s { get; set; }

        [Tag("V41")]
        public IoValve V41 { get; set; }

        [Tag("V42")]
        public IoValve V42 { get; set; }

        [Tag("V43")]
        public IoValve V43 { get; set; }

        [Tag("V43s")]
        public IoValve V43s { get; set; }

        [Tag("V45")]
        public IoValve V45 { get; set; }

        [Tag("V46")]
        public IoValve V46 { get; set; }

        [Tag("V46s")]
        public IoValve V46s { get; set; }

        [Tag("V48")]
        public IoValve V48 { get; set; }

        [Tag("V48s")]
        public IoValve V48s { get; set; }

        [Tag("V49")]
        public IoValve V49 { get; set; }

        [Tag("V50")]
        public IoValve V50 { get; set; }

        [Tag("V50s")]
        public IoValve V50s { get; set; }

        [Tag("V51")]
        public IoValve V51 { get; set; }

        [Tag("V51s")]
        public IoValve V51s { get; set; }

        [Tag("V52")]
        public IoValve V52 { get; set; }

        [Tag("V52s")]
        public IoValve V52s { get; set; }

        [Tag("V53")]
        public IoValve V53 { get; set; }

        [Tag("V53s")]
        public IoValve V53s { get; set; }

        [Tag("V54")]
        public IoValve V54 { get; set; }

        [Tag("V54s")]
        public IoValve V54s { get; set; }

        [Tag("V55")]
        public IoValve V55 { get; set; }

        [Tag("V56")]
        public IoValve V56 { get; set; }


        [Tag("V58")]
        public IoValve V58 { get; set; }

        [Tag("V58s")]
        public IoValve V58s { get; set; }

        [Tag("V59")]
        public IoValve V59 { get; set; }

        [Tag("V60")]
        public IoValve V60 { get; set; }

        [Tag("V61")]
        public IoValve V61 { get; set; }

        [Tag("V62")]
        public IoValve V62 { get; set; }

        [Tag("V63")]
        public IoValve V63 { get; set; }

        [Tag("V64")]
        public IoValve V64 { get; set; }

        [Tag("V65")]
        public IoValve V65 { get; set; }

        [Tag("V68")]
        public IoValve V68 { get; set; }

        [Tag("V69")]
        public IoValve V69 { get; set; }

        [Tag("V70")]
        public IoValve V70 { get; set; }

        [Tag("V72")]
        public IoValve V72 { get; set; }

        [Tag("V73")]
        public IoValve V73 { get; set; }


        [Tag("V74")]
        public IoValve V74 { get; set; }

        [Tag("V75")]
        public IoValve V75 { get; set; }

        [Tag("V76")]
        public IoValve V76 { get; set; }
        [Tag("V97")]
        public IoValve V97 { get; set; }

        [Tag("EPV2")]
        public IoValve EPV2 { get; set; }
        #endregion

        #region MFC

        [Tag("Mfc1")]
        public IoMFC Mfc1 { get; set; }

        [Tag("Mfc2")]
        public IoMFC Mfc2 { get; set; }

        [Tag("Mfc3")]
        public IoMFC Mfc3 { get; set; }

        [Tag("Mfc4")]
        public IoMFC Mfc4 { get; set; }

        [Tag("Mfc5")]
        public IoMFC Mfc5 { get; set; }

        [Tag("Mfc6")]
        public IoMFC Mfc6 { get; set; }

        [Tag("Mfc7")]
        public IoMFC Mfc7 { get; set; }

        [Tag("Mfc8")]
        public IoMFC Mfc8 { get; set; }

        [Tag("Mfc9")]
        public IoMFC Mfc9 { get; set; }

        [Tag("Mfc10")]
        public IoMFC Mfc10 { get; set; }

        [Tag("Mfc11")]
        public IoMFC Mfc11 { get; set; }

        [Tag("Mfc12")]
        public IoMFC Mfc12 { get; set; }

        [Tag("Mfc13")]
        public IoMFC Mfc13 { get; set; }

        [Tag("Mfc14")]
        public IoMFC Mfc14 { get; set; }

        [Tag("Mfc15")]
        public IoMFC Mfc15 { get; set; }

        [Tag("Mfc16")]
        public IoMFC Mfc16 { get; set; }

        [Tag("Mfc19")]
        public IoMFC Mfc19 { get; set; }

        [Tag("Mfc20")]
        public IoMFC Mfc20 { get; set; }

        [Tag("Mfc22")]
        public IoMFC Mfc22 { get; set; }

        [Tag("Mfc23")]
        public IoMFC Mfc23 { get; set; }

        [Tag("Mfc25")]
        public IoMFC Mfc25 { get; set; }

        [Tag("Mfc26")]
        public IoMFC Mfc26 { get; set; }

        [Tag("Mfc27")]
        public IoMFC Mfc27 { get; set; }

        [Tag("Mfc28")]
        public IoMFC Mfc28 { get; set; }

        [Tag("Mfc29")]
        public IoMFC Mfc29 { get; set; }

        [Tag("Mfc40")]
        public IoMFC Mfc40 { get; set; }

        [Tag("Mfc31")]
        public IoMFC Mfc31 { get; set; }

        [Tag("Mfc32")]
        public IoMFC Mfc32 { get; set; }

        [Tag("Mfc33")]
        public IoMFC Mfc33 { get; set; }

        [Tag("Mfc35")]
        public IoMFC Mfc35 { get; set; }

        [Tag("Mfc36")]
        public IoMFC Mfc36 { get; set; }

        [Tag("Mfc37")]
        public IoMFC Mfc37 { get; set; }

        [Tag("Mfc38")]
        public IoMFC Mfc38 { get; set; }

        #endregion

        #region Pressure

        [Tag("Pressure1")]
        public IoPressure Pressure1 { get; set; }

        [Tag("Pressure2")]
        public IoPressure Pressure2 { get; set; }

        [Tag("Pressure3")]
        public IoPressure Pressure3 { get; set; }
        [Tag("Pressure4")]
        public IoPressure Pressure4 { get; set; }

        [Tag("Pressure5")]
        public IoPressure Pressure5 { get; set; }

        [Tag("Pressure6")]
        public IoPressure Pressure6 { get; set; }

        [Tag("Pressure7")]
        public IoPressure Pressure7 { get; set; }

        [Tag("PT1")]
        public IoPressure PT1 { get; set; }

        [Tag("PT2")]
        public IoPressure PT2 { get; set; }

        #endregion

        #region Sensor

        [Tag("SensorChamPressAboveATMSW")]
        public Devices.IoSensor SensorChamPressAboveATMSW { get; set; }

        [Tag("SensorDORPressATMSW")]
        public Devices.IoSensor SensorDORPressATMSW { get; set; }

        [Tag("SensorPMATMSW")]
        public Devices.IoSensor SensorPMATMSW { get; set; }

        [Tag("SensorSusceptorAtSafeSpeed")]
        public Devices.IoSensor SensorSusceptorAtSafeSpeed { get; set; }


        #endregion

        #region ChamberMoveBody

        [Tag("ChamberMoveBody")]
        public IoChamberMoveBody ChamberMoveBody { get; set; }

        #endregion

        #region SHLidSwing

        [Tag("SHLidSwing")]
        public IoLidSwing SHLidSwing { get; set; }


        [Tag("MiddleLidSwing")]
        public IoLidSwing MiddleLidSwing { get; set; }


        #endregion

        #region ConfinementRing

        [Tag("ConfinementRing")]
        public IoConfinementRing ConfinementRing { get; set; }

        #endregion

        #region Pump
        [Tag("Pump")]
        public Devices.IoPump Pump { get; set; }
        #endregion

        #region Sevro

        [Subscription("PM1.NAISServo.AlarmStatus")]
        public bool IsAlarmStatus { get; set; }

        [Subscription("PM1.NAISServo.PositionComplete")]
        public bool IsPositionComplete { get; set; }

        [Subscription("PM1.NAISServo.MotorBusy")]
        public bool IsMotorBusy { get; set; }

        #endregion

        #region Heater

        [Tag("TC1")]
        public IoTC TC1 { get; set; }

        [Tag("TC2")]
        public IoTC TC2 { get; set; }

        [Tag("PSU1")]
        public IoPSU PSU1 { get; set; }

        [Tag("PSU2")]
        public IoPSU PSU2 { get; set; }

        [Tag("PSU3")]
        public IoPSU PSU3 { get; set; }

        [Tag("SCR1")]
        public IoSCR SCR1 { get; set; }

        [Tag("SCR2")]
        public IoSCR SCR2 { get; set; }

        [Tag("SCR3")]
        public IoSCR SCR3 { get; set; }

        #endregion


        Func<MemberInfo, bool> _hasTagAttribute;
        Func<object, bool> _isTagAttribute;

        public override double ChamberPressure
        {
            get
            {
                if (PT1 != null)
                {
                    return PT1.FeedBack;
                }
                return 0;
            }
        }

        public IAdsPlc Plc { get; set; }

        private List<IDevice> _allModuleDevice = new List<IDevice>();

        private List<string> _mfc = new List<string>() { "Mfc1", "Mfc2", "Mfc3", "Mfc4", "Mfc5", "Mfc6", "Mfc7", "Mfc8",
                                                         "Mfc9", "Mfc10", "Mfc11", "Mfc12", "Mfc13", "Mfc14", "Mfc15", "Mfc16",
                                                         "Mfc19", "Mfc20","Mfc22", "Mfc23", "Mfc26", "Mfc27", "Mfc28", "Mfc29",
                                                         "Mfc31", "Mfc32", "Mfc33", "Mfc34", "Mfc35", "Mfc36", "Mfc37", "Mfc38"};

        private List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        public List<IoMFC> _mfcList
        {
            get
            {
                var list = new List<IoMFC>();
                list.Add(Mfc1);
                list.Add(Mfc2);
                list.Add(Mfc3);
                list.Add(Mfc4);
                list.Add(Mfc5);
                list.Add(Mfc6);
                list.Add(Mfc7);
                list.Add(Mfc8);
                list.Add(Mfc9);
                list.Add(Mfc10);
                list.Add(Mfc11);
                list.Add(Mfc12);
                list.Add(Mfc13);
                list.Add(Mfc14);
                list.Add(Mfc15);
                list.Add(Mfc16);
                list.Add(Mfc19);
                list.Add(Mfc20);
                list.Add(Mfc22);
                list.Add(Mfc23);
                list.Add(Mfc25);
                list.Add(Mfc26);
                list.Add(Mfc27);
                list.Add(Mfc28);
                list.Add(Mfc29);
                list.Add(Mfc31);
                list.Add(Mfc32);
                list.Add(Mfc33);
                list.Add(Mfc35);
                list.Add(Mfc36);
                list.Add(Mfc37);
                list.Add(Mfc38);

                return list;
            }
        }

        public List<IoPressure> _pcList
        {
            get
            {
                var list = new List<IoPressure>();
                list.Add(Pressure1);
                list.Add(Pressure2);
                list.Add(Pressure3);
                list.Add(Pressure4);
                list.Add(Pressure5);
                list.Add(Pressure6);
                list.Add(Pressure7);

                return list;
            }
        }


        public void InitDevice()
        {
            if (IsInstalled)
            {
                if (SC.GetValue<bool>("System.IsSimulatorMode"))
                {
                    Plc = DEVICE.GetOptionDevice($"{Module}.MainPLC", typeof(WcfPlc)) as IAdsPlc;

                    (Plc as WcfPlc).Initialize();
                }
                else
                {
                    Plc = DEVICE.GetOptionDevice($"{Module}.MainPLC", typeof(SicAds)) as IAdsPlc;

                    (Plc as SicAds).Initialize();
                }
            }

            if (Plc != null)
            {
                Plc.OnDeviceAlarmStateChanged += OnModuleDeviceAlarmStateChanged;
                Plc.OnConnected += PlcConnected;
                Plc.OnDisconnected += PlcDisconnected;

                _allModuleDevice.Add(Plc);
            }

            _isTagAttribute = attribute => attribute is TagAttribute;
            _hasTagAttribute = mi => mi.GetCustomAttributes(false).Any(_isTagAttribute);
            Parallel.ForEach(this.GetType().GetProperties().Where(_hasTagAttribute),
                field =>
                {
                    TagAttribute tag = field.GetCustomAttributes(false).First(_isTagAttribute) as TagAttribute;
                    IDevice device = DEVICE.GetDevice<IDevice>($"{Module}.{tag.Tag}");
                    device.OnDeviceAlarmStateChanged += OnModuleDeviceAlarmStateChanged;

                    _allModuleDevice.Add(device);

                    PropertyInfo pi = (PropertyInfo)field;

                    var convertedValue = Convert.ChangeType(device, pi.PropertyType);

                    System.Diagnostics.Debug.Assert(convertedValue != null);

                    pi.SetValue(this, convertedValue);
                });

            if (_pmInterLock == null)
            {
                _pmInterLock = DEVICE.GetDevice<SicPM.Devices.IoInterLock>($"{Module}.PMInterLock");
            }
        }

        private void StopRamp()
        {
            SetMfcStopRampByGroup(MfcGroupName.All);
            SetPCStopRamp(_lstPcList);
            SetHeaterStopRamp();
            SetTVStopRamp();
            SetRotationStopRamp();

            //_ioThrottleValve.StopRamp();
        }

        private void StopProcess()
        {
            SetHeatEnable(false);
            SetRotationServo(0, 1);

            SetIOValueByGroup(IoGroupName.B, false);
            SetIOValueByGroup(IoGroupName.C, false);
            SetIOValueByGroup(IoGroupName.E, false);
            SetIOValueByGroup(IoGroupName.F, false);
            SetIOValueByGroup(IoGroupName.H, false);
            SetIOValueByGroup(IoGroupName.K, false);
            SetIOValueByGroup(IoGroupName.A, true);

            SetMfcToDefaultByGroupRamp(MfcGroupName.M1to16, SC.GetConfigItem($"PM.{Module}.ProcessIdle.MFC1to16RampTime").IntValue);
            SetMfcToDefaultByGroupRamp(MfcGroupName.M2toM40, SC.GetConfigItem($"PM.{Module}.ProcessIdle.MFC19to40RampTime").IntValue);


        }

        private void PlcDisconnected()
        {
            CheckToPostMessage(MSG.Disconnected);
        }

        private void PlcConnected()
        {
            CheckToPostMessage(MSG.Connected);
        }

        private void OnModuleDeviceAlarmStateChanged(string deviceId, AlarmEventItem alarmItem)
        {
            if (!alarmItem.IsAcknowledged)
            {
                if (alarmItem.Level == EventLevel.Alarm)
                {
                    EV.PostAlarmLog(alarmItem.Source, alarmItem.Description);
                }
                else
                {
                    EV.PostWarningLog(alarmItem.Source, alarmItem.Description);
                }
            }
        }

        public bool CheckIsEableMonitor()
        {
            if (!IsAlarmStatus && !IsMotorBusy && IsPositionComplete)
                return true;
            return false;
        }
        public bool CheckServoAlarm()
        {
            return IsAlarmStatus;
        }
        public bool CheckServoIsBusy()
        {
            return IsMotorBusy;
        }
        public bool CheckServoPositionComplete()
        {
            return IsPositionComplete;
        }
        public override bool CheckAcked(int entityTaskToken)
        {
            return true;
        }
        public override void Terminate()
        {
        }

        public override void Reset()
        {

        }

        public override bool Home()
        {
            return true;
        }

        public override bool IsProcessed()
        {
            return true;
        }

        public override bool IsPrepareTransferReady(ModuleName robot, EnumTransferType type, int slot)
        {
            if (SC.GetValue<bool>("System.IsATMMode"))
            {
                return true;
            }
            else if (SC.GetConfigItem("TM.NeedPressureBalance").BoolValue)
            {
                if (Math.Abs(SC.GetConfigItem("TM.PressureBalance.BalancePressure").DoubleValue - GetChamberPressure()) > SC.GetConfigItem("TM.PressureBalance.BalanceMaxDiffPressure").DoubleValue)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool PrepareTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override int InvokePrepareTransfer(ModuleName robot, EnumTransferType type, int slot)
        {
            if (CheckToPostMessage((int)MSG.PrepareTransfer, robot, slot))
                return 0;
            return 0;
        }

        public override int InvokeProcess(string recipeName, bool isCleanRecipe, bool withWafer)
        {
            SC.SetItemValue($"PM.{Module}.LastRecipeName", recipeName); // 记录最后一次工艺文件名称

            CheckToPostMessage((int)MSG.RunRecipe, recipeName, isCleanRecipe, withWafer);

            return 0;
        }

        public override int InvokeCleanProcess(string recipeName)
        {
            CheckToPostMessage((int)MSG.CleanProcess, recipeName);
            return 0;
        }

        public override bool InvokeCheckHeaterDisable()
        {
            return CheckHeatEnableTC(false, true) && CheckHeatEnableTC(false, true);
        }

        public override int InvokeSetHeatDisable()
        {
            if (CheckToPostMessage((int)MSG.StopHeat))
                return 0;

            return 0;
        }

        #region pump device operation

        public override bool PreparePump(out string reason)
        {
            //if (!VentValve.TurnValve(false, out reason))
            //    return false;

            //if (!GasLine1.SetFlow(out reason, 0, 0))
            //    return false;

            //if (!GasLine2.SetFlow(out reason, 0, 0))
            //    return false;

            //if (!GasLine3.SetFlow(out reason, 0, 0))
            //    return false;

            //if (!GasLine4.SetFlow(out reason, 0, 0))
            //    return false;

            //if (!FinalValve.TurnValve(false, out reason))
            //    return false;

            //if (!Microwave.SetPowerOnOff(false, out reason))
            //    return false;

            //if (!Rf.SetPowerOnOff(false, out reason))
            //    return false;

            reason = string.Empty;
            return true;
        }

        public override bool CheckPreparePump()
        {
            //if (FinalValve.Status)
            //    return false;
            //if (VentValve.Status)
            //    return false;
            //if (Microwave.IsRfOn)
            //    return false;
            //if (Rf.IsRfOn)
            //    return false;
            return true;
        }


        public override bool TurnOnPump(out string reason)
        {
            reason = string.Empty;
            return false;//MainPump.SetPump(out reason, 0, true);
        }

        public override bool ShutDownPump(out string reason)
        {
            reason = string.Empty;
            return false; //MainPump.SetPump(out reason, 0, false);
        }

        public override bool CheckPumpIsOn()
        {
            return Pump.DryPump1Running;//MainPump.IsRunning;
        }

        public override bool SlowPump(int tvPosition, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override bool FastPump(int tvPosition, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override bool AbortPump()
        {
            return true;
        }



        #endregion

        #region vent device operation
        public override bool PrepareVent(out string reason)
        {
            //if (!ThrottleValve.SetMode(PressureCtrlMode.TVPositionCtrl, out reason))
            //{
            //    return false;
            //}

            //if (!ThrottleValve.SetPosition(0, out reason))
            //    return false;

            //if (!PumpValve.TurnValve(false, out reason))
            //    return false;

            //if (!GasLine1.SetFlow(out reason, 0, 0))
            //    return false;

            //if (!GasLine2.SetFlow(out reason, 0, 0))
            //    return false;

            //if (!GasLine3.SetFlow(out reason, 0, 0))
            //    return false;

            //if (!GasLine4.SetFlow(out reason, 0, 0))
            //    return false;

            //if (!FinalValve.TurnValve(false, out reason))
            //    return false;

            //if (!Microwave.SetPowerOnOff(false, out reason))
            //    return false;

            //if (!Rf.SetPowerOnOff(false, out reason))
            //    return false;

            reason = string.Empty;
            return true;
        }
        public override bool CheckPrepareVent()
        {
            //if (FinalValve.Status)
            //    return false;
            //if (PumpValve.Status)
            //    return false;
            //if (ThrottleValve.PositionFeedback >0.1)
            //    return false;
            //if (Microwave.IsRfOn)
            //    return false;
            //if (Rf.IsRfOn)
            //    return false;
            return true;
        }

        public override bool Vent(out string reason)
        {
            //if (!VentValve.TurnValve(true, out reason))
            //{
            //    LOG.Write(reason);
            //    return false;
            //}
            reason = string.Empty;
            return true;
        }

        public override bool StopVent(out string reason)
        {
            //if (!VentValve.TurnValve(false, out reason))
            //{
            //    LOG.Write(reason);
            //    return false;
            //}
            reason = string.Empty;
            return true;
        }

        #endregion

        public bool CheckIsConnected()
        {
            return Plc.CheckIsConnected();
        }

        public bool CheckHasAlarm()
        {
            foreach (var device in _allModuleDevice)
            {
                if (device.HasAlarm)
                    return true;
            }

            return false;
        }

        public override bool CheckSlitValveClose()
        {
            return false; //ChamberDoor.IsClose;
        }




        /// <summary>
        /// 获取组内的IoValue名称集合
        /// </summary>
        /// <param name="eGroupName"></param>6
        /// <returns></returns>
        public List<string> GetIoListByGroupName(IoGroupName eGroupName)
        {
            List<string> lst = new List<string>();
            if (eGroupName == IoGroupName.VentPump)
            {
                lst = new List<string>() { "V72" };
            }
            else if (eGroupName == IoGroupName.ArSupply)
            {
                lst = new List<string>() { "V32", "V35", "V36" };
            }
            else if (eGroupName == IoGroupName.V35V36)
            {
                lst = new List<string>() { "V35", "V36" };
            }
            else if (eGroupName == IoGroupName.GasSupply)
            {
                lst = new List<string>() { "V31", "V32", "V35", "V36" };
            }
            else if (eGroupName == IoGroupName.V31)
            {
                lst = new List<string>() { "V31" };
            }
            else if (eGroupName == IoGroupName.V32)
            {
                lst = new List<string>() { "V32" };
            }
            else if (eGroupName == IoGroupName.A)
            {
                lst = new List<string>() { "V33s", "V65" };
            }
            else if (eGroupName == IoGroupName.B)
            {
                lst = new List<string>() { "V33", "V64" };
            }
            else if (eGroupName == IoGroupName.C)
            {
                lst = new List<string>() { "V43", "V48" };
            }
            else if (eGroupName == IoGroupName.D)
            {
                lst = new List<string>() { "V45", "V49" };
            }
            else if (eGroupName == IoGroupName.E)
            {
                lst = new List<string>() { "V46", "V50" };
            }
            else if (eGroupName == IoGroupName.F)
            {
                lst = new List<string>() { "V39", "V40", "V41", "V53", "V54", "V55", "V59" };
            }
            else if (eGroupName == IoGroupName.G)
            {
                lst = new List<string>() { "V42", "V56", "V60" };
            }
            else if (eGroupName == IoGroupName.H)
            {
                lst = new List<string>() { "V51", "V52", "V58", "V37s" };
            }
            else if (eGroupName == IoGroupName.I)
            {
                lst = new List<string>() { "V87", "V88", "V89", "V97", "V90", "V91", "V92", "V93", "V94", "V95", "V96" };
            }
            else if (eGroupName == IoGroupName.J)
            {
                lst = new List<string>() { "V61", "V62", "V63" };
            }
            else if (eGroupName == IoGroupName.K)
            {
                lst = new List<string>() { "V69", "V73", "V74" };
            }
            else if (eGroupName == IoGroupName.Final1)
            {
                lst = new List<string>() { "V92", "V93", "V94", "V95", "V96" };
            }
            else if (eGroupName == IoGroupName.Final2)
            {
                lst = new List<string>() { "V87", "V88", "V97", "V90", "V89", "V91" };
            }
            else if (eGroupName == IoGroupName.GasIn1)
            {
                lst = new List<string> { "V68" };
            }
            else if (eGroupName == IoGroupName.GasIn2)
            {
                lst = new List<string> { "V33s", "V35", "V36" };
            }
            else if (eGroupName == IoGroupName.EPV2)
            {
                lst = new List<string> { "EPV2" };
            }
            else if (eGroupName == IoGroupName.V27)
            {
                lst = new List<string> { "V27" };
            }
            else if (eGroupName == IoGroupName.V75)
            {
                lst = new List<string> { "V75" };
            }
            else if (eGroupName == IoGroupName.V76)
            {
                lst = new List<string> { "V76" };
            }
            else if (eGroupName == IoGroupName.V69)
            {
                lst = new List<string> { "V69" };
            }
            else if (eGroupName == IoGroupName.V70)
            {
                lst = new List<string> { "V70" };
            }
            else if (eGroupName == IoGroupName.V25)
            {
                lst = new List<string> { "V25" };
            }
            else if (eGroupName == IoGroupName.V65)
            {
                lst = new List<string> { "V65" };
            }
            else if (eGroupName == IoGroupName.V68)
            {
                lst = new List<string> { "V68" };
            }
            else if (eGroupName == IoGroupName.V94)
            {
                lst = new List<string> { "V94" };
            }
            else if (eGroupName == IoGroupName.V92V93V95)
            {
                lst = new List<string> { "V92", "V93", "V95" };
            }
            else if (eGroupName == IoGroupName.V96)
            {
                lst = new List<string> { "V96" };
            }
            else if (eGroupName == IoGroupName.GroupIWithoutV94)
            {
                lst = new List<string> { "V87", "V88", "V89", "V97", "V90", "V91", "V92", "V93", "V95", "V96" };
            }
            else if (eGroupName == IoGroupName.All)
            {
                lst = new List<string> { "V32","V33",
                                        "V48","V49","V50","V45","V43","V46","V37s",
                                        "V39", "V40", "V41", "V51", "V52","V53","V54","V55","V58","V59","V60","V61",
                                        "V62", "V63", "V87", "V88","V89", "V97", "V90","V91","V92","V93","V94","V95","V96","V97","V75","V76",
                                        "V27", "V69", "V72", "V73", "V74","V64", "V65"};
            }
            return lst;
        }

        /// <summary>
        /// 获取组内的MFC名称集合
        /// </summary>
        /// <param name="mGroupName"></param>
        /// <returns></returns>
        public List<int> GetMfcListByGroupName(MfcGroupName mGroupName)
        {
            List<int> lst = new List<int>();
            if (mGroupName == MfcGroupName.Final1)
            {
                lst = new List<int>() { 35, 36, 37, 38 };
            }
            else if (mGroupName == MfcGroupName.Final2)
            {
                lst = new List<int>() { 27, 28, 29, 40, 33, 32, 31 };
            }
            else if (mGroupName == MfcGroupName.M1to16)
            {
                lst = new List<int>() { 1, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 16 };
            }
            else if (mGroupName == MfcGroupName.M2toM40)
            {
                lst = new List<int>() { 2, 9, 15, 19, 20, 22, 23, 25, 26, 27, 28, 29, 31, 32, 33, 35, 36, 37, 38, 40 };
            }
            else if (mGroupName == MfcGroupName.M2toM40NoFinal1MFC)
            {
                lst = new List<int>() { 2, 9, 15, 19, 20, 22, 23, 25, 26, 27, 28, 29, 31, 33, 40 };
            }
            else if (mGroupName == MfcGroupName.M27toM38)
            {
                lst = new List<int>() { 27, 28, 29, 40, 31, 32, 33, 35, 36, 37, 38 };
            }
            else if (mGroupName == MfcGroupName.M2toM26)
            {
                lst = new List<int>() { 19, 20, 22, 23, 25, 26, 2, 15, 9 };
            }
            else if (mGroupName == MfcGroupName.M19toM26)
            {
                lst = new List<int>() { 19, 20, 21, 22, 23, 24, 25, 26 };
            }
            else if (mGroupName == MfcGroupName.M27toM40)
            {
                lst = new List<int>() { 27, 28, 29, 31, 33, 32, 35, 36, 37, 38, 40 };
            }
            else if (mGroupName == MfcGroupName.M28293140)
            {
                lst = new List<int>() { 28, 29, 31, 40 };
            }
            else if (mGroupName == MfcGroupName.M19toM33)
            {
                lst = new List<int>() { 19, 20, 22, 23, 25, 26, 27, 28, 29, 40, 31, 32, 33, 2, 15, 9 };
            }
            else if (mGroupName == MfcGroupName.M32toM38)
            {
                lst = new List<int>() { 35, 36, 37, 38 };
            }
            else if (mGroupName == MfcGroupName.M36)
            {
                lst = new List<int>() { 36 };
            }
            else if (mGroupName == MfcGroupName.M38)
            {
                lst = new List<int>() { 38 };
            }
            else if (mGroupName == MfcGroupName.M32M35M37)
            {
                lst = new List<int>() { 32, 35, 37 };
            }
            else if (mGroupName == MfcGroupName.AllWithoutM28M29M31M40)
            {
                lst = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 19, 20, 22, 23, 25, 26, 27, 32, 33, 35, 36, 37, 38 };
            }
            else if (mGroupName == MfcGroupName.All)
            {
                lst = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 19, 20, 22, 23, 25, 26, 27, 28, 29, 40, 31, 32, 33, 35, 36, 37, 38 };
            }


            return lst;
        }

        /// <summary>
        /// 设置MFC到默认值
        /// </summary>
        /// <param name="lstMFCID"></param>
        /// <returns></returns>
        public bool SetMfcValueToDefault(List<int> lstMFCID)
        {
            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                device.SetToDefaultByRamp(0);
            }

            return true;
        }

        /// <summary>
        /// 设置MFC到默认值
        /// </summary>
        /// <param name="lstMFCID"></param>
        /// <returns></returns>
        public bool SetMfcValueToDefaultByRamp(List<int> lstMFCID, int time)
        {
            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                device.SetToDefaultByRamp(time);
            }

            return true;
        }

        public bool SetMfcPurgeValue(List<int> lstMFCID, string configName, int time)
        {
            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int mfcID in lstMFCID)
            {
                double value = SC.GetValue<double>($"PM.{Module}.{configName}.Mfc{mfcID}Flow");
                IoMFC device = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                device.Ramp(value, time);
            }

            return true;
        }

        /// <summary>
        /// 设置MFC的值
        /// </summary>
        /// <param name="lstMFCID"></param>
        /// <param name="toDefaultValue"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetMfcValue(List<int> lstMFCID, double value, int time)
        {
            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                device.Ramp(value, time);
            }

            return true;
        }

        public bool SetMfcModelToNormal(List<int> lstMFCID)
        {
            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                device.SetMfcMode(MfcCtrlMode.Normal, out string reason);
            }

            return true;
        }

        /// <summary>
        /// 设置MFC动态流量的值
        /// </summary>
        /// <param name="lstMFCID"></param>
        /// <param name="toDefaultValue"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetMfcValueRampByPress(List<int> lstMFCID, double press)
        {
            //检查MFC是否存在
            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");

                //根据特殊的参数获取特定的目标值和Ramp时间
                double setValue = 0;
                int time = 0;
                GetMfcDynamicFlowRampValueAndTime($"Mfc{mfcID}", press, out setValue, out time);

                device.Ramp(setValue, time * 1000);
            }

            return true;
        }

        /// <summary>
        /// MFC28,29,31,40设置为特定值
        /// </summary>
        /// <param name="mfc28Flow"></param>
        /// <param name="mfc29Flow"></param>
        /// <param name="mfc31Flow"></param>
        /// <param name="mfc40Flow"></param>
        /// <returns></returns>
        public bool SetMFC28to40Special(double mfc28Flow, double mfc29Flow, double mfc31Flow, double mfc40Flow, int rampTime)
        {
            IoMFC device28 = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc28");
            IoMFC device29 = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc29");
            IoMFC device31 = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc31");
            IoMFC device40 = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc40");

            if (device28 == null || device29 == null || device31 == null || device40 == null)
            {
                return false;
            }

            device28.Ramp(mfc28Flow, rampTime * 1000);
            device29.Ramp(mfc29Flow, rampTime * 1000);
            device31.Ramp(mfc31Flow, rampTime * 1000);
            device40.Ramp(mfc40Flow, rampTime * 1000);
            return true;
        }

        private void GetMfcDynamicFlowRampValueAndTime(string mfcName, double press, out double setValue, out int time)
        {
            setValue = 0;
            time = 0;
            try
            {
                string mfcFlowStr = "";
                string configStr = "";

                configStr = SC.GetConfigItem($"PM.{Module}.{mfcName}DynamicFlow").StringValue;


                string[] array = configStr.Split(',');
                if (array.Length >= 10)
                {
                    //int index = (int)press / 100;

                    //index = index >= 9 ? 9 : index;

                    //mfcFlowStr = array[index];

                    if (press >= 20 && press < 100)
                    {
                        mfcFlowStr = array[0];
                    }
                    else if (press >= 100 && press < 200)
                    {
                        mfcFlowStr = array[1];
                    }
                    else if (press >= 200 && press < 300)
                    {
                        mfcFlowStr = array[2];
                    }
                    else if (press >= 300 && press < 400)
                    {
                        mfcFlowStr = array[3];
                    }
                    else if (press >= 400 && press < 500)
                    {
                        mfcFlowStr = array[4];
                    }
                    else if (press >= 500 && press < 600)
                    {
                        mfcFlowStr = array[5];
                    }
                    else if (press >= 600 && press < 700)
                    {
                        mfcFlowStr = array[6];
                    }
                    else if (press >= 700 && press < 800)
                    {
                        mfcFlowStr = array[7];
                    }
                    else if (press >= 800 && press < 900)
                    {
                        mfcFlowStr = array[8];
                    }
                    else if (press >= 900)
                    {
                        mfcFlowStr = array[9];
                    }
                }

                string[] mfcDetail = mfcFlowStr.Split('*');
                if (mfcDetail.Length == 2)
                {
                    if (!double.TryParse(mfcDetail[0], out setValue))
                    {
                        setValue = 0;
                        time = 0;
                        return;
                    }
                    if (!Int32.TryParse(mfcDetail[1], out time))
                    {
                        time = 0;
                        return;
                    }
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// 设置MFC的值(按最大值的百分比)
        /// </summary>
        /// <param name="lstMFCID"></param>
        /// <param name="toDefaultValue"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetMfcValueByPercent(int mfcID, double percent)
        {

            IoMFC device = DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
            if (device == null)
            {
                return false;
            }

            double setPoint = device.Scale * percent / 100;
            device.Ramp(setPoint, 0);
            return true;
        }

        /// <summary>
        /// MFC停止Ramp
        /// </summary>
        /// <param name="lstMFCID"></param>
        /// <returns></returns>
        public bool SetMfcStopRamp(List<int> lstMFCID)
        {
            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int mfcID in lstMFCID)
            {
                IoMFC device = (IoMFC)DEVICE.GetDevice<IoMFC>($"{Module}.Mfc{mfcID}");
                device.StopRamp();
            }

            return true;
        }

        /// <summary>
        /// 设置PC到默认值
        /// </summary>
        /// <param name="lstPCID"></param>
        /// <returns></returns>
        public bool SetPCValueToDefault(List<int> lstPCID)
        {
            foreach (int pcID in lstPCID)
            {
                IoPressure device = DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int pcID in lstPCID)
            {
                IoPressure device = (IoPressure)DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                device.Terminate();
            }

            return true;
        }

        /// <summary>
        /// 设置PC,停止Ramp
        /// </summary>
        /// <param name="lstPCID"></param>
        /// <returns></returns>
        public bool SetPCStopRamp(List<int> lstPCID)
        {
            foreach (int pcID in lstPCID)
            {
                IoPressure device = DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int pcID in lstPCID)
            {
                IoPressure device = (IoPressure)DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                device.StopRamp();
            }

            return true;
        }

        public bool SetPcModelToNormal(List<int> lstPCID)
        {
            foreach (int pcID in lstPCID)
            {
                IoPressure device = DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int pcID in lstPCID)
            {
                IoPressure device = (IoPressure)DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                device.SetPcMode(PcCtrlMode.Normal, out string reason);
            }

            return true;
        }

        /// <summary>
        /// 设置PC模式
        /// </summary>
        /// <param name="lstPCID"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool SetPcModel(List<int> lstPCID, PcCtrlMode mode)
        {
            foreach (int pcID in lstPCID)
            {
                IoPressure device = DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int pcID in lstPCID)
            {
                IoPressure device = (IoPressure)DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                device.SetPcMode(mode, out string reason);
            }

            return true;
        }

        /// <summary>
        /// 设置MFC到默认值
        /// </summary>
        /// <param name="lstPCID"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetPCValue(List<int> lstPCID, double value)
        {
            foreach (int pcID in lstPCID)
            {
                IoPressure device = DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (int pcID in lstPCID)
            {
                IoPressure device = (IoPressure)DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
                device.Ramp(value);
            }

            return true;
        }

        /// <summary>
        /// 设置MFC到默认值(按百分比)
        /// </summary>
        /// <param name="lstPCID"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetPCValueByPercent(int pcID, double percent)
        {
            IoPressure device = DEVICE.GetDevice<IoPressure>($"{Module}.Pressure{pcID}");
            if (device == null)
            {
                return false;
            }

            double setPoint = device.MaxPressure * percent / 100;

            device.Ramp(setPoint);
            return true;
        }

        /// <summary>
        /// 设置IO Value
        /// </summary>
        /// <param name="lstPCID"></param>
        /// <returns></returns>
        public bool SetIoValue(List<string> lstIOID, bool open)
        {
            string reason;
            foreach (string IoID in lstIOID)
            {
                IoValve device = DEVICE.GetDevice<IoValve>($"{Module}.{IoID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (string IoID in lstIOID)
            {
                IoValve device = (IoValve)DEVICE.GetDevice<IoValve>($"{Module}.{IoID}");
                if (device.Status != open)
                {
                    device.TurnValve(open, out reason);
                }
            }

            return true;
        }

        /// <summary>
        /// 设置MFC到默认值
        /// </summary>
        /// <param name="lstPCID"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool CheckIoValue(List<string> lstPCID, bool open)
        {
            foreach (string IoID in lstPCID)
            {
                IoValve device = DEVICE.GetDevice<IoValve>($"{Module}.{IoID}");
                if (device == null)
                {
                    return false;
                }
            }

            foreach (string IoID in lstPCID)
            {
                IoValve device = (IoValve)DEVICE.GetDevice<IoValve>($"{Module}.{IoID}");

                if (device.Status != open)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 按组别名称对开关进行操作
        /// </summary>
        /// <param name="GroupName"></param>
        /// <param name="Open">Open=true</param>
        public bool SetIOValueByGroup(IoGroupName eGroupName, bool open)
        {
            List<string> lst = GetIoListByGroupName(eGroupName);
            return SetIoValue(lst, open);
        }

        /// <summary>
        /// 按组别名称Check开关的状态
        /// </summary>
        /// <param name="GroupName"></param>
        /// <param name="Open">Open=true</param>
        /// <returns></returns>
        public bool CheckIOValueByGroup(IoGroupName eGroupName, bool open)
        {
            List<string> lst = GetIoListByGroupName(eGroupName);
            return CheckIoValue(lst, open);
        }

        public void SetMfcModelToNormal(MfcGroupName mfcGroupName)
        {
            List<int> lst = GetMfcListByGroupName(mfcGroupName);
            SetMfcModelToNormal(lst);
        }

        /// <summary>
        /// 根据组别设置Mfc的值到默认值
        /// </summary>
        /// <param name="open"></param>
        /// <returns></returns>
        public bool SetMfcToDefaultByGroup(MfcGroupName mfcGroupName)
        {
            List<int> lst = GetMfcListByGroupName(mfcGroupName);
            return SetMfcValueToDefault(lst);
        }

        /// <summary>
        /// 根据组别设置Mfc的值到默认值(Ramp)
        /// </summary>
        /// <param name="open"></param>
        /// <returns></returns>
        public bool SetMfcToDefaultByGroupRamp(MfcGroupName mfcGroupName, int time)
        {
            List<int> lst = GetMfcListByGroupName(mfcGroupName);
            return SetMfcValueToDefaultByRamp(lst, time);
        }

        /// <summary>
        /// 根据组别设置Mfc,停止Ramp
        /// </summary>
        /// <param name="open"></param>
        /// <returns></returns>
        public bool SetMfcStopRampByGroup(MfcGroupName mfcGroupName)
        {
            List<int> lst = GetMfcListByGroupName(mfcGroupName);
            return SetMfcStopRamp(lst);
        }

        /// <summary>
        /// 根据组别设置Mfc的值(Ramp)
        /// </summary>
        /// <param name="open"></param>
        /// <returns></returns>
        public bool SetMfcRampByGroupAndPressure(MfcGroupName mfcGroupName, double pressure)
        {
            List<int> lst = GetMfcListByGroupName(mfcGroupName);
            return SetMfcValueRampByPress(lst, pressure);
        }

        /// <summary>
        /// 根据组别设置Mfc的值
        /// </summary>
        /// <param name="open"></param>
        /// <returns></returns>
        public bool SetMfcByGroup(MfcGroupName mfcGroupName, double dValue, int time)
        {
            List<int> lst = GetMfcListByGroupName(mfcGroupName);
            return SetMfcValue(lst, dValue, time);
        }

        /// <summary>
        /// 根据组别设置Mfc的值
        /// </summary>
        /// <param name="open"></param>
        /// <returns></returns>
        public bool SetMfcForPurgeConfigByGroup(MfcGroupName mfcGroupName, string configName, int time)
        {
            List<int> lst = GetMfcListByGroupName(mfcGroupName);
            return SetMfcPurgeValue(lst, configName, time);
        }

        /// <summary>
        /// 设置旋转电机速度(停止)
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public bool SetRotationServo(float speed, int time)
        {
            //if (_sicServo != null && _sicServo.ServoReady)
            if (_sicServo != null)
            {
                _sicServo.SetActualSpeed(speed, time);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check旋转电机速度是否达到设定以下
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public bool CheckRotationServoOn(double speed, bool bigThanSpeed)
        {
            // if (_sicServo != null && _sicServo.ServoReady)
            if (_sicServo != null)
            {
                if (bigThanSpeed)
                {
                    if (_sicServo.ActualSpeedFeedback >= (speed - 2))
                    {
                        return true;
                    }
                }
                else
                {
                    if (_sicServo.ActualSpeedFeedback <= speed)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// 加热
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public bool SetHeatEnable(bool enable)
        {
            if (SCR1 != null && SCR2 != null && SCR3 != null && PSU1 != null && PSU2 != null && PSU3 != null)
            {
                string reason = "";

                //PSU1.SetHeadHeaterEnable(enable, out reason);

                SCR1.SetEnable(enable, out reason);
                SCR2.SetEnable(enable, out reason);
                SCR3.SetEnable(enable, out reason);

                PSU1.SetPSUEnable(enable, out reason);
                PSU2.SetPSUEnable(enable, out reason);
                PSU3.SetPSUEnable(enable, out reason);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Check加热Enable状态
        /// </summary>
        /// <returns></returns>
        public bool CheckHeatEnable(bool enable)
        {
            if (SCR1 != null && SCR2 != null && SCR3 != null && PSU1 != null && PSU2 != null && PSU3 != null)
            {
                //if (PSU1.AllHeatEnable != enable)
                //    return false; 

                if (PSU1.StatusFeedBack != enable)
                    return false;

                if (PSU2.StatusFeedBack != enable)
                    return false;

                if (PSU3.StatusFeedBack != enable)
                    return false;

                if (SCR1.StatusFeedBack != enable)
                    return false;

                if (SCR2.StatusFeedBack != enable)
                    return false;

                if (SCR3.StatusFeedBack != enable)
                    return false;

                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查总的Enable是否开启
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public bool CheckHeadHeaterEnable(bool enable)
        {
            if (PSU1 != null)
            {
                return PSU1.AllHeatEnable == enable;
            }
            return false;
        }

        /// <summary>
        /// SCR Reset
        /// </summary>
        /// <returns></returns>
        public bool SetSCRReset()
        {
            if (SCR1 != null && SCR2 != null && SCR3 != null)
            {
                SCR1.SetReset(true, out string reason);
                SCR2.SetReset(true, out reason);
                SCR3.SetReset(true, out reason);
            }
            return true;
        }

        /// <summary>
        /// TC1和TC2单独设置
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="tc1"></param>
        /// <returns></returns>
        public bool SetHeatEnableTC(bool enable, bool tc1)
        {
            if (tc1 && PSU1 != null && PSU2 != null && PSU3 != null)
            {
                string reason = "";
                PSU1.SetPSUEnable(enable, out reason);
                PSU2.SetPSUEnable(enable, out reason);
                PSU3.SetPSUEnable(enable, out reason);
                return true;
            }
            else if (SCR1 != null && SCR2 != null && SCR3 != null)
            {
                string reason = "";
                SCR1.SetEnable(enable, out reason);
                SCR2.SetEnable(enable, out reason);
                SCR3.SetEnable(enable, out reason);
                return true;

            }
            return false;
        }

        /// <summary>
        /// Check使能状态 TC1和TC2单独设置
        /// </summary>
        /// <returns></returns>
        public bool CheckHeatEnableTC(bool enable, bool tc1)
        {
            if (tc1 && PSU1 != null && PSU2 != null && PSU3 != null)
            {
                if (PSU1.StatusFeedBack != enable)
                    return false;

                if (PSU2.StatusFeedBack != enable)
                    return false;

                if (PSU3.StatusFeedBack != enable)
                    return false;

                return true;
            }
            else if (SCR1 != null && SCR2 != null && SCR3 != null)
            {
                if (SCR1.StatusFeedBack != enable)
                    return false;

                if (SCR2.StatusFeedBack != enable)
                    return false;

                if (SCR3.StatusFeedBack != enable)
                    return false;

                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取PM的腔体压力
        /// </summary>
        /// <returns></returns>
        public double GetChamberPressure()
        {
            if (PT1 != null)
            {
                return PT1.FeedBack;
            }
            return 0;
        }



        /// <summary>
        /// 初始化Device的委托
        /// </summary>
        private void InitalDeviceFunc()
        {

            if (ChamberMoveBody != null)
            {
                ChamberMoveBody.FuncCheckSwingUnlock = () =>
                {
                    if (SHLidSwing != null && MiddleLidSwing != null)
                    {
                        return SHLidSwing.LidUnlockFaceback || MiddleLidSwing.LidUnlockFaceback;
                    }
                    else
                    {
                        return false;
                    }
                };

                ChamberMoveBody.FuncUpDownEnable = (setValue) =>
                {
                    if (setValue)
                    {
                        if (_pmInterLock != null && SensorPMATMSW != null && SensorDORPressATMSW != null && _sicServo != null)
                        {
                            if (!CheckIOValueByGroup(IoGroupName.H, false))
                            {
                                EV.PostWarningLog(Module, "Condition:V51,V52,V58,V37s should be Closed ");
                                return false;
                            }
                            if (!CheckIOValueByGroup(IoGroupName.F, false))
                            {
                                EV.PostWarningLog(Module, "Condition:V39,V40,V41,V53,V54,V55,V59 should be Closed ");
                                return false;
                            }
                            if (!_pmInterLock.DoLidOpenRoutineSucceed)
                            {
                                EV.PostWarningLog(Module, "Condition:LidOpenRoutineSucceed DO-172");
                                return false;
                            }
                            if (SensorPMATMSW.Value)
                            {
                                EV.PostWarningLog(Module, "Condition:DI-9 Chamber At ATM !");
                                return false;
                            }
                            if (!SensorDORPressATMSW.Value)
                            {
                                EV.PostWarningLog(Module, "Condition:DI-7 Dor Press At ATM !");
                                return false;
                            }
                            if (!IsServiceMode)
                            {
                                EV.PostWarningLog(Module, "Condition:ServiceMode");
                                return false;
                            }
                            if (_sicServo.ActualSpeedFeedback > 0)
                            {
                                EV.PostWarningLog(Module, "Condition:Rotation stopped AI-118");
                                return false;
                            }
                            if (!SensorSusceptorAtSafeSpeed.Value) //DI-13
                            {
                                EV.PostWarningLog(Module, "Condition:DI-13 Susceptor Not At Safe Speed");
                                return false;
                            }
                            if (PSU1.AllHeatEnable)
                            {
                                EV.PostWarningLog(Module, "Condition:PSU should be Disable");
                                return false;
                            }
                            //DI-205->V72,这里没设置条件;DI-14，IO配置文件里面没有定义；所以指添加了一个DI-13;
                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:device model is not null");
                            return false;
                        }
                    }
                    return true;
                };
            } //Chamber


            #region Enable Table中起保护作用的内容

            ////4.C (MO Line valve)(DO13 V43 常闭)(DO18 V48 常闭) {V43s,V48s同步增加条件}
            if (V43 != null)
            {
                V43.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (_pmInterLock != null)
                        {
                            if (!(_pmInterLock.DoExchangeMoRoutineRunning || _pmInterLock.DoLidCloseRoutineSucceed))
                            {
                                EV.PostWarningLog(Module, "Condition:ExchangeMo routine running or LidClose routine finished!");
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制关阀
                V43.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        if (!_pmInterLock.DoExchangeMoRoutineRunning && !_pmInterLock.DoLidCloseRoutineSucceed)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                };
            }
            if (V43s != null)
            {
                V43s.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (_pmInterLock != null)
                        {
                            if (!(_pmInterLock.DoExchangeMoRoutineRunning || _pmInterLock.DoLidCloseRoutineSucceed))
                            {
                                EV.PostWarningLog(Module, "Condition:ExchangeMo routine running or LidClose routine finished!");
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                };
            }
            if (V48 != null)
            {
                V48.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (_pmInterLock != null)
                        {
                            if (!(_pmInterLock.DoExchangeMoRoutineRunning || _pmInterLock.DoLidCloseRoutineSucceed))
                            {
                                EV.PostWarningLog(Module, "Condition:ExchangeMo routine running or LidClose routine finished!");
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制关阀
                V48.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        if (!_pmInterLock.DoExchangeMoRoutineRunning && !_pmInterLock.DoLidCloseRoutineSucceed)
                        {
                            return true;
                        }
                    }
                    return false;
                };
            }
            if (V48s != null)
            {
                V48s.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (_pmInterLock != null)
                        {
                            if (!(_pmInterLock.DoExchangeMoRoutineRunning || _pmInterLock.DoLidCloseRoutineSucceed))
                            {
                                EV.PostWarningLog(Module, "Condition:ExchangeMo routine running or LidClose routine finished!");
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                };
            }

            //5.IsolateValve2 (DO50 EPV2) 
            if (EPV2 != null)
            {
                EPV2.FuncCheckInterLock = (setValue) =>
                {
                    if (PT1 != null && PT2 != null && _ioThrottleValve != null)
                    {

                        if (!setValue)
                        {
                            if (_ioThrottleValve.TVValveEnable)
                            {
                                EV.PostWarningLog(Module, "Condition:TV should be disable!");
                                return false;
                            }
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                };
            }


            //6.GasBox.Vent.Pump (DO45 V72常闭)
            if (V72 != null)
            {
                V72.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (EPV2 != null && _ioThrottleValve != null)
                        {

                            if (!EPV2.Status)
                            {
                                EV.PostWarningLog(Module, "Condition:EPV2 should be open!");
                                return false;
                            }
                            if (!_ioThrottleValve.TVValveEnable)
                            {
                                EV.PostWarningLog(Module, "Condition:TV should be enable!");
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制关阀
                V72.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (EPV2 != null && _ioThrottleValve != null)
                        {
                            if (!EPV2.Status || !_ioThrottleValve.TVValveEnable)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                };
            }

            //13.MO Source.VAc (DO16 V46常闭,DO20 V50常闭) [DI206 V73 常闭]  {V46s,V50s同步增加条件}
            if (V46 != null)
            {
                V46.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (V73 != null && V43 != null && _pmInterLock != null)
                        {
                            if (V43.Status)
                            {
                                EV.PostWarningLog(Module, "Condition:V43 should be closed");
                                return false;
                            }
                            if (!IsServiceMode)
                            {
                                EV.PostWarningLog(Module, "Condition:PM should in service mode");
                                return false;
                            }
                            if (!(V73.Status || !_pmInterLock.DoExchangeMoRoutineRunning))
                            {
                                EV.PostWarningLog(Module, "Condition:V73 is open or Not ExchangeMo routine running ");
                                return false;
                            }
                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:V43,V73,V48 is not null!");
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制关阀
                V46.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (V73 != null && V43 != null && _pmInterLock != null)
                        {
                            if (V43.Status || !IsServiceMode || (!(V73.Status || !_pmInterLock.DoExchangeMoRoutineRunning)))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                };
            }
            if (V46s != null)
            {
                V46s.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (V73 != null && V43 != null && _pmInterLock != null)
                        {
                            if (V43.Status)
                            {
                                EV.PostWarningLog(Module, "Condition:V43 should be closed");
                                return false;
                            }
                            if (!IsServiceMode)
                            {
                                EV.PostWarningLog(Module, "Condition:PM should in service mode");
                                return false;
                            }
                            if (!(V73.Status || !_pmInterLock.DoExchangeMoRoutineRunning))
                            {
                                EV.PostWarningLog(Module, "Condition:V73 is open or Not ExchangeMo routine running ");
                                return false;
                            }
                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:V43,V73,V48 is not null!");
                            return false;
                        }
                    }
                    return true;
                };
            }
            if (V50 != null)
            {
                V50.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (V73 != null && V48 != null && _pmInterLock != null)
                        {
                            if (V48.Status)
                            {
                                EV.PostWarningLog(Module, "Condition:V48 should be closed");
                                return false;
                            }
                            if (!IsServiceMode)
                            {
                                EV.PostWarningLog(Module, "Condition:PM should in service mode");
                                return false;
                            }
                            if (!(V73.Status || !_pmInterLock.DoExchangeMoRoutineRunning))
                            {
                                EV.PostWarningLog(Module, "Condition:V73 be open or Not ExchangeMo routine running ");
                                return false;
                            }
                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:V73,V43,V48 is not null!");
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制关阀
                V50.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (V73 != null && V48 != null && _pmInterLock != null)
                        {
                            if (V48.Status || !IsServiceMode || (!(V73.Status || !_pmInterLock.DoExchangeMoRoutineRunning)))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                };

            }
            if (V50s != null)
            {
                V50s.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (V73 != null && V48 != null && _pmInterLock != null)
                        {
                            if (V48.Status)
                            {
                                EV.PostWarningLog(Module, "Condition:V48 should be closed");
                                return false;
                            }
                            if (!IsServiceMode)
                            {
                                EV.PostWarningLog(Module, "Condition:PM should in service mode");
                                return false;
                            }
                            if (!(V73.Status || !_pmInterLock.DoExchangeMoRoutineRunning))
                            {
                                EV.PostWarningLog(Module, "Condition:V73 be open or Not ExchangeMo routine running ");
                                return false;
                            }
                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:V73,V43,V48 should not be null!");
                            return false;
                        }
                    }
                    return true;
                };
            }

            if (V64 != null)
            {
                V64.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (PT1 != null)
                        {
                            if (PT1.FeedBack > SC.GetValue<double>($"PM.{Module}.PressureForV27ForceClose") && !IsServiceMode) //if (!IsServiceMode && SensorPMATMSW.Value)
                            {
                                EV.PostWarningLog(Module, "Condition:React below atm while in non-service mode!");
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制关阀
                V64.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (PT1 != null)
                        {
                            if (PT1.FeedBack > SC.GetValue<double>($"PM.{Module}.PressureForV27ForceClose") && !IsServiceMode) //if (!IsServiceMode && SensorPMATMSW.Value)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                };
            }

            //Lid Ar Purge (DO36 V65常开)[V64常闭]
            if (V65 != null)
            {
                V65.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (V64 != null)
                        {
                            if (!(!V64.Status || IsServiceMode))
                            {
                                EV.PostWarningLog(Module, "Condition:V64 should be Closed or ServiceMode");
                                return false;
                            }

                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:V64 is not null!");
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制关阀
                V65.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (V64 != null)
                        {
                            if (!(!V64.Status || IsServiceMode))/* || (!IsServiceMode && !SensorPMATMSW.Value))*/
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                };
            }

            //14.DOR Vac (DO48 V75常闭)[V76常闭]
            if (V75 != null)
            {
                V75.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (V76 != null)
                        {
                            if (V76.Status)
                            {
                                EV.PostWarningLog(Module, "Condition:V76 should be closed");
                                return false;
                            }
                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:V76 should be not null");
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制关阀
                V75.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (V76 != null)
                        {
                            if (V76.Status)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                };
            }

            //15.DOR Refill (DO49 V76常闭)
            if (V76 != null)
            {
                V76.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (SensorPMATMSW != null && _pmInterLock != null && V75 != null)
                        {
                            if (SensorPMATMSW.Value) //DI9=1 
                            {
                                EV.PostWarningLog(Module, "Condition:DI-4 PM AT ATM ");
                                return false;
                            }
                            if (!_pmInterLock.DoLidOpenRoutineSucceed)
                            {
                                EV.PostWarningLog(Module, "Condition:LidOpenRoutineSucceed should be on ");
                                return false;
                            }
                            if (V75.Status)
                            {
                                EV.PostWarningLog(Module, "Condition:V75 should be Closed!");
                                return false;
                            }
                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:SensorPMATMSW should be not null");
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制关阀
                V76.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (SensorPMATMSW != null)
                        {
                            if (SensorPMATMSW.Value)
                            {
                                return true;
                            }
                        }
                        //V76和V75只能开一个
                        if (V75 != null)
                        {
                            if (V75.Status)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                };
            }

            //16.17.PM pump bypass valve force open (DO52 V27) Checked
            if (V27 != null)
            {
                //返回False,表明条件不满足,限制住不开阀
                V27.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (PT1 != null)
                        {
                            if (PT1.FeedBack <= SC.GetValue<double>($"PM.{Module}.PressureForV27ForceClose"))
                            {
                                EV.PostWarningLog(Module, $"Condition:Chamber Pressure should larger than {SC.GetValue<double>($"PM.{Module}.PressureForV27ForceClose")}mabr");
                                return false;
                            }
                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:SensorPMATMSW is not null");
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制开阀
                V27.FuncForceOpen = (curStatue) =>
                {
                    if (!curStatue)
                    {
                        //满足条件，强制执行
                        if (SensorChamPressAboveATMSW != null && PT1 != null)
                        {
                            if (!SensorChamPressAboveATMSW.Value || PT1.FeedBack > SC.GetValue<double>($"PM.{Module}.PressureForV27ForceOpen"))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (PT1 != null)
                        {
                            if (PT1.FeedBack <= SC.GetValue<double>($"PM.{Module}.PressureForV27ForceClose"))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return false;
                };
            }


            //23.Reactor Leak Check  (DO42)
            if (V69 != null)
            {
                //返回False,表明条件不满足,限制住不开阀
                V69.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (V27 != null)
                        {
                            if (V27.Status)
                            {
                                EV.PostWarningLog(Module, $"Condition:V27 should be closed");
                                return false;
                            }
                            if (!CheckIOValueByGroup(IoGroupName.E, false))
                            {
                                EV.PostWarningLog(Module, $"Condition:E valves should be closed");
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制开阀
                V69.FuncForceOpen = (curStatue) =>
                {
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (V27 != null)
                        {
                            if (V27.Status || !CheckIOValueByGroup(IoGroupName.E, false))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return false;
                };
            }

            //24.H valvas


            //25.GasBox.Vent.Bypass open enable（DO50）
            if (V25 != null)
            {
                //返回False,表明条件不满足,限制住不开阀
                V25.FuncCheckInterLock = (setValue) =>
                {
                    if (setValue)
                    {
                        if (V72 != null)
                        {
                            if (V72.Status)
                            {
                                EV.PostWarningLog(Module, "Condition:V72 should be closed");
                                return false;
                            }
                        }
                        else
                        {
                            EV.PostWarningLog(Module, "Condition:V72 is not null");
                            return false;
                        }
                    }
                    return true;
                };

                //返回True，表明条件满足，强制开阀
                V25.FuncForceOpen = (curStatue) =>
                {
                    if (!curStatue)
                    {
                        //满足条件，强制执行
                        if (_pmInterLock != null && V72 != null)
                        {
                            if (!V72.Status && (_pmInterLock.DoPreprocessRunning || _pmInterLock.DoProcessRunning || _pmInterLock.DoProcessIdleRunning))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    if (curStatue)
                    {
                        //满足条件，强制执行
                        if (_pmInterLock != null && V72 != null)
                        {
                            if (V72.Status)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return false;
                };
            }


            #endregion Enable Table中起保护作用的内容


            #region 以下内容为InterLock表中复合条件内容

            ////10.TMA.Vent (DO12 V42常闭)[V41常闭]
            //if (V42 != null)
            //{

            //    V42.FuncCheckInterLock = (setValue) =>
            //    {
            //        if (setValue)
            //        {

            //            if (V41 != null)
            //            {
            //                if (V41.Status && !_pmInterLock.DoProcessRunning)
            //                {
            //                    EV.PostWarningLog(Module, "Condition:V41 is Closed while process running");
            //                    return false;
            //                }
            //            }
            //            else
            //            {
            //                EV.PostWarningLog(Module, "Condition:V41 is not null!");
            //                return false;
            //            }
            //        }
            //        return true;
            //    };

            //    //返回True，表明条件满足，强制关阀
            //    V42.FuncForceOpen = (curStatue) =>
            //    {
            //        if (curStatue)
            //        {
            //            //满足条件，强制执行
            //            if (V41 != null)
            //            {
            //                if (V41.Status && !_pmInterLock.DoProcessRunning)
            //                {
            //                    return true;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return false;
            //    };
            //}

            ////11.SiH4.Vent(DO26 V56常开)[V55常闭]
            //if (V56 != null)
            //{

            //    V56.FuncCheckInterLock = (setValue) =>
            //    {
            //        if (setValue)
            //        {
            //            if (V55 != null)
            //            {
            //                if (V55.Status && !_pmInterLock.DoProcessRunning)
            //                {
            //                    EV.PostWarningLog(Module, "Condition:V55 is Closed while process running");
            //                    return false;
            //                }
            //            }
            //            else
            //            {
            //                EV.PostWarningLog(Module, "Condition:V55 is not null!");
            //                return false;
            //            }
            //        }
            //        return true;
            //    };

            //    //返回True，表明条件满足，强制关阀
            //    V56.FuncForceOpen = (curStatue) =>
            //    {
            //        if (curStatue)
            //        {
            //            //满足条件，强制执行
            //            if (V55 != null)
            //            {
            //                if (V55.Status && !_pmInterLock.DoProcessRunning)
            //                {
            //                    return true;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return false;
            //    };
            //}

            ////12.C2H4.Vent (DO30 V60常开)[V59常闭]
            //if (V60 != null)
            //{

            //    V60.FuncCheckInterLock = (setValue) =>
            //    {
            //        if (setValue)
            //        {
            //            if (V59 != null)
            //            {
            //                if (V59.Status && !_pmInterLock.DoProcessRunning)
            //                {
            //                    EV.PostWarningLog(Module, "Condition:V59 is Closed while process running");
            //                    return false;
            //                }
            //            }
            //            else
            //            {
            //                EV.PostWarningLog(Module, "Condition:V59 is not null!");
            //                return false;
            //            }
            //        }
            //        return true;
            //    };

            //    //返回True，表明条件满足，强制关阀
            //    V60.FuncForceOpen = (curStatue) =>
            //    {
            //        if (curStatue)
            //        {
            //            //满足条件，强制执行
            //            if (V59 != null)
            //            {
            //                if (V59.Status && !_pmInterLock.DoProcessRunning)
            //                {
            //                    return true;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return false;
            //    };
            //}




            ////InterLock Only in non-service mode
            //if (V33 != null)
            //{
            //    V33.FuncCheckInterLock = (setValue) =>
            //    {
            //        if (setValue)
            //        {
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    EV.PostWarningLog(Module, "Condition:React below atm while in non-service mode!");
            //                    return false;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return true;
            //    };

            //    //返回True，表明条件满足，强制关阀
            //    V33.FuncForceOpen = (curStatue) =>
            //    {
            //        if (curStatue)
            //        {
            //            //满足条件，强制执行
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    return true;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return false;
            //    };
            //}


            ////InterLock Only in non-service mode
            //if (V37s != null)
            //{
            //    V37s.FuncCheckInterLock = (setValue) =>
            //    {
            //        if (setValue)
            //        {
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    EV.PostWarningLog(Module, "Condition:React below atm while in non-service mode!");
            //                    return false;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return true;
            //    };

            //    //返回True，表明条件满足，强制关阀
            //    V37s.FuncForceOpen = (curStatue) =>
            //    {
            //        if (curStatue)
            //        {
            //            //满足条件，强制执行
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    return true;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return false;
            //    };
            //}

            ////InterLock Only in non-service mode
            //if (V51 != null)
            //{
            //    V51.FuncCheckInterLock = (setValue) =>
            //    {
            //        if (setValue)
            //        {
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    EV.PostWarningLog(Module, "Condition:React below atm while in non-service mode!");
            //                    return false;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return true;
            //    };

            //    //返回True，表明条件满足，强制关阀
            //    V51.FuncForceOpen = (curStatue) =>
            //    {
            //        if (curStatue)
            //        {
            //            //满足条件，强制执行
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    return true;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return false;
            //    };
            //}

            ////InterLock Only in non-service mode
            //if (V52 != null)
            //{
            //    V52.FuncCheckInterLock = (setValue) =>
            //    {
            //        if (setValue)
            //        {
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    EV.PostWarningLog(Module, "Condition:React below atm while in non-service mode!");
            //                    return false;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return true;
            //    };

            //    //返回True，表明条件满足，强制关阀
            //    V52.FuncForceOpen = (curStatue) =>
            //    {
            //        if (curStatue)
            //        {
            //            //满足条件，强制执行
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    return true;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return false;
            //    };
            //}

            ////InterLock Only in non-service mode
            //if (V58 != null)
            //{
            //    V58.FuncCheckInterLock = (setValue) =>
            //    {
            //        if (setValue)
            //        {
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    EV.PostWarningLog(Module, "Condition:React below atm while in non-service mode!");
            //                    return false;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return true;
            //    };

            //    //返回True，表明条件满足，强制关阀
            //    V58.FuncForceOpen = (curStatue) =>
            //    {
            //        if (curStatue)
            //        {
            //            //满足条件，强制执行
            //            if (SensorPMATMSW != null)
            //            {
            //                if (!IsServiceMode && SensorPMATMSW.Value)
            //                {
            //                    return true;
            //                }
            //            }
            //            else
            //            {
            //                return false;
            //            }
            //        }
            //        return false;
            //    };
            //}
            #endregion 以下内容为InterLock表中复合条件内容
        }

        public void SetHeaterStopRamp()
        {
            TC1.StopRamp();
            TC2.StopRamp();
        }

        public void SetTVStopRamp()
        {
            _ioThrottleValve.StopRamp();
        }

        public void SetRotationStopRamp()
        {
            _sicServo.StopRamp();
        }

        public override bool CheckPreProcessCondition(Dictionary<string, string> recipeCommands, out string reason)
        {
            reason = string.Empty;

            double.TryParse(recipeCommands["TV.SetPressure"], out double pressure);
            if ((PT1.FeedBack > pressure + 2) || (PT1.FeedBack < pressure - 2))
            {
                reason = $"Current chamber pressure is {PT1.FeedBack} mbar, set pressure is {pressure} mbar";
                return false;
            }

            double.TryParse(recipeCommands["PMServo.SetActualSpeed"], out double servoSpeed);
            if ((_sicServo.ActualSpeedFeedback > servoSpeed + 2) || (_sicServo.ActualSpeedFeedback < servoSpeed - 2))
            {
                reason = $"Current Servo speed is {_sicServo.ActualSpeedFeedback}, set Servo speed is {servoSpeed}";
                return false;
            }

            //string heaterControlMode = recipeCommands["HeaterControlMode"];

            //if (heaterControlMode == HeaterControlMode.Power.ToString())
            //{
            //    //double.TryParse(recipeCommands[""], out double x);
            //}
            //else if (heaterControlMode == HeaterControlMode.Pyro.ToString())
            //{
            //    //double.TryParse(recipeCommands[""], out double x);
            //}
            //else if (heaterControlMode == HeaterControlMode.TC.ToString())
            //{
            //    //double.TryParse(recipeCommands[""], out double x);
            //}


            return true;
        }

        public override bool CloseHeaterEnable(out string reason)
        {
            reason = string.Empty;

            if (!PSU1.SetPSUEnable(false, out reason))
                return false;

            if (!PSU2.SetPSUEnable(false, out reason))
                return false;

            if (!PSU3.SetPSUEnable(false, out reason))
                return false;

            if (!SCR1.SetEnable(false, out reason))
                return false;

            if (!SCR2.SetEnable(false, out reason))
                return false;

            if (!SCR3.SetEnable(false, out reason))
                return false;

            return true;
        }


        public override bool EnableHeater(bool enable, out string reason)
        {
            reason = string.Empty;

            //如果之前开启过了就不需要归0
            if (!enable || !PSU1.CheckPSUEnable() || !PSU2.CheckPSUEnable() || !PSU3.CheckPSUEnable())
            {
                if (!TC1.SetTargetSPAll(0, 0, 0, 1))
                    return false;

                TC1.RecipeSetPowerRef(0, 1);

                if (!TC1.RecipeSetRatio("L1", 0, 1))
                    return false;

                if (!TC1.RecipeSetRatio("L2", 0, 1))
                    return false;

                if (!TC1.RecipeSetRatio("L3", 0, 1))
                    return false;
            }

            if (!enable || !SCR1.CheckSCREnable() || !SCR2.CheckSCREnable() || !SCR3.CheckSCREnable())
            {
                if (!TC2.SetTargetSPAll(0, 0, 0, 1))
                    return false;

                TC2.RecipeSetPowerRef(0, 1);

                if (!TC2.RecipeSetRatio("L1", 0, 1))
                    return false;

                if (!TC2.RecipeSetRatio("L2", 0, 1))
                    return false;

                if (!TC2.RecipeSetRatio("L3", 0, 1))
                    return false;
            }

            //if (!PSU1.SetHeadHeaterEnable(true, out reason))
            //    return false;

            if (!PSU1.SetPSUEnable(true, out reason))
                return false;

            if (!PSU2.SetPSUEnable(true, out reason))
                return false;

            if (!PSU3.SetPSUEnable(true, out reason))
                return false;

            if (!SCR1.SetReset(true, out reason))
                return false;

            if (!SCR2.SetReset(true, out reason))
                return false;

            if (!SCR3.SetReset(true, out reason))
                return false;

            if (!SCR1.SetEnable(true, out reason))
                return false;

            if (!SCR2.SetEnable(true, out reason))
                return false;

            if (!SCR3.SetEnable(true, out reason))
                return false;

            return true;
        }

        public override bool CheckHeaterEnable()
        {
            if (!PSU1.AllHeatEnable)
                return false;

            if (!PSU1.StatusFeedBack)
                return false;

            if (!PSU2.StatusFeedBack)
                return false;

            if (!PSU3.StatusFeedBack)
                return false;

            if (!SCR1.StatusFeedBack)
                return false;

            if (!SCR2.StatusFeedBack)
                return false;

            if (!SCR3.StatusFeedBack)
                return false;

            return true;
        }

        public override bool CheckPlacetoPMTemp()
        {
            if(TC1.MiddleTemp <= SC.GetValue<double>($"PM.{_module}.Heater.PlacePVMiddleTempLimit"))
            {
                return true;
            }

            return false;
        }

        public override bool SetRotationEnable(bool enable, out string reason)
        {
            reason = string.Empty;

            if (!_sicServo.SetServoEnable(enable, out reason))
                return false;

            return true;
        }

        public override bool CheckRotationEnable()
        {
            //if (!_sicServo.ServoEnable || !_sicServo.ServoReady || _sicServo.ServoError)
            if (!_sicServo.ServoEnable || _sicServo.ServoError)
            return false;

            return true;
        }

        /// <summary>
        /// SignalTower SwitchOffBuzzerEx
        /// </summary>
        /// <param name="args">bool isOff, int iCount, int iInterval = 500, int iRemainTime=1000</param>
        /// <returns></returns>
        public bool SwitchOffBuzzerEx(object[] args)
        {
            try
            {
                switch (args.Length)
                {
                    case 1:
                        _signalTower.SwitchOffBuzzerEx(Convert.ToInt32(args[0]));
                        break;
                    case 2:
                        _signalTower.SwitchOffBuzzerEx(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]));
                        break;
                    case 3:
                        _signalTower.SwitchOffBuzzerEx(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]), Convert.ToInt32(args[2]));
                        break;
                    default:
                        EV.PostWarningLog(Module, "SingalTower SwitchOffBuzzerEx wrong parameters.");
                        break;
                }
                return false;
            }
            catch (Exception ex)
            {
                EV.PostWarningLog(Module, "SingalTower SwitchOffBuzzerEx Exception:" + ex.Message);
            }
            return true;
        }


    }
}
