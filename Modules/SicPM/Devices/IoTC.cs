using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Hardcodet.Wpf.TaskbarNotification.Interop;

namespace SicPM.Devices
{
    public class IoTC : BaseDevice, IDevice
    {
        private bool _isFloatAioType = false;

        private AIAccessor _aiL1WorkingOPFeedBack = null;
        private AIAccessor _aiL2WorkingOPFeedBack = null;
        private AIAccessor _aiL3WorkingOPFeedBack = null;
        private AIAccessor _aiL1PVFeedBack = null;
        private AIAccessor _aiL2PVFeedBack = null;
        private AIAccessor _aiL3PVFeedBack = null;
        private DIAccessor _diL1TempHighAlarmFeedBack = null;
        private DIAccessor _diL2TempHighAlarmFeedBack = null;
        private DIAccessor _diL3TempHighAlarmFeedBack = null;
        private DIAccessor _diL1TempLowAlarmFeedBack = null;
        private DIAccessor _diL2TempLowAlarmFeedBack = null;
        private DIAccessor _diL3TempLowAlarmFeedBack = null;

        private DOAccessor _doPyroWarnMaxDiff;
        private DOAccessor _doAeTempRaisingFast;
        

        private AOAccessor _aoL1LoopModeSetPoint = null;
        private AOAccessor _aoL2LoopModeSetPoint = null;
        private AOAccessor _aoL3LoopModeSetPoint = null;
        private AOAccessor _aoL1TargetSPSetPoint = null;
        private AOAccessor _aoL2TargetSPSetPoint = null;
        private AOAccessor _aoL3TargetSPSetPoint = null;
        private AOAccessor _aoL1TargetOPSetPoint = null;
        private AOAccessor _aoL2TargetOPSetPoint = null;
        private AOAccessor _aoL3TargetOPSetPoint = null;
        private AOAccessor _aoL1RecipeValueSetPoint = null;
        private AOAccessor _aoL2RecipeValueSetPoint = null;
        private AOAccessor _aoL3RecipeValueSetPoint = null;
        private AOAccessor _aoL1InputTempSetPoint = null;
        private AOAccessor _aoL2InputTempSetPoint = null;
        private AOAccessor _aoL3InputTempSetPoint = null;
        private AOAccessor _aoTCPyroModeSetPoint = null;
        private AOAccessor _aoL1TempHighLimitSetPoint = null;
        private AOAccessor _aoL2TempHighLimitSetPoint = null;
        private AOAccessor _aoL3TempHighLimitSetPoint = null;
        private AOAccessor _aoL1TempLowLimitSetPoint = null;
        private AOAccessor _aoL2TempLowLimitSetPoint = null;
        private AOAccessor _aoL3TempLowLimitSetPoint = null;
        private AOAccessor _aoHeaterModeSetPoint = null;
        private AOAccessor _aoPowerRefSetPoint = null;
        private AOAccessor _aoL1RatioSetPoint = null;
        private AOAccessor _aoL2RatioSetPoint = null;
        private AOAccessor _aoL3RatioSetPoint = null;
        private AOAccessor _aoL1RatedSetPoint = null;
        private AOAccessor _aoL2RatedSetPoint = null;
        private AOAccessor _aoL3RatedSetPoint = null;
        private AOAccessor _aoL1VoltageLimited = null;
        private AOAccessor _aoL2VoltageLimited = null;
        private AOAccessor _aoL3VoltageLimited = null;
        private AIAccessor _aiTtempCtrlTCIN = null;

        private AOAccessor _aoPSU1Y = null;
        private AOAccessor _aoPSU2Y = null;
        private AOAccessor _aoPSU3Y = null;

        private DeviceTimer _delaySetTime = new DeviceTimer();


        private DeviceTimer _rampTimerL1 = new DeviceTimer();
        private float _rampTargetL1;
        private float _rampInitValueL1;
        private int _rampTimeL1;

        private DeviceTimer _rampTimerL2 = new DeviceTimer();
        private float _rampTargetL2;
        private float _rampInitValueL2;
        private int _rampTimeL2;

        private DeviceTimer _rampTimerL3 = new DeviceTimer();
        private float _rampTargetL3;
        private float _rampInitValueL3;
        private int _rampTimeL3;

        private DeviceTimer _rampTimerSetPowerRef = new DeviceTimer();
        private float _rampTargetSetPowerRefRecipe;
        private float _rampInitValueSetPowerRefRecipe;
        private int _rampTimeSetPowerRefRecipe;

        private DeviceTimer _rampTimerL1RatiorRecipe = new DeviceTimer();
        private float _rampTargetL1RatiorRecipe;
        private float _rampInitValueL1RatiorRecipe;
        private int _rampTimeL1RatiorRecipe;

        private DeviceTimer _rampTimerL2RatiorRecipe = new DeviceTimer();
        private float _rampTargetL2RatiorRecipe;
        private float _rampInitValueL2RatiorRecipe;
        private int _rampTimeL2RatiorRecipe;

        private DeviceTimer _rampTimerL3RatiorRecipe = new DeviceTimer();
        private float _rampTargetL3RatiorRecipe;
        private float _rampInitValueL3RatiorRecipe;
        private int _rampTimeL3RatiorRecipe;

        private string _selecetedLoop = string.Empty;
        private SCConfigItem _tempRampRatio;
        private SCConfigItem _ampereRampRatio;
        private SCConfigItem _kwRampRatio;
        private SCConfigItem _pyroWarmBaseTemp;     //Pyro温度报警需要满足BaseTemp
        private SCConfigItem _pyroWarmMaxDiff;    //Pyro的MidlleTemp,OuterTemp任意两个温度大于此数值需要报警
        private SCConfigItem _pyroWarmEffectFromTime; //温差过大只在进入Process多久后才有效
        private SCConfigItem _pyroWarmEffectEndTime; //温差过大只在Process即将结束前多久才有效

        private SCConfigItem _PSUHighLimit;
        private SCConfigItem _PSULowLimit;
        private SCConfigItem _SCRHighLimit;
        private SCConfigItem _SCRLowLimit;

        private SCConfigItem _AETempEnable;
        private SCConfigItem _PyroWarmIsAlarm;
        private SCConfigItem _AETempRasingFastIsAlarm;


        //private SCConfigItem _AETempInnerRasingRate;
        //private SCConfigItem _AETempMiddleRasingRate;
        //private SCConfigItem _AETempOuterRasingRate;
        //private SCConfigItem _AETempFrequency;

