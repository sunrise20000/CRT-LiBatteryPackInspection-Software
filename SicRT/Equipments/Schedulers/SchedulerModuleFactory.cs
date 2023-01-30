using MECF.Framework.Common.Equipment;
using System.Collections.Generic;

namespace SicRT.Modules.Schedulers
{
    public class SchedulerModuleFactory
    {
        #region Variables

        protected SchedulerFeeder SchFeederA;
        protected SchedulerFeeder SchFeederB;
        
        protected Dictionary<ModuleName, SchedulerModule> dictAllSchedulers;

        #endregion

        protected SchedulerModuleFactory()
        {
            SchFeederA = new SchedulerFeeder(ModuleName.FeederA);
            SchFeederB = new SchedulerFeeder(ModuleName.FeederB);

            dictAllSchedulers = new Dictionary<ModuleName, SchedulerModule>
            {
                {
                    ModuleName.FeederA, SchFeederA
                },
                {
                    ModuleName.FeederB, SchFeederB
                },
            };
        }

        protected SchedulerModule GetModule(string name)
        {
            var module = ModuleHelper.Converter(name);
            return GetModule(module);
        }

        protected SchedulerModule GetModule(ModuleName module)
        {
            if(dictAllSchedulers.ContainsKey(module))
                return dictAllSchedulers[module];

            return null;
        }

        public void Reset()
        {
           
        }

    }
}
