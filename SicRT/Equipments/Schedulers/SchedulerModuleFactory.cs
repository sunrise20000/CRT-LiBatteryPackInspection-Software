using SicRT.Scheduler;
using MECF.Framework.Common.Equipment;
using System.Collections.Generic;

namespace SicRT.Modules.Schedulers
{
    public class SchedulerModuleFactory
    {
        #region Variables

        
        protected SchedulerTMRobot SchTmRobot;
        protected SchedulerWaferRobot SchWaferRobot;
        protected SchedulerTrayRobot SchTrayRobot;

        protected SchedulerBuffer SchBuffer;
        protected SchedulerLoadLock SchLoadLock;
        protected SchedulerUnLoad SchUnLoad;
        protected SchedulerAligner SchAligner;
        protected SchedulerPM SchPm1;
        //protected SchedulerPM _pm2 = new SchedulerPM(ModuleName.PM2);

        protected SchedulerCassette SchCassAL;
        protected SchedulerCassette SchCassAR;
        protected SchedulerCassette SchCassBL;

        protected List<SchedulerPM> LstPmSchedulers;
        protected List<SchedulerCassette> LstCassetteSchedulers;
        protected List<SchedulerModule> LstAllSchedulers;

        #endregion

        protected SchedulerModuleFactory()
        {
            SchTmRobot = new SchedulerTMRobot();
            SchWaferRobot = new SchedulerWaferRobot();
            SchTrayRobot = new SchedulerTrayRobot();
            SchBuffer = new SchedulerBuffer();
            SchLoadLock = new SchedulerLoadLock();
            SchUnLoad = new SchedulerUnLoad();
            SchAligner = new SchedulerAligner();
            SchPm1 = new SchedulerPM(ModuleName.PM1);
            SchCassAL = new SchedulerCassette(ModuleName.CassAL);
            SchCassAR = new SchedulerCassette(ModuleName.CassAR);
            SchCassBL = new SchedulerCassette(ModuleName.CassBL);

            LstPmSchedulers = new List<SchedulerPM>(new[] { SchPm1 });
            LstCassetteSchedulers = new List<SchedulerCassette>(new[] { SchCassAL, SchCassAR, SchCassBL });
            LstAllSchedulers = new List<SchedulerModule>(new SchedulerModule[]
            {
                SchTmRobot,
                SchWaferRobot,
                SchTrayRobot,
                SchBuffer,
                SchLoadLock,
                SchUnLoad,
                SchAligner,
                SchPm1,
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
                case ModuleName.Buffer:
                    return SchBuffer;
                case ModuleName.TMRobot:
                    return SchTmRobot;
                case ModuleName.WaferRobot:
                    return SchWaferRobot;
                case ModuleName.TrayRobot:
                    return SchTrayRobot;
                case ModuleName.PM1:
                    return SchPm1;
                case ModuleName.CassAL:
                    return SchCassAL;
                case ModuleName.CassAR:
                    return SchCassAR;
                case ModuleName.CassBL:
                    return SchCassBL;
                case ModuleName.LoadLock:
                    return SchLoadLock;
                case ModuleName.UnLoad:
                    return SchUnLoad;
                case ModuleName.Aligner:
                    return SchAligner;
            }

            return null;
        }

        public void Reset()
        {
            SchTmRobot.ResetTask();
            foreach (var pm in LstPmSchedulers)
            {
                pm.ResetTask();
            }

            SchBuffer.ResetTask();
        }

    }
}
