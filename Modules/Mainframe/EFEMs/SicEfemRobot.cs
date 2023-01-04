using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobot;
using System.Xml;

namespace Mainframe.EFEMs
{
    public class SicWaferRobot : HwinRobot
    {
        private ModuleName _module;

        public SicWaferRobot(string module, XmlElement node, string ioModule = "") : base("WaferRobot", "WaferRobot", "\r\n")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            _module = ModuleHelper.Converter(Module);
        }
    }


    public class SicTrayRobot : HwinRobotB
    {
        private ModuleName _module;

        public SicTrayRobot(string module, XmlElement node, string ioModule = "") : base("TrayRobot", "TrayRobot", "\r\n")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            _module = ModuleHelper.Converter(Module);
        }
    }
}
