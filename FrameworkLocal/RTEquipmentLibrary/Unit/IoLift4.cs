using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoLift4 : BaseDevice, IDevice
    {
        private DIAccessor _diUp;
        private DIAccessor _diDown;
        private DOAccessor _doUp;
        private DOAccessor _doDown;

        public bool IsUp
        {
            get { return _diUp == null ? false : _diUp.Value; }
        }

        public bool IsDown
        {
            get { return _diDown == null ? false : _diDown.Value; }
        }

        public IoLift4(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diUp = ParseDiNode("diUp", node, ioModule);
            _diDown = ParseDiNode("diDown", node, ioModule);
            _doUp = ParseDoNode("doUp", node, ioModule);
            _doDown = ParseDoNode("doDown", node, ioModule);
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.UpSensor", () => _diUp.Value);
            DATA.Subscribe($"{Module}.{Name}.DownSensor", () => _diDown.Value);
            DATA.Subscribe($"{Module}.{Name}.DoUp", () => _doUp.Value);
            DATA.Subscribe($"{Module}.{Name}.DoDown", () => _doDown.Value);

            OP.Subscribe($"{Module}.{Name}.MoveUp", (function, args) =>
            {
                return MoveUp(out string reason);
            });
            OP.Subscribe($"{Module}.{Name}.MoveDown", (function, args) =>
            {
                return MoveDown(out string reason);
            });

            return true;
        }


        public void Terminate()
        {
            
        }

        public bool MoveUp(out string reason)
        {
            _doDown.SetValue(false, out reason);
            _doUp.SetValue(true, out reason);

            return true;
        }

        public bool MoveDown(out string reason)
        {
            _doDown.SetValue(true, out reason);
            _doUp.SetValue(false, out reason);

            return true;
        }

        public void Reset()
        {
        }

        public void Monitor()
        {
           
        }
    }
}
