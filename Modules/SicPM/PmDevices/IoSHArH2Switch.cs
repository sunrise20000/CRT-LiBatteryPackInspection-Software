using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static SicPM.PmDevices.DicMode;


namespace SicPM.Devices
{
    public class IoSHArH2Switch : BaseDevice, IDevice
    {
        private IValve _valve1;
        private IValve _valve2;
        private IValve _valve3;
        private IValve _valve4;
        private IValve _valve5;
        private IValve _valve6;

        private int _openValveInfo = 1;
        private DeviceTimer _timer = new DeviceTimer();

        public IoSHArH2Switch(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _valve1 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve1")}") as IValve;
            _valve2 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve2")}") as IValve;
            _valve3 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve3")}") as IValve;
            _valve4 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve4")}") as IValve;
            _valve5 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve5")}") as IValve;
            _valve6 = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve6")}") as IValve;

            System.Diagnostics.Debug.Assert(_valve1 != null, $"Valve setting not valid");
            System.Diagnostics.Debug.Assert(_valve2 != null, $"Valve setting not valid");
            System.Diagnostics.Debug.Assert(_valve3 != null, $"Valve setting not valid");
            System.Diagnostics.Debug.Assert(_valve4 != null, $"Valve setting not valid");
            System.Diagnostics.Debug.Assert(_valve5 != null, $"Valve setting not valid");
            System.Diagnostics.Debug.Assert(_valve6 != null, $"Valve setting not valid");
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

        public bool SetValve(out string reason, string arH2Switch)
        {
            reason = string.Empty;
            ArH2Switch gas = (ArH2Switch)Enum.Parse(typeof(ArH2Switch), arH2Switch, true);

            switch (gas)
            {
                case ArH2Switch.Ar:
                    if (!_valve1.TurnValve(false, out reason))
                        return false;
                    if (!_valve2.TurnValve(true, out reason))
                        return false;
                    if (!_valve3.TurnValve(true, out reason))
                        return false;
                    if (!_valve4.TurnValve(true, out reason))
                        return false;
                    if (!_valve5.TurnValve(false, out reason))
                        return false;
                    //if (!_valve6.TurnValve(true, out reason)) //放到定时器延迟处理，防止InterLock
                    //    return false;

                    _openValveInfo = 2;
                    break;
                case ArH2Switch.H2:
                    if (!_valve1.TurnValve(true, out reason))
                        return false;
                    if (!_valve2.TurnValve(false, out reason))
                        return false;
                    if (!_valve3.TurnValve(true, out reason))
                        return false;
                    if (!_valve4.TurnValve(true, out reason))
                        return false;
                    //if (!_valve5.TurnValve(true, out reason))//放到定时器延迟处理，防止InterLock
                    //    return false;
                    if (!_valve6.TurnValve(false, out reason))
                        return false;

                    _openValveInfo = 1;
                    break;
                default:
                    break;
            }


            _timer.Start(600);
            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            try
            {
                if (_timer.IsTimeout())
                {
                    _timer.Stop();
                    if (_openValveInfo == 1)
                    {
                        _valve5.TurnValve(true, out string reason);
                    }
                    else if (_openValveInfo == 2)
                    {
                        _valve6.TurnValve(true, out string reason);
                    }

                }
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

