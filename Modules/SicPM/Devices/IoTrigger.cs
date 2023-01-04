using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
   public partial class IoTrigger : BaseDevice, IDevice
    {
        private DOAccessor _doPSUEnable = null;
        private DOAccessor _doInnerHeaterEnable = null;
        private DOAccessor _doMiddleHeaterEnable = null;
        private DOAccessor _doOuterHeaterEnable = null;
        private DOAccessor _doLineHeaterEnable = null;
        private DOAccessor _doLidMotionEnable = null;
        private DOAccessor _doRotationMotorEnable = null;

        #region DO
        public bool PSUEnable
        {
            get
            {
                if (_doPSUEnable != null)
                    return _doPSUEnable.Value;

                return false;
            }
        }

        public bool InnerHeaterEnable
        {
            get
            {
                if (_doInnerHeaterEnable != null)
                    return _doInnerHeaterEnable.Value;

                return false;
            }
        }
        public bool MiddleHeaterEnable
        {
            get
            {
                if (_doMiddleHeaterEnable != null)
                    return _doMiddleHeaterEnable.Value;

                return false;
            }
        }

        public bool OuterHeaterEnable
        {
            get
            {
                if (_doOuterHeaterEnable != null)
                    return _doOuterHeaterEnable.Value;

                return false;
            }
        }

        public bool LineHeaterEnable
        {
            get
            {
                if (_doLineHeaterEnable != null)
                    return _doLineHeaterEnable.Value;

                return false;
            }
        }
        public bool LidMotionEnable
        {
            get
            {
                if (_doLidMotionEnable != null)
                    return _doLidMotionEnable.Value;

                return false;
            }
        }

        public bool RotationMotorEnable
        {
            get
            {
                if (_doRotationMotorEnable != null)
                    return _doRotationMotorEnable.Value;

                return false;
            }
        }
        #endregion
        public AITDeviceData DeviceData
        {
            get
            {
                AITDeviceData data = new AITDeviceData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DisplayName = Display,
                    DeviceSchematicId = DeviceID,
                    UniqueName = UniqueName,

                };
                //data.AttrValue["PV"] = _aiFeedBack.Value;
                return data;
            }
        }
        public IoTrigger(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _doPSUEnable = ParseDoNode("doPSUEnable", node, ioModule);
            _doInnerHeaterEnable = ParseDoNode("doInnerHeaterEnable", node, ioModule);
            _doMiddleHeaterEnable = ParseDoNode("doMiddleHeaterEnable", node, ioModule);
            _doOuterHeaterEnable = ParseDoNode("doOuterHeaterEnable", node, ioModule);
            _doLineHeaterEnable = ParseDoNode("doLineHeaterEnable", node, ioModule);
            _doLidMotionEnable = ParseDoNode("doLidMotionEnable", node, ioModule);
            _doRotationMotorEnable = ParseDoNode("doRotationMotorEnable", node, ioModule);
        }
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.PSUEnable", () => PSUEnable);
            DATA.Subscribe($"{Module}.{Name}.InnerHeaterEnable", () => InnerHeaterEnable);
            DATA.Subscribe($"{Module}.{Name}.MiddleHeaterEnable", () => MiddleHeaterEnable);
            DATA.Subscribe($"{Module}.{Name}.OuterHeaterEnable", () => OuterHeaterEnable);
            DATA.Subscribe($"{Module}.{Name}.LineHeaterEnable", () => LineHeaterEnable);
            DATA.Subscribe($"{Module}.{Name}.LidMotionEnable", () => LidMotionEnable);
            DATA.Subscribe($"{Module}.{Name}.RotationMotorEnable", () => RotationMotorEnable);

            OP.Subscribe($"{Module}.{Name}.SetPSUEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetPSUEnable(isTrue);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetInnerHeaterEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetInnerHeaterEnable(isTrue);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetMiddleHeaterEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetMiddleHeaterEnable(isTrue);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetOuterHeaterEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetOuterHeaterEnable(isTrue);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetLineHeaterEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetLineHeaterEnable(isTrue);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetLidMotionEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetLidMotionEnable(isTrue);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetRotationMotorEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetRotationMotorEnable(isTrue);
                return true;
            });
            return false;
        }
        string reason = string.Empty;
        public bool SetPSUEnable(bool falg)
        {
            if (_doPSUEnable.SetValue(falg, out reason))
            {
                return true;
            }
            return false;
        }
        public bool SetInnerHeaterEnable(bool falg)
        {
            if (_doInnerHeaterEnable.SetValue(falg, out reason))
            {
                return true;
            }
            return false;
        }
        public bool SetMiddleHeaterEnable(bool falg)
        {
            if (_doMiddleHeaterEnable.SetValue(falg, out reason))
            {
                return true;
            }
            return false;
        }
        public bool SetOuterHeaterEnable(bool falg)
        {
            if (_doOuterHeaterEnable.SetValue(falg, out reason))
            {
                return true;
            }
            return false;
        }

        public bool SetLineHeaterEnable(bool falg)
        {
            if (_doLineHeaterEnable.SetValue(falg, out reason))
            {
                return true;
            }
            return false;
        }
        public bool SetLidMotionEnable(bool falg)
        {
            if (_doLidMotionEnable.SetValue(falg, out reason))
            {
                return true;
            }
            return false;
        }
        public bool SetRotationMotorEnable(bool falg)
        {
            if (_doRotationMotorEnable.SetValue(falg, out reason))
            {
                return true;
            }
            return false;
        }
        public void Monitor()
        {
             
        }

        public void Reset()
        {
             
        }

        public void Terminate()
        {
             
        }
    }
}
