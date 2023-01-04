using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    public enum BottomSectionStateEnum
    {
        Unknown,
        Up,
        Down,
        Error,
    }
    public partial class IoBottomSection : BaseDevice, IDevice
    {
        //public BottomSectionStateEnum State
        //{
        //    get
        //    {
        //        if (_diUpFaceback.Value && _diDownFaceback.Value)
        //            return BottomSectionStateEnum.Error;
        //        if (_diUpFaceback.Value && !_diDownFaceback.Value)
        //            return BottomSectionStateEnum.Up;
        //        if (!_diUpFaceback.Value && _diDownFaceback.Value)
        //            return BottomSectionStateEnum.Down;
        //        return BottomSectionStateEnum.Unknown;
        //    }
        //}
        enum DeviceState
        {
            Idle,
            MovingUp,
            MovingDown,
            Error,
        }
        //private DIAccessor _diUpFaceback = null;
        //private DIAccessor _diDownFaceback = null;
        //private DOAccessor _doUpSetpoint = null;
        //private DOAccessor _doDownSetpoint = null;

        private DeviceState _state = DeviceState.Idle;
        private DeviceTimer _timer = new DeviceTimer();

        private SCConfigItem _scTimeout;

        //public bool IsUp { get { return !_diDownFaceback.Value && _diUpFaceback.Value; } }
        //public bool IsDown { get { return _diDownFaceback.Value && !_diUpFaceback.Value; } }
        #region DI
        //public bool UpFaceback
        //{
        //    get
        //    {
        //        if (_diUpFaceback != null)
        //            return _diUpFaceback.Value;

        //        return false;
        //    }
        //}

        //public bool DownFaceback
        //{
        //    get
        //    {
        //        if (_diDownFaceback != null)
        //            return _diDownFaceback.Value;

        //        return false;
        //    }
        //}

        #endregion

        #region DO
        //public bool UpSetpoint
        //{
        //    get
        //    {
        //        if (_doUpSetpoint != null)
        //            return _doUpSetpoint.Value;

        //        return false;
        //    }
        //}

        //public bool DownSetpoint
        //{
        //    get
        //    {
        //        if (_doDownSetpoint != null)
        //            return _doDownSetpoint.Value;

        //        return false;
        //    }
        //}
        #endregion

        //public AITDeviceData DeviceData
        //{
        //    get
        //    {
        //        AITDeviceData data = new AITDeviceData()
        //        {
        //            Module = Module,
        //            DeviceName = Name,
        //            DisplayName = Display,
        //            DeviceSchematicId = DeviceID,
        //            UniqueName = UniqueName,

        //        };
        //        data.AttrValue["UpFaceback"] = _diUpFaceback.Value;
        //        data.AttrValue["DownFaceback"] = _diDownFaceback.Value;
        //        data.AttrValue["UpSetpoint"] = _doUpSetpoint.Value;
        //        data.AttrValue["DownSetpoint"] = _doDownSetpoint.Value;
        //        //data.AttrValue["PV"] = _aiFeedBack.Value;
        //        return data;
        //    }
        //}
        public IoBottomSection(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            //_diUpFaceback = ParseDiNode("diUpFaceback", node, ioModule);
            //_diDownFaceback = ParseDiNode("diDownFaceback", node, ioModule);

            //_doUpSetpoint = ParseDoNode("doUpSetpoint", node, ioModule);
            //_doDownSetpoint = ParseDoNode("doDownSetpoint", node, ioModule);

            _scTimeout = ParseScNode("ButtomSectionUpTimeout", node, "PM", "PM.Motion.ButtomSectionUpTimeout");
        }
        public bool Initialize()
        {
            //DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            //DATA.Subscribe($"{Module}.{Name}.Status", () => ((BottomSectionStateEnum)State).ToString());
            //DATA.Subscribe($"{Module}.{Name}.UpFaceback", () => UpFaceback);
            //DATA.Subscribe($"{Module}.{Name}.DownFaceback", () => DownFaceback);
            //DATA.Subscribe($"{Module}.{Name}.UpSetpoint", () => UpSetpoint);
            //DATA.Subscribe($"{Module}.{Name}.DownSetpoint", () => DownSetpoint);

            OP.Subscribe($"{Module}.{Name}.Up", (function, args) =>
            {
                bool ret = MoveUp(out reason);
                if (ret)
                {
                    reason = string.Format("Open Lid {0}", Name);
                    return true;
                }
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Down", (function, args) =>
            {
                bool ret = MoveDown(out reason);
                if (ret)
                {
                    reason = string.Format("Close Lid {0}", Name);
                    return true;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetUpSetpoint", (function, args) =>
            {
                MoveUp(out reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetDownSetpoint", (function, args) =>
            {
                MoveDown(out reason);
                return true;
            });
            return false;
        }
        string reason = string.Empty;
        public bool MoveUp(out string reason)
        {
            //if (State == BottomSectionStateEnum.Up)
            //{
            reason = "";
            //    return true;
            //}

            //if (!_doDownSetpoint.Check(false, out reason))
            //    return false;
            //if (!_doUpSetpoint.Check(true, out reason))
            //    return false;

            //if (!_doDownSetpoint.SetValue(false, out reason))
            //    return false;
            //if (!_doUpSetpoint.SetValue(true, out reason))
            //    return false;


            //_timer.Start(_scTimeout.IntValue * 1000);

            //_state = DeviceState.MovingUp;
            return true;
        }
        public bool MoveDown(out string reason)
        {
            //if (State == BottomSectionStateEnum.Down)
            //{
            reason = "";
            //    return true;
            //}

            //if (!_doDownSetpoint.Check(true, out reason))
            //{
            //    EV.PostMessage(Module, EventEnum.DefaultWarning, reason);
            //    return false;
            //}
            //if (!_doUpSetpoint.Check(false, out reason))
            //{
            //    EV.PostMessage(Module, EventEnum.DefaultWarning, reason);
            //    return false;
            //}

            //if (!_doDownSetpoint.SetValue(true, out reason))
            //    return false;
            //if (!_doUpSetpoint.SetValue(false, out reason))
            //    return false;


            //_timer.Start(_scTimeout.IntValue * 1000);

            //_state = DeviceState.MovingDown;

            return true;
        }
        public void Monitor()
        {
            //switch (_state)
            //{
            //    case DeviceState.MovingUp:
            //        if (IsUp)
            //        {
            //            if (!_doUpSetpoint.Check(false, out string reason))
            //            {
            //                LOG.Error($"{Module} reset DO failed, {reason}");
            //            }

            //            _doUpSetpoint.Value = false;
            //            _state = DeviceState.Idle;
            //        }
            //        else if (_timer.IsTimeout())
            //        {
            //            _timer.Stop();
            //            if (!_doUpSetpoint.Check(false, out string reason))
            //            {
            //                LOG.Error($"{Module} reset DO failed, {reason}");
            //            }

            //            EV.PostAlarmLog(Module, $"{Module} {Name} Can not move up in {_scTimeout.IntValue} seconds");
            //            _doUpSetpoint.Value = false;
            //            _state = DeviceState.Error;
            //        }
            //        break;
            //    case DeviceState.MovingDown:
            //        if (IsDown)
            //        {
            //            if (!_doDownSetpoint.Check(false, out string reason))
            //            {
            //                LOG.Error($"{Module} reset DO failed, {reason}");
            //            }

            //            _doDownSetpoint.Value = false;
            //            _state = DeviceState.Idle;
            //        }
            //        else if (_timer.IsTimeout())
            //        {
            //            _timer.Stop();
            //            if (!_doDownSetpoint.Check(false, out string reason))
            //            {
            //                LOG.Error($"{Module} reset DO failed, {reason}");
            //            }

            //            EV.PostAlarmLog(Module, $"{Module} {Name} Can not move down in {_scTimeout.IntValue} seconds");

            //            _doDownSetpoint.Value = false;
            //            _state = DeviceState.Error;
            //        }
            //        break;
            //    default:
            //        break;
            //}
        }

        public void Reset()
        {
           
        }

        public void Terminate()
        {
            //_doDownSetpoint.SetValue(false, out _);
            //_doUpSetpoint.SetValue(false, out _);
        }
    }
}
