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
    public enum GasConnectorState
    {
        Tighten = 0,
        Loosen = 1,
        Unknown = 2,
        Error = 3,
    }
    public class AITGasConnectorOperation
    {
        public const string GasConnectorLoosen = "Loosen";
        public const string GasConnectorTighten = "Tighten";
    }
    public partial class IoGasConnector : BaseDevice, IDevice
    {
        public int SetPoint
        {
            get
            {
                if (_doGasConnectorLoosen.Value && _doGasConnectorTighten.Value) return (int)GasConnectorState.Error;
                if (_doGasConnectorLoosen.Value && !_doGasConnectorTighten.Value) return (int)GasConnectorState.Loosen;
                if (!_doGasConnectorLoosen.Value && _doGasConnectorTighten.Value) return (int)GasConnectorState.Tighten;
                if (!_doGasConnectorLoosen.Value && !_doGasConnectorTighten.Value) return (int)GasConnectorState.Unknown;

                return (int)GasConnectorState.Unknown;
            }
        }
        public int Status
        {
            get
            {
                if (_diGasConnectorLoosen.Value && _diGasConnectorTighten.Value) return (int)GasConnectorState.Error;
                if (_diGasConnectorLoosen.Value && !_diGasConnectorTighten.Value) return (int)GasConnectorState.Loosen;
                if (!_diGasConnectorLoosen.Value && _diGasConnectorTighten.Value) return (int)GasConnectorState.Tighten;
                if (!_diGasConnectorLoosen.Value && !_diGasConnectorTighten.Value) return (int)GasConnectorState.Unknown;

                return (int)GasConnectorState.Unknown;
            }
        }
        private DIAccessor _diGasConnectorLoosen = null;
        private DIAccessor _diGasConnectorTighten = null;
        private DOAccessor _doGasConnectorLoosen = null;
        private DOAccessor _doGasConnectorTighten = null;

        private GasConnectorState _operation;
        private DeviceTimer _timer = new DeviceTimer();
        private R_TRIG _trigReset = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigTimer = new R_TRIG();

        #region DI
        public bool GasConnectorLoosenFeedback
        {
            get
            {
                if (_diGasConnectorLoosen != null)
                    return _diGasConnectorLoosen.Value;

                return false;
            }
        }

        public bool GasConnectorTightenFeedback
        {
            get
            {
                if (_diGasConnectorTighten != null)
                    return _diGasConnectorTighten.Value;

                return false;
            }
        }

        #endregion

        #region DO
        public bool GasConnectorLoosenSetpoint
        {
            get
            {
                if (_doGasConnectorLoosen != null)
                    return _doGasConnectorLoosen.Value;

                return false;
            }
        }

        public bool GasConnectorTightenSetpoint
        {
            get
            {
                if (_doGasConnectorTighten != null)
                    return _doGasConnectorTighten.Value;

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
                data.AttrValue["GasConnectorLoosen"] = _diGasConnectorLoosen.Value;
                data.AttrValue["GasConnectorTighten"] = _diGasConnectorTighten.Value;
                data.AttrValue["GasConnectorLoosen"] = _doGasConnectorLoosen.Value;
                data.AttrValue["GasConnectorTighten"] = _doGasConnectorTighten.Value;
                //data.AttrValue["PV"] = _aiFeedBack.Value;
                return data;
            }
        }

        public IoGasConnector(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diGasConnectorLoosen = ParseDiNode("diGasConnectorLoosen", node, ioModule);
            _diGasConnectorTighten = ParseDiNode("diGasConnectorTighten", node, ioModule);

            _doGasConnectorLoosen = ParseDoNode("doGasConnectorLoosen", node, ioModule);
            _doGasConnectorTighten = ParseDoNode("doGasConnectorTighten", node, ioModule);


        }
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.GasConnectorLoosenSetpoint", () => GasConnectorLoosenSetpoint);
            DATA.Subscribe($"{Module}.{Name}.GasConnectorTightenSetpoint", () => GasConnectorTightenSetpoint);
            DATA.Subscribe($"{Module}.{Name}.GasConnectorLoosenFeedback", () => GasConnectorLoosenFeedback);
            DATA.Subscribe($"{Module}.{Name}.GasConnectorTightenFeedback", () => GasConnectorTightenFeedback);

            OP.Subscribe($"{Module}.{Name}.SetGasConnectorLoosen", (function, args) =>
            {
                SetGasConnector(false, out string reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetGasConnectorTighten", (function, args) =>
            {
                SetGasConnector(true,out string reason);
                return true;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, AITGasConnectorOperation.GasConnectorLoosen), (out string reason, int time, object[] param) =>
            {
                bool ret = SetGasConnector(false, out reason);
                if (ret)
                {
                    reason = string.Format("Open Lid {0}", Name);
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, AITGasConnectorOperation.GasConnectorTighten), (out string reason, int time, object[] param) =>
            {
                bool ret = SetGasConnector(true, out reason);
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

        public bool SetGasConnector(bool isTigen, out string reason)
        {
            if ((Status == (int)GasConnectorState.Loosen && !isTigen) || (Status == (int)GasConnectorState.Tighten && isTigen))
            {
                reason = "";
                return true;
            }

            if (!_doGasConnectorLoosen.Check(!isTigen, out reason))
            {
                EV.PostMessage(Module, EventEnum.DefaultWarning, reason);
                return false;
            }
            if (!_doGasConnectorTighten.Check(isTigen, out reason))
            {
                EV.PostMessage(Module, EventEnum.DefaultWarning, reason);
                return false;
            }

            if (!_doGasConnectorLoosen.SetValue(!isTigen, out reason))
                return false;
            if (!_doGasConnectorTighten.SetValue(isTigen, out reason))
                return false;

            _operation = isTigen ?  GasConnectorState.Tighten: GasConnectorState.Loosen;
            _timer.Start(1000 * 5);
            return true;
        }
        public void Monitor()
        {
            try
            {
                _trigTimer.CLK = _timer.IsIdle();
                if (_trigTimer.Q)
                {
                    _doGasConnectorTighten.Value = false;
                    _doGasConnectorLoosen.Value = false;
                }

                if (_timer.IsTimeout())
                {
                    _timer.Stop();
                    if (Status != (int)_operation)
                    {
                        if (_operation == GasConnectorState.Loosen)
                        {
                            string reason;
                            if (!_doGasConnectorLoosen.Check(true, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason：{2}", Display, "Close", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason {2}", Display, "Close", "PLC kept"));
                        }
                        else
                        {
                            string reason;
                            if (!_doGasConnectorLoosen.Check(false, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason：{2}", Display, "Open", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason {2}", Display, "Open", "PLC Kept"));
                        }
                        _operation = (GasConnectorState)SetPoint;
                    }
                    
                }
                else if (_timer.IsIdle())
                {
                    
                }
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
            _doGasConnectorTighten.Value = false;
            _doGasConnectorLoosen.Value = false;
        }
    }
}