        private R_TRIG _trigSetTempLimit = new R_TRIG();
        public IoTC(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _aiL1WorkingOPFeedBack = ParseAiNode("aiL1WorkingOPFeedBack", node, ioModule);
            _aiL2WorkingOPFeedBack = ParseAiNode("aiL2WorkingOPFeedBack", node, ioModule);
            _aiL3WorkingOPFeedBack = ParseAiNode("aiL3WorkingOPFeedBack", node, ioModule);
            _aiL1PVFeedBack = ParseAiNode("aiL1PVFeedBack", node, ioModule);
            _aiL2PVFeedBack = ParseAiNode("aiL2PVFeedBack", node, ioModule);
            _aiL3PVFeedBack = ParseAiNode("aiL3PVFeedBack", node, ioModule);
            _diL1TempHighAlarmFeedBack = ParseDiNode("diL1TempHighAlarmFeedBack", node, ioModule);
            _diL2TempHighAlarmFeedBack = ParseDiNode("diL2TempHighAlarmFeedBack", node, ioModule);
            _diL3TempHighAlarmFeedBack = ParseDiNode("diL3TempHighAlarmFeedBack", node, ioModule);
            _diL1TempLowAlarmFeedBack = ParseDiNode("diL1TempLowAlarmFeedBack", node, ioModule);
            _diL2TempLowAlarmFeedBack = ParseDiNode("diL2TempLowAlarmFeedBack", node, ioModule);
            _diL3TempLowAlarmFeedBack = ParseDiNode("diL3TempLowAlarmFeedBack", node, ioModule);

            _doPyroWarnMaxDiff = ParseDoNode("doPyroWarnMaxDiff", node, ioModule);
            _doAeTempRaisingFast = ParseDoNode("doAeTempRaisingFast", node, ioModule);

            _aoL1LoopModeSetPoint = ParseAoNode("aoL1LoopModeSetPoint", node, ioModule);
            _aoL2LoopModeSetPoint = ParseAoNode("aoL2LoopModeSetPoint", node, ioModule);
            _aoL3LoopModeSetPoint = ParseAoNode("aoL3LoopModeSetPoint", node, ioModule);
            _aoL1TargetSPSetPoint = ParseAoNode("aoL1TargetSPSetPoint", node, ioModule);
            _aoL2TargetSPSetPoint = ParseAoNode("aoL2TargetSPSetPoint", node, ioModule);
            _aoL3TargetSPSetPoint = ParseAoNode("aoL3TargetSPSetPoint", node, ioModule);
            _aoL1TargetOPSetPoint = ParseAoNode("aoL1TargetOPSetPoint", node, ioModule);
            _aoL2TargetOPSetPoint = ParseAoNode("aoL2TargetOPSetPoint", node, ioModule);
            _aoL3TargetOPSetPoint = ParseAoNode("aoL3TargetOPSetPoint", node, ioModule);
            _aoL1RecipeValueSetPoint = ParseAoNode("aoL1RecipeValueSetPoint", node, ioModule);
            _aoL2RecipeValueSetPoint = ParseAoNode("aoL2RecipeValueSetPoint", node, ioModule);
            _aoL3RecipeValueSetPoint = ParseAoNode("aoL3RecipeValueSetPoint", node, ioModule);
            _aoL1InputTempSetPoint = ParseAoNode("aoL1InputTempSetPoint", node, ioModule);
            _aoL2InputTempSetPoint = ParseAoNode("aoL2InputTempSetPoint", node, ioModule);
            _aoL3InputTempSetPoint = ParseAoNode("aoL3InputTempSetPoint", node, ioModule);
            _aoTCPyroModeSetPoint = ParseAoNode("aoTCPyroModeSetPoint", node, ioModule);
            _aoL1TempHighLimitSetPoint = ParseAoNode("aoL1TempHighLimitSetPoint", node, ioModule);
            _aoL2TempHighLimitSetPoint = ParseAoNode("aoL2TempHighLimitSetPoint", node, ioModule);
            _aoL3TempHighLimitSetPoint = ParseAoNode("aoL3TempHighLimitSetPoint", node, ioModule);
            _aoL1TempLowLimitSetPoint = ParseAoNode("aoL1TempLowLimitSetPoint", node, ioModule);
            _aoL2TempLowLimitSetPoint = ParseAoNode("aoL2TempLowLimitSetPoint", node, ioModule);
            _aoL3TempLowLimitSetPoint = ParseAoNode("aoL3TempLowLimitSetPoint", node, ioModule);
            _aoHeaterModeSetPoint = ParseAoNode("aoHeaterModeSetPoint", node, ioModule);
            _aoPowerRefSetPoint = ParseAoNode("aoPowerRefSetPoint", node, ioModule);
            _aoL1RatioSetPoint = ParseAoNode("aoL1RatioSetPoint", node, ioModule);
            _aoL2RatioSetPoint = ParseAoNode("aoL2RatioSetPoint", node, ioModule);
            _aoL3RatioSetPoint = ParseAoNode("aoL3RatioSetPoint", node, ioModule);
            _aoL1RatedSetPoint = ParseAoNode("aoL1RatedSetPoint", node, ioModule);
            _aoL2RatedSetPoint = ParseAoNode("aoL2RatedSetPoint", node, ioModule);
            _aoL3RatedSetPoint = ParseAoNode("aoL3RatedSetPoint", node, ioModule);

            _aoL1VoltageLimited = ParseAoNode("aoL1VoltageLimited", node, ioModule);
            _aoL2VoltageLimited = ParseAoNode("aoL2VoltageLimited", node, ioModule);
            _aoL3VoltageLimited = ParseAoNode("aoL3VoltageLimited", node, ioModule);

            _aoPSU1Y = ParseAoNode("aoPSU1Y", node, ioModule);
            _aoPSU2Y = ParseAoNode("aoPSU2Y", node, ioModule);
            _aoPSU3Y = ParseAoNode("aoPSU3Y", node, ioModule);

            _aiTtempCtrlTCIN = ParseAiNode("aiTtempCtrlTCIN", node, ioModule);


            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _pyroWarmEffectFromTime = ParseScNode("PyroWarmEffectFromTime", node, ioModule, $"PM.{Module}.Heater.PyroWarmEffectFromTime");
            _pyroWarmEffectEndTime = ParseScNode("PyroWarmEffectFromTime", node, ioModule, $"PM.{Module}.Heater.PyroWarmEffectEndTime");

            _tempRampRatio = ParseScNode("TempRampRatio", node, ioModule, $"PM.{Module}.Heater.TempRampRate");
            _ampereRampRatio = ParseScNode("AmpereRampRate", node, ioModule, $"PM.{Module}.Heater.AmpereRampRate");
            _kwRampRatio = ParseScNode("KWRampRate", node, ioModule, $"PM.{Module}.Heater.KWRampRate");
            _pyroWarmBaseTemp = ParseScNode("PyroWarmBaseTemp", node, ioModule, $"PM.{Module}.Heater.PyroWarmBaseTemp");
            _pyroWarmMaxDiff = ParseScNode("PyroWarmBaseTemp", node, ioModule, $"PM.{Module}.Heater.PyroWarmMaxDiff");

            _PSUHighLimit = ParseScNode("PSUTempHighLimit", node, ioModule, $"PM.{Module}.Heater.PSUTempHighLimit");
            _PSULowLimit = ParseScNode("PSUTempLowLimit", node, ioModule, $"PM.{Module}.Heater.PSUTempLowLimit");

            _SCRHighLimit = ParseScNode("SCRTempHighLimit", node, ioModule, $"PM.{Module}.Heater.SCRTempHighLimit");
            _SCRLowLimit = ParseScNode("SCRTempLowLimit", node, ioModule, $"PM.{Module}.Heater.SCRTempLowLimit");

            _AETempEnable = ParseScNode("AETempEnable", node, ioModule, $"AETemp.EnableDevice");

            //_AETempInnerRasingRate = ParseScNode("AETempInnerRasingRate", node, ioModule, $"PM.{Module}.Heater.SCRTempRasingRate");
            //_AETempMiddleRasingRate = ParseScNode("AETempMiddleRasingRate", node, ioModule, $"PM.{Module}.Heater.AETempMiddleRasingRate");
            //_AETempOuterRasingRate = ParseScNode("AETempOuterRasingRate", node, ioModule, $"PM.{Module}.Heater.AETempOuterRasingRate");
            //_AETempFrequency = ParseScNode("AETempFrequency", node, ioModule, $"PM.{Module}.Heater.AETempFrequency");
            _PyroWarmIsAlarm = ParseScNode("PyroWarmIsAlarm", node, ioModule, $"PM.{Module}.Heater.PyroWarmIsAlarm");
            _AETempRasingFastIsAlarm = ParseScNode("AETempRasingFastIsAlarm", node, ioModule, $"PM.{Module}.Heater.AETempRasingFastIsAlarm");
        }

        string reason = string.Empty;

