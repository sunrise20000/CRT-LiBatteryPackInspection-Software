using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Equipment;
using SicPM;
using SicPM.Devices;
using SicPM.Routines;


namespace SicPM.Routines
{
    public class PMPcCalibrationRoutine : PMBaseRoutine
    {
        
        private PMModule _pmModule;

        

        public double pc4FB1;
        public double pc4FB2;
        public double pc4FB3;
        public double pc5FB1;
        public double pc6FB2;
        public double pc7FB3;

        public List<double> pc4FeedBack = new List<double>();
        public List<double> pc5FeedBack = new List<double>();
        public List<double> pc6FeedBack = new List<double>();
        public List<double> pc7FeedBack = new List<double>();

        private Stopwatch _swTimer = new Stopwatch();
        public int ElapsedTime
        {
            get { return _swTimer.IsRunning ? (int)(_swTimer.ElapsedMilliseconds / 1000) : 0; }
        }

        enum RoutineStep
        {
            SetCloseV41,
            SetOpenV42,
            SetCloseV55,
            SetOpenV56,
            SetCloseV59,
            SetOpenV60,

            SetPC4Mode,
            TimeDelay1,
            SetPC4Default,

            TimeDelay2,

            SetOpenV41,
            SetPC5Close,

            TimeDelay3,

            SetClose2V41,
            SetPC5Mode,
            TimeDelay4,
            SetPC5Default,
            TimeDelay5,

            SetOpenV55,
            SetPC6Close,
            TimeDelay6,

            SetClose2V55,
            SetPC6Mode,
            TimeDelay7,
            SetPC6Default,

            TimeDelay8,
            SetOpenV59,
            SetPC7Close,
            TimeDelay9,

            SetClose2V59,
            SetPC7Mode,
            TimeDelay10,
            SetPC7Default,

            GetPC45FeedBack,
            SetPC5Offset,
            GetPC46FeedBack,
            SetPC6Offset,
            GetPC47FeedBack,
            SetPC7Offset
        }

        public PMPcCalibrationRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "PcCalibration";
            _pmModule = pm;

            
            
        }

        public override Result Start(params object[] objs)
        {
            _swTimer.Restart();
            Reset();
            Notify("Start");
            
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                //CloseV41
                SetIoValueByName((int)RoutineStep.SetCloseV41, "V41", false, 2);
                //OpenV42
                SetIoValueByName((int)RoutineStep.SetOpenV42, "V42", true, 2);
                //CloseV55
                SetIoValueByName((int)RoutineStep.SetCloseV55, "V55", false, 2);
                //OpenV56
                SetIoValueByName((int)RoutineStep.SetOpenV56, "V56", true, 2);
                //CloseV59
                SetIoValueByName((int)RoutineStep.SetCloseV59, "V59", false, 2);
                //OpenV60
                SetIoValueByName((int)RoutineStep.SetOpenV60, "V60", true, 2);

                //PC4设置为默认值
                SetPcModeToNormal((int)RoutineStep.SetPC4Mode, new List<int> { 4 });
                TimeDelay((int)RoutineStep.TimeDelay1, 1);
                SetPcToDefault((int)RoutineStep.SetPC4Default, new List<int> { 4 });
                //等待2S
                TimeDelay((int)RoutineStep.TimeDelay2, 2);

                //OpenV41
                SetIoValueByName((int)RoutineStep.SetOpenV41, "V41", true, 2);
                //ClosePC5
                SetPcModel((int)RoutineStep.SetPC5Close, new List<int> { 5 }, Aitex.Core.Common.DeviceData.PcCtrlMode.Close);
                //等待5S
                TimeDelay((int)RoutineStep.TimeDelay3, 5);

                //计算10s PC4 PC5 反馈值平均数
                GetPCFeedBackByName((int)RoutineStep.GetPC45FeedBack, "Pressure5", 12);
                SetPcOffsetToConfig((int)RoutineStep.SetPC5Offset, "Pressure5");

                //CloseV41 2
                SetIoValueByName((int)RoutineStep.SetClose2V41, "V41", false, 2);

                //PC5设置为默认值
                SetPcModeToNormal((int)RoutineStep.SetPC5Mode, new List<int> { 5 });
                TimeDelay((int)RoutineStep.TimeDelay4, 1);
                SetPcToDefault((int)RoutineStep.SetPC5Default, new List<int> { 5 });

                //等待2S
                TimeDelay((int)RoutineStep.TimeDelay5, 2);

                //OpenV55
                SetIoValueByName((int)RoutineStep.SetOpenV55, "V55", true, 2);
                //ClosePC6
                SetPcModel((int)RoutineStep.SetPC6Close, new List<int> { 6 }, Aitex.Core.Common.DeviceData.PcCtrlMode.Close);
                //等待5S
                TimeDelay((int)RoutineStep.TimeDelay6, 5);

                //计算10s PC4 PC6 反馈值平均数
                GetPCFeedBackByName((int)RoutineStep.GetPC46FeedBack, "Pressure6", 12);
                SetPcOffsetToConfig((int)RoutineStep.SetPC6Offset, "Pressure6");

                //CloseV55 2
                SetIoValueByName((int)RoutineStep.SetClose2V55, "V55", false, 2);

                //PC6设置为默认值
                SetPcModeToNormal((int)RoutineStep.SetPC6Mode, new List<int> { 6 });
                TimeDelay((int)RoutineStep.TimeDelay7, 1);
                SetPcToDefault((int)RoutineStep.SetPC6Default, new List<int> { 6 });

                //等待2S
                TimeDelay((int)RoutineStep.TimeDelay8, 2);

                //OpenV59
                SetIoValueByName((int)RoutineStep.SetOpenV59, "V59", true, 2);
                //ClosePC7
                SetPcModel((int)RoutineStep.SetPC7Close, new List<int> { 7 }, Aitex.Core.Common.DeviceData.PcCtrlMode.Close);
                //等待5S
                TimeDelay((int)RoutineStep.TimeDelay9, 5);

                //计算10s PC4 PC6 反馈值平均数
                GetPCFeedBackByName((int)RoutineStep.GetPC47FeedBack, "Pressure7", 12);
                SetPcOffsetToConfig((int)RoutineStep.SetPC7Offset, "Pressure7");

                //CloseV59 2
                SetIoValueByName((int)RoutineStep.SetClose2V59, "V59", false, 2);
                //PC7设置为默认值
                SetPcModeToNormal((int)RoutineStep.SetPC7Mode, new List<int> { 7 });
                TimeDelay((int)RoutineStep.TimeDelay10, 1);
                SetPcToDefault((int)RoutineStep.SetPC7Default, new List<int> { 7 });

            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException ex)
            {
                if (ex.Message == "Timeout")
                {
                    Notify("Timeout");
                }
                return Result.FAIL;
            }
            _swTimer.Stop();
            Notify("Finished");

