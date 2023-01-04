using System;
using System.Collections.Generic;
using Aitex.Common.Util;
using Aitex.Core.RT.ConfigCenter;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.DataCollection;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.RecipeCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using SicRT.Modules;
using MECF.Framework.Common.Account;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.IOCore;
using MECF.Framework.Common.SCCore;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.Core.Applications;
using MECF.Framework.RT.Core.Backend;
using MECF.Framework.RT.Core.IoProviders;
using SorterRT.Modules;
using SicRT.Equipments;
using SicRT.Equipments.Systems;
using SicPM.RecipeExecutions;
using System.IO;
using System.Reflection;
using DocumentFormat.OpenXml.ExtendedProperties;
using MECF.Framework.RT.EquipmentLibrary.Core;

namespace SicRT.Instances
{
    internal class ToolLoader : IRtLoader
    {
        #region Variables

        

        #endregion

        public void Initialize()
        {

            Singleton<LogManager>.Instance.Initialize();

            LOG.Write($"RT v{RtInstance.SystemVersion} launch ...");

            Singleton<ConfigManager>.Instance.Initialize();

            Singleton<DatabaseManager>.Instance.Initialize(
                System.Configuration.ConfigurationManager.ConnectionStrings["PostgreSQL"].ConnectionString
                , RtInstance.DATABASE_NAME
                , PathManager.GetCfgDir() + "DBModel.sql");

            Singleton<EventManager>.Instance.Initialize(PathManager.GetCfgDir() + "EventDefine.xml");
            
            // 需要最先初始化，其它对象可能依赖此类。
            DeviceLockerManager.Instance.Initialize();

            Singleton<OperationManager>.Instance.Initialize();

            Singleton<DataManager>.Instance.Initialize();

         

            AccountExManager.Instance.Initialize(true);

            Singleton<SystemConfigManager>.Instance.Initialize(PathManager.GetCfgDir() + "System.sccfg");

            ConnectionManager.Instance.Initialize();


            string ioProviderPathFile = PathManager.GetCfgDir() + "IoProviderConfig.xml";
 
            IoProviderManager.Instance.Initialize(ioProviderPathFile);

            IoManager.Instance.Initialize(PathManager.GetCfgDir() + "interlock.xml");

  
            IoManager.Instance.Initialize(PathManager.GetCfgDir() + "interlockPM.xml");

            WaferManager.Instance.Initialize();

            Singleton<DeviceManager>.Instance.DisableAsyncInitialize = true;

            Singleton<DeviceManager>.Instance.Initialize(PathManager.GetCfgDir() + "DeviceModelPM.xml", "Sic", ModuleName.PM1, "PM1", false);
            //Singleton<DeviceManager>.Instance.Initialize(PathManager.GetCfgDir() + "DeviceModelPM.xml", "Sic", ModuleName.PM2, "PM2", false);

            Singleton<DeviceManager>.Instance.Initialize(PathManager.GetCfgDir() + "DeviceModelSystem.xml", "Sic", ModuleName.System, "TM", true);

            Singleton<DeviceEntity>.Instance.Initialize();

            //DataCollectionManager.Instance.Initialize(new string[] { "System", "PM1", "PM2" }, RtInstance.DATABASE_NAME); 
            DataCollectionManager.Instance.Initialize(new string[] { "System",  "IO.PM1" ,  "IO.TM" ,  "PM1" }, RtInstance.DATABASE_NAME);

            RtSystemManager.Instance.AddCustomBackend("SC", new BackendSCConfigView());

            RecipeFileManager.Instance.Initialize(new SicRecipeFileContext(),true,SC.GetValue<bool>("System.RecipeSaveToDB"));

            Singleton<EquipmentManager>.Instance.Initialize();

           // Singleton<RouteManager>.Instance.Initialize();
            //Singleton<FAJobManager>.Instance.Initialize();

            Singleton<EventManager>.Instance.SubscribeOperationAndData();
            
           
        }


        public void Terminate()
        {
            Singleton<DeviceEntity>.Instance.Terminate();

            Singleton<DeviceManager>.Instance.Terminate();

            IoProviderManager.Instance.Terminate();

            Singleton<SystemConfigManager>.Instance.Terminate();

            DataCollectionManager.Instance.Terminate();

            Singleton<WcfServiceManager>.Instance.Terminate();

            Singleton<EventManager>.Instance.Terminate();

            Singleton<LogManager>.Instance.Terminate();

            Singleton<DatabaseManager>.Instance.Terminate();
        }
    }
}
