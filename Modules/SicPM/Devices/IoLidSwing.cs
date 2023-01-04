using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    public enum LidSwingState
    {
        Lock = 0,
        UnLock = 1,
        Unknown = 2,
        Error = 3,
    }
    public class LidSwingOperation
    {
        public const string LidSwingLock = "Lock";
        public const string LidSwingUnLock = "Unlock";

    }
    public partial class IoLidSwing : BaseDevice, IDevice
    {
        public int SetPoint
        {
            get
            {
                if (_doLidLockSetpoint.Value && _doLidUnlockSetpoint.Value) return (int)LidSwingState.Error;
                if (_doLidLockSetpoint.Value && !_doLidUnlockSetpoint.Value) return (int)LidSwingState.Lock;
                if (!_doLidLockSetpoint.Value && _doLidUnlockSetpoint.Value) return (int)LidSwingState.UnLock;
                if (!_doLidLockSetpoint.Value && !_doLidUnlockSetpoint.Value) return (int)LidSwingState.Unknown;

                return (int)LidSwingState.Unknown;
            }
        }
        public int Status
        {
            get
            {
                if (_diLidLockFaceback.Value && _diLidUnlockFaceback.Value) return (int)LidSwingState.Error;
                if (_diLidLockFaceback.Value && !_diLidUnlockFaceback.Value) return (int)LidSwingState.Lock;
                if (!_diLidLockFaceback.Value && _diLidUnlockFaceback.Value) return (int)LidSwingState.UnLock;
                if (!_diLidLockFaceback.Value && !_diLidUnlockFaceback.Value) return (int)LidSwingState.Unknown;

                return (int)LidSwingState.Unknown;
            }
        }
        private DIAccessor _diLidLockFaceback = null;
        private DIAccessor _diLidUnlockFaceback = null;

        private DOAccessor _doLidLockSetpoint = null;
        private DOAccessor _doLidUnlockSetpoint = null;

        private SCConfigItem _scTimeout;
        private LidSwingState _operation;
        private DeviceTimer _timer = new DeviceTimer();
        private R_TRIG _trigReset = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigTimer = new R_TRIG();

        #region DI
        public bool LidLockFaceback
        {
            get
            {
                if (_diLidLockFaceback != null)
                    return _diLidLockFaceback.Value;

                return false;
            }
        }

        public bool LidUnlockFaceback
        {
            get
            {
                if (_diLidUnlockFaceback != null)
                    return _diLidUnlockFaceback.Value;

                return false;
            }
        }

        #endregion

        #region DO
        public bool LidLockSetpoint
        {
            get
            {
                if (_doLidLockSetpoint != null)
                    return _doLidLockSetpoint.Value;

                return false;
            }
        }

        public bool LidUnlockSetpoint
        {
            get
            {
                if (_doLidUnlockSetpoint != null)
                    return _doLidUnlockSetpoint.Value;

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
                data.AttrValue["LidLockFaceback"] = _diLidLockFaceback.Value;
                data.AttrValue["LidUnlockFaceback"] = _diLidUnlockFaceback.Value;
                data.AttrValue["LidLockSetpoint"] = _doLidLockSetpoint.Value;
                data.AttrValue["LidUnlockSetpoint"] = _doLidUnlockSetpoint.Value;
                //data.AttrValue["PV"] = _aiFeedBack.Value;
                return data;
            }
        }
        public IoLidSwing(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diLidLockFaceback = ParseDiNode("diLidLockFaceback", node, ioModule);
            _diLidUnlockFaceback = ParseDiNode("diLidUnlockFaceback", node, ioModule);
            _doLidLockSetpoint = ParseDoNode("doLidLockSetpoint", node, ioModule);
            _doLidUnlockSetpoint = ParseDoNode("doLidUnlockSetpoint", node, ioModule);

            _scTimeout = ParseScNode("LidTimeout", node, "PM", "PM.LidMotionTimeout");
        }
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.LidLockFaceback", () => LidLockFaceback);
            DATA.Subscribe($"{Module}.{Name}.LidUnlockFaceback", () => LidUnlockFaceback);
            
            DATA.Subscribe($"{Module}.{Name}.LidLockSetpoint", () => LidLockSetpoint);
            DATA.Subscribe($"{Module}.{Name}.LidUnlockSetpoint", () => LidUnlockSetpoint);

            OP.Subscribe($"{Module}.{Name}.SetLidLockSetpoint", (function, args) =>
            {
                SetLidSwing(true, out string reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetLidUnlockSetpoint", (function, args) =>
            {
                SetLidSwing(false, out string reason);
                return true;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, LidSwingOperation.LidSwingLock), (out string reason, int time, object[] param) =>
            {
                bool ret = SetLidSwing(true, out reason);
                if (ret)
                {
                    reason = string.Format("{0} is Lock", Name);
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, LidSwingOperation.LidSwingUnLock), (out string reason, int time, object[] param) =>
            {
                bool ret = SetLidSwing(false, out reason);
                if (ret)
                {
                    reason = string.Format("{0} is Unlock", Name);
                    return true;
                }

                return false;
            });
            return false;
        }
        string reason = string.Empty;
        //public bool SetLidLockSetpoint(bool falg)
        //{
        //    if (_doLidLockSetpoint.SetValue(falg, out reason))
        //    {
        //        return true;
        //    }
        //    return false;
        //}
        //public bool SetLidUnlockSetpoint(bool falg)
        //{
        //    if (_doLidUnlockSetpoint.SetValue(falg, out reason))
        //    {
        //        return true;
        //    }
        //    return false;
        //}
        public bool SetLidSwing(bool isLock, out string reason)
        {
            if (isLock && Status == (int)LidSwingState.Lock)
            {
                _doLidUnlockSetpoint.SetValue(!isLock, out reason);
                reason = "";
                return true;
            }

            if (!isLock && Status == (int)LidSwingState.UnLock)
            {
                _doLidLockSetpoint.SetValue(isLock, out reason);
                reason = "";
                return true;
            }

            if (!_doLidLockSetpoint.Check(isLock, out reason))
            {
                EV.PostMessage(Module, EventEnum.DefaultWarning, reason);
                return false;
            }                
            if (!_doLidUnlockSetpoint.Check(!isLock, out reason))
            {
                EV.PostMessage(Module, EventEnum.DefaultWarning, reason);
                return false;
            }

            if (!_doLidLockSetpoint.SetValue(isLock, out reason))
                return false;
            if (!_doLidUnlockSetpoint.SetValue(!isLock, out reason))
                return false;

            _operation = isLock ? LidSwingState.Lock : LidSwingState.UnLock;
            _timer.Start(1000 * _scTimeout.IntValue);
            return true;
        }
        public void Monitor()
        {
            try
            {
                _trigTimer.CLK = _timer.IsIdle();
                if (_trigTimer.Q)
                {
                    _doLidLockSetpoint.Value = false;
                    _doLidUnlockSetpoint.Value = false;
                }

                if (_timer.IsTimeout())
                {
                    _timer.Stop();

                    //_trigReset.CLK = Status != (int)_operation;   // fire event only check at first, SetPoint set by interlock
                    //if (_trigReset.Q)
                    
                    if (Status != (int)_operation)
                    {
                        if (_operation == LidSwingState.Lock)
                        {
                            string reason;
                            if (!_doLidLockSetpoint.Check(true, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason：{2}", Display, "UnLock", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason {2}", Display, "UnLock", "PLC kept"));
                        }
                        else
                        {
                            string reason;
                            if (!_doLidLockSetpoint.Check(false, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason：{2}", Display, "Lock", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason {2}", Display, "Lock", "PLC Kept"));
                        }
                        _operation = (LidSwingState)SetPoint;
                    }
                }
                else if (_timer.IsIdle())
                {
                    
                }

                //_trigError.CLK = Status == (int)GasConnectorState.Error;
                //if (_trigError.Q)
                //{
                //    EV.PostMessage(Module, EventEnum.DefaultWarning, "LidSwing in error status,lock and unlock are both Off");
                //}

                //if (SetPoint == Status)// && (SetPoint == (int)LidSwingState.Lock || SetPoint == (int)LidSwingState.UnLock))
                //{
                //    _timer.Stop();
                //    //_doLidUnlockSetpoint.Value = false;
                //    //_doLidLockSetpoint.Value = false;
                //}


            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void Reset()
        {
            _trigReset.RST = true;
            _trigError.RST = true;
            _trigTimer.RST = true;
        }

        public void Terminate()
        {
            _doLidUnlockSetpoint.Value = false;
            _doLidLockSetpoint.Value = false;
        }
    }
}
