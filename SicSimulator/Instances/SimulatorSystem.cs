using System;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.WCF;

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
            SetSysDefaultValue("Crt");

            Singleton<DataManager>.Instance.Initialize(false);

            WcfServiceManager.Instance.Initialize(new Type[] { typeof(SimulatorAdsPlcService) });

            _thread = new PeriodicJob(200, OnMonitor, nameof(SimulatorSystem), true);
        }


        private void SetSysDefaultValue(string mod)
        {
            //IO.DI["DI_Dummy0"].Value = true;
           
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
        

        public void Terminate()
        {
            _thread.Stop();
        }
    }
}
