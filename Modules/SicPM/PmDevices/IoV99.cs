using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static SicPM.PmDevices.DicMode;

namespace SicPM.Devices
{
    public class IoV99 : BaseDevice, IDevice
    {
        private IValve _valve1;
        private IValve _valve2;
        //private IValve _valve3;
        //private IValve _valve4;
        //private IValve _valve5;
        //private IValve _valve6;

        public IoV99(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _valve1 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve1")}") as IValve;
            _valve2 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve2")}") as IValve;
            //_valve3 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve3")}") as IValve;
            //_valve4 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve4")}") as IValve;
            //_valve5 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve5")}") as IValve;
            //_valve6 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve6")}") as IValve;

            System.Diagnostics.Debug.Assert(_valve1 != null, $"Valve setting not valid");
            System.Diagnostics.Debug.Assert(_valve2 != null, $"Valve setting not valid");
            //System.Diagnostics.Debug.Assert(_valve3 != null, $"Valve setting not valid");
            //System.Diagnostics.Debug.Assert(_valve4 != null, $"Valve setting not valid");
            //System.Diagnostics.Debug.Assert(_valve5 != null, $"Valve setting not valid");
            //System.Diagnostics.Debug.Assert(_valve6 != null, $"Valve setting not valid");
        }

        public bool Initialize()
        {
            OP.Subscribe($"{Module}.{Name}.SetValve", SetValve);

            return true;
        }

        private bool SetValve(out string reason, int time, object[] param)
        {
            return SetValve(out reason, param[0].ToString());
        }

        public bool SetValve(out string reason, string flowMode)
        {
            reason = string.Empty;
            FlowMode mode = (FlowMode)Enum.Parse(typeof(FlowMode), flowMode, true);

            switch (mode)
            {
                case FlowMode.Purge:
                    if (!_valve1.TurnValve(false, out reason))
                        return false;
                    if (!_valve2.TurnValve(true, out reason))
                        return false;
                    //if (!_valve3.TurnValve(true, out reason))
                    //    return false;
                    //if (!_valve4.TurnValve(true, out reason))
                    //    return false;   
                    //if (!_valve5.TurnValve(false, out reason))
                    //    return false;
                    //if (!_valve6.TurnValve(true, out reason))
                    //    return false;
                    break;
                case FlowMode.Vent:
                    if (!_valve1.TurnValve(true, out reason))
                        return false;
                    if (!_valve2.TurnValve(false, out reason))
                        return false;
                    //if (!_valve3.TurnValve(true, out reason))
                    //    return false;
                    //if (!_valve4.TurnValve(true, out reason))
                    //    return false;
                    //if (!_valve5.TurnValve(true, out reason))
                    //    return false;
                    //if (!_valve6.TurnValve(false, out reason))
                    //    return false;
                    break;
                default:
                    break;
            }

            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            try
            {

            }
            catch (Exception ex)
            {

                LOG.Write(ex);
            }

        }

        public void Reset()
        {

        }
    }
}
