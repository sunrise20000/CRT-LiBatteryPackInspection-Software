using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    public class IoHeat : BaseDevice, IDevice
    {

        private DIAccessor _diEnableFaceBack = null;
        private DOAccessor _doEnableSetPoint = null;

        public bool HeatEnableFeedback
        {
            get
            {
                return _diEnableFaceBack.Value;
            }
        }

        public IoHeat(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diEnableFaceBack = ParseDiNode("diEnable", node, ioModule);
            _doEnableSetPoint = ParseDoNode("doEnable", node, ioModule);
        }

        public bool SetHeatEnable(bool setValue)
        {
            string reason = "";

            if (!_doEnableSetPoint.Check(setValue, out reason))
            {
                return false;
            }
            if (!_doEnableSetPoint.SetValue(setValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool Initialize()
        {
            return true;
            //throw new NotImplementedException();
        }

        public void Monitor()
        {
            //throw new NotImplementedException();
        }

        public void Reset()
        {
            //throw new NotImplementedException();
        }

        public void Terminate()
        {
            //throw new NotImplementedException();
        }
    }
}
