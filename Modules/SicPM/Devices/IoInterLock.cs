using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using System;
using System.Diagnostics;
using System.Xml;

namespace SicPM.Devices
{
    public class IoInterLock : BaseDevice, IDevice
    {
        private DOAccessor _doReactorATMTransferReady = null;
        private DOAccessor _doReactorVACTransferReady = null;
        private DIAccessor _diChamLidClosed = null;
        private DIAccessor _diServoDriverFaultSW = null;
        private DIAccessor _diH2PressureSW = null;

        private DIAccessor _diPSU1Status = null;
        private DIAccessor _diPSU2Status = null;
        private DIAccessor _diPSU3Status = null;
        private DIAccessor _diSCR1Status = null;
        private DIAccessor _diSCR2Status = null;
        private DIAccessor _diSCR3Status = null;
        private DOAccessor _doPSU1Enable = null;
        private DOAccessor _doPSU2Enable = null;
        private DOAccessor _doPSU3Enable = null;
        private DOAccessor _doSCR1Enable = null;
        private DOAccessor _doSCR2Enable = null;
        private DOAccessor _doSCR3Enable = null;

        private DOAccessor _doTCSSupply = null;


        private DOAccessor _doLidOpenRoutineSucceed = null;
        private DOAccessor _doLidCloseRoutineSucceed = null;
        private DOAccessor _doProcessRunning = null;
        private DOAccessor _doPreprocessRunning = null;
        private DOAccessor _doCyclePurgeRoutineRunning = null;
        private DOAccessor _doExchangeMoRoutineRunning = null;
        private DOAccessor _doLidCloseRoutineRunning = null;
        private DOAccessor _doLidOpenRoutineRunning = null;
        private DOAccessor _doPumpDownRoutineRunning = null;
        private DOAccessor _doVentRoutineRunning = null;
        private DOAccessor _doVACTransferAllowed = null;
        private DOAccessor _doATMTransferAllowed = null;
        private DOAccessor _doPostProcessRunning = null;
        private DOAccessor _doProcessIdleRunning = null;
        private DOAccessor _doATMIdleRoutineRunning = null;
        private DOAccessor _doVACIdleRoutineRunning = null;


        private DIAccessor _diPMATMSW = null;
        private DIAccessor _diPSUEnable = null;
        private DIAccessor _diHeaterTempBelow900CSW = null;
        private DIAccessor _diConfinementRingDown = null;
        private AIAccessor _aiActualSpeed = null;
        private AIAccessor _aiChamPress1000Torr1333mbar = null; 
        private AIAccessor _aiTempCtrl1 = null;

        private DOAccessor _doPMASlitDoorClosed = null;  //同步TM的SlitDoor状态

        private DOAccessor _doReactorPressRisingRate = null;
        private DOAccessor _doUPSLowBattery = null;
        private DOAccessor _doChamMiddleFlow2Temp = null;
        private DOAccessor _doSHFlowTemp = null;
        private DOAccessor _doChamTopFlowTemp = null;
        private DOAccessor _doChamMiddleFlow1Temp = null;
        private DOAccessor _doChamBottomFlowTemp = null;
        private DOAccessor _doBottomPlateFlowTemp = null;
        private DOAccessor _doPowerRod1FlowTemp = null;
        private DOAccessor _doPowerRod2FlowTemp = null;
        private DOAccessor _doInSituFlowTemp = null;
        private DOAccessor _doTMPump2FlowTemp = null;
        private DOAccessor _doTMPump1FlowTemp = null;
        private DOAccessor _doTransformerFlowTemp = null;
        private DOAccessor _doForelineColdTrapFlowTemp = null;
        private DOAccessor _doTMTopLidTemp = null;
        private DOAccessor _doTMBufferFlowTemp = null;

        private AIAccessor _aiChamMiddleFlow2Temp = null;
        private AIAccessor _aiSHFlowTemp = null;
        private AIAccessor _aiChamTopFlowTemp = null;
        private AIAccessor _aiChamMiddleFlowTemp = null;
        private AIAccessor _aiChamBottomFlowTemp = null;
        private AIAccessor _aiBottomPlateFlowTemp = null;
        private AIAccessor _aiPowerRod1FlowTemp = null;
        private AIAccessor _aiPowerRod2FlowTemp = null;
        private AIAccessor _aiInSituFlowTemp = null;
        private AIAccessor _aiTMPump2FlowTemp = null;
        private AIAccessor _aiTMPump1FlowTemp = null;
        private AIAccessor _aiTransformerFlowTemp = null;
        private AIAccessor _aiForelineColdTrapFlowTemp = null;
        private AIAccessor _aiTMTopLidTemp = null;
        private AIAccessor _aiTMBufferFlowTemp = null;


        private SCConfigItem _scPmVacPress;  //判断腔体真空的BasePressure

