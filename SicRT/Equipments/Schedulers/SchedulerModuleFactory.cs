using MECF.Framework.Common.Equipment;
using System.Collections.Generic;

namespace SicRT.Modules.Schedulers
{
    public class SchedulerModuleFactory
    {
        #region Variables

        
        protected SchedulerAligner SchAligner;
        //protected SchedulerPM _pm2 = new SchedulerPM(ModuleName.PM2);

        protected SchedulerCassette SchCassAL;
        protected SchedulerCassette SchCassAR;
        protected SchedulerCassette SchCassBL;
        
        protected List<SchedulerCassette> LstCassetteSchedulers;
        protected List<SchedulerModule> LstAllSchedulers;

        #endregion

        protected SchedulerModuleFactory()
        {
            SchAligner = new SchedulerAligner();
            SchCassAL = new SchedulerCassette(ModuleName.CassAL);
            SchCassAR = new SchedulerCassette(ModuleName.CassAR);
            SchCassBL = new SchedulerCassette(ModuleName.CassBL);
            LstCassetteSchedulers = new List<SchedulerCassette>(new[] { SchCassAL, SchCassAR, SchCassBL });
            LstAllSchedulers = new List<SchedulerModule>(new SchedulerModule[]
            {
                SchAligner,
                SchCassAL,
                SchCassAR,
                SchCassBL
            });
        }

        protected SchedulerModule GetModule(string name)
        {
            var module = ModuleHelper.Converter(name);
            return GetModule(module);
        }

        protected SchedulerModule GetModule(ModuleName name)
        {
            switch (name)
            {
                case ModuleName.TMRobot:
                case ModuleName.WaferRobot:
                case ModuleName.TrayRobot:
                case ModuleName.PM1:
                    return null;
                case ModuleName.CassAL:
                    return SchCassAL;
                case ModuleName.CassAR:
                    return SchCassAR;
                case ModuleName.CassBL:
                    return SchCassBL;
                case ModuleName.UnLoad:
                case ModuleName.Aligner:
                    return SchAligner;
            }

            return null;
        }

        public void Reset()
        {
           
        }

    }
}
