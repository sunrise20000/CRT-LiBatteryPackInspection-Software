using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    public partial class SicAETemp : BaseDevice, IDevice
    {
        private DOAccessor _doPyroCommunicationError = null;
      

        //private R_TRIG _trigInital = new R_TRIG();
        //private R_TRIG _trigError = new R_TRIG();
        //private R_TRIG _trigDisable = new R_TRIG();


        public SicAETemp(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _doPyroCommunicationError = ParseDoNode("doPyroCommunicationError", node, ioModule);            
        }

        public bool Initialize()
        {
            //DATA.Subscribe($"{Module}.{Name}.ServoReady", () => ServoReady);
            //DATA.Subscribe($"{Module}.{Name}.ServoError", () => ServoError); 
            //DATA.Subscribe($"{Module}.{Name}.ServoEnable", () => ServoEnable);

            OP.Subscribe($"{Module}.{Name}.SetPyroCommunicationError", (function, args) =>
            {
                bool enable = Convert.ToBoolean(args[0].ToString());
                SetPyroCommunicationError(enable, out string reason);
                return true;
            });
           

            return true;
        }

        public bool SetPyroCommunicationError(bool enable, out string reason)
        {
            //DO200暂时没有Interlock

            //if (!_doPyroCommunicationError.Check(enable, out reason))
            //{
            //    EV.PostWarningLog(Module, reason);
            //    return false;
            //}
            reason = string.Empty;
            if (_doPyroCommunicationError.Value == enable)
            {
                return true;
            }

            if (!_doPyroCommunicationError.SetValue(enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            if (enable)
            {
                //_trigDisable.RST = true;
            }
            return true;
        }
       

      

        public void Monitor()
        {
           
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