        private SCConfigItem _scReactorPressRisingRateLimit;
        private SCConfigItem _scChamMiddleFlow2Temp;
        private SCConfigItem _scSHFlowTemp;
        private SCConfigItem _scChamTopFlowTemp;
        private SCConfigItem _scChamMiddleFlowTemp;
        private SCConfigItem _scChamBottomFlowTemp;
        private SCConfigItem _scBottomPlateFlowTemp;
        private SCConfigItem _scPowerRod1FlowTemp;
        private SCConfigItem _scPowerRod2FlowTemp;
        private SCConfigItem _scInSituFlowTemp;
        private SCConfigItem _scTMPump1FlowTemp;
        private SCConfigItem _scTMPump2FlowTemp;
        private SCConfigItem _scTransformerFlowTemp;
        private SCConfigItem _scForelineColdTrapFlowTemp;
        private SCConfigItem _scTMTopLidTemp;
        private SCConfigItem _scTMBufferFlowTemp;


        //private SCConfigItem _scPurgeRoutineSucceedTemp;

        private SCConfigItem _scTCSFluidInfusionTime;              //TCS补液时间

        private double _lastChamberPressure = 0;
        private long currentSeconds = 0;
        private Stopwatch _swTimer = new Stopwatch();
        private DeviceTimer _timerTCSSupply = new DeviceTimer();

        private R_TRIG _alarmPSU1 = new R_TRIG();
        private R_TRIG _alarmPSU2 = new R_TRIG();
        private R_TRIG _alarmPSU3 = new R_TRIG();
        private R_TRIG _alarmSCR1 = new R_TRIG();
        private R_TRIG _alarmSCR2 = new R_TRIG();
        private R_TRIG _alarmSCR3 = new R_TRIG();

        private R_TRIG _processingStartTRIG = new R_TRIG();
        private R_TRIG _tcsSupply = new R_TRIG();
        private DateTime _processingStartTime = DateTime.Now;
        private bool _tcsSupplyFlag = false;


        #region DO100使能
        /* ChamMoveBody.UpDown.Enable (DO100), condintion A Logic AND
           F closed(Run/Vent Valves)，去掉V94 //需要确认close时value是true还是false
           H closed(process switch Valves) 
           B closd
           I closed(Final Valves)
        //---------------------------------
        DI13=1 DI_SusceptorAtSafeSpeed
        Isolation valve 1 closed (DI280=0)
        Lid Open Routine suscessed (DO172=1)
        DOR Pres at ATM (DI75=1) //没有DI75，改成DI7
        PM at ATM (DI4=1)
        Rotation stopped(转速为0)(AI118)

        private DIAccessor _diEPV11_FB = null; //DI-280
        private DOAccessor _doTMAVent_FB = null; //DO-172     
        private DIAccessor _diDORPressATMSW = null; //DI-7
        private DOAccessor _diPMATMSW = null; //DI-4
        private AIAccessor _aiActualSpeed = null; //AI118 ,前面已定义

        //-------------------------以上添加到interlockPM.xml中，下面手工判断        
        PSU disable  //
        Service mode //手动模式
        No related alarm according to the interlock table
        <Limit ai="PM1.AI_ActualSpeed" value="0" tip="" tip.zh-CN="" tip.en-US="AI-118" />  这个放在配置文件里面，程序会变得很慢，所以放到ChamMoveBodyUpDownEnableCanDo函数里
       */
        [Subscription("PSU1.AllHeatEnable")]
        public bool AllHeatEnable { get; set; }

        [Subscription("IsService")]
        public bool IsService { get; set; }
        /// <summary>
        /// DO-100
        /// </summary>
        public bool ChamMoveBodyUpDownEnable
        {
            get
            {
                return this.ChamMoveBodyUpDownEnableCanDo();
            }
        }

        #endregion

        public int ElapsedTime
        {
            get { return _swTimer.IsRunning ? (int)(_swTimer.ElapsedMilliseconds / 1000) : 0; }
        }

