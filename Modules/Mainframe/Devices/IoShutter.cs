using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using System;
using System.Xml;

namespace Mainframe.Devices
{
    public class IoShutter : BaseDevice, IDevice
    {
        private DIAccessor _diUpTightenFaceback = null;
        private DIAccessor _diDownLoosenFaceback = null;
        private DOAccessor _doUpTightenFaceback = null;
        private DOAccessor _doDownLoosenFaceback = null;

        private DeviceTimer _timer = new DeviceTimer();
        private ShutterState _operation;

        public IoShutter(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diUpTightenFaceback = ParseDiNode("diUpTightenFaceback", node, ioModule);
            _diDownLoosenFaceback = ParseDiNode("diDownLoosenFaceback", node, ioModule);
            _doUpTightenFaceback = ParseDoNode("doUpTightenFaceback", node, ioModule);
            _doDownLoosenFaceback = ParseDoNode("doDownLoosenFaceback", node, ioModule);
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.UpTightenFeedback", () => DiUpTightenFaceback);
            DATA.Subscribe($"{Module}.{Name}.DownLoosenFeedback", () => DiDownLoosenFaceback);
            DATA.Subscribe($"{Module}.{Name}.UpTightenSetPoint", () => DoUpTightenFaceback);
            DATA.Subscribe($"{Module}.{Name}.DownLoosenSetPoint", () => DoDownLoosenFaceback);

            OP.Subscribe($"{Module}.{Name}.SetUpOrClamp", (function, args) =>
            {
                SetValue(true);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetDownOrOpen", (function, args) =>
            {
                SetValue(false);                
                return true;
            });

            return true;
        }

        public void Monitor()
        {
            if (_timer.IsTimeout())
            {
                _timer.Stop();
                _doUpTightenFaceback.Value = false;
                _doDownLoosenFaceback.Value = false;

                if (_operation == ShutterState.UpTighten && (!_diUpTightenFaceback.Value || _diDownLoosenFaceback.Value))
                {
                    EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format($"Set{Module} value failed! Reason:PLC kept"));
                }
                else if (_operation == ShutterState.DownLoosen && (_diUpTightenFaceback.Value || !_diDownLoosenFaceback.Value))
                {
                    EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format($"Set{Module} value failed! Reason:PLC kept"));
                }
            }
        }

        public void Reset()
        {
        }

        public void Terminate()
        {
        }



        #region Function
        public enum ShutterState
        {
            UpTighten = 0,
            DownLoosen = 1,
            Unknown = 2,
        }

        public bool SetValue(bool upOrTigten)
        {
            _timer.Stop();

            string reason =string.Empty;
            if (!_doUpTightenFaceback.Check(upOrTigten, out reason))
            {
                EV.PostMessage(Module, EventEnum.SwInterlock, string.Format($"Can not set {Module} :" + reason));
                return false;
            }
            if (!_doDownLoosenFaceback.Check(!upOrTigten, out reason))
            {
                EV.PostMessage(Module, EventEnum.SwInterlock, string.Format($"Can not set {Module} :" + reason));
                return false;
            }

            if (!_doUpTightenFaceback.SetValue(upOrTigten, out reason))
            {
                EV.PostMessage(Module, EventEnum.SwInterlock, string.Format($"Can not set {Module} :" + reason));
                return false;
            }
            if (!_doDownLoosenFaceback.SetValue(!upOrTigten, out reason))
            {
                EV.PostMessage(Module, EventEnum.SwInterlock, string.Format($"Can not set {Module} :" + reason));
                return false;
            }

            _operation = upOrTigten ? ShutterState.UpTighten : ShutterState.DownLoosen;
            _timer.Start(1000 * 10);
            return true;
        }

        #endregion

        #region Properties

        public bool DiUpTightenFaceback
        {
            get
            {
                if (_diUpTightenFaceback != null)
                    return _diUpTightenFaceback.Value;

                return false;
            }
        }
        public bool DiDownLoosenFaceback
        {
            get
            {
                if (_diDownLoosenFaceback != null)
                    return _diDownLoosenFaceback.Value;

                return false;
            }
        }

        public bool DoUpTightenFaceback
        {
            get
            {
                if (_doUpTightenFaceback != null)
                    return _doUpTightenFaceback.Value;

                return false;
            }
        }


        public bool DoDownLoosenFaceback
        {
            get
            {
                if (_doDownLoosenFaceback != null)
                    return _doDownLoosenFaceback.Value;

                return false;
            }
        }
        #endregion
    }
}
