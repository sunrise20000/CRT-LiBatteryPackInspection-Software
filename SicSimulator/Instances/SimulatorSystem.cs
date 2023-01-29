using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.PLC;
using SicSimulator.Views;

namespace SicSimulator.Instances
{
    public class SimulatorSystem : Singleton<SimulatorSystem>
    {
        private PeriodicJob _thread;
        private int _simSpeed = 1;
        Random _rd = new Random();

        public SimulatorSystem()
        {

        }

        /// <summary>
        /// 设置仿真速度。
        /// </summary>
        /// <param name="speed"></param>
        public void SetSimulationSpeed(int speed)
        {
            _simSpeed = speed;
            if (_simSpeed <= 1)
                _simSpeed = 1; // simulation speed must be always >=1.
        }

        public void Initialize()
        {
            SetTMDefaultValue("TM");

            Singleton<DataManager>.Instance.Initialize(false);

            WcfServiceManager.Instance.Initialize(new Type[] { typeof(SimulatorAdsPlcService) });

            _thread = new PeriodicJob(200, OnMonitor, nameof(SimulatorSystem), true);
        }


        private void SetTMDefaultValue(string mod)
        {
            IO.DI["DI_Dummy0"].Value = true;
           
        }


        private bool OnMonitor()
        {
            try
            {
              
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
            return true;
        }

        void MonitorFlowRamp(string pm)
        {
            var rampList = new List<Tuple<string, string>>
                            {
                                new Tuple<string, string>(
                                    "AI_M1",
                                    "AO_M1"
                                    ),
                                new Tuple<string, string>(
                                    "AI_M2",
                                    "AO_M2"
                                    ),
                                new Tuple<string, string>("AI_M3","AO_M3"),
                                new Tuple<string, string>("AI_M4","AO_M4"),
                                new Tuple<string, string>("AI_M5","AO_M5"),
                                new Tuple<string, string>("AI_M6","AO_M6"),
                                new Tuple<string, string>("AI_M7","AO_M7"),
                                new Tuple<string, string>("AI_M8","AO_M8"),
                                new Tuple<string, string>("AI_M9","AO_M9"),
                                new Tuple<string, string>("AI_M10","AO_M10"),
                                new Tuple<string, string>("AI_M11","AO_M11"),
                                new Tuple<string, string>("AI_M12","AO_M12"),
                                new Tuple<string, string>("AI_M13","AO_M13"),
                                new Tuple<string, string>("AI_M14","AO_M14"),
                                new Tuple<string, string>("AI_M15","AO_M15"),
                                new Tuple<string, string>("AI_M16","AO_M16"),
                                new Tuple<string, string>("AI_M19","AO_M19"),
                                new Tuple<string, string>("AI_M20","AO_M20"),
                                new Tuple<string, string>("AI_M22","AO_M22"),
                                new Tuple<string, string>("AI_M23","AO_M23"),
                                new Tuple<string, string>("AI_M25","AO_M25"),
                                new Tuple<string, string>("AI_M26","AO_M26"),
                                new Tuple<string, string>("AI_M27","AO_M27"),
                                new Tuple<string, string>("AI_M28","AO_M28"),
                                new Tuple<string, string>("AI_M29","AO_M29"),
                                new Tuple<string, string>("AI_M40","AO_M40"),
                                new Tuple<string, string>("AI_M31","AO_M31"),
                                new Tuple<string, string>("AI_M32","AO_M32"),
                                new Tuple<string, string>("AI_M33","AO_M33"),

                                new Tuple<string, string>("AI_M35","AO_M35"),
                                new Tuple<string, string>("AI_M36","AO_M36"),
                                new Tuple<string, string>("AI_M37","AO_M37"),
                                new Tuple<string, string>("AI_M38","AO_M38"),

                                new Tuple<string, string>("AI_PressCtrl1","AO_PressCtrl1"),
                                new Tuple<string, string>("AI_PressCtrl2","AO_PressCtrl2"),
                                new Tuple<string, string>("AI_PressCtrl3","AO_PressCtrl3"),
                                new Tuple<string, string>("AI_PressCtrl4","AO_PressCtrl4"),
                                new Tuple<string, string>("AI_PressCtrl5","AO_PressCtrl5"),
                                new Tuple<string, string>("AI_PressCtrl6","AO_PressCtrl6"),
                                new Tuple<string, string>("AI_PressCtrl7","AO_PressCtrl7"),
                              
                                

                                //new Tuple<string, string>(
                                //    "AI_Gas2Flow",
                                //    "AO_Gas2Flow"
                                //),

                                //new Tuple<string, string>(
                                //    "AI_Gas3Flow",
                                //    "AO_Gas3Flow"
                                //),

                                //new Tuple<string, string>(
                                //    "AI_Gas4Flow",
                                //    "AO_Gas4Flow"
                                //),
                            };

            foreach (var item in rampList)
            {
                float setpoint = 0;
                float result = 0;
                float current = 0;
                if (IO.AI[$"{pm}.{item.Item1}"] != null)
                {
                    current = IO.AI[$"{pm}.{item.Item1}"].FloatValue;
                    setpoint = IO.AO[$"{pm}.{item.Item2}"].FloatValue;
                }


                if (current >= setpoint)
                {
                    result = current - (50 * _simSpeed);
                    if (result < setpoint)
                        result = setpoint;
                }
                if (current < setpoint)
                {
                    result = current + (50 * _simSpeed);
                    if (result > setpoint)
                        result = setpoint;
                }

                result = result + (float)_rd.NextDouble();

                IO.AI[$"{pm}.{item.Item1}"].FloatValue = result;
            }
        }
        public object locker = new object();
        private void MonitorPm(string pm)
        {
            IO.AI[$"{pm}.AI_TVmode"].FloatValue = IO.AO[$"{pm}.AO_ValveModeSetpoint"].FloatValue;
            IO.AI[$"{pm}.AI_HeartBeat"].FloatValue = IO.AO[$"{pm}.AO_HeartBeat"].FloatValue;

            IO.AI[$"{pm}.AI_ActualPositon"].FloatValue = IO.AO[$"{pm}.AO_SetpointPositon"].FloatValue;

            //蝶阀开启,跟着蝶阀的设置值走
            if (IO.DO[$"{pm}.DO_TVEnable"].Value)
            {
                if (IO.AI[$"{pm}.AI_ActualPressure"].FloatValue > IO.AO[$"{pm}.AO_SetpointPressure"].FloatValue)
                {
                    if (IO.AI[$"{pm}.AI_ActualPressure"].FloatValue - (10 * _simSpeed) < IO.AO[$"{pm}.AO_SetpointPressure"].FloatValue)
                    {
                        IO.AI[$"{pm}.AI_ActualPressure"].FloatValue -= (10 * _simSpeed);
                    }
                    else
                    {
                        IO.AI[$"{pm}.AI_ActualPressure"].FloatValue = IO.AO[$"{pm}.AO_SetpointPressure"].FloatValue;
                    }
                }
                else if (IO.AI[$"{pm}.AI_ActualPressure"].FloatValue < IO.AO[$"{pm}.AO_SetpointPressure"].FloatValue)
                {
                    if (IO.AI[$"{pm}.AI_ActualPressure"].FloatValue + (10 * _simSpeed) < IO.AO[$"{pm}.AO_SetpointPressure"].FloatValue)
                    {
                        IO.AI[$"{pm}.AI_ActualPressure"].FloatValue += (10 * _simSpeed);
                    }
                    else
                    {
                        IO.AI[$"{pm}.AI_ActualPressure"].FloatValue = IO.AO[$"{pm}.AO_SetpointPressure"].FloatValue;
                    }
                }
            }
            else
            {
                //判断是否有进气(V89到V97是否有开阀)
                if (IO.DI[$"{pm}.DI_InnerGasFinalFB(V89)"].Value || IO.DI[$"{pm}.DI_MiddleGasFinalFB(V90)"].Value || IO.DI[$"{pm}.DI_OpticPurgeFinalFB(V91)"].Value || IO.DI[$"{pm}.DI_GasRingPurgeFinalFB(V92)"].Value
                    || IO.DI[$"{pm}.DI_ChamberPurgeFinalFB(V93)"].Value || IO.DI[$"{pm}.DI_RotationUpPurgeFinalFB(V94)"].Value || IO.DI[$"{pm}.DI_ConfinementRingFinalFB(V95)"].Value || IO.DI[$"{pm}.DI_HeaterWFFinalFB(V96)"].Value)
                {
                    if (IO.AI[$"{pm}.AI_ActualPressure"].FloatValue < 1200)
                    {
                        IO.AI[$"{pm}.AI_ActualPressure"].FloatValue += (10 * _simSpeed);
                    }
                }
            }

            //蝶阀Ramp有Bug,设置AO值可以化解
            if (IO.AI[$"{pm}.AI_ActualPressure"].FloatValue >= 1100)
            {
                IO.AO[$"{pm}.AO_SetpointPressure"].FloatValue = IO.AI[$"{pm}.AI_ActualPressure"].FloatValue;
            }

            IO.AI[$"{pm}.AI_ChamPress"].FloatValue = IO.AI[$"{pm}.AI_ActualPressure"].FloatValue;

            //V27自动打开
            if (IO.AI[$"{pm}.AI_ChamPress"].FloatValue > 1020 && IO.DO[$"{pm}.DO_PumpBypass(V27)"].Value != true)
            {
                IO.DO[$"{pm}.DO_PumpBypass(V27)"].Value = true;
            }
            if (IO.AI[$"{pm}.AI_ChamPress"].FloatValue < 900 && IO.DO[$"{pm}.DO_PumpBypass(V27)"].Value)
            {
                IO.DO[$"{pm}.DO_PumpBypass(V27)"].Value = false;
            }

            //设置PT2的压力比PT1的小20

            IO.AI[$"{pm}.AI_ForelinePress"].FloatValue = IO.AI[$"{pm}.AI_ChamPress"].FloatValue - 20 > 0 ? IO.AI[$"{pm}.AI_ChamPress"].FloatValue - 20 : 0;

        }

        private void MonitorPressureLoad()
        {
            IO.DI["DI_LoadPressureATM"].Value = IO.AI["AI_LoadPressure"].Value > 960;
            IO.DI["DI_LoadPressureVAC"].Value = IO.AI["AI_LoadPressure"].Value < 310;


            bool isSlowPum = IO.DI["DI_LoadSlowPumpFB"].Value;
            bool isFastPump = IO.DI["DI_LoadFastPumpFB"].Value;
            bool isSlowVent = IO.DI["DI_LoadVentFB"].Value;

            ////200毫秒调用一次，10秒内实现从0到1020 
            double pressure = IO.AI["AI_LoadPressure"].Value;

            double factor = pressure / (3 * 10);

            double pressureStep = factor < 1 ? 1 : factor;



            if (isFastPump || isSlowPum)
            {
                if (pressure > 400)
                {
                    pressure -= (50 * _simSpeed);
                }
                
            }
            if (isSlowPum)
            {
                pressure -= (2 * _simSpeed);
            }
            if (isFastPump)
            {
                pressure -= (20 * _simSpeed);
            }

            if (isSlowVent)
            {
                if (pressure < 400)
                {
                    pressure += (2 * _simSpeed);
                }
                else if (pressure < 450)
                {
                    pressure += (10 * _simSpeed);
                }
                else
                {
                    pressure += (100 * _simSpeed);
                }
            }

            if (pressure < 0)
                pressure = 0;
            if (pressure > 1020)
                pressure = 1020;

            if (!isFastPump && pressure < 1)
                pressure = 0;

            IO.AI["AI_LoadPressure"].Value = Convert.ToInt16(pressure);
        }

        private void MonitorPressureUnLoad()
        {
            IO.DI["DI_UnloadSlowPumpFB"].Value = IO.DO["DO_UnloadSlowPump"].Value;
            IO.DI["DI_UnloadFastPumpFB"].Value = IO.DO["DO_UnloadFastPump"].Value;



            bool isSlowPum = IO.DI["DI_UnloadSlowPumpFB"].Value;
            bool isFastPump = IO.DI["DI_UnloadFastPumpFB"].Value;
            bool isSlowVent = IO.DI["DI_UnloadVentFB"].Value;
            bool isPressureBalance = IO.DO["DO_TMToUnloadBanlance"].Value;

            ////200毫秒调用一次，10秒内实现从0到1020 
            double unloadPressure = IO.AI["AI_UnloadPressure"].Value;
            double tmPressure = IO.AI["AI_TMPressure1"].Value;

            double factor = unloadPressure / (3 * 10);

            double pressureStep = factor < 1 ? 1 : factor;

            // V124打开，平衡Unload和TM的压力。
            if (isPressureBalance)
            {
                var diff = (unloadPressure - tmPressure) / 2;
                unloadPressure -= diff;
                tmPressure += diff;
            }

            if (isFastPump || isSlowPum)
            {
                if (unloadPressure > 400)
                {
                    unloadPressure -= (50 * _simSpeed);
                }

            }
            if (isSlowPum)
            {
                unloadPressure -= (2 * _simSpeed);
            }
            if (isFastPump)
            {
                unloadPressure -= (20 * _simSpeed);
            }

            if (isSlowVent)
            {
                if (unloadPressure < 400)
                {
                    unloadPressure += (2 * _simSpeed);
                }
                else if (unloadPressure < 450)
                {
                    unloadPressure += (10 * _simSpeed);
                }
                else
                {
                    unloadPressure += 100;
                }
            }

            if (unloadPressure < 0)
                unloadPressure = 0;
            if (unloadPressure > 1020)
                unloadPressure = 1020;

            if (!isFastPump && unloadPressure < 1)
                unloadPressure = 0;

            IO.AI["AI_UnloadPressure"].Value = Convert.ToInt16(unloadPressure);
            IO.AI["AI_TMPressure1"].Value = Convert.ToInt16(tmPressure);
        }

        private void MonitorPressureTM()
        {

            IO.DI["DI_TMPressure1ATM"].Value = IO.AI["AI_TMPressure1"].Value>900;
            IO.DI["DI_TMPressure1VAC"].Value = IO.AI["AI_TMPressure1"].Value < 500;

            bool isSlowPum = IO.DI["DI_TMSlowPumpFB"].Value;
            bool isSlowVent = IO.DI["DI_TMVentFB"].Value;
            bool isFastPump = IO.DI["DI_TMFastPumpFB"].Value;

            bool isV121 = IO.DI["DI_TMToUnloadBanlanceFB"].Value;

            if (IO.DI["DI_ReactorADoorOpened"].Value || IO.DI["PM1.DI_TMPressBalanceFB(V70)"].Value)
            {
                IO.AI["AI_TMPressure1"].Value = Convert.ToInt16(IO.AI[$"PM1.AI_ChamPress"].FloatValue);
                return;
            }
            else
            {

                bool isV85Opne = IO.DI["DI_TMLoadBanlanceFB"].Value;
                if (isV85Opne)
                {
                    short tmPress = IO.AI["AI_TMPressure1"].Value;
                    short llPress = IO.AI["AI_LoadPressure"].Value;
                    short avergePress = Convert.ToInt16((tmPress + llPress) / 2);

                    IO.AI["AI_TMPressure1"].Value = avergePress;
                    IO.AI["AI_LoadPressure"].Value = avergePress;
                }

                //200毫秒调用一次，10秒内实现从0到1020 
                double pressure = IO.AI["AI_TMPressure1"].Value;

                double factor = pressure / (3 * 10);

                double pressureStep = factor < 1 ? 1 : factor;

                if (isFastPump)
                {
                    pressure -= pressureStep;
                }
                else if (isSlowPum)
                {
                    pressure -= (2 * _simSpeed);
                }
                if (isSlowVent)
                {
                    if (pressure < 300)
                    {
                        pressure += (2 * _simSpeed);
                    }
                    else if (pressure < 400)
                    {
                        pressure += (10 * _simSpeed);
                    }
                    else
                    {
                        pressure += (100 * _simSpeed);
                    }

                }

                if (isV121)
                {
                    pressure -= (0.5 * _simSpeed);
                }



                if (pressure < 0)
                    pressure = 0;
                if (pressure > 1020)
                    pressure = 1020;

                if (!isFastPump && pressure < 1)
                {
                    pressure = 0;
                }

                IO.AI["AI_TMPressure1"].Value = Convert.ToInt16(pressure);

                if (IO.AI["AI_LoadPressure"].FloatValue > 960)
                {
                    IO.DI["DI_LoadPressureATM"].Value = true;
                }
                else
                {
                    IO.DI["DI_LoadPressureATM"].Value = false;
                }

                if (IO.AI["AI_TMPressure1"].FloatValue > 960)
                {
                    IO.DI["DI_TMPressure1ATM"].Value = false; //此信号是反过来的
                }
                else
                {
                    IO.DI["DI_TMPressure1ATM"].Value = true;
                }
            }
        }

        private void MonitorSlitValveFlow()
        {

            if (IO.DO["DO_LoadDoorClose"].Value != IO.DO["DO_LoadDoorOpen"].Value)
            {
                IO.DI["DI_LoadDoorOpened"].Value = IO.DO["DO_LoadDoorOpen"].Value;
                IO.DI["DI_LoadDoorClosed"].Value = IO.DO["DO_LoadDoorClose"].Value;
            }

            if (IO.DO["DO_RectorADoorClose"].Value != IO.DO["DO_RectorADoorOpen"].Value)
            {
                IO.DI["DI_ReactorADoorOpened"].Value = IO.DO["DO_RectorADoorOpen"].Value;
                IO.DI["DI_ReactorADoorClosed"].Value = IO.DO["DO_RectorADoorClose"].Value;
            }

            if (IO.DO["DO_RectorBDoorClose"].Value != IO.DO["DO_RectorBDoorOpen"].Value)
            {
                IO.DI["DI_ReactorBDoorOpened"].Value = IO.DO["DO_RectorBDoorOpen"].Value;
                IO.DI["DI_ReactorBDoorClosed"].Value = IO.DO["DO_RectorBDoorClose"].Value;
            }

            if (IO.DO["DO_UnloadStationDoorOpen"].Value != IO.DO["DO_UnloadStationDoorClose"].Value)
            {
                IO.DI["DI_UnloadStationDoorOpened"].Value = IO.DO["DO_UnloadStationDoorOpen"].Value;
                IO.DI["DI_UnloadStationDoorClosed"].Value = IO.DO["DO_UnloadStationDoorClose"].Value;
            }

            if (IO.DO["DO_BufferStationDoorOpen"].Value != IO.DO["DO_BufferStationDoorClose"].Value)
            {
                IO.DI["DI_BufferStationDoorOpened"].Value = IO.DO["DO_BufferStationDoorOpen"].Value;
                IO.DI["DI_BufferStationDoorClosed"].Value = IO.DO["DO_BufferStationDoorClose"].Value;
            }

            if (IO.DO["DO_UnloadSubStationDoorOpen"].Value != IO.DO["DO_UnloadSubStationDoorClose"].Value)
            {
                IO.DI["DI_UnloadSubStationDoorOpened"].Value = IO.DO["DO_UnloadSubStationDoorOpen"].Value;
                IO.DI["DI_UnloadSubStationDoorClosed"].Value = IO.DO["DO_UnloadSubStationDoorClose"].Value;
            }

            if (IO.DO["DO_LoadLSideDoorOpen"].Value != IO.DO["DO_LoadLSideDoorClose"].Value)
            {
                IO.DI["DI_LoadLSideDoorOpened"].Value = IO.DO["DO_LoadLSideDoorOpen"].Value;
                IO.DI["DI_LoadLSideDoorClosed"].Value = IO.DO["DO_LoadLSideDoorClose"].Value;
            }

            if (IO.DO["DO_LoadRSideDoorOpen"].Value != IO.DO["DO_LoadRSideDoorClose"].Value)
            {
                IO.DI["DI_LoadRSideDoorOpened"].Value = IO.DO["DO_LoadRSideDoorOpen"].Value;
                IO.DI["DI_LoadRSideDoorClosed"].Value = IO.DO["DO_LoadRSideDoorClose"].Value;
            }
        }


        private int time1 = 0;
        private int time2 = 0;
        private int time3 = 0;
        private int time4 = 0;
        private int time5 = 0;

        private void MonitorTMValve()
        {
            IO.DI["DI_VacRobotExtendBufferEnableFB"].Value = IO.DO["DO_VacRobotExtendBufferEnable"].Value;
            IO.DI["DI_VacRobotExtenLoadEnableFB"].Value = IO.DO["DO_VacRobotExtenLoadEnable"].Value;
            IO.DI["DI_VacRobotExtendPMAEnableFB"].Value = IO.DO["DO_VacRobotExtendPMAEnable"].Value;
            IO.DI["DI_VacRobotExtendPMBEnableFB"].Value = IO.DO["DO_VacRobotExtendPMBEnable"].Value;
            IO.DI["DI_VacRobotExtendUnloadEnableFB"].Value = IO.DO["DO_VacRobotExtendUnloadEnable"].Value;


            IO.DI["DI_ATMRobotExtendUnloadEnableFB"].Value = IO.DO["DO_ATMRobotExtendUnloadEnable"].Value;
            IO.DI["DI_ATMRobotExtendLoadLSideEnableFB"].Value = IO.DO["DO_ATMRobotExtendLoaLSideEnable"].Value;
            IO.DI["DI_ATMRobotExtendLoadRSideEnableFB"].Value = IO.DO["DO_ATMRobotExtendLoaRSideEnable"].Value;


            IO.DI["DI_LoadSlowPumpFB"].Value = IO.DO["DO_LoadSlowPump"].Value;
            IO.DI["DI_LoadFastPumpFB"].Value = IO.DO["DO_LoadFastPump"].Value;
            IO.DI["DI_TMSlowPumpFB"].Value = IO.DO["DO_TMSlowPump"].Value;
            IO.DI["DI_TMFastPumpFB"].Value = IO.DO["DO_TMFastPump"].Value;
            IO.DI["DI_BufferVentFB"].Value = IO.DO["DO_BufferVent"].Value;
            IO.DI["DI_LoadVentFB"].Value = IO.DO["DO_LoadVent"].Value;
            IO.DI["DI_TMVentFB"].Value = IO.DO["DO_TMVent"].Value;
            IO.DI["DI_UnloadVentFB"].Value = IO.DO["DO_UnloadVent"].Value;
            IO.DI["DI_TMLoadBanlanceFB"].Value = IO.DO["DO_TMLoadBanlance"].Value;


            IO.DI["DI_VacRobotExtenLoadEnableFB"].Value = IO.DO["DO_VacRobotExtenLoadEnable"].Value;
            IO.DI["DI_VacRobotExtendBufferEnableFB"].Value = IO.DO["DO_VacRobotExtendBufferEnable"].Value;
            IO.DI["DI_VacRobotExtendPMAEnableFB"].Value = IO.DO["DO_VacRobotExtendPMAEnable"].Value;


            if (IO.DO["DO_LoadLifterCYUp"].Value != IO.DO["DO_LoadLifterCYDown"].Value)
            {
                time1 += _simSpeed;
                if (time1 > 10)
                {
                    time1 = 0;
                    IO.DI["DI_LoadLifterCYUp"].Value = IO.DO["DO_LoadLifterCYUp"].Value;
                    IO.DI["DI_LoadLifterCYDown"].Value = IO.DO["DO_LoadLifterCYDown"].Value;
                }
            }
            if (IO.DO["DO_LoadWaferCYClamp"].Value != IO.DO["DO_LoadWaferCYOpen"].Value)
            {
                time2 += _simSpeed;
                if (time2 > 10)
                {
                    time2 = 0;
                    IO.DI["DI_LoadWaferCYClamped"].Value = IO.DO["DO_LoadWaferCYClamp"].Value;
                    IO.DI["DI_LoadWafeCYOpened"].Value = IO.DO["DO_LoadWaferCYOpen"].Value;
                }
            }
            if (IO.DO["DO_LoadTrayCYClamp"].Value != IO.DO["DO_LoadTrayCYOpen"].Value)
            {
                time3 += _simSpeed;
                if (time3 > 10)
                {
                    time3 = 0;
                    IO.DI["DI_LoadTrayCYClamped"].Value = IO.DO["DO_LoadTrayCYClamp"].Value;
                    IO.DI["DI_LoadTrayCYOpend"].Value = IO.DO["DO_LoadTrayCYOpen"].Value;
                }
            }
            if (IO.DO["DO_UnloadStationWaferCYClamp"].Value != IO.DO["DO_UnloadStationWaferCYOpen"].Value)
            {
                time4 += _simSpeed;
                if (time4 > 10)
                {
                    time4 = 0;
                    IO.DI["DI_UnloadStationWaferCYClamped"].Value = IO.DO["DO_UnloadStationWaferCYClamp"].Value;
                    IO.DI["DI_UnloadStationWaferCYOpened"].Value = IO.DO["DO_UnloadStationWaferCYOpen"].Value;
                }
            }
            if (IO.DO["DO_UnloadStationLifterCYUp"].Value != IO.DO["DO_UnloadStationLifterCYDown"].Value)
            {
                time5 += _simSpeed;
                if (time5 > 10)
                {
                    time5 = 0;
                    IO.DI["DI_UnloadStationLifterCYUp"].Value = IO.DO["DO_UnloadStationLifterCYUp"].Value;
                    IO.DI["DI_UnloadStationLifterCYDown"].Value = IO.DO["DO_UnloadStationLifterCYDown"].Value;
                }
            }

            //IO.DI["DI_LdRotationServoOn"].Value = IO.DO["DO_LdRotationServoOn"].Value;
            
        }

        private void MonitorValve(string pm)
        {

            IO.DI[$"{pm}.DI_DORPressATMSW"].Value = IO.DI[$"{pm}.DI_DORRefillFB(V76)"].Value;
            IO.DI[$"{pm}.DI_PSUEnableFB"].Value = IO.DO[$"{pm}.DO_HeaterEnable"].Value;

            IO.DI[$"{pm}.DI_InnerHeaterEnableFB"].Value = IO.DO[$"{pm}.DO_PSU1Enable"].Value;
            IO.DI[$"{pm}.DI_MiddleHeaterEnableFB"].Value = IO.DO[$"{pm}.DO_PSU2Enable"].Value;
            IO.DI[$"{pm}.DI_OuterHeaterEnableFB"].Value = IO.DO[$"{pm}.DO_PSU3Enable"].Value;

            //IO.DI[$"{pm}.DI_SCR1Status"].Value = IO.DO[$"{pm}.DO_SCR1Enable"].Value;
            //IO.DI[$"{pm}.DI_SCR2Status"].Value = IO.DO[$"{pm}.DO_SCR2Enable"].Value;
            //IO.DI[$"{pm}.DI_SCR3Status"].Value = IO.DO[$"{pm}.DO_SCR3Enable"].Value;

            IO.DI[$"{pm}.DI_ChamMoveBodyUpDownEnableFB"].Value = IO.DO[$"{pm}.DO_ChamMoveBodyUpDownEnable"].Value;

            IO.DI[$"{pm}.DI_PumpBypassFB(V27)"].Value = IO.DO[$"{pm}.DO_PumpBypass(V27)"].Value;
            IO.DI[$"{pm}.DI_H2SupplyFB(V31)"].Value = IO.DO[$"{pm}.DO_H2Supply(V31)"].Value;
            IO.DI[$"{pm}.DI_ArSupplyFB(V32)"].Value = IO.DO[$"{pm}.DO_ArSupply(V32)"].Value;
            IO.DI[$"{pm}.DI_SHH2/ArSwitchFB(V33)"].Value = IO.DO[$"{pm}.DO_SHH2/ArSwitch(V33)"].Value;
            IO.DI[$"{pm}.DI_H2ArLine1FB(V35)"].Value = IO.DO[$"{pm}.DO_H2ArLine1(V35)"].Value;
            IO.DI[$"{pm}.DI_H2ArLine2FB(V36)"].Value = IO.DO[$"{pm}.DO_H2ArLine2(V36)"].Value;
            //IO.DI[$"{pm}.DI_DoppingDilute_FB"].Value = IO.DO[$"{pm}.DO_HighN2Flow"].Value;
            IO.DI[$"{pm}.DI_DilutedN2Run/VentFB(V39)"].Value = IO.DO[$"{pm}.DO_DilutedN2Run/Vent(V39)"].Value;
            IO.DI[$"{pm}.DI_HighN2Run/VentFB(V40)"].Value = IO.DO[$"{pm}.DO_HighN2Run/Vent(V40)"].Value;
            IO.DI[$"{pm}.DI_TMARunFB(V41)"].Value = IO.DO[$"{pm}.DO_TMARun(V41)"].Value;
            IO.DI[$"{pm}.DI_TMAVentFB(V42)"].Value = IO.DO[$"{pm}.DO_TMAVent(V42)"].Value;
            IO.DI[$"{pm}.DI_TMAReleaseFB(V43)"].Value = IO.DO[$"{pm}.DO_TMARelease(V43)"].Value;
            IO.DI[$"{pm}.DI_TMABypassFB(V45)"].Value = IO.DO[$"{pm}.DO_TMABypass(V45)"].Value;
            IO.DI[$"{pm}.DI_TMAVacFB(V46)"].Value = IO.DO[$"{pm}.DO_TMAVac(V46)"].Value;
            IO.DI[$"{pm}.DI_TCSReleaseFB(V48)"].Value = IO.DO[$"{pm}.DO_TCSRelease(V48)"].Value;
            IO.DI[$"{pm}.DI_TCSBypassFB(V49)"].Value = IO.DO[$"{pm}.DO_TCSBypass(V49)"].Value;
            IO.DI[$"{pm}.DI_TCSVacFB(V50)"].Value = IO.DO[$"{pm}.DO_TCSVac(V50)"].Value;
            IO.DI[$"{pm}.DI_HCLSwitchFB(V51)"].Value = IO.DO[$"{pm}.DO_HCLSwitch(V51)"].Value;
            IO.DI[$"{pm}.DI_SiH4SwitchFB(V52)"].Value = IO.DO[$"{pm}.DO_SiH4Switch(V52)"].Value;
            IO.DI[$"{pm}.DI_TCSRun/VentFB(V53)"].Value = IO.DO[$"{pm}.DO_TCSRun/Vent(V53)"].Value;
            IO.DI[$"{pm}.DI_HCLRun/VentFB(V54)"].Value = IO.DO[$"{pm}.DO_HCLRun/Vent(V54)"].Value;
            IO.DI[$"{pm}.DI_SiH4RunFB(V55)"].Value = IO.DO[$"{pm}.DO_SiH4Run(V55)"].Value;
            IO.DI[$"{pm}.DI_SiH4VentFB(V56)"].Value = IO.DO[$"{pm}.DO_SiH4Vent(V56)"].Value;
            IO.DI[$"{pm}.DI_C2H4SwitchFB(V58)"].Value = IO.DO[$"{pm}.DO_C2H4Switch(V58)"].Value;
            IO.DI[$"{pm}.DI_C2H4RunFB(V59)"].Value = IO.DO[$"{pm}.DO_C2H4Run(V59)"].Value;
            IO.DI[$"{pm}.DI_C2H4VentFB(V60)"].Value = IO.DO[$"{pm}.DO_C2H4Vent(V60)"].Value;
            IO.DI[$"{pm}.DI_DoppingFinalFB(V61)"].Value = IO.DO[$"{pm}.DO_DoppingFinal(V61)"].Value;
            IO.DI[$"{pm}.DI_SilaneFinalFB(V62)"].Value = IO.DO[$"{pm}.DO_SilaneFinal(V62)"].Value;
            IO.DI[$"{pm}.DI_PropaneFinalFB(V63)"].Value = IO.DO[$"{pm}.DO_PropaneFinal(V63)"].Value;
            //IO.DI[$"{pm}.DI_GasRingH2Purge_FB"].Value = IO.DO[$"{pm}.DO_GasRingH2Purge"].Value;
            //IO.DI[$"{pm}.DI_GasRingArPurge_FB"].Value = IO.DO[$"{pm}.DO_GasRingArPurge"].Value;
            IO.DI[$"{pm}.DI_ChamBodyArPurgeFB(V68)"].Value = IO.DO[$"{pm}.DO_ChamBodyArPurge(V68)"].Value;
            IO.DI[$"{pm}.DI_ReactorLeakCheckFB(V69)"].Value = IO.DO[$"{pm}.DO_ReactorLeakCheck(V69)"].Value;
            IO.DI[$"{pm}.DI_TMPressBalanceFB(V70)"].Value = IO.DO[$"{pm}.DO_TMPressBalance(V70)"].Value;
            //IO.DI[$"{pm}.DI_CarryGasFinal_FB"].Value = IO.DO[$"{pm}.DO_CarryGasFinal(V97)"].Value;
            IO.DI[$"{pm}.DI_GasBoxVentPumpFB(V72)"].Value = IO.DO[$"{pm}.DO_GasBoxVentPump(V72)"].Value;
            IO.DI[$"{pm}.DI_MOVacFB(V73)"].Value = IO.DO[$"{pm}.DO_MOVac(V73)"].Value;
            IO.DI[$"{pm}.DI_GasboxLeakCheckFB(V74)"].Value = IO.DO[$"{pm}.DO_GasboxLeakCheck(V74)"].Value;
            IO.DI[$"{pm}.DI_DORVacFB(V75)"].Value = IO.DO[$"{pm}.DO_DORVac(V75)"].Value;
            IO.DI[$"{pm}.DI_DORRefillFB(V76)"].Value = IO.DO[$"{pm}.DO_DORRefill(V76)"].Value;
            IO.DI[$"{pm}.DI_SHPurgeFinalFB(V87)"].Value = IO.DO[$"{pm}.DO_SHPurgeFinal(V87)"].Value;
            IO.DI[$"{pm}.DI_OuterGasFinalFB(V88)"].Value = IO.DO[$"{pm}.DO_OuterGasFinal(V88)"].Value;
            IO.DI[$"{pm}.DI_InnerGasFinalFB(V89)"].Value = IO.DO[$"{pm}.DO_InnerGasFinal(V89)"].Value;
            IO.DI[$"{pm}.DI_MiddleGasFinalFB(V90)"].Value = IO.DO[$"{pm}.DO_MiddleGasFinal(V90)"].Value;
            IO.DI[$"{pm}.DI_OpticPurgeFinalFB(V91)"].Value = IO.DO[$"{pm}.DO_GasRingPurgeFinal(V92)"].Value;
            IO.DI[$"{pm}.DI_GasRingPurgeFinalFB(V92)"].Value = IO.DO[$"{pm}.DO_OpticPurgeFinal(V91)"].Value;
            IO.DI[$"{pm}.DI_ChamberPurgeFinalFB(V93)"].Value = IO.DO[$"{pm}.DO_ChamberPurgeFinal(V93)"].Value;
            IO.DI[$"{pm}.DI_RotationUpPurgeFinalFB(V94)"].Value = IO.DO[$"{pm}.DO_RotationUpPurgeFinal(V94)"].Value;
            IO.DI[$"{pm}.DI_ConfinementRingFinalFB(V95)"].Value = IO.DO[$"{pm}.DO_ConfinementRingFinal(V95)"].Value;
            IO.DI[$"{pm}.DI_HeaterWFFinalFB(V96)"].Value = IO.DO[$"{pm}.DO_HeaterWFFinal(V96)"].Value;
            IO.DI[$"{pm}.DI_N2SwitchFB(V37)"].Value = IO.DO[$"{pm}.DO_N2Switch(V37)"].Value;


            IO.DI[$"{pm}.DI_CarrierGasH2_FB"].Value = IO.DO[$"{pm}.DO_CarrierGasH2"].Value;
            IO.DI[$"{pm}.DI_CarrierGasAr_FB"].Value = IO.DO[$"{pm}.DO_CarrierGasAr"].Value;
            


            IO.DI[$"{pm}.DI_OpticPurgeFinalFB(V91)"].Value = IO.DO[$"{pm}.DO_OpticPurgeFinal(V91)"].Value;
            IO.DI[$"{pm}.DI_GasRingPurgeFinalFB(V92)"].Value = IO.DO[$"{pm}.DO_GasRingPurgeFinal(V92)"].Value;

            //IO.DI[$"{pm}.DI_GasBoxVentBypass"].Value = IO.DO[$"{pm}.DO_GasBoxVentBypass(V25)"].Value;
            //IO.DI[$"{pm}.DI_C2H4SwitchHTPurge"].Value = IO.DO[$"{pm}.DO_C2H4SwitchHTPurge"].Value;

            IO.DI[$"{pm}.DI_EPV1-1FB"].Value = IO.DO[$"{pm}.DO_EPV1"].Value;
            IO.DI[$"{pm}.DI_EPV2-1FB"].Value = IO.DO[$"{pm}.DO_EPV2"].Value;

            IO.AI[$"{pm}.AI_PressCtrl1"].Value = IO.AO[$"{pm}.AO_PressCtrl1"].Value;
            IO.AI[$"{pm}.AI_PressCtrl2"].Value = IO.AO[$"{pm}.AO_PressCtrl2"].Value;
            IO.AI[$"{pm}.AI_PressCtrl3"].Value = IO.AO[$"{pm}.AO_PressCtrl3"].Value;
            IO.AI[$"{pm}.AI_PressCtrl4"].Value = IO.AO[$"{pm}.AO_PressCtrl4"].Value;
            IO.AI[$"{pm}.AI_PressCtrl5"].Value = IO.AO[$"{pm}.AO_PressCtrl5"].Value;
            IO.AI[$"{pm}.AI_PressCtrl6"].Value = IO.AO[$"{pm}.AO_PressCtrl6"].Value;
            IO.AI[$"{pm}.AI_PressCtrl7"].Value = IO.AO[$"{pm}.AO_PressCtrl7"].Value;
        }

        int timeMoveUp = 0;
        int timeMove = 0;
        R_TRIG _UpTrig = new R_TRIG();

        private void MonitorChamberMoveBodyUp(string pm)
        {
            if (IO.DO[$"{pm}.DO_ChamMoveBodyUp"].Value != IO.DO[$"{pm}.DO_ChamMoveBodyDown"].Value)
            {
                timeMoveUp += _simSpeed;
                if (timeMoveUp > 20)
                {
                    IO.DI[$"{pm}.DI_ChamMoveBodyUp"].Value = IO.DO[$"{pm}.DO_ChamMoveBodyUp"].Value;
                    IO.DI[$"{pm}.DI_ChamMoveBodyDown"].Value = IO.DO[$"{pm}.DO_ChamMoveBodyDown"].Value;
                }
            }
            if (timeMoveUp > 20)
            {
                timeMoveUp = 0;
            }
        }

        R_TRIG forwardLatch_R = new R_TRIG();
        R_TRIG backLatchLatch_R = new R_TRIG();
        private void MonitorChamberMoveBody(string pm)
        {
            if (IO.DO[$"{pm}.DO_ChamMoveBodyForward"].Value != IO.DO[$"{pm}.DO_ChamMoveBodyBackward"].Value)
            {
                timeMove += _simSpeed;
                if (timeMove > 20)
                {
                    IO.DI[$"{pm}.DI_ChamMoveBodyFront"].Value = IO.DO[$"{pm}.DO_ChamMoveBodyForward"].Value;
                    IO.DI[$"{pm}.DI_ChamMoveBodyEnd"].Value = IO.DO[$"{pm}.DO_ChamMoveBodyBackward"].Value;
                }
            }

            forwardLatch_R.CLK = IO.DO[$"{pm}.DO_ChamMoveBodyBrakerForward"].Value;
            if (forwardLatch_R.Q)
            {
                IO.DI[$"{pm}.DI_ChamMoveBodyUpLatchFW"].Value = true;
                IO.DI[$"{pm}.DI_ChamMoveBodyEndLatchBW"].Value = false;
                backLatchLatch_R.RST = true;
            }

            backLatchLatch_R.CLK = IO.DO[$"{pm}.DO_ChamMoveBodyBrakerBackward"].Value;
            if (backLatchLatch_R.Q)
            {
                IO.DI[$"{pm}.DI_ChamMoveBodyEndLatchBW"].Value = true;
                IO.DI[$"{pm}.DI_ChamMoveBodyUpLatchFW"].Value = false;
                forwardLatch_R.RST = true;
            }

            if (timeMove > 20)
            {
                timeMove = 0;
            }
        }

        private void MonitorHeater(string pm)
        {
            IO.DI[$"{pm}.DI_PSU1Status"].Value = IO.DO[$"{pm}.DO_PSU1Enable"].Value;
            IO.DI[$"{pm}.DI_PSU2Status"].Value = IO.DO[$"{pm}.DO_PSU2Enable"].Value;
            IO.DI[$"{pm}.DI_PSU3Status"].Value = IO.DO[$"{pm}.DO_PSU3Enable"].Value;
            IO.DI[$"{pm}.DI_SCR1Status"].Value = IO.DO[$"{pm}.DO_SCR1Enable"].Value;
            IO.DI[$"{pm}.DI_SCR2Status"].Value = IO.DO[$"{pm}.DO_SCR2Enable"].Value;
            IO.DI[$"{pm}.DI_SCR3Status"].Value = IO.DO[$"{pm}.DO_SCR3Enable"].Value;

            //IO.AI[$"{pm}.AI_PresentLoop1Temp"].FloatValue = IO.AO[$"{pm}.AO_SetpointLoop1Temp"].FloatValue;
            //IO.AI[$"{pm}.AI_Loop1ControlMode"].FloatValue = IO.AO[$"{pm}.AO_Loop1ControlMode"].FloatValue;
            //IO.AI[$"{pm}.AI_Loop1ActualPower"].FloatValue = IO.AO[$"{pm}.AO_Loop1ManualOP"].FloatValue;
            //IO.DI[$"{pm}.DI_InnerHeaterEnable"].Value = IO.DO[$"{pm}.DO_InnerHeaterEnable"].Value;

            //IO.AI[$"{pm}.AI_PresentLoop2Temp"].FloatValue = IO.AO[$"{pm}.AO_SetpointLoop2Temp"].FloatValue;
            //IO.AI[$"{pm}.AI_Loop2ControlMode"].FloatValue = IO.AO[$"{pm}.AO_Loop2ControlMode"].FloatValue;
            //IO.AI[$"{pm}.AI_Loop2ActualPower"].FloatValue = IO.AO[$"{pm}.AO_Loop2ManualOP"].FloatValue;
            //IO.DI[$"{pm}.DI_MiddleHeaterEnable"].Value = IO.DO[$"{pm}.DO_MiddleHeaterEnable"].Value;

            //IO.AI[$"{pm}.AI_PresentLoop3Temp"].FloatValue = IO.AO[$"{pm}.AO_SetpointLoop3Temp"].FloatValue;
            //IO.AI[$"{pm}.AI_Loop3ControlMode"].FloatValue = IO.AO[$"{pm}.AO_Loop3ControlMode"].FloatValue;
            //IO.AI[$"{pm}.AI_Loop3ActualPower"].FloatValue = IO.AO[$"{pm}.AO_Loop3ManualOP"].FloatValue;
            //IO.DI[$"{pm}.DI_OuterHeaterEnable"].Value = IO.DO[$"{pm}.DO_OuterHeaterEnable"].Value;
        }
        private void MonitorGasConnector(string pm)
        {
            if (IO.DO[$"{pm}.DO_SHGasConnectorLoosen"].Value != IO.DO[$"{pm}.DO_SHGasConnectorTighten"].Value)
            {
                IO.DI[$"{pm}.DI_SHGasConnectorLoosen"].Value = IO.DO[$"{pm}.DO_SHGasConnectorLoosen"].Value;
                IO.DI[$"{pm}.DI_SHGasConnectorTighten"].Value = IO.DO[$"{pm}.DO_SHGasConnectorTighten"].Value;
            }
        }

        private void MonitorLid(string pm)
        {

            if (IO.DO[$"{pm}.DO_SHLidLoosen"].Value != IO.DO[$"{pm}.DO_SHLidTighten"].Value)
            {
                timeMove += _simSpeed;
                if (timeMove > 20)
                {
                    IO.DI[$"{pm}.DI_SHLidLoosen"].Value = IO.DO[$"{pm}.DO_SHLidLoosen"].Value;
                    IO.DI[$"{pm}.DI_SHLidTighten"].Value = IO.DO[$"{pm}.DO_SHLidTighten"].Value;
                    IO.DI[$"{pm}.DI_SHLidClosed"].Value = IO.DO[$"{pm}.DO_SHLidTighten"].Value;
                }
                return;
            }
            if (IO.DO[$"{pm}.DO_MiddleLidLoosen"].Value != IO.DO[$"{pm}.DO_MiddleLidTighten"].Value)
            {
                timeMove += _simSpeed;
                if (timeMove > 20)
                {
                    IO.DI[$"{pm}.DI_MiddleLidLoosen"].Value = IO.DO[$"{pm}.DO_MiddleLidLoosen"].Value;
                    IO.DI[$"{pm}.DI_MiddleLidTighten"].Value = IO.DO[$"{pm}.DO_MiddleLidTighten"].Value;
                    IO.DI[$"{pm}.DI_MiddleLidClosed"].Value = IO.DO[$"{pm}.DO_MiddleLidTighten"].Value;
                }
                return;
            }
            if (timeMove > 20)
            {
                timeMove = 0;
            }
        }

        private void MonitorLidSwing(string pm)
        {

            if (IO.DO[$"{pm}.DO_SHLidSwingLock"].Value != IO.DO[$"{pm}.DO_SHLidSwingUnlock"].Value)
            {
                IO.DI[$"{pm}.DI_SHLidSwingLock"].Value = IO.DO[$"{pm}.DO_SHLidSwingLock"].Value;
                IO.DI[$"{pm}.DI_SHLidSwingUnlock"].Value = IO.DO[$"{pm}.DO_SHLidSwingUnlock"].Value;
            }
            if (IO.DO[$"{pm}.DO_MiddleLidSwingLock"].Value != IO.DO[$"{pm}.DO_MiddleLidSwingUnlock"].Value)
            {
                IO.DI[$"{pm}.DI_MiddleLidSwingLock"].Value = IO.DO[$"{pm}.DO_MiddleLidSwingLock"].Value;
                IO.DI[$"{pm}.DI_MiddleLidSwingUnlock"].Value = IO.DO[$"{pm}.DO_MiddleLidSwingUnlock"].Value;
            }

        }


        private void MonitorConfinementRing(string pm)
        {
            if (IO.DO[$"{pm}.DO_ConfinementRingMoveDown"].Value != IO.DO[$"{pm}.DO_ConfinementRingMoveUp"].Value)
            {
                IO.DI[$"{pm}.DI_ConfinementRingDone"].Value = true;
                IO.DI[$"{pm}.DI_ConfinementRingDown"].Value = IO.DO[$"{pm}.DO_ConfinementRingMoveDown"].Value;
                IO.DI[$"{pm}.DI_ConfinementRingUp"].Value = IO.DO[$"{pm}.DO_ConfinementRingMoveUp"].Value;
            }

            if(IO.DI[$"{pm}.DI_ConfinementRingUp"].Value)
            {
                IO.AI[$"{pm}.AI_ConfinementRingCurPos"].FloatValue = IO.AO[$"{pm}.AO_ConfinementRingUpPos"].FloatValue;
            }

            if (IO.DI[$"{pm}.DI_ConfinementRingDown"].Value)
            {
                IO.AI[$"{pm}.AI_ConfinementRingCurPos"].FloatValue = IO.AO[$"{pm}.AO_ConfinementRingDownPos"].FloatValue;
            }

            IO.AI[$"{pm}.AI_ConfinementRingUpPos"].FloatValue = IO.AO[$"{pm}.AO_ConfinementRingUpPos"].FloatValue;
            IO.AI[$"{pm}.AI_ConfinementRingDownPos"].FloatValue = IO.AO[$"{pm}.AO_ConfinementRingDownPos"].FloatValue;
        }



        private void MonitorRotation(string pm)
        {
            IO.AI[$"{pm}.AI_ActualSpeed"].FloatValue = IO.AO[$"{pm}.AO_SpindleSpeed"].FloatValue;

            IO.DI[$"{pm}.DI_ConfinementRingServoOn"].Value = IO.DO[$"{pm}.DO_ConfinementRingServoOn"].Value;
            
        }

        public void Terminate()
        {
            _thread.Stop();
        }
    }
}
