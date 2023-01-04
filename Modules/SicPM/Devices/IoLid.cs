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
    public enum LidState
    {
        Tighten = 0,
        Loosen = 1,
        Unknown = 2,
        Closed=3,
        Error = 4,
    }
    public class AITLidOperation
    {
        public const string LidLoosen = "Loosen";
        public const string LidTighten = "Tighten";
    }
    public partial class IoLid : BaseDevice, IDevice
    {
        public int SetPoint
        {
            get
            {
                if (_doLoosenSetpoint.Value && _doTightenSetpoint.Value) return (int)LidState.Error;
                if (_doLoosenSetpoint.Value && !_doTightenSetpoint.Value) return (int)LidState.Loosen;
                if (!_doLoosenSetpoint.Value && _doTightenSetpoint.Value) return (int)LidState.Tighten;
                if (!_doLoosenSetpoint.Value && !_doTightenSetpoint.Value) return (int)LidState.Unknown;

                return (int)LidState.Unknown;
            }
        }
        public int Status
        {
            get
            {
                if (_diLoosenFaceback.Value &&( _diTightenFaceback.Value|| _diClosedFaceback.Value))
                    return (int)LidState.Error;
                if (_diLoosenFaceback.Value  && !_diTightenFaceback.Value )
                    return (int)LidState.Loosen;
                if (!_diLoosenFaceback.Value  && _diTightenFaceback.Value )
                    return (int)LidState.Tighten;
                if (!_diLoosenFaceback.Value && !_diTightenFaceback.Value)
                    return (int)LidState.Unknown;
                return (int)LidState.Unknown;
            }
        }

        private DIAccessor _diLoosenFaceback = null;
        private DIAccessor _diTightenFaceback = null;
        private DIAccessor _diClosedFaceback = null;

        private DOAccessor _doLoosenSetpoint = null;
        private DOAccessor _doTightenSetpoint = null;
       
        private LidState _operation;
        private SCConfigItem _scTimeout;
        private DeviceTimer _timer = new DeviceTimer();
        private R_TRIG _trigReset = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigTimer = new R_TRIG();


        #region DI
        public bool LoosenFaceback
        {
            get
            {
                if (_diLoosenFaceback != null )
                    return _diLoosenFaceback.Value;

                return false;
            }
        }

        public bool TightenFaceback
        {
            get
            {
                if (_diTightenFaceback != null)
                    return _diTightenFaceback.Value ;

                return false;
            }
        }

        public bool ClosedFaceback
        {
            get
            {
                if (_diClosedFaceback != null)
                    return _diClosedFaceback.Value;

                return false;
            }
        }
        #endregion

        #region DO
        public bool LoosenSetpoint
        {
            get
            {
                if (_doLoosenSetpoint != null)
                    return _doLoosenSetpoint.Value;

                return false;
            }
        }

        public bool TightenSetpoint
        {
            get
            {
                if (_doTightenSetpoint != null)
                    return _doTightenSetpoint.Value;

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
                //data.AttrValue["Status"] = Status;
                data.AttrValue["LoosenFaceback"] = _diLoosenFaceback.Value;
                data.AttrValue["TightenFaceback"] = _diTightenFaceback.Value;
                data.AttrValue["ClosedFaceback"] = _diClosedFaceback.Value;
                data.AttrValue["LoosenSetpoint"] = _doLoosenSetpoint.Value;
                data.AttrValue["TightenSetpoint"] = _doTightenSetpoint.Value;
                return data;
            }
        }
        public IoLid(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");
            
            _diLoosenFaceback = ParseDiNode("diLoosenFaceback", node, ioModule);
            _diTightenFaceback = ParseDiNode("diTightenFaceback", node, ioModule);
            _diClosedFaceback = ParseDiNode("diClosedFaceback", node, ioModule);
            _doLoosenSetpoint = ParseDoNode("doLoosenSetpoint", node, ioModule);
            _doTightenSetpoint = ParseDoNode("doTightenSetpoint", node, ioModule);

            _scTimeout= ParseScNode("LidTimeout", node, "PM", "PM.LidMotionTimeout");
        }
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.Status", () =>((LidState)Status).ToString());
            DATA.Subscribe($"{Module}.{Name}.LoosenFaceback", () => LoosenFaceback);
            DATA.Subscribe($"{Module}.{Name}.TightenFaceback", () => TightenFaceback);
            DATA.Subscribe($"{Module}.{Name}.ClosedFaceback", () => ClosedFaceback);
            DATA.Subscribe($"{Module}.{Name}.LoosenSetpoint", () => LoosenSetpoint);
            DATA.Subscribe($"{Module}.{Name}.TightenSetpoint", () => LoosenSetpoint);

            OP.Subscribe($"{Module}.{Name}.Loosen", (function, args) =>
            {
                bool ret = SetLid(true, out reason);
                if (ret)
                {
                    reason = string.Format("Open Lid {0}", Name);
                    return true;
                }
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Tighten", (function, args) =>
            {
                bool ret = SetLid(false, out reason);
                if (ret)
                {
                    reason = string.Format("Close Lid {0}", Name);
                    return true;
                }
                return true;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, AITLidOperation.LidLoosen), (out string reason, int time, object[] param) =>
            {
                bool ret = SetLid(true, out reason);
                if (ret)
                {
                    reason = string.Format("Open Lid {0}", Name);
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, AITLidOperation.LidTighten), (out string reason, int time, object[] param) =>
            {
                bool ret = SetLid(false, out reason);
                if (ret)
                {
                    reason = string.Format("Close Lid {0}", Name);
                    return true;
                }

                return false;
            });
            return false;
        }
        string reason = string.Empty;

        public bool SetLid(bool isLoosen, out string reason)
        {
            if ((isLoosen && Status== (int)LidState.Loosen))
            {
                _doTightenSetpoint.SetValue(!isLoosen, out reason);
                reason = "";
                return true;
            }

            if ((!isLoosen && Status == (int)LidState.Tighten))
            {
                _doLoosenSetpoint.SetValue(isLoosen, out reason);
                reason = "";
                return true;
            }


            if (!_doLoosenSetpoint.Check(isLoosen, out reason))
            {
                EV.PostMessage(Module, EventEnum.DefaultWarning, reason);
                return false;
            }
            if (!_doTightenSetpoint.Check(!isLoosen, out reason))
            {
                EV.PostMessage(Module, EventEnum.DefaultWarning, reason);
                return false;
            }


            if (!_doLoosenSetpoint.SetValue(isLoosen, out reason))
                return false;
            if (!_doTightenSetpoint.SetValue(!isLoosen, out reason))
                return false;

            _operation = isLoosen ? LidState.Loosen : LidState.Tighten;
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
                    _doLoosenSetpoint.Value = false;
                    _doTightenSetpoint.Value = false;
                }


                if (_timer.IsTimeout())
                {
                    _timer.Stop();
                    //_trigReset.CLK = Status != (int)_operation;   // fire event only check at first, SetPoint set by interlock
                    //if (_trigReset.Q)
                    //{

                    if (Status != (int)_operation)
                    {
                        if (_operation == LidState.Loosen)
                        {
                            string reason;
                            if (!_doLoosenSetpoint.Check(true, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason：{2}", Display, "Close", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason {2}", Display, "Close", "PLC kept"));
                        }
                        else
                        {
                            string reason;
                            if (!_doLoosenSetpoint.Check(false, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason：{2}", Display, "Open", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason {2}", Display, "Open", "PLC Kept"));
                        }
                        _operation = (LidState)SetPoint;
                    }
                    
                }
                else if (_timer.IsIdle())
                {
                   
                }

                //_trigError.CLK = Status == (int)LidState.Error;
                //if (_trigError.Q)
                //{
                //    EV.PostMessage(Module, EventEnum.DefaultWarning, "Lid in error status,loosen and tighten are both Off");
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
            _doLoosenSetpoint.Value = false;
            _doTightenSetpoint.Value = false;
        }
    }
}
