using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public enum ClawStateEnum
    {
        Unknown,
        Clamp,
        Open,
        Error,
    }

    public class IoClaw : BaseDevice, IDevice
    {
        public ClawStateEnum State
        {
            get
            {
                if (_diUp.Value && _diDown.Value)
                    return ClawStateEnum.Error;
                if (_diUp.Value && !_diDown.Value)
                    return ClawStateEnum.Clamp;
                if (!_diUp.Value && _diDown.Value)
                    return ClawStateEnum.Open;
                return ClawStateEnum.Unknown;
            }
        }
        enum DeviceState
        {
            Idle,
            Clamping,
            UnClamping,
            Error,
        }

        private DIAccessor _diUp;
        private DIAccessor _diDown;
        private DOAccessor _doUp;
        private DOAccessor _doDown;

        private DeviceState _state = DeviceState.Idle;
        private DeviceTimer _timer = new DeviceTimer();

        private int _scTimeout=20;

        public bool IsClamp { get { return !_diDown.Value && _diUp.Value; } }
        public bool IsUnClamp { get { return _diDown.Value && !_diUp.Value; } }


        public IoClaw(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diUp = ParseDiNode("diUp", node, ioModule);
            _diDown = ParseDiNode("diDown", node, ioModule);
            _doUp = ParseDoNode("doUp", node, ioModule);
            _doDown = ParseDoNode("doDown", node, ioModule);

            //_scTimeout = ParseScNode("scTimeout", node, ioModule);
        }

        public bool Initialize()
        {
            _state = DeviceState.Idle;

            DATA.Subscribe($"{Module}.{Name}.ClampFeedback", () => _diUp.Value);
            DATA.Subscribe($"{Module}.{Name}.UnClampFeedback", () => _diDown.Value);
            DATA.Subscribe($"{Module}.{Name}.State", () => State.ToString());

            OP.Subscribe($"{Module}.{Name}.Clamping", (function, args) =>
            {
                return Clamp(out string reason);
            }); 
            OP.Subscribe($"{Module}.{Name}.UnClamping", (function, args) =>
            {
                return UnClamp(out string reason);
            });
            return true;
        }


        public void Monitor()
        {
            switch (_state)
            {
                case DeviceState.Clamping:
                    if (IsClamp)
                    {
                        _timer.Stop();
                        if (!_doUp.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        _timer.Stop();
                        if (!_doUp.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        EV.PostAlarmLog(Module, $"{Module} {Name} Can not move up in {_scTimeout} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                case DeviceState.UnClamping:
                    if (IsUnClamp)
                    {
                        _timer.Stop();
                        if (!_doDown.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        _timer.Stop();
                        if (!_doDown.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        EV.PostAlarmLog(Module, $"{Module} {Name} Can not move down in {_scTimeout} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                default:
                    break;
            }
        }

        public void Terminate()
        {
            _doDown.SetValue(false, out _);
            _doUp.SetValue(false, out _);
        }

        public bool SetValue(bool up, out string reason)
        {
            if (up)
            {
                return Clamp(out reason);
            }

            return UnClamp(out reason);

        }

        public bool Clamp(out string reason)
        {
            if (!_doDown.SetValue(false, out reason) || !_doUp.SetValue(true, out reason))
            {
                _doDown.SetValue(false, out _);
                _doUp.SetValue(false, out _);
                return false;
            }

            _timer.Start(_scTimeout * 1000);
            _state = DeviceState.Clamping;
            return true;
        }


        public bool UnClamp(out string reason)
        {
            if (!_doDown.SetValue(true, out reason) || !_doUp.SetValue(false, out reason))
            {
                _doDown.SetValue(false, out _);
                _doUp.SetValue(false, out _);
                return false;
            }

            _timer.Start(_scTimeout * 1000);
            _state = DeviceState.UnClamping;

            return true;
        }


        public void Reset()
        {
        }
    }
}