        public bool Initialize()
        {

            DATA.Subscribe($"{Module}.{Name}.L1WorkingOPFeedBack", () => L1WorkingOPFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L2WorkingOPFeedBack", () => L2WorkingOPFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L3WorkingOPFeedBack", () => L3WorkingOPFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L1PVFeedBack", () => L1PVFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L2PVFeedBack", () => L2PVFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L3PVFeedBack", () => L3PVFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L1TempHighAlarmFeedBack", () => L1TempHighAlarmFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L2TempHighAlarmFeedBack", () => L2TempHighAlarmFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L3TempHighAlarmFeedBack", () => L3TempHighAlarmFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L1TempLowAlarmFeedBack", () => L1TempLowAlarmFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L2TempLowAlarmFeedBack", () => L2TempLowAlarmFeedBack);
            DATA.Subscribe($"{Module}.{Name}.L3TempLowAlarmFeedBack", () => L3TempLowAlarmFeedBack);

            DATA.Subscribe($"{Module}.{Name}.L1LoopModeSetPoint", () => L1LoopModeSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L2LoopModeSetPoint", () => L2LoopModeSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L3LoopModeSetPoint", () => L3LoopModeSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L1TargetSPSetPoint", () => L1TargetSPSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L2TargetSPSetPoint", () => L2TargetSPSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L3TargetSPSetPoint", () => L3TargetSPSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L1TargetOPSetPoint", () => L1TargetOPSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L2TargetOPSetPoint", () => L2TargetOPSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L3TargetOPSetPoint", () => L3TargetOPSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L1RecipeValueSetPoint", () => L1RecipeValueSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L2RecipeValueSetPoint", () => L2RecipeValueSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L3RecipeValueSetPoint", () => L3RecipeValueSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L1InputTempSetPoint", () => L1InputTempSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L2InputTempSetPoint", () => L2InputTempSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L3InputTempSetPoint", () => L3InputTempSetPoint);
            DATA.Subscribe($"{Module}.{Name}.TCPyroModeSetPoint", () => TCPyroModeSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L1TempHighLimitSetPoint", () => L1TempHighLimitSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L2TempHighLimitSetPoint", () => L2TempHighLimitSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L3TempHighLimitSetPoint", () => L3TempHighLimitSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L1TempLowLimitSetPoint", () => L1TempLowLimitSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L2TempLowLimitSetPoint", () => L2TempLowLimitSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L3TempLowLimitSetPoint", () => L3TempLowLimitSetPoint);
            DATA.Subscribe($"{Module}.{Name}.HeaterModeSetPoint", () => HeaterModeSetPoint);
            DATA.Subscribe($"{Module}.{Name}.PowerRefSetPoint", () => PowerRefSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L1RatioSetPoint", () => L1RatioSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L2RatioSetPoint", () => L2RatioSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L3RatioSetPoint", () => L3RatioSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L1RatedSetPoint", () => L1RatedSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L2RatedSetPoint", () => L2RatedSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L3RatedSetPoint", () => L3RatedSetPoint);
            DATA.Subscribe($"{Module}.{Name}.L1VoltageLimited", () => L1VoltageLimited);
            DATA.Subscribe($"{Module}.{Name}.L2VoltageLimited", () => L2VoltageLimited);
            DATA.Subscribe($"{Module}.{Name}.L3VoltageLimited", () => L3VoltageLimited);
            //DATA.Subscribe($"{Module}.{Name}.TempCtrlTCIN", () => TtempCtrlTCIN);
            DATA.Subscribe($"{Module}.{Name}.PyroTempMaxDiff", () => PyroTempMaxDiff);

            //
            DATA.Subscribe($"{Module}.{Name}.PSU1Y", () => PSU1Y);
            DATA.Subscribe($"{Module}.{Name}.PSU2Y", () => PSU2Y);
            DATA.Subscribe($"{Module}.{Name}.PSU3Y", () => PSU3Y);

            DATA.Subscribe($"{Module}.{Name}.PCPSU1Y", () => PCPSU1Y);
            DATA.Subscribe($"{Module}.{Name}.PCPSU2Y", () => PCPSU2Y);
            DATA.Subscribe($"{Module}.{Name}.PCPSU3Y", () => PCPSU3Y);

            DATA.Subscribe($"{Module}.{Name}.PSU1Power", () => PSU1Power);
            DATA.Subscribe($"{Module}.{Name}.PSU2Power", () => PSU2Power);
            DATA.Subscribe($"{Module}.{Name}.PSU3Power", () => PSU3Power);
            //
            GetPCPSUPower();

            //SetPCPSUY();
            //

            OP.Subscribe($"{Module}.{Name}.SetTargetSP", (out string reason, int time, object[] args) =>
            {
                reason = "";
                _selecetedLoop = "L1";
                SetTargetSPAll(Convert.ToSingle(args[0]), Convert.ToSingle(args[1]), Convert.ToSingle(args[2]), time);

                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetL1TargetSP", (out string reason, int time, object[] args) =>
            {
                _selecetedLoop = "L1";
                float targetSP = Convert.ToSingle(args[0]);

                reason = $"{Display} {_selecetedLoop} Target SP set to {targetSP}";
                EV.PostInfoLog(Module, reason);

                SetTargetSP(_selecetedLoop, targetSP, time);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetL2TargetSP", (out string reason, int time, object[] args) =>
            {
                _selecetedLoop = "L2";
                float targetSP = Convert.ToSingle(args[0]);

                reason = $"{Display} {_selecetedLoop} Target SP set to {targetSP}";
                EV.PostInfoLog(Module, reason);

                SetTargetSP(_selecetedLoop, targetSP, time);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetL3TargetSP", (out string reason, int time, object[] args) =>
            {
                _selecetedLoop = "L3";
                float targetSP = Convert.ToSingle(args[0]);

                reason = $"{Display} {_selecetedLoop} Target SP set to {targetSP}";
                EV.PostInfoLog(Module, reason);

                SetTargetSP(_selecetedLoop, targetSP, time);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetHeaterMode", (out string reason, int time, object[] args) =>
            {
                reason = "";
                SetHeaterMode(Convert.ToSingle(args[0]), time);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetHeaterMode2", (out string reason, int time, object[] args) =>
            {
                reason = "";
                SetHeaterMode2(Convert.ToSingle(args[0]), time);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetRatio", (function, args) =>
            {
                SetRatio(Convert.ToSingle(args[0]), Convert.ToSingle(args[1]), Convert.ToSingle(args[2]));
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetRatedValue", (function, args) =>
            {
                SetRatedValue(Convert.ToSingle(args[0]), Convert.ToSingle(args[1]), Convert.ToSingle(args[2]));
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetRecipeValue", (function, args) =>
            {
                SetRecipeValue(Convert.ToSingle(args[0]), Convert.ToSingle(args[1]), Convert.ToSingle(args[2]));
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.RecipeSetPowerRef", (out string reason, int time, object[] args) =>
            {
                float PowerRef = Convert.ToSingle(args[0]);
                float _PowerRef = PowerRef > 0 ? (PowerRef > 100 ? 100 : PowerRef) : 0;

                if (time > 0)
                {
                    reason = $"{Display}Recipe power Ref ramp to {_PowerRef}";
                }
                else
                {
                    reason = $"{Display}Recipe power Ref set to {_PowerRef}";
                }

                EV.PostInfoLog(Module, reason);

                RecipeSetPowerRef(_PowerRef, time);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.RecipeSetL1Ratio", (out string reason, int time, object[] args) =>
            {
                _selecetedLoop = "L1";
                float ratio = Convert.ToSingle(args[0]);

                reason = $"{Display} Recipe {_selecetedLoop} Ratio set to {ratio}";
                EV.PostInfoLog(Module, reason);

                RecipeSetRatio(_selecetedLoop, ratio, time);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.RecipeSetL2Ratio", (out string reason, int time, object[] args) =>
            {
                _selecetedLoop = "L2";
                float ratio = Convert.ToSingle(args[0]);

                reason = $"{Display} Recipe {_selecetedLoop} Ratio set to {ratio}";
                EV.PostInfoLog(Module, reason);

                RecipeSetRatio(_selecetedLoop, ratio, time);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.RecipeSetL3Ratio", (out string reason, int time, object[] args) =>
            {
                _selecetedLoop = "L3";
                float ratio = Convert.ToSingle(args[0]);

                reason = $"{Display} Recipe {_selecetedLoop} Ratio set to {ratio}";
                EV.PostInfoLog(Module, reason);

                RecipeSetRatio(_selecetedLoop, ratio, time);
                return true;
            });

            //
            OP.Subscribe($"{Module}.{Name}.SetPSUY", (function, args) =>
            {
                SetPSUY(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]), Convert.ToDouble(args[2]));
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetPSUPower", (function, args) =>
            {
                SetPSUPower(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]), Convert.ToDouble(args[2]));
                return true;
            });

            return true;
        }

        #region 手动设置比例 RatioSetPoint
        public bool RecipeSetRatio(string selecetedLoop, float ratio, int time)
        {
            switch (selecetedLoop)
            {
                case "L1":
                    _rampInitValueL1RatiorRecipe = L1RatioSetPoint;
                    _rampTimerL1RatiorRecipe.Stop();

                    if (time > 0)
                    {
                        _rampTimeL1RatiorRecipe = time;
                        _rampTargetL1RatiorRecipe = ratio;
                        _rampTimerL1RatiorRecipe.Start(time);
                    }
                    else
                    {
                        L1RatioSetPoint = ratio;
                    }

                    break;
                case "L2":
                    _rampInitValueL2RatiorRecipe = L2RatioSetPoint;
                    _rampTimerL2RatiorRecipe.Stop();

                    if (time > 0)
                    {
                        _rampTimeL2RatiorRecipe = time;
                        _rampTargetL2RatiorRecipe = ratio;
                        _rampTimerL2RatiorRecipe.Start(time);
                    }
                    else
                    {
                        L2RatioSetPoint = ratio;
                    }
                    break;
                case "L3":
                    _rampInitValueL3RatiorRecipe = L3RatioSetPoint;
                    _rampTimerL3RatiorRecipe.Stop();

                    if (time > 0)
                    {
                        _rampTimeL3RatiorRecipe = time;
                        _rampTargetL3RatiorRecipe = ratio;
                        _rampTimerL3RatiorRecipe.Start(time);
                    }
                    else
                    {
                        L3RatioSetPoint = ratio;
                    }
                    break;
            }

            if (HeaterModeSetPoint != 1 && HeaterModeSetPoint != 2)//设置输出功率
            {
                float f1 = L1RatioSetPoint > 0 ? L1RatioSetPoint : 0;
                float f2 = L2RatioSetPoint > 0 ? L2RatioSetPoint : 0;
                float f3 = L3RatioSetPoint > 0 ? L3RatioSetPoint : 0;

                L1TargetOPSetPoint = f1 > 100 ? 100 : f1;
                L2TargetOPSetPoint = f2 > 100 ? 100 : f2;
                L3TargetOPSetPoint = f3 > 100 ? 100 : f3;
            }

            return true;
        }

        private void MonitorL1RecipeRatioRamping()
        {
            if (!_rampTimerL1RatiorRecipe.IsIdle())
            {
                if (_rampTimerL1RatiorRecipe.IsTimeout() || _rampTimeL1RatiorRecipe == 0)
                {
                    _rampTimerL1RatiorRecipe.Stop();
                    L1RatioSetPoint = _rampTargetL1RatiorRecipe;
                }
                else
                {
                    L1RatioSetPoint = _rampInitValueL1RatiorRecipe + (_rampTargetL1RatiorRecipe - _rampInitValueL1RatiorRecipe) * (float)_rampTimerL1RatiorRecipe.GetElapseTime() / _rampTimeL1RatiorRecipe;

                }
            }
        }

        private void MonitorL2RecipeRatioRamping()
        {
            if (!_rampTimerL2RatiorRecipe.IsIdle())
            {
                if (_rampTimerL2RatiorRecipe.IsTimeout() || _rampTimeL2RatiorRecipe == 0)
                {
                    _rampTimerL2RatiorRecipe.Stop();
                    L2RatioSetPoint = _rampTargetL2RatiorRecipe;
                }
                else
                {
                    L2RatioSetPoint = _rampInitValueL2RatiorRecipe + (_rampTargetL2RatiorRecipe - _rampInitValueL2RatiorRecipe) * (float)_rampTimerL2RatiorRecipe.GetElapseTime() / _rampTimeL2RatiorRecipe;

                }
            }
        }

        private void MonitorL3RecipeRatioRamping()
        {
            if (!_rampTimerL3RatiorRecipe.IsIdle())
            {
                if (_rampTimerL3RatiorRecipe.IsTimeout() || _rampTimeL3RatiorRecipe == 0)
                {
                    _rampTimerL3RatiorRecipe.Stop();
                    L3RatioSetPoint = _rampTargetL3RatiorRecipe;
                }
                else
                {
                    L3RatioSetPoint = _rampInitValueL3RatiorRecipe + (_rampTargetL3RatiorRecipe - _rampInitValueL3RatiorRecipe) * (float)_rampTimerL3RatiorRecipe.GetElapseTime() / _rampTimeL3RatiorRecipe;

                }
            }
        }

        #endregion 手动设置比例 RatioSetPoint

        //TC1
        public bool SetHeaterMode(float HeaterMode, int time)
        {
            string strHeaterMode = "Power";
            float preMode = HeaterModeSetPoint; //前一个模式

            HeaterModeSetPoint = HeaterMode;
            switch (HeaterMode)
            {
                case 0:
                    strHeaterMode = "Power";
                    L1LoopModeSetPoint = 1;
                    L2LoopModeSetPoint = 1;
                    L3LoopModeSetPoint = 1;
                    TCPyroModeSetPoint = 0;

                    if (preMode == 2) //Pyro切到Power之后设置为600
                    {
                        _rampTimerL1.Stop();
                        _rampTimerL2.Stop();
                        _rampTimerL3.Stop();

                        //L1TargetSPSetPoint = 600; 
                        //L2TargetSPSetPoint = 600; 
                        //L3TargetSPSetPoint = 600;
                    }
                    break;
                case 1:
                    strHeaterMode = "Pyro";
                    if (L1LoopModeSetPoint == 1)
                    {
                        _rampTimerL1.Stop();
                        //L2TargetSPSetPoint = L1InputTempSetPoint;
                    }
                    if (L2LoopModeSetPoint == 1)
                    {
                        _rampTimerL2.Stop();
                        //L2TargetSPSetPoint = L2InputTempSetPoint;
                    }
                    if (L3LoopModeSetPoint == 1)
                    {
                        _rampTimerL3.Stop();
                        L3TargetSPSetPoint = L3InputTempSetPoint;
                    }

                    L1LoopModeSetPoint = 1;
                    L2LoopModeSetPoint = 0;

                    //Outer Pyro 模式要根据系统参数来确定Auto或Manual
                    bool IsAuto = SC.GetValue<bool>($"PM.{Module}.Heater.PSUOuterAutoEnable");
                    L3LoopModeSetPoint = IsAuto ? 0 : 1;

                    TCPyroModeSetPoint = 1;
                    break;

            }

            //TC1切换模式时,需要把PV的值设置到设定值


            string reason = $"{Display} Heater Mode set to {strHeaterMode}";

            EV.PostInfoLog(Module, reason);

            return true;
        }

        //TC2
        public bool SetHeaterMode2(float HeaterMode, int time)
        {
            string strHeaterMode = "Power";
            HeaterModeSetPoint = HeaterMode;
            switch (HeaterMode)
            {
                case 0:
                    strHeaterMode = "Power";
                    L1LoopModeSetPoint = 1;
                    L2LoopModeSetPoint = 1;
                    L3LoopModeSetPoint = 1;
                    TCPyroModeSetPoint = 0;
                    break;
                case 1:

                    strHeaterMode = "Pyro";
                    L1LoopModeSetPoint = 1;
                    L2LoopModeSetPoint = 1;
                    if (L3LoopModeSetPoint != 0)
                    {
                        L3TargetSPSetPoint = L3PVFeedBack;
                    }
                    L3LoopModeSetPoint = 0;
                    TCPyroModeSetPoint = 1;
                    break;
            }


            string reason = $"{Display} Heater Mode set to {strHeaterMode}";

            EV.PostInfoLog(Module, reason);

            return true;
        }


        #region Pyro自动模式

        /// <summary>
        /// 都从AE温度开始Ramp
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="TargetSP"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private bool SetTargetSP(string loop, float TargetSP, int time)
        {
            float startPoint = 0;
            switch (loop)
            {
                case "L1":

                    //如果是Power模式直接设置到TargeSP即可
                    if (HeaterModeSetPoint == 0)
                    {
                        L1TargetSPSetPoint = TargetSP;
                        return true;
                    }

                    _rampTimerL1.Stop();

                    _rampInitValueL1 = L1InputTempSetPoint;
                    startPoint = L1InputTempSetPoint;

                    _rampTimeL1 = time;
                    if (time == 0)
                    {
                        _rampTimeL1 = Math.Abs((int)((TargetSP - startPoint) / _tempRampRatio.DoubleValue) * 1000);
                    }
                    _rampTargetL1 = TargetSP;
                    _rampTimerL1.Start(_rampTimeL1);
                    break;
                case "L2":

                    //如果是Power模式直接设置到TargeSP即可
                    if (HeaterModeSetPoint == 0)
                    {
                        L2TargetSPSetPoint = TargetSP;
                        return true;
                    }

                    _rampTimerL2.Stop();

                    _rampInitValueL2 = L2InputTempSetPoint;
                    startPoint = L2InputTempSetPoint;

                    _rampTimeL2 = time;
                    if (time == 0)
                    {
                        _rampTimeL2 = Math.Abs((int)((TargetSP - startPoint) / _tempRampRatio.DoubleValue) * 1000);
                    }
                    _rampTargetL2 = TargetSP;
                    _rampTimerL2.Start(_rampTimeL2);
                    break;
                case "L3":

                    //如果是Power模式直接设置到TargeSP即可
                    if (HeaterModeSetPoint == 0)
                    {
                        L3TargetSPSetPoint = TargetSP;
                        return true;
                    }

                    _rampTimerL3.Stop();
                    _rampInitValueL3 = L3InputTempSetPoint;
                    startPoint = L3InputTempSetPoint;

                    _rampTimeL3 = time;
                    if (time == 0)
                    {
                        _rampTimeL3 = Math.Abs((int)((TargetSP - startPoint) / _tempRampRatio.DoubleValue) * 1000);
                    }
                    _rampTargetL3 = TargetSP;
                    _rampTimerL3.Start(_rampTimeL3);
                    break;
            }

            return true;
        }

        public bool SetTargetSPAll(float TargetSP1, float TargetSP2, float TargetSP3, int time)
        {
            float startPoint = 0;
            _rampTimerL1.Stop();
            if (HeaterModeSetPoint == 2)//pyro
            {
                _rampInitValueL1 = L1InputTempSetPoint;
                startPoint = L1InputTempSetPoint;
            }
            else if (HeaterModeSetPoint == 1)//Tc
            {
                _rampInitValueL1 = L1PVFeedBack;
                startPoint = L1PVFeedBack;
            }
            else if (HeaterModeSetPoint == 0)//power
            {
                _rampInitValueL1 = L1TargetSPSetPoint;
                startPoint = L1TargetSPSetPoint;
            }
            _rampTimeL1 = time;
            if (time == 0)
            {
                _rampTimeL1 = Math.Abs((int)((TargetSP1 - startPoint) / _tempRampRatio.DoubleValue) * 1000);
            }
            _rampTargetL1 = TargetSP1;
            _rampTimerL1.Start(_rampTimeL1);



            _rampTimerL2.Stop();
            if (HeaterModeSetPoint == 2)//pyro
            {
                _rampInitValueL2 = L2InputTempSetPoint;
                startPoint = L2InputTempSetPoint;
            }
            else if (HeaterModeSetPoint == 1)//Tc
            {
                _rampInitValueL2 = L2PVFeedBack;
                startPoint = L2PVFeedBack;
            }
            else if (HeaterModeSetPoint == 0)//power
            {
                _rampInitValueL2 = L2TargetSPSetPoint;
                startPoint = L2TargetSPSetPoint;
            }

            _rampTimeL2 = time;
            if (time == 0)
            {
                _rampTimeL2 = Math.Abs((int)((TargetSP2 - startPoint) / _tempRampRatio.DoubleValue) * 1000);
            }
            _rampTargetL2 = TargetSP2;
            _rampTimerL2.Start(_rampTimeL2);



            _rampTimerL3.Stop();
            if (HeaterModeSetPoint == 2)//pyro
            {
                _rampInitValueL3 = L3InputTempSetPoint;
                startPoint = L3InputTempSetPoint;
            }
            else if (HeaterModeSetPoint == 1)//Tc
            {
                _rampInitValueL3 = L3PVFeedBack;
                startPoint = L3PVFeedBack;
            }
            else if (HeaterModeSetPoint == 0)//power
            {
                _rampInitValueL3 = L3TargetSPSetPoint;
                startPoint = L3TargetSPSetPoint;
            }
            _rampTimeL3 = time;
            if (time == 0)
            {
                _rampTimeL3 = Math.Abs((int)((TargetSP3 - startPoint) / _tempRampRatio.DoubleValue) * 1000);
            }
            _rampTargetL3 = TargetSP3;
            _rampTimerL3.Start(_rampTimeL3);

            string reason = $"{Display} Set Target SP ";

            EV.PostInfoLog(Module, reason);

            return true;
        }

        private void MonitorL1Ramping()
        {
            if (!_rampTimerL1.IsIdle())
            {
                if (_rampTimerL1.IsTimeout() || _rampTimeL1 == 0)
                {
                    _rampTimerL1.Stop();
                    L1TargetSPSetPoint = _rampTargetL1;
                }
                else
                {
                    L1TargetSPSetPoint = _rampInitValueL1 + (_rampTargetL1 - _rampInitValueL1) * (float)_rampTimerL1.GetElapseTime() / _rampTimeL1;
                }
            }
        }

        private void MonitorL2Ramping()
        {
            if (!_rampTimerL2.IsIdle())
            {
                if (_rampTimerL2.IsTimeout() || _rampTimeL2 == 0)
                {
                    _rampTimerL2.Stop();
                    L2TargetSPSetPoint = _rampTargetL2;
                }
                else
                {
                    L2TargetSPSetPoint = _rampInitValueL2 + (_rampTargetL2 - _rampInitValueL2) * (float)_rampTimerL2.GetElapseTime() / _rampTimeL2;
                }
            }
        }

        private void MonitorL3Ramping()
        {
            if (!_rampTimerL3.IsIdle())
            {
                if (_rampTimerL3.IsTimeout() || _rampTimeL3 == 0)
                {
                    _rampTimerL3.Stop();
                    L3TargetSPSetPoint = _rampTargetL3;
                }
                else
                {
                    L3TargetSPSetPoint = _rampInitValueL3 + (_rampTargetL3 - _rampInitValueL3) * (float)_rampTimerL3.GetElapseTime() / _rampTimeL3;
                }
            }
        }
        #endregion Pyro自动模式


        #region Power手动模式
        //PSU SCR
        public bool SetRatio(float Ratio1, float Ratio2, float Ratio3)
        {
            L2RatioSetPoint = Ratio2 > 100 ? 100 : Ratio2;
            L1RatioSetPoint = Ratio1 > 100 ? 100 : Ratio1;
            L3RatioSetPoint = Ratio3 > 100 ? 100 : Ratio3;

            string reason = $"{Display} Set Ratio ";
            EV.PostInfoLog(Module, reason);

            return true;
        }

        //自动执行(比例和模式由其它方法设置)
        private void MonitorSetOP()
        {
            if (Name == "TC1")
            {
                if (HeaterModeSetPoint == 1 || HeaterModeSetPoint == 2) //Pyro和TC
                {
                    L2TargetOPSetPoint = L2WorkingOPFeedBack;
                    float l1 = L2RatioSetPoint > 0 ? (float)(L2WorkingOPFeedBack * L1RatioSetPoint / L2RatioSetPoint) : 0;
                    L1TargetOPSetPoint = l1 > 100 ? 100 : l1;
                    float l3 = L2RatioSetPoint > 0 ? (float)(L2WorkingOPFeedBack * L3RatioSetPoint / L2RatioSetPoint) : 0;
                    L3TargetOPSetPoint = l3 > 100 ? 100 : l3;
                }
                else //Power
                {
                    L1TargetOPSetPoint = L1RatioSetPoint > 100 ? 100 : L1RatioSetPoint;
                    L2TargetOPSetPoint = L2RatioSetPoint > 100 ? 100 : L2RatioSetPoint;
                    L3TargetOPSetPoint = L3RatioSetPoint > 100 ? 100 : L3RatioSetPoint;
                }
            }
            else if (Name == "TC2")
            {
                if (HeaterModeSetPoint == 1 || HeaterModeSetPoint == 2)//Pyro和TC
                {
                    float l1 = L3RatioSetPoint > 0 ? (float)(L3WorkingOPFeedBack * L1RatioSetPoint / L3RatioSetPoint) : 0;
                    float l2 = L3RatioSetPoint > 0 ? (float)(L3WorkingOPFeedBack * L2RatioSetPoint / L3RatioSetPoint) : 0;

                    L3TargetOPSetPoint = L3WorkingOPFeedBack;
                    L1TargetOPSetPoint = l1 > 100 ? 100 : l1;
                    L2TargetOPSetPoint = l2 > 100 ? 100 : l2;
                }
                else //Power
                {
                    L1TargetOPSetPoint = L1RatioSetPoint > 100 ? 100 : L1RatioSetPoint;
                    L2TargetOPSetPoint = L2RatioSetPoint > 100 ? 100 : L2RatioSetPoint;
                    L3TargetOPSetPoint = L3RatioSetPoint > 100 ? 100 : L3RatioSetPoint;
                }
            }
        }
        #endregion Power手动模式


        public void Monitor()
        {
            try
            {
                MonitorL1Ramping();
                MonitorL2Ramping();
                MonitorL3Ramping();


                MonitorSetOP();

                MonitorL1RecipeRatioRamping();
                MonitorL2RecipeRatioRamping();
                MonitorL3RecipeRatioRamping();

                MonitorRecipeSetPowerRefRamping();

                MonitorTempLimit();
                MonitorAKTemp();
                MonitorAlarm();
                //MonitorAETempRasingFastAlarm();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void StopRamp()
        {
            if (!_rampTimerL1.IsIdle())
            {
                _rampTimerL1.Stop();
            }
            if (!_rampTimerL2.IsIdle())
            {
                _rampTimerL2.Stop();
            }
            if (!_rampTimerL3.IsIdle())
            {
                _rampTimerL3.Stop();
            }
            if (!_rampTimerL1RatiorRecipe.IsIdle())
            {
                _rampTimerL1RatiorRecipe.Stop();
            }
            if (!_rampTimerL2RatiorRecipe.IsIdle())
            {
                _rampTimerL2RatiorRecipe.Stop();
            }
            if (!_rampTimerL3RatiorRecipe.IsIdle())
            {
                _rampTimerL3RatiorRecipe.Stop();
            }
        }



        #region 不重要
        //设置额定功率
        private bool SetRatedValue(float Ratio1, float Ratio2, float Ratio3)
        {
            L1RatedSetPoint = Ratio1;
            L2RatedSetPoint = Ratio2;
            L3RatedSetPoint = Ratio3;

            string reason = $"{Display} Set RatedValue";
            EV.PostInfoLog(Module, reason);

            return true;
        }


        private bool SetRecipeValue(float Ratio1, float Ratio2, float Ratio3)
        {
            L1RecipeValueSetPoint = Ratio1;
            L2RecipeValueSetPoint = Ratio2;
            L3RecipeValueSetPoint = Ratio3;

            string reason = $"{Display} Set RecipeValue ";
            EV.PostInfoLog(Module, reason);

            return true;
        }


        public void RecipeSetPowerRef(float PowerRef, int time)
        {
            _rampTimerSetPowerRef.Stop();

            if (time > 0)
            {
                _rampInitValueSetPowerRefRecipe = PowerRefSetPoint;
                _rampTimeSetPowerRefRecipe = time;
                _rampTargetSetPowerRefRecipe = PowerRef;
                _rampTimerSetPowerRef.Start(_rampTimeSetPowerRefRecipe);
            }
            else
            {
                PowerRefSetPoint = PowerRef;
            }
        }

        private void MonitorRecipeSetPowerRefRamping()
        {
            if (!_rampTimerSetPowerRef.IsIdle())
            {
                if (_rampTimerSetPowerRef.IsTimeout() || _rampTimeSetPowerRefRecipe == 0)
                {
                    _rampTimerSetPowerRef.Stop();
                    PowerRefSetPoint = _rampTargetSetPowerRefRecipe;
                }
                else
                {
                    PowerRefSetPoint = _rampInitValueSetPowerRefRecipe + (_rampTargetSetPowerRefRecipe - _rampInitValueSetPowerRefRecipe) * (float)_rampTimerSetPowerRef.GetElapseTime() / _rampTimeSetPowerRefRecipe;
                }
            }
        }
        #endregion 不重要


        #region 报警
        /// <summary>
        /// 昂坤三个温度相互之间的最大差值
        /// </summary>
        public double PyroTempMaxDiff
        {
            get
            {
                if (MiddleTemp > _pyroWarmBaseTemp.DoubleValue || OuterTemp > _pyroWarmBaseTemp.DoubleValue)
                {
                    double tempMaxDiff = Math.Abs(OuterTemp - MiddleTemp);
                    //if (Math.Abs(MiddleTemp - InnerTemp) > tempMaxDiff)
                    //{
                    //    tempMaxDiff = Math.Abs(MiddleTemp - InnerTemp);
                    //}
                    //if (Math.Abs(OuterTemp - InnerTemp) > tempMaxDiff)
                    //{
                    //    tempMaxDiff = Math.Abs(OuterTemp - InnerTemp);
                    //}

                    return tempMaxDiff;
                }
                return 0;
            }
        }


        private R_TRIG _pyroTempMaxDiffWarm = new R_TRIG();
        private void MonitorAlarm()
        {
            string status = DATA.Poll($"{Module}.Status") == null ? "" : DATA.Poll($"{Module}.Status").ToString(); //状态
            int processCountTime = DATA.Poll($"{Module}.RecipeTotalTime") == null ? 0 : Convert.ToInt32(DATA.Poll($"{Module}.RecipeTotalTime")); //Process总时间
            int recipeStepTime = DATA.Poll($"{Module}.RecipeTotalElapseTime") == null ? 0 : Convert.ToInt32(DATA.Poll($"{Module}.RecipeTotalElapseTime"));     //已经运行的时间

            if (status == "Process" && recipeStepTime > _pyroWarmEffectFromTime.IntValue && processCountTime - recipeStepTime > _pyroWarmEffectEndTime.IntValue)
            {
                _pyroTempMaxDiffWarm.CLK = PyroTempMaxDiff > _pyroWarmMaxDiff.DoubleValue && _pyroWarmMaxDiff.DoubleValue > 0;
                if (_pyroTempMaxDiffWarm.Q)
                {
                    if (_PyroWarmIsAlarm.BoolValue)
                    {
                        EV.PostAlarmLog(Module, $"Pyro Temp Max Difference is over {_pyroWarmMaxDiff.DoubleValue} ℃");
                        _doPyroWarnMaxDiff.Value = true;
                    }
                    else
                    {
                        EV.PostWarningLog(Module, $"Pyro Temp Max Difference is over {_pyroWarmMaxDiff.DoubleValue} ℃");
                    }
                }
            }
        }

        //DateTime dtLastRecordTime = DateTime.Now;
        DeviceTimer tempMonitorDT = new DeviceTimer();
        private double lastInnerTemp = 0;
        private double lastMiddleTemp = 0;
        private double lastOuterTemp = 0;

        //private void MonitorAETempRasingFastAlarm()
        //{
        //    string pmStatus = DATA.Poll($"{Module}.Status") == null ? "" : DATA.Poll($"{Module}.Status").ToString();

        //    if (pmStatus == "Process")
        //    {
        //        if (Math.Abs(InnerTemp - lastInnerTemp) * 1000 > _AETempInnerRasingRate.DoubleValue * tempMonitorDT.GetElapseTime())
        //        {
        //            if (_AETempRasingFastIsAlarm.BoolValue)
        //            {
        //                EV.PostAlarmLog(Module, $"AETemp Inner rasing fast");
        //            }
        //            else
        //            {
        //                EV.PostWarningLog(Module, $"AETemp Inner rasing fast");
        //            }
        //        }

        //        if (Math.Abs(MiddleTemp - lastMiddleTemp) * 1000 > _AETempMiddleRasingRate.DoubleValue * tempMonitorDT.GetElapseTime())
        //        {
        //            if (_AETempRasingFastIsAlarm.BoolValue)
        //            {
        //                EV.PostAlarmLog(Module, $"AETemp Middle rasing fast");
        //            }
        //            else
        //            {
        //                EV.PostWarningLog(Module, $"AETemp Middle rasing fast");
        //            }
        //        }

        //        if (Math.Abs(OuterTemp - lastOuterTemp) * 1000 > _AETempOuterRasingRate.DoubleValue * tempMonitorDT.GetElapseTime())
        //        {
        //            if (_AETempRasingFastIsAlarm.BoolValue)
        //            {
        //                EV.PostAlarmLog(Module, $"AETemp Outer rasing fast");
        //            }
        //            else
        //            {
        //                EV.PostWarningLog(Module, $"AETemp Outer rasing fast");
        //            }

        //            lastInnerTemp = InnerTemp;
        //            lastMiddleTemp = MiddleTemp;
        //            lastOuterTemp = OuterTemp;
        //        }
        //    }

        //    tempMonitorDT.Start(0);
        //}


        public void MonitorAKTemp()
        {
            if (Name == "TC1" || Name == "PSU")
            {
                L2InputTempSetPoint = (float)MiddleTemp;
                L3InputTempSetPoint = (float)OuterTemp;
            }
            else
            {
                L3InputTempSetPoint = (float)InnerTemp;
            }
        }

        private int tryCount = 0;
        /// <summary>
        /// 掉线设置温度上下限
        /// </summary>
        private void MonitorTempLimit()
        {
            if (Name == "TC1" || Name == "PSU")
            {
                if (L1TempHighLimitSetPoint != _PSUHighLimit.DoubleValue)
                {
                    L1TempHighLimitSetPoint = (float)_PSUHighLimit.DoubleValue;
                }
                if (L2TempHighLimitSetPoint != _PSUHighLimit.DoubleValue)
                {
                    L2TempHighLimitSetPoint = (float)_PSUHighLimit.DoubleValue;
                }
                if (L3TempHighLimitSetPoint != _PSUHighLimit.DoubleValue)
                {
                    L3TempHighLimitSetPoint = (float)_PSUHighLimit.DoubleValue;
                }

                if (L1TempLowLimitSetPoint != _PSULowLimit.DoubleValue)
                {
                    L1TempLowLimitSetPoint = (float)_PSULowLimit.DoubleValue;
                }
                if (L2TempLowLimitSetPoint != _PSULowLimit.DoubleValue)
                {
                    L2TempLowLimitSetPoint = (float)_PSULowLimit.DoubleValue;
                }
                if (L3TempLowLimitSetPoint != _PSULowLimit.DoubleValue)
                {
                    L3TempLowLimitSetPoint = (float)_PSULowLimit.DoubleValue;
                }
            }
            else if (Name == "TC2" || Name == "SCR")
            {
                if (L1TempHighLimitSetPoint != _SCRHighLimit.DoubleValue)
                {
                    L1TempHighLimitSetPoint = (float)_PSUHighLimit.DoubleValue;
                }
                if (L2TempHighLimitSetPoint != _SCRHighLimit.DoubleValue)
                {
                    L2TempHighLimitSetPoint = (float)_SCRHighLimit.DoubleValue;
                }
                if (L3TempHighLimitSetPoint != _SCRHighLimit.DoubleValue)
                {
                    L3TempHighLimitSetPoint = (float)_SCRHighLimit.DoubleValue;
                }

                if (L1TempLowLimitSetPoint != _SCRLowLimit.DoubleValue)
                {
                    L1TempLowLimitSetPoint = (float)_SCRLowLimit.DoubleValue;
                }
                if (L2TempLowLimitSetPoint != _SCRLowLimit.DoubleValue)
                {
                    L2TempLowLimitSetPoint = (float)_SCRLowLimit.DoubleValue;
                }
                if (L3TempLowLimitSetPoint != _SCRLowLimit.DoubleValue)
                {
                    L3TempLowLimitSetPoint = (float)_SCRLowLimit.DoubleValue;
                }
            }

        }

        /// <summary>
        /// 激活AeTempRaisingFast报警DO输出。
        /// </summary>
        public void ActivateAlarmDoAeTempRaisingFast()
        {
            _doAeTempRaisingFast.Value = true;
        }

        #endregion 报警


        public void Terminate()
        {

        }

        public void Reset()
        {
            _trigSetTempLimit.RST = true;
            _pyroTempMaxDiffWarm.RST = true;

            // 复位给PLC的报警信号
            _doPyroWarnMaxDiff.Value = false;
            _doAeTempRaisingFast.Value = false;
        }

        #region PSU Y Calc

        public void SetPCPSUY()
        {
            PSU1Y = Convert.ToInt32(PCPSU1Y);
            PSU2Y = Convert.ToInt32(PCPSU2Y);
            PSU3Y = Convert.ToInt32(PCPSU3Y);
        }
        public bool SetPSUY(double _dbPSU1Y, double _dbPSU2Y, double _dbPSU3Y)
        {
            int iPSU1Y = Convert.ToInt32(_dbPSU1Y * 10);
            int iPSU2Y = Convert.ToInt32(_dbPSU2Y * 10);
            int iPSU3Y = Convert.ToInt32(_dbPSU3Y * 10);

            //写入IO
            PSU1Y = iPSU1Y;
            PSU2Y = iPSU2Y;
            PSU3Y = iPSU3Y;

            //写入配置
            if ((iPSU1Y > 0 && iPSU1Y < 1000)
                && (iPSU2Y > 0 && iPSU2Y < 1000)
                && (iPSU3Y > 0 && iPSU3Y < 1000)
                )
            {
                SC.SetItemValue($"PM.{Module}.Heater.PSU1Y", (double)iPSU1Y);
                SC.SetItemValue($"PM.{Module}.Heater.PSU2Y", (double)iPSU2Y);
                SC.SetItemValue($"PM.{Module}.Heater.PSU3Y", (double)iPSU3Y);
            }

            string reason = $"{Display} Set PSUY values.";
            EV.PostInfoLog(Module, reason);

            return true;
        }


        public bool SetPSUPower(double psu1Power, double psu2Power, double psu3Power)
        {
            //写入配置
            if (psu1Power > 0
                && psu2Power > 0
                && psu3Power > 0
                )
            {
                SC.SetItemValue($"PM.{Module}.Heater.PSU1Power", psu1Power);
                SC.SetItemValue($"PM.{Module}.Heater.PSU2Power", psu2Power);
                SC.SetItemValue($"PM.{Module}.Heater.PSU3Power", psu3Power);
            }

            string reason = $"{Display} Save SystemConfig PSUPower values.";
            EV.PostInfoLog(Module, reason);

            return true;
        }
        public void GetPCPSUPower()
        {
            PSU1Power = SC.GetConfigItem("PM.PM1.Heater.PSU1Power").DoubleValue;
            PSU2Power = SC.GetConfigItem("PM.PM1.Heater.PSU2Power").DoubleValue;
            PSU3Power = SC.GetConfigItem("PM.PM1.Heater.PSU3Power").DoubleValue;
        }


        #endregion

        #region Property
        public double AETemp1
        {
            get
            {
                if (_AETempEnable.BoolValue)
                {
                    try
                    {

                        object temp1 = DATA.Poll($"{Module}.AETemp.AETemp1");
                        return temp1 == null ? 0 : (double)temp1;

                    }
                    catch (Exception ex)
                    {
                        return 0;
                    }
                }
                return 0;
            }
        }
        public double AETemp2
        {
            get
            {
                if (_AETempEnable.BoolValue)
                {
                    try
                    {
                        object temp1 = DATA.Poll($"{Module}.AETemp.AETemp2");
                        return temp1 == null ? 0 : (double)temp1;
                    }
                    catch (Exception ex)
                    {
                        return 0;
                    }
                }
                return 0;
            }

        }
        public double AETemp3
        {
            get
            {
                if (_AETempEnable.BoolValue)
                {
                    try
                    {
                        object temp1 = DATA.Poll($"{Module}.AETemp.AETemp3");
                        return temp1 == null ? 0 : (double)temp1;
                    }
                    catch (Exception ex)
                    {
                        return 0;
                    }
                }
                return 0;
            }
        }

        public double InnerTemp
        {
            get
            {
                return AETemp1;
            }
        }

        public double MiddleTemp
        {
            get
            {
                return AETemp2;
            }
        }

        public double OuterTemp
        {
            get
            {
                return AETemp3;
            }
        }

        public float L1WorkingOPFeedBack
        {
            get
            {
                return _aiL1WorkingOPFeedBack == null ? 0 : (_isFloatAioType ? _aiL1WorkingOPFeedBack.FloatValue : _aiL1WorkingOPFeedBack.Value);
            }
        }
        public float L2WorkingOPFeedBack
        {
            get
            {
                return _aiL2WorkingOPFeedBack == null ? 0 : (_isFloatAioType ? _aiL2WorkingOPFeedBack.FloatValue : _aiL2WorkingOPFeedBack.Value);
            }
        }
        public float L3WorkingOPFeedBack
        {
            get
            {
                return _aiL3WorkingOPFeedBack == null ? 0 : (_isFloatAioType ? _aiL3WorkingOPFeedBack.FloatValue : _aiL3WorkingOPFeedBack.Value);
            }
        }
        public float L1PVFeedBack
        {
            get
            {
                return _aiL1PVFeedBack == null ? 0 : (_isFloatAioType ? _aiL1PVFeedBack.FloatValue : _aiL1PVFeedBack.Value);
            }
        }
        public float L2PVFeedBack
        {
            get
            {
                return _aiL2PVFeedBack == null ? 0 : (_isFloatAioType ? _aiL2PVFeedBack.FloatValue : _aiL2PVFeedBack.Value);
            }
        }
        public float L3PVFeedBack
        {
            get
            {
                return _aiL3PVFeedBack == null ? 0 : (_isFloatAioType ? _aiL3PVFeedBack.FloatValue : _aiL3PVFeedBack.Value);
            }
        }
        public bool L1TempHighAlarmFeedBack
        {
            get
            {
                return _diL1TempHighAlarmFeedBack == null ? false : _diL1TempHighAlarmFeedBack.Value;
            }
        }
        public bool L2TempHighAlarmFeedBack
        {
            get
            {
                return _diL2TempHighAlarmFeedBack == null ? false : _diL2TempHighAlarmFeedBack.Value;
            }
        }
        public bool L3TempHighAlarmFeedBack
        {
            get
            {
                return _diL3TempHighAlarmFeedBack == null ? false : _diL3TempHighAlarmFeedBack.Value;
            }
        }
        public bool L1TempLowAlarmFeedBack
        {
            get
            {
                return _diL1TempLowAlarmFeedBack == null ? false : _diL1TempLowAlarmFeedBack.Value;
            }
        }
        public bool L2TempLowAlarmFeedBack
        {
            get
            {
                return _diL2TempLowAlarmFeedBack == null ? false : _diL2TempLowAlarmFeedBack.Value;
            }
        }
        public bool L3TempLowAlarmFeedBack
        {
            get
            {
                return _diL3TempLowAlarmFeedBack == null ? false : _diL3TempLowAlarmFeedBack.Value;
            }
        }


        public float L1LoopModeSetPoint
        {
            get
            {
                return _aoL1LoopModeSetPoint == null ? 0 : (_isFloatAioType ? _aoL1LoopModeSetPoint.FloatValue : _aoL1LoopModeSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1LoopModeSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL1LoopModeSetPoint.Value = (short)value;
                }
            }
        }
        public float L2LoopModeSetPoint
        {
            get
            {
                return _aoL2LoopModeSetPoint == null ? 0 : (_isFloatAioType ? _aoL2LoopModeSetPoint.FloatValue : _aoL2LoopModeSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2LoopModeSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL2LoopModeSetPoint.Value = (short)value;
                }
            }
        }
        public float L3LoopModeSetPoint
        {
            get
            {
                return _aoL3LoopModeSetPoint == null ? 0 : (_isFloatAioType ? _aoL3LoopModeSetPoint.FloatValue : _aoL3LoopModeSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3LoopModeSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL3LoopModeSetPoint.Value = (short)value;
                }
            }
        }
        public float L1TargetSPSetPoint
        {
            get
            {
                return _aoL1TargetSPSetPoint == null ? 0 : (_isFloatAioType ? _aoL1TargetSPSetPoint.FloatValue : _aoL1TargetSPSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1TargetSPSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL1TargetSPSetPoint.Value = (short)value;
                }
            }
        }
        public float L2TargetSPSetPoint
        {
            get
            {
                return _aoL2TargetSPSetPoint == null ? 0 : (_isFloatAioType ? _aoL2TargetSPSetPoint.FloatValue : _aoL2TargetSPSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2TargetSPSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL2TargetSPSetPoint.Value = (short)value;
                }
            }
        }
        public float L3TargetSPSetPoint
        {
            get
            {
                return _aoL3TargetSPSetPoint == null ? 0 : (_isFloatAioType ? _aoL3TargetSPSetPoint.FloatValue : _aoL3TargetSPSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3TargetSPSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL3TargetSPSetPoint.Value = (short)value;
                }
            }
        }
        public float L1TargetOPSetPoint
        {
            get
            {
                return _aoL1TargetOPSetPoint == null ? 0 : (_isFloatAioType ? _aoL1TargetOPSetPoint.FloatValue : _aoL1TargetOPSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1TargetOPSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL1TargetOPSetPoint.Value = (short)value;
                }
            }
        }
        public float L2TargetOPSetPoint
        {
            get
            {
                return _aoL2TargetOPSetPoint == null ? 0 : (_isFloatAioType ? _aoL2TargetOPSetPoint.FloatValue : _aoL2TargetOPSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2TargetOPSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL2TargetOPSetPoint.Value = (short)value;
                }
            }
        }
        public float L3TargetOPSetPoint
        {
            get
            {
                return _aoL3TargetOPSetPoint == null ? 0 : (_isFloatAioType ? _aoL3TargetOPSetPoint.FloatValue : _aoL3TargetOPSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3TargetOPSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL3TargetOPSetPoint.Value = (short)value;
                }
            }
        }
        public float L1RecipeValueSetPoint
        {
            get
            {
                return _aoL1RecipeValueSetPoint == null ? 0 : (_isFloatAioType ? _aoL1RecipeValueSetPoint.FloatValue : _aoL1RecipeValueSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1RecipeValueSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL1RecipeValueSetPoint.Value = (short)value;
                }
            }
        }
        public float L2RecipeValueSetPoint
        {
            get
            {
                return _aoL2RecipeValueSetPoint == null ? 0 : (_isFloatAioType ? _aoL2RecipeValueSetPoint.FloatValue : _aoL2RecipeValueSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2RecipeValueSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL2RecipeValueSetPoint.Value = (short)value;
                }
            }
        }
        public float L3RecipeValueSetPoint
        {
            get
            {
                return _aoL3RecipeValueSetPoint == null ? 0 : (_isFloatAioType ? _aoL3RecipeValueSetPoint.FloatValue : _aoL3RecipeValueSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3RecipeValueSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL3RecipeValueSetPoint.Value = (short)value;
                }
            }
        }

        public float L1InputTempSetPoint
        {
            get
            {
                return _aoL1InputTempSetPoint == null ? 0 : (_isFloatAioType ? _aoL1InputTempSetPoint.FloatValue : _aoL1InputTempSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1InputTempSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL1InputTempSetPoint.Value = (short)value;
                }
            }
        }
        public float L2InputTempSetPoint
        {
            get
            {
                return _aoL2InputTempSetPoint == null ? 0 : (_isFloatAioType ? _aoL2InputTempSetPoint.FloatValue : _aoL2InputTempSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2InputTempSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL2InputTempSetPoint.Value = (short)value;
                }
            }
        }

        public float L3InputTempSetPoint
        {
            get
            {
                return _aoL3InputTempSetPoint == null ? 0 : (_isFloatAioType ? _aoL3InputTempSetPoint.FloatValue : _aoL3InputTempSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3InputTempSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL3InputTempSetPoint.Value = (short)value;
                }
            }
        }
        public float TCPyroModeSetPoint
        {
            get
            {
                return _aoTCPyroModeSetPoint == null ? 0 : (_isFloatAioType ? _aoTCPyroModeSetPoint.FloatValue : _aoTCPyroModeSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoTCPyroModeSetPoint.FloatValue = value;
                }
                else
                {
                    _aoTCPyroModeSetPoint.Value = (short)value;
                }
            }
        }
        public float L1TempHighLimitSetPoint
        {
            get
            {
                return _aoL1TempHighLimitSetPoint == null ? 0 : (_isFloatAioType ? _aoL1TempHighLimitSetPoint.FloatValue : _aoL1TempHighLimitSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1TempHighLimitSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL1TempHighLimitSetPoint.Value = (short)value;
                }
            }
        }
        public float L2TempHighLimitSetPoint
        {
            get
            {
                return _aoL2TempHighLimitSetPoint == null ? 0 : (_isFloatAioType ? _aoL2TempHighLimitSetPoint.FloatValue : _aoL2TempHighLimitSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2TempHighLimitSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL2TempHighLimitSetPoint.Value = (short)value;
                }
            }
        }
        public float L3TempHighLimitSetPoint
        {
            get
            {
                return _aoL3TempHighLimitSetPoint == null ? 0 : (_isFloatAioType ? _aoL3TempHighLimitSetPoint.FloatValue : _aoL3TempHighLimitSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3TempHighLimitSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL3TempHighLimitSetPoint.Value = (short)value;
                }
            }
        }
        public float L1TempLowLimitSetPoint
        {
            get
            {
                return _aoL1TempLowLimitSetPoint == null ? 0 : (_isFloatAioType ? _aoL1TempLowLimitSetPoint.FloatValue : _aoL1TempLowLimitSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1TempLowLimitSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL1TempLowLimitSetPoint.Value = (short)value;
                }
            }
        }
        public float L2TempLowLimitSetPoint
        {
            get
            {
                return _aoL2TempLowLimitSetPoint == null ? 0 : (_isFloatAioType ? _aoL2TempLowLimitSetPoint.FloatValue : _aoL2TempLowLimitSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2TempLowLimitSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL2TempLowLimitSetPoint.Value = (short)value;
                }
            }
        }
        public float L3TempLowLimitSetPoint
        {
            get
            {
                return _aoL3TempLowLimitSetPoint == null ? 0 : (_isFloatAioType ? _aoL3TempLowLimitSetPoint.FloatValue : _aoL3TempLowLimitSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3TempLowLimitSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL3TempLowLimitSetPoint.Value = (short)value;
                }
            }
        }
        public float HeaterModeSetPoint
        {
            get
            {
                return _aoHeaterModeSetPoint == null ? 0 : (_isFloatAioType ? _aoHeaterModeSetPoint.FloatValue : _aoHeaterModeSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoHeaterModeSetPoint.FloatValue = value;
                }
                else
                {
                    _aoHeaterModeSetPoint.Value = (short)value;
                }
            }
        }
        public float PowerRefSetPoint
        {
            get
            {
                return _aoPowerRefSetPoint == null ? 0 : (_isFloatAioType ? _aoPowerRefSetPoint.FloatValue : _aoPowerRefSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPowerRefSetPoint.FloatValue = value;
                }
                else
                {
                    _aoPowerRefSetPoint.Value = (short)value;
                }
            }
        }
        public float L1RatioSetPoint
        {
            get
            {
                return _aoL1RatioSetPoint == null ? 0 : (_isFloatAioType ? _aoL1RatioSetPoint.FloatValue : _aoL1RatioSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1RatioSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL1RatioSetPoint.Value = (short)value;
                }
            }
        }
        public float L2RatioSetPoint
        {
            get
            {
                return _aoL2RatioSetPoint == null ? 0 : (_isFloatAioType ? _aoL2RatioSetPoint.FloatValue : _aoL2RatioSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2RatioSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL2RatioSetPoint.Value = (short)value;
                }
            }
        }
        public float L3RatioSetPoint
        {
            get
            {
                return _aoL3RatioSetPoint == null ? 0 : (_isFloatAioType ? _aoL3RatioSetPoint.FloatValue : _aoL3RatioSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3RatioSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL3RatioSetPoint.Value = (short)value;
                }
            }
        }

        public float L1RatedSetPoint
        {
            get
            {
                return _aoL1RatedSetPoint == null ? 0 : (_isFloatAioType ? _aoL1RatedSetPoint.FloatValue : _aoL1RatedSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1RatedSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL1RatedSetPoint.Value = (short)value;
                }
            }
        }
        public float L2RatedSetPoint
        {
            get
            {
                return _aoL2RatedSetPoint == null ? 0 : (_isFloatAioType ? _aoL2RatedSetPoint.FloatValue : _aoL2RatedSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2RatedSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL2RatedSetPoint.Value = (short)value;
                }
            }
        }
        public float L3RatedSetPoint
        {
            get
            {
                return _aoL3RatedSetPoint == null ? 0 : (_isFloatAioType ? _aoL3RatedSetPoint.FloatValue : _aoL3RatedSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3RatedSetPoint.FloatValue = value;
                }
                else
                {
                    _aoL3RatedSetPoint.Value = (short)value;
                }
            }
        }

        public float L1VoltageLimited
        {
            get
            {
                return _aoL1VoltageLimited == null ? 0 : (_isFloatAioType ? _aoL1VoltageLimited.FloatValue : _aoL1VoltageLimited.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL1VoltageLimited.FloatValue = value;
                }
                else
                {
                    _aoL1VoltageLimited.Value = (short)value;
                }
            }
        }

        public float L2VoltageLimited
        {
            get
            {
                return _aoL2VoltageLimited == null ? 0 : (_isFloatAioType ? _aoL2VoltageLimited.FloatValue : _aoL2VoltageLimited.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL2VoltageLimited.FloatValue = value;
                }
                else
                {
                    _aoL2VoltageLimited.Value = (short)value;
                }
            }
        }

        public float L3VoltageLimited
        {
            get
            {
                return _aoL3VoltageLimited == null ? 0 : (_isFloatAioType ? _aoL3VoltageLimited.FloatValue : _aoL3VoltageLimited.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoL3VoltageLimited.FloatValue = value;
                }
                else
                {
                    _aoL3VoltageLimited.Value = (short)value;
                }
            }
        }

        public float TtempCtrlTCIN
        {
            get
            {
                if (_aiTtempCtrlTCIN != null)
                {
                    return _aiTtempCtrlTCIN.FloatValue;
                }
                return 0;
            }
        }

        public int PSU1Y
        {
            get
            {
                return _aoPSU1Y == null ? 0 : (_isFloatAioType ? (int)_aoPSU1Y.FloatValue : _aoPSU1Y.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPSU1Y.FloatValue = value;
                }
                else
                {
                    _aoPSU1Y.Value = (short)value;
                }
            }
        }

        public int PSU2Y
        {
            get
            {
                return _aoPSU2Y == null ? 0 : (_isFloatAioType ? (int)_aoPSU2Y.FloatValue : _aoPSU2Y.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPSU2Y.FloatValue = value;
                }
                else
                {
                    _aoPSU2Y.Value = (short)value;
                }
            }
        }
        public int PSU3Y
        {
            get
            {
                return _aoPSU3Y == null ? 0 : (_isFloatAioType ? (int)_aoPSU3Y.FloatValue : _aoPSU3Y.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPSU3Y.FloatValue = value;
                }
                else
                {
                    _aoPSU3Y.Value = (short)value;
                }
            }
        }
        //
        public double PSU1Power
        {
            get; set;
        }
        public double PSU2Power
        {
            get; set;
        }
        public double PSU3Power
        {
            get; set;
        }
        //
        public double PCPSU1Y
        {
            get
            {
                return SC.GetValue<double>($"PM.{Module}.Heater.PSU1Y");
            }
        }
        public double PCPSU2Y
        {
            get
            {
                return SC.GetValue<double>($"PM.{Module}.Heater.PSU2Y");
            }
        }
        public double PCPSU3Y
        {
            get
            {
                return SC.GetValue<double>($"PM.{Module}.Heater.PSU3Y");
            }
        }
        #endregion Property
    }
}
