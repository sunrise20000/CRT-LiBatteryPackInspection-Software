using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Equipment;
using MECF.Framework.UI.Client.ClientBase;
using System.Collections.Generic;


namespace SicUI.Client
{
    public class ClientApp : BaseApp
    {
        public MainViewModel ViewModelSwitcher { get; set; }

        public ClientApp()
        {

        }

        protected override void OnInitialize()
        {
            ModuleManager.Initialize(new List<ModuleInfo>()
                {
                    new ModuleInfo(ModuleName.Buffer.ToString(),null, $"{ModuleName.Buffer}.ModuleWaferList", true, (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsBufferInstalled")),
                    new ModuleInfo(ModuleName.CassAL.ToString(),null,$"{ModuleName.CassAL}.ModuleWaferList", true, true),
                    new ModuleInfo(ModuleName.CassAR.ToString(),null,$"{ModuleName.CassAR}.ModuleWaferList", true, true),
                    new ModuleInfo(ModuleName.CassBL.ToString(),null,$"{ModuleName.CassBL}.ModuleWaferList", true, true),
                    new ModuleInfo(ModuleName.PM1.ToString(),null, $"{ModuleName.PM1}.ModuleWaferList", true, (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsPM1Installed")),
                    
                    new ModuleInfo(ModuleName.LoadLock.ToString(),null, $"{ModuleName.LoadLock}.ModuleWaferList", true, (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsLoadLockInstalled")),
                    new ModuleInfo(ModuleName.UnLoad.ToString(),null, $"{ModuleName.UnLoad}.ModuleWaferList", true, (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsUnLoadInstalled")),
                    new ModuleInfo(ModuleName.Aligner.ToString(),null, $"{ModuleName.Aligner}.ModuleWaferList", true, (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsAlignerInstalled")),
                    new ModuleInfo(ModuleName.TMRobot.ToString(),null, $"{ModuleName.TMRobot}.ModuleWaferList", true, true),
                    new ModuleInfo(ModuleName.TrayRobot.ToString(),null,$"{ModuleName.TrayRobot}.ModuleWaferList", true, true),
                    new ModuleInfo(ModuleName.WaferRobot.ToString(),null,$"{ModuleName.WaferRobot}.ModuleWaferList", true, true),
                }
            );
        }


    }
}
