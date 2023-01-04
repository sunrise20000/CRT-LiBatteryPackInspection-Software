
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Cassette;
using System.Xml;

namespace Mainframe.Cassettes
{
    public class SicCassette : Cassette
    {
        private ModuleName _module;

        public SicCassette(string module, XmlElement node, string ioModule = "") : base(module)
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _module = ModuleHelper.Converter(Module);
        }


        public override bool Initialize()
        {
            return base.Initialize();
        }
    }
}
