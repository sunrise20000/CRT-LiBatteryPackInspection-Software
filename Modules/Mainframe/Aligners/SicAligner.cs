using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligner;
using System.Xml;

namespace Mainframe.Aligners
{
    public class SicAligner : Aligner
    {
        private ModuleName _module;

        public SicAligner(string module, XmlElement node, string ioModule = "") : base(module)
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
