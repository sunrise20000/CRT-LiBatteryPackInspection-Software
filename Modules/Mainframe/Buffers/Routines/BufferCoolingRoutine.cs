using System;
using System.Diagnostics;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Routine;
using MECF.Framework.Common.Equipment;
using SicPM.Devices;

namespace Mainframe.Buffers.Routines
{
    public class BufferCoolingRoutine : ModuleRoutine, IRoutine
    {
        enum RoutineStep
        {
            Cooling,
        }

        private int _timeout = 3600;
        public float _coolingValue = 0.0f;
        public bool _coolingTypeIsTime = true;
        private IoTempMeter deviceBufferTemp;

        private Stopwatch _swTimer = new Stopwatch();

        public int ElapsedTime
        {
            get { return _swTimer.IsRunning ? (int)(_swTimer.ElapsedMilliseconds / 1000) : 0; }
        }

        public BufferCoolingRoutine(ModuleName module)
        {
            Module = module.ToString();
            Name = "Cooling";
            deviceBufferTemp = DEVICE.GetDevice<IoTempMeter>($"Buffer.BufferTemp");
        }

        public bool Initalize()
        {
            return true;
        }

        public void Init(bool coolingTypeIsTime,float coolingTime)
        {
            _coolingTypeIsTime = coolingTypeIsTime;
            _coolingValue = coolingTime;
            
        }

        public Result Start(params object[] objs)
        {
            Reset();
            _swTimer.Restart();
            return Result.RUN;
        }

        public Result Monitor()
        {
            try
            {
                if (_coolingTypeIsTime)
                {
                    TimeDelay((int)RoutineStep.Cooling, _coolingValue);
                }
                else
                {
                    WaitBufferTempBelowSetValue((int)RoutineStep.Cooling, _coolingValue, _timeout);
                }
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify("Finished");

            return Result.DONE;
        }

        protected void WaitBufferTempBelowSetValue(int id,double tempValue,int timeout)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                return deviceBufferTemp.FeedBack <= tempValue;
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
                    Stop($"Wait Buffer Temprature below {tempValue} timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void Abort()
        {
            Notify("Abort");
            _swTimer.Stop();
        }     
    }
}