        public IoInterLock(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _scPmVacPress = ParseScNode("VacuumPressureBase", node, ioModule, $"PM.VacuumPressureBase");
            _scReactorPressRisingRateLimit = ParseScNode("ReactorPressRisingRateLimit", node, ioModule, $"PM.{Module}.ReactorPressRisingRateLimit");

            _scChamMiddleFlow2Temp = ParseScNode("ChamMiddleFlow2TempLimit", node, ioModule, $"PM.{Module}.CoolingWater.ChamMiddleFlow2TempLimit");
            _scSHFlowTemp = ParseScNode("SHFlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.SHFlowTempLimit");
            _scChamTopFlowTemp = ParseScNode("ChamTopFlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.ChamTopFlowTempLimit");
            _scChamMiddleFlowTemp = ParseScNode("ChamMiddleFlow1TempLimit", node, ioModule, $"PM.{Module}.CoolingWater.ChamMiddleFlow1TempLimit");
            _scChamBottomFlowTemp = ParseScNode("ChamBottomFlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.ChamBottomFlowTempLimit");
            _scBottomPlateFlowTemp = ParseScNode("SpareTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.SpareTempLimit");
            _scPowerRod1FlowTemp = ParseScNode("PowerRod1FlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.PowerRod1FlowTempLimit");
            _scPowerRod2FlowTemp = ParseScNode("PowerRod2FlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.PowerRod2FlowTempLimit");
            _scInSituFlowTemp = ParseScNode("ElectrodeWFlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.ElectrodeWFlowTempLimit");
            _scTMPump1FlowTemp = ParseScNode("TMPumpFlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.TMPump1FlowTempLimit");
            _scTMPump2FlowTemp = ParseScNode("TMPumpFlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.TMPump2FlowTempLimit");
            _scTransformerFlowTemp = ParseScNode("TransformerFlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.TransformerFlowTempLimit");
            _scForelineColdTrapFlowTemp = ParseScNode("ForelineFlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.ForelineFlowTempLimit");
            _scTMTopLidTemp = ParseScNode("TMTopLidTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.TMTopLidTempLimit");
            _scTMBufferFlowTemp = ParseScNode("TMBufferFlowTempLimit", node, ioModule, $"PM.{Module}.CoolingWater.TMBufferFlowTempLimit");


            //_scPurgeRoutineSucceedTemp = ParseScNode("PurgeRoutineSucceedTemp", node, ioModule, $"PM.{Module}.Purge.PurgeRoutineSucceedTemp");
            _scTCSFluidInfusionTime = ParseScNode("TCSSupplyTime", node, ioModule, $"PM.{Module}.TCSFluidInfusionTime");

            _doReactorATMTransferReady = ParseDoNode("doReactorATMTransferReady", node, ioModule);
            _doReactorVACTransferReady = ParseDoNode("doReactorVACTransferReady", node, ioModule);
            _diChamLidClosed = ParseDiNode("diChamLidClosed", node, ioModule);
            _doLidOpenRoutineSucceed = ParseDoNode("doLidOpenRoutineSucceed", node, ioModule);
            _doLidCloseRoutineSucceed = ParseDoNode("doLidCloseRoutineSucceed", node, ioModule);
            _diServoDriverFaultSW = ParseDiNode("diServoDriverFaultSW", node, ioModule);
            _diH2PressureSW = ParseDiNode("diH2PressureSW", node, ioModule);

            _diPSU1Status = ParseDiNode("diPSU1Status", node, ioModule);
            _diPSU2Status = ParseDiNode("diPSU2Status", node, ioModule);
            _diPSU3Status = ParseDiNode("diPSU3Status", node, ioModule);
            _diSCR1Status = ParseDiNode("diSCR1Status", node, ioModule);
            _diSCR2Status = ParseDiNode("diSCR2Status", node, ioModule);
            _diSCR3Status = ParseDiNode("diSCR3Status", node, ioModule);

            _doPSU1Enable = ParseDoNode("doPSU1Enable", node, ioModule);
            _doPSU2Enable = ParseDoNode("doPSU2Enable", node, ioModule);
            _doPSU3Enable = ParseDoNode("doPSU3Enable", node, ioModule);
            _doSCR1Enable = ParseDoNode("doSCR1Enable", node, ioModule);
            _doSCR2Enable = ParseDoNode("doSCR2Enable", node, ioModule);
            _doSCR3Enable = ParseDoNode("doSCR3Enable", node, ioModule);

            _doTCSSupply = ParseDoNode("doTCSSupply", node, ioModule);
            _aiTempCtrl1 = ParseAiNode("aiTempCtrl1", node, ioModule);

            _doProcessRunning = ParseDoNode("doProcessRunning", node, ioModule);
            _doPreprocessRunning = ParseDoNode("doPreprocessRunning", node, ioModule);
            _doPostProcessRunning = ParseDoNode("doPostProcessRunning", node, ioModule);
            _doCyclePurgeRoutineRunning = ParseDoNode("doCyclePurgeRoutineRunning", node, ioModule);
            _doExchangeMoRoutineRunning = ParseDoNode("doExchangeMoRoutineRunning", node, ioModule);
            _doLidCloseRoutineRunning = ParseDoNode("doLidCloseRoutineRunning", node, ioModule);
            _doLidOpenRoutineRunning = ParseDoNode("doLidOpenRoutineRunning", node, ioModule);
            _doPumpDownRoutineRunning = ParseDoNode("doPumpDownRoutineRunning", node, ioModule);
            _doVentRoutineRunning = ParseDoNode("doVentRoutineRunning", node, ioModule);
            _doProcessIdleRunning= ParseDoNode("doProcessIdleRunning", node, ioModule);
            _doATMIdleRoutineRunning= ParseDoNode("doATMIdleRoutineRunning", node, ioModule);
            _doVACIdleRoutineRunning= ParseDoNode("doVACIdleRoutineRunning", node, ioModule);

            _doVACTransferAllowed = ParseDoNode("doVACTransferAllowed", node, ioModule);
            _doATMTransferAllowed = ParseDoNode("doATMTransferAllowed", node, ioModule);


            _diPMATMSW = ParseDiNode("diPMATMSW", node, ioModule);
            _diPSUEnable = ParseDiNode("diPSUEnable", node, ioModule);
            _diHeaterTempBelow900CSW = ParseDiNode("diHeaterTempBelow900CSW", node, ioModule);
            _diConfinementRingDown = ParseDiNode("diConfinementRingDown", node, ioModule);
            _aiActualSpeed = ParseAiNode("aiActualSpeed", node, ioModule);
            _aiChamPress1000Torr1333mbar = ParseAiNode("aiChamPress1000Torr1333mbar", node, ioModule);

            _doPMASlitDoorClosed = ParseDoNode("doPMASlitDoorClosed", node, ioModule);

            _doReactorPressRisingRate = ParseDoNode("doReactorPressRisingRate", node, ioModule);
            _doUPSLowBattery = ParseDoNode("doUPSLowBattery", node, ioModule);
            _doChamMiddleFlow2Temp = ParseDoNode("doChamMiddleFlow2Temp", node, ioModule);
            _doSHFlowTemp = ParseDoNode("doSHFlowTemp", node, ioModule);
            _doChamTopFlowTemp = ParseDoNode("doChamTopFlowTemp", node, ioModule);
            _doChamMiddleFlow1Temp = ParseDoNode("doChamMiddleFlow1Temp", node, ioModule);
            _doChamBottomFlowTemp = ParseDoNode("doChamBottomFlowTemp", node, ioModule);
            _doBottomPlateFlowTemp = ParseDoNode("doBottomPlateFlowTemp", node, ioModule);
            _doPowerRod1FlowTemp = ParseDoNode("doPowerRod1FlowTemp", node, ioModule);
            _doPowerRod2FlowTemp = ParseDoNode("doPowerRod2FlowTemp", node, ioModule);
            _doInSituFlowTemp = ParseDoNode("doInSituFlowTemp", node, ioModule);
            _doTMPump1FlowTemp = ParseDoNode("doTMPump1FlowTemp", node, ioModule);
            _doTMPump2FlowTemp = ParseDoNode("doTMPump2FlowTemp", node, ioModule);
            _doTransformerFlowTemp = ParseDoNode("doTransformerFlowTemp", node, ioModule);
            _doForelineColdTrapFlowTemp = ParseDoNode("doForelineColdTrapFlowTemp", node, ioModule);
            _doTMTopLidTemp = ParseDoNode("doTMTopLidTemp", node, ioModule);
            _doTMBufferFlowTemp = ParseDoNode("doTMBufferFlowTemp", node, ioModule);

            _aiChamMiddleFlow2Temp = ParseAiNode("aiChamMiddleFlow2Temp", node, ioModule);
            _aiSHFlowTemp = ParseAiNode("aiSHFlowTemp", node, ioModule);
            _aiChamTopFlowTemp = ParseAiNode("aiChamTopFlowTemp", node, ioModule);
            _aiChamMiddleFlowTemp = ParseAiNode("aiChamMiddleFlowTemp", node, ioModule);
            _aiChamBottomFlowTemp = ParseAiNode("aiChamBottomFlowTemp", node, ioModule);
            _aiBottomPlateFlowTemp = ParseAiNode("aiBottomPlateFlowTemp", node, ioModule);
            _aiPowerRod1FlowTemp = ParseAiNode("aiPowerRod1FlowTemp", node, ioModule);
            _aiPowerRod2FlowTemp = ParseAiNode("aiPowerRod2FlowTemp", node, ioModule);
            _aiInSituFlowTemp = ParseAiNode("aiInSituFlowTemp", node, ioModule);
            _aiTMPump1FlowTemp = ParseAiNode("aiTMPump1FlowTemp", node, ioModule);
            _aiTMPump2FlowTemp = ParseAiNode("aiTMPump2FlowTemp", node, ioModule);
            _aiTransformerFlowTemp = ParseAiNode("aiTransformerFlowTemp", node, ioModule);
            _aiForelineColdTrapFlowTemp = ParseAiNode("aiForelineColdTrapFlowTemp", node, ioModule);
            _aiTMTopLidTemp = ParseAiNode("aiTMTopLidTemp", node, ioModule);
            _aiTMBufferFlowTemp = ParseAiNode("aiTMBufferFlowTemp", node, ioModule);

            _swTimer.Start();
        }