            return Result.DONE;
        }


        
        

       



        protected void SetIoValueByName(int id, string ioName, bool close, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify((close ? "Open" : "Close") + $"{ioName} value");
                _pmModule.SetIoValue(new System.Collections.Generic.List<string> { ioName }, close);
                return true;
            }, () =>
            {
                return _pmModule.CheckIoValue(new System.Collections.Generic.List<string> { ioName }, close);
            },
            timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop((close ? "Open" : "Close") + $"{ioName} value timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void GetPCFeedBackByName(int id, string ioName, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                pc4FeedBack.Clear();
                pc5FeedBack.Clear();
                pc6FeedBack.Clear();
                pc7FeedBack.Clear();
                _swTimer.Restart();
                Notify($"Get {ioName} FeedBack 10s");
                return true;
            }, () =>
            {
                pc4FeedBack.Add(PMDevice.Pressure4.FeedBack);
                if (ioName == "Pressure5")
                {
                    pc5FeedBack.Add(PMDevice.Pressure5.FeedBack);
                    
                }
                else if(ioName == "Pressure6")
                {
                    pc6FeedBack.Add(PMDevice.Pressure6.FeedBack);
                }
                else if (ioName == "Pressure7")
                {
                    pc7FeedBack.Add(PMDevice.Pressure7.FeedBack);
                }
                
                return ElapsedTime>=10;
            },
            timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {

                    throw (new RoutineFaildException("Timeout"));
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetPcOffsetToConfig(int id, string ioName)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                _swTimer.Stop();
                double average = 0;
                if (ioName == "Pressure5")
                {
                    pc4FB1 = pc4FeedBack.Average();
                    pc5FB1 = pc5FeedBack.Average();
                    average = Convert.ToDouble((pc5FB1 - pc4FB1).ToString("0.00")) ;
                    Notify($"PC4FeedBack average: {pc4FB1.ToString("0.00")} , PC5FeedBack average: {pc5FB1.ToString("0.00")}");
                    //SC.SetItemValue($"PM.{Module}.PC5Offset", average);
                }
                else if (ioName == "Pressure6")
                {
                    pc4FB2 = pc4FeedBack.Average();
                    pc6FB2 = pc6FeedBack.Average();
                    average = Convert.ToDouble((pc6FB2 - pc4FB2).ToString("0.00"));
                    Notify($"PC4FeedBack average: {pc4FB2.ToString("0.00")} , PC6FeedBack average: {pc6FB2.ToString("0.00")}");
                    //SC.SetItemValue($"PM.{Module}.PC6Offset", average);
                }
                else if (ioName == "Pressure7")
                {
                    pc4FB3 = pc4FeedBack.Average();
                    pc7FB3 = pc7FeedBack.Average();
                    average = Convert.ToDouble((pc7FB3 - pc4FB3).ToString("0.00"));
                    Notify($"PC4FeedBack average: {pc4FB3.ToString("0.00")} , PC7FeedBack average: {pc7FB3.ToString("0.00")}");
                    //SC.SetItemValue($"PM.{Module}.PC7Offset", average);
                }

                
                Notify($"Set {ioName} offset {average} to config");
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
    }
}
