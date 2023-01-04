using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.SiasunPhoenixB;
using System.Xml;

namespace Mainframe.TMs
{
    public class SicTMRobot : RobotSiasunPhoenixB
    {
        private ModuleName _module;

        private static bool isSimulator = SC.GetConfigItem("System.IsSimulatorMode").BoolValue; 

        public SicTMRobot(string module, XmlElement node, string ioModule = "") : base("TMRobot", "TMRobot", "", isSimulator ? "\r" : "\r\n")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            _module = ModuleHelper.Converter(Module);
        }
    }

}