        public bool SetLidOpenRoutineSucceed(bool eValue, out string reason)
        {
            reason = String.Empty;
            //if (_aiTempCtrl1 != null && eValue)
            //{
            //    if (_aiTempCtrl1.FloatValue > 100)
            //    {
            //        EV.PostWarningLog(Module, $"Can not set [DO-172]DO_PurgeRoutineSucceed to {eValue}，Reason：Interlock triggered, AI-193({Module}.AI_PSUTC) > 100");
            //        return false;
            //    }
            //}
            if (!_doLidOpenRoutineSucceed.Check(eValue, out reason))
            {
                EV.PostWarningLog(Module, $"Can not set [DO-172]DO_PurgeRoutineSucceed to {eValue}，Reason："+ reason);

                return false;
            }
            //if (!_doPreprocessRunning.SetValue(eValue, out reason))
            if (!_doLidOpenRoutineSucceed.SetValue(eValue, out reason))
            {
                EV.PostWarningLog(Module, $"Can not set [DO-172]DO_PurgeRoutineSucceed to {eValue}，Reason："+ reason);

                return false;
            }

            return true;
        }


        public bool SetLidClosedRoutineSucceed(bool eValue, out string reason)
        {
            reason = String.Empty;
            if (!_doLidCloseRoutineSucceed.Check(eValue, out reason))
            {
                EV.PostWarningLog(Module, $"Can not set [DO-173]DO_CleanRoutineSucceed to {eValue}，Reason：" + reason);
                return false;
            }
            if (!_doLidCloseRoutineSucceed.SetValue(eValue, out reason))
            {
                EV.PostWarningLog(Module, $"Can not set [DO-173]DO_CleanRoutineSucceed to {eValue}，Reason：" + reason);
                return false;
            }

            return true;
        }

        public bool SetPMProcessIdleRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doProcessIdleRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doProcessIdleRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }
        
        public bool SetPMAtmIdleRountingRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doATMIdleRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doATMIdleRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }
        
        public bool SetPMVacIdleRountingRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doVACIdleRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doVACIdleRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }


        public bool SetPMPreProcessRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doPreprocessRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doPreprocessRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }


        public bool SetPMProcessRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doProcessRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doProcessRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }


        public bool SetPMCyclePurgeRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doCyclePurgeRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doCyclePurgeRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }


        public bool SetPMExchangeMoRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doExchangeMoRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doExchangeMoRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetPMCleanRoutineRunning(bool eValue, out string reason)
        {
            reason = string.Empty;

            if (!_doLidCloseRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doLidCloseRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetPMPurgeRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doLidOpenRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doLidOpenRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetPMPumpRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doPumpDownRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doPumpDownRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetPMVentRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doVentRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doVentRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.ChamMoveBodyUpDownEnable", () => ChamMoveBodyUpDownEnable);
            return true;
            //throw new NotImplementedException();
        }

        public void Monitor()
        {
            MonitorPSUSCRAlarm();
            SetTempWarm();
            SetReactorATMTransferReady();
            SetReactorVACTransferReady();
            SetPressureRisingUpRate();
            MonitorTCSSupply();
        }


        private void MonitorTCSSupply()
        {
            string pmStatus = DATA.Poll($"{Module}.Status") == null ? "" : DATA.Poll($"{Module}.Status").ToString();         
            _tcsSupply.CLK = pmStatus == "ProcessIdle"; 
            if (_tcsSupply.Q)
            {
                if (!_doTCSSupply.SetValue(true, out string reason))
                {
                    EV.PostWarningLog(Module, "Set DO_110 Fail," + reason);
                }
                else
                {
                    _timerTCSSupply.Start(_scTCSFluidInfusionTime.IntValue*1000);//补液计时
                }
            }

            if (_timerTCSSupply.IsTimeout())
            {
                _doTCSSupply.Value = false;
                _timerTCSSupply.Stop(); //进入Idle状态
            }            
        }


        public void Reset()
        {
            _tRIGEnable1.RST = true;
            _tRIGEnable2.RST = true;
            _tRIGEnable3.RST = true;
            _tRIGEnable4.RST = true;
            _tRIGEnable5.RST = true;
            _tRIGEnable6.RST = true;



            _alarmPSU1.RST = true;
            _alarmPSU2.RST = true;
            _alarmPSU3.RST = true;
            _alarmSCR1.RST = true;
            _alarmSCR2.RST = true;
            _alarmSCR3.RST = true;

            // 软件启动时默认将 （DO-192）DO_ReactorPressRisingRateFast 设置为无报警（高电平）。
            //TODO 是否存在可靠性问题？
            _doReactorPressRisingRate.Value = true;
        }

        public void Terminate()
        {
            //throw new NotImplementedException();
        }

        private void SetReactorATMTransferReady()
        {
            if (_doReactorATMTransferReady != null)
            {
                if (_diPMATMSW != null && _diPSUEnable != null && _aiActualSpeed != null && _diConfinementRingDown != null)
                {
                    if (!_diPSUEnable.Value && _diPMATMSW.Value && _aiActualSpeed.FloatValue == 0 && _diConfinementRingDown.Value)
                    {
                        if (!_doReactorATMTransferReady.Value)
                        {
                            _doReactorATMTransferReady.Value = true;
                        }
                        return;
                    }
                }
            }

            if (_doReactorATMTransferReady.Value)
            {
                _doReactorATMTransferReady.Value = false;
            }
        }

        private void SetReactorVACTransferReady()
        {
            if (_doReactorVACTransferReady != null)
            {
                if (_aiChamPress1000Torr1333mbar != null && _scPmVacPress != null  && _aiActualSpeed != null && _diConfinementRingDown != null)
                {
                    if (_aiChamPress1000Torr1333mbar.FloatValue <= _scPmVacPress.DoubleValue && _aiActualSpeed.FloatValue == 0 && _diConfinementRingDown.Value)
                    {
                        if (!_doReactorVACTransferReady.Value)
                        {
                            _doReactorVACTransferReady.Value = true;
                        }
                        return;
                    }

                }
                if (_doReactorVACTransferReady.Value)
                {
                    _doReactorVACTransferReady.Value = false;
                }
            }

        }


        private void SetTempWarm()
        {
            if (_doChamMiddleFlow2Temp != null && _aiChamMiddleFlow2Temp != null && _scChamMiddleFlow2Temp.DoubleValue > 0)
            {
                _doChamMiddleFlow2Temp.Value = _aiChamMiddleFlow2Temp.FloatValue < _scChamMiddleFlow2Temp.DoubleValue;
            }

            if (_doSHFlowTemp != null && _aiSHFlowTemp != null && _scSHFlowTemp.DoubleValue > 0)
            {
                _doSHFlowTemp.Value = _aiSHFlowTemp.FloatValue < _scSHFlowTemp.DoubleValue;
            }
            if (_doChamTopFlowTemp != null && _aiChamTopFlowTemp != null && _scChamTopFlowTemp.DoubleValue > 0)
            {
                _doChamTopFlowTemp.Value = _aiChamTopFlowTemp.FloatValue < _scChamTopFlowTemp.DoubleValue;
            }
            if (_doChamMiddleFlow1Temp != null && _aiChamMiddleFlowTemp != null && _scChamMiddleFlowTemp.DoubleValue > 0)
            {
                _doChamMiddleFlow1Temp.Value = _aiChamMiddleFlowTemp.FloatValue < _scChamMiddleFlowTemp.DoubleValue;
            }
            if (_doChamBottomFlowTemp != null && _aiChamBottomFlowTemp != null && _scChamBottomFlowTemp.DoubleValue > 0)
            {
                _doChamBottomFlowTemp.Value = _aiChamBottomFlowTemp.FloatValue < _scChamBottomFlowTemp.DoubleValue;
            }
            if (_doBottomPlateFlowTemp != null && _aiBottomPlateFlowTemp != null && _scBottomPlateFlowTemp.DoubleValue > 0)
            {
                _doBottomPlateFlowTemp.Value = _aiBottomPlateFlowTemp.FloatValue < _scBottomPlateFlowTemp.DoubleValue;
            }
            if (_doPowerRod1FlowTemp != null && _aiPowerRod1FlowTemp != null && _scPowerRod1FlowTemp.DoubleValue > 0)
            {
                _doPowerRod1FlowTemp.Value = _aiPowerRod1FlowTemp.FloatValue < _scPowerRod1FlowTemp.DoubleValue;
            }
            if (_doPowerRod2FlowTemp != null && _aiPowerRod2FlowTemp != null && _scPowerRod2FlowTemp.DoubleValue > 0)
            {
                _doPowerRod2FlowTemp.Value = _aiPowerRod2FlowTemp.FloatValue < _scPowerRod2FlowTemp.DoubleValue;
            }
            if (_doInSituFlowTemp != null && _aiInSituFlowTemp != null && _scInSituFlowTemp.DoubleValue > 0)
            {
                _doInSituFlowTemp.Value = _aiInSituFlowTemp.FloatValue < _scInSituFlowTemp.DoubleValue;
            }
            if (_doTMPump1FlowTemp != null && _aiTMPump1FlowTemp != null && _scTMPump1FlowTemp.DoubleValue > 0)
            {
                _doTMPump1FlowTemp.Value = _aiTMPump1FlowTemp.FloatValue < _scTMPump1FlowTemp.DoubleValue;
            }
            if (_doTMPump2FlowTemp != null && _aiTMPump2FlowTemp != null && _scTMPump2FlowTemp.DoubleValue > 0)
            {
                _doTMPump2FlowTemp.Value = _aiTMPump2FlowTemp.FloatValue < _scTMPump2FlowTemp.DoubleValue;
            }
            if (_doTransformerFlowTemp != null && _aiTransformerFlowTemp != null && _scTransformerFlowTemp.DoubleValue > 0)
            {
                _doTransformerFlowTemp.Value = _aiTransformerFlowTemp.FloatValue < _scTransformerFlowTemp.DoubleValue;
            }

            if (_doForelineColdTrapFlowTemp != null && _aiForelineColdTrapFlowTemp != null && _scForelineColdTrapFlowTemp.DoubleValue > 0)
            {
                _doForelineColdTrapFlowTemp.Value = _aiForelineColdTrapFlowTemp.FloatValue < _scForelineColdTrapFlowTemp.DoubleValue;
            }

            if (_doTMTopLidTemp != null && _aiTMTopLidTemp != null && _scTMTopLidTemp.DoubleValue > 0)
            {
                _doTMTopLidTemp.Value = _aiTMTopLidTemp.FloatValue < _scTMTopLidTemp.DoubleValue;
            }

            if (_doTMBufferFlowTemp != null && _aiTMBufferFlowTemp != null && _scTMBufferFlowTemp.DoubleValue > 0)
            {
                _doTMBufferFlowTemp.Value = _aiTMBufferFlowTemp.FloatValue < _scTMBufferFlowTemp.DoubleValue;
            }
        }

        private void SetPressureRisingUpRate()
        {
            if (_aiChamPress1000Torr1333mbar != null && _scReactorPressRisingRateLimit.DoubleValue > 0 && _scReactorPressRisingRateLimit.DoubleValue < 200)
            {
                //在PM处于Process状态时才进行判断
                string pmStatus = DATA.Poll($"{Module}.Status") == null ? "" : DATA.Poll($"{Module}.Status").ToString();
                if (pmStatus == "Process")
                {
                    // 每5s统计一次。
                    var statInterval = ElapsedTime - currentSeconds;
                    if (statInterval > 5)
                    {
                        // 压力变化率绝对值没有超过限制时，_doReactorPressRisingRate = true
                        _doReactorPressRisingRate.Value =
                            !(Math.Abs((_aiChamPress1000Torr1333mbar.FloatValue - _lastChamberPressure) /
                                       statInterval) > _scReactorPressRisingRateLimit.DoubleValue);

                        // 更新最后统计时间和最后压力值
                        _lastChamberPressure = _aiChamPress1000Torr1333mbar.FloatValue;
                        currentSeconds = ElapsedTime;
                    }
                }
            }
            else
            {
                if (_doReactorPressRisingRate.Value != true)
                {
                    _doReactorPressRisingRate.Value = true;
                }
            }
        }

        private DeviceTimer _timer = new DeviceTimer();
        private R_TRIG _tRIGEnable1 = new R_TRIG();
        private R_TRIG _tRIGEnable2 = new R_TRIG();
        private R_TRIG _tRIGEnable3 = new R_TRIG();
        private R_TRIG _tRIGEnable4 = new R_TRIG();
        private R_TRIG _tRIGEnable5 = new R_TRIG();
        private R_TRIG _tRIGEnable6 = new R_TRIG();

        private void MonitorPSUSCRAlarm()
        {
            _tRIGEnable1.CLK = _doPSU1Enable.Value && _diPSUEnable.Value;
            if (_tRIGEnable1.Q)
            {
                _timer.Stop();
                _timer.Start(10 * 1000);
            }
            _tRIGEnable2.CLK = _doPSU2Enable.Value && _diPSUEnable.Value;
            if (_tRIGEnable2.Q)
            {
                _timer.Stop();
                _timer.Start(10 * 1000);
            }
            _tRIGEnable3.CLK = _doPSU3Enable.Value && _diPSUEnable.Value;
            if (_tRIGEnable3.Q)
            {
                _timer.Stop();
                _timer.Start(10 * 1000);
            }
            _tRIGEnable4.CLK = _doSCR1Enable.Value && _diPSUEnable.Value;
            if (_tRIGEnable4.Q)
            {
                _timer.Stop();
                _timer.Start(10 * 1000);
            }
            _tRIGEnable5.CLK = _doSCR2Enable.Value && _diPSUEnable.Value;
            if (_tRIGEnable5.Q)
            {
                _timer.Stop();
                _timer.Start(10 * 1000);
            }
            _tRIGEnable6.CLK = _doSCR3Enable.Value && _diPSUEnable.Value;
            if (_tRIGEnable6.Q)
            {
                _timer.Stop();
                _timer.Start(10 * 1000);
            }


            if (_timer.IsTimeout())
            {
                /*
                _alarmPSU1.CLK = _doPSU1Enable.Value && !_diPSU1Status.Value && _diPSUEnable.Value;
                if (_alarmPSU1.Q)
                {
                    EV.PostAlarmLog(Module, "Alarm19 PSU1 Status Is Off");
                }

                _alarmPSU2.CLK = _doPSU2Enable.Value && !_diPSU2Status.Value && _diPSUEnable.Value;
                if (_alarmPSU2.Q)
                {
                    EV.PostAlarmLog(Module, "Alarm20 PSU2 Status Is Off");
                }

                _alarmPSU3.CLK = _doPSU3Enable.Value && !_diPSU3Status.Value && _diPSUEnable.Value;
                if (_alarmPSU3.Q)
                {
                    EV.PostAlarmLog(Module, "Alarm21 PSU3 Status Is Off");
                }

                _alarmSCR1.CLK = _doSCR1Enable.Value && !_diSCR1Status.Value && _diPSUEnable.Value;
                if (_alarmSCR1.Q)
                {
                    EV.PostAlarmLog(Module, "Alarm22 SCR1 Status Is Off");
                }

                _alarmSCR2.CLK = _doSCR2Enable.Value && !_diSCR2Status.Value && _diPSUEnable.Value;
                if (_alarmSCR2.Q)
                {
                    EV.PostAlarmLog(Module, "Alarm23 SCR2 Status Is Off");
                }

                _alarmSCR3.CLK = _doSCR3Enable.Value && !_diSCR3Status.Value && _diPSUEnable.Value;
                if (_alarmSCR3.Q)
                {
                    EV.PostAlarmLog(Module, "Alarm24 SCR3 Status Is Off");
                }
                */

                _timer.Stop();
            }


        }



        public bool DoReactorATMTransferReady
        {
            get
            {
                if (_doReactorATMTransferReady != null)
                {
                    return _doReactorATMTransferReady.Value;
                }
                return false;
            }
            //set
            //{
            //    if (_doReactorATMTransferReady != null)
            //    {
            //        _doReactorATMTransferReady.Value = value;
            //    }
            //}
        }

        public bool DoReactorVACTransferReady
        {
            get
            {
                if (_doReactorVACTransferReady != null)
                {
                    return _doReactorVACTransferReady.Value;
                }
                return false;
            }
            //set
            //{
            //    if (_doReactorVACTransferReady != null)
            //    {
            //        _doReactorVACTransferReady.Value = value;
            //    }
            //}
        }

        public bool DiChamLidClosed
        {
            get
            {
                if (_diChamLidClosed != null)
                {
                    return _diChamLidClosed.Value;
                }
                return false;
            }
        }


        public bool DoProcessRunning
        {
            get
            {
                if (_doProcessRunning != null)
                {
                    return _doProcessRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doProcessRunning != null)
                {
                    _doProcessRunning.Value = value;
                }
            }
        }

        public bool DoPreprocessRunning
        {
            get
            {
                if (_doPreprocessRunning != null)
                {
                    return _doPreprocessRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doPreprocessRunning != null)
                {
                    _doPreprocessRunning.Value = value;
                }
            }
        }

        public bool DoCyclePurgeRoutineRunning
        {
            get
            {
                if (_doCyclePurgeRoutineRunning != null)
                {
                    return _doCyclePurgeRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doCyclePurgeRoutineRunning != null)
                {
                    _doCyclePurgeRoutineRunning.Value = value;
                }
            }
        }

        public bool DoExchangeMoRoutineRunning
        {
            get
            {
                if (_doExchangeMoRoutineRunning != null)
                {
                    return _doExchangeMoRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doExchangeMoRoutineRunning != null)
                {
                    _doExchangeMoRoutineRunning.Value = value;
                }
            }
        }

        public bool DoLidCloseRoutineRunning
        {
            get
            {
                if (_doLidCloseRoutineRunning != null)
                {
                    return _doLidCloseRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doLidCloseRoutineRunning != null)
                {
                    _doLidCloseRoutineRunning.Value = value;
                }
            }
        }

        public bool DoLidOpenRoutineRunning
        {
            get
            {
                if (_doLidOpenRoutineRunning != null)
                {
                    return _doLidOpenRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doLidOpenRoutineRunning != null)
                {
                    _doLidOpenRoutineRunning.Value = value;
                }
            }
        }

        public bool DoPumpDownRoutineRunning
        {
            get
            {
                if (_doPumpDownRoutineRunning != null)
                {
                    return _doPumpDownRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doPumpDownRoutineRunning != null)
                {
                    _doPumpDownRoutineRunning.Value = value;
                }
            }
        }

        public bool DoLidOpenRoutineSucceed
        {
            get
            {
                if (_doLidOpenRoutineSucceed != null)
                {
                    return _doLidOpenRoutineSucceed.Value;
                }
                return false;
            }
            set
            {
                if (_doLidOpenRoutineSucceed != null)
                {
                    _doLidOpenRoutineSucceed.Value = value;
                }
            }
        }

        public bool DoLidCloseRoutineSucceed
        {
            get
            {
                if (_doLidCloseRoutineSucceed != null)
                {
                    return _doLidCloseRoutineSucceed.Value;
                }
                return false;
            }
            set
            {
                if (_doLidCloseRoutineSucceed != null)
                {
                    _doLidCloseRoutineSucceed.Value = value;
                }
            }
        }

        public bool DoProcessIdleRunning
        {
            get
            {
                if (_doProcessIdleRunning != null)
                {
                    return _doProcessIdleRunning.Value;
                }
                return false;
            }
        }

        public bool DoAtmIdleRoutingRunning
        {
            get
            {
                if (_doATMIdleRoutineRunning != null)
                {
                    return _doATMIdleRoutineRunning.Value;
                }
                return false;
            }
        }
        
        public bool DoVacIdleRoutingRunning
        {
            get
            {
                if (_doVACIdleRoutineRunning != null)
                {
                    return _doVACIdleRoutineRunning.Value;
                }
                return false;
            }
        }

        public bool DiServoDriverFaultSW
        {
            get
            {
                if (_diServoDriverFaultSW != null)
                {
                    return _diServoDriverFaultSW.Value;
                }
                return false;
            }
        }

        public bool DiH2PressureSW
        {
            get
            {
                if (_diH2PressureSW != null)
                {
                    return _diH2PressureSW.Value;
                }
                return false;
            }
        }


        public bool DiSCR1Status
        {
            get
            {
                if (_diSCR1Status != null)
                {
                    return _diSCR1Status.Value;
                }
                return false;
            }
        }

        public bool DiSCR2Status
        {
            get
            {
                if (_diSCR2Status != null)
                {
                    return _diSCR2Status.Value;
                }
                return false;
            }
        }

        public bool DiSCR3Status
        {
            get
            {
                if (_diSCR3Status != null)
                {
                    return _diSCR3Status.Value;
                }
                return false;
            }
        }

        public bool DoPMASlitDoorClosed
        {
            get
            {
                if (_doPMASlitDoorClosed != null)
                {
                    return _doPMASlitDoorClosed.Value;
                }
                return false;
            }
            set
            {
                if (_doPMASlitDoorClosed != null)
                {
                    _doPMASlitDoorClosed.Value = value;
                }
            }
        }
        //public bool DoUPSLowBattery
        //{
        //    get
        //    {
        //        if (_doUPSLowBattery != null)
        //        {
        //            return _doUPSLowBattery.Value;
        //        }
        //        return false;
        //    }
        //    set
        //    {
        //        if (_doUPSLowBattery != null)
        //        {
        //            _doUPSLowBattery.Value = value;
        //        }
        //    }
        //}

        public DOAccessor DoUPSLowBattery
        {
            get
            {
                return _doUPSLowBattery;
            }
        }

        public bool DiHeaterTempBelow900CSW
        {
            get
            {
                if (_diHeaterTempBelow900CSW != null)
                {
                    return _diHeaterTempBelow900CSW.Value;
                }
                return false;
            }
        }

        public float AITempCtrl1
        {
            get
            {
                return _aiTempCtrl1 == null ? 0 : _aiTempCtrl1.FloatValue;
            }
        }

        
        /// <summary>
        /// ChamMoveBodyUpDownEnableCanDo
        /// </summary>
        /// <returns></returns>
        public bool ChamMoveBodyUpDownEnableCanDo()
        {
            //PSU disable

            //Service mode

            return (!AllHeatEnable)&&IsService&& (_aiActualSpeed.Value == 0);
        }

    }
}
