using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    public class IoSingalLight : SignalLightBase
    {
        public LightType Type { get; set; }

        private F_TRIG _trigError = new F_TRIG();

        public DOAccessor LightDO { get; set; }

        private DeviceTimer BlinkingDT = new DeviceTimer();

        public IoSingalLight(string module, string name) : base(module, name)
        {
        }

        protected override void SetOn()
        {
            SetLight(TowerLightStatus.On);
        }

        protected override void SetOff()
        {
            SetLight(TowerLightStatus.Off);
        }

        protected override void SetBlinking(bool token)
        {
            SetLight(TowerLightStatus.Blinking);
        }

        private void SetLight(TowerLightStatus setpoint)
        {
            try
            {
                if (setpoint == TowerLightStatus.Blinking)
                {
                    if (BlinkingDT.GetElapseTime() < 500)
                    {
                        LightDO.SetValue(true, out _);
                    }
                    else if (BlinkingDT.GetElapseTime() < 1500)
                    {
                        LightDO.SetValue(false, out _);
                    }
                    else
                    {
                        BlinkingDT.Start(0);
                    }
                    return;
                }

                _trigError.CLK = LightDO.SetValue(setpoint == TowerLightStatus.On, out string reason);
                if (_trigError.Q)
                {
                    EV.PostWarningLog(Module, $"Set {Type} signal light {setpoint} error, {reason}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public override void Reset()
        {
            _trigError.RST = true;
            base.Reset();
        }
    }

    public class IoSignalTower : SignalTowerBase
    {
        private Dictionary<LightType, DOAccessor> _lightDoDic;
        public IoSignalTower(string module, XmlElement node, string ioModule = "") : base(module, module)
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _lightDoDic = new Dictionary<LightType, DOAccessor>();
            DOAccessor doRed = ParseDoNode("doRed", node, ioModule);
            if (doRed != null)
            {
                _lightDoDic.Add(LightType.Red, doRed);
            }

            DOAccessor doYellow = ParseDoNode("doYellow", node, ioModule);
            if (doYellow != null)
            {
                _lightDoDic.Add(LightType.Yellow, doYellow);
            }

            DOAccessor doGreen = ParseDoNode("doGreen", node, ioModule);
            if (doGreen != null)
            {
                _lightDoDic.Add(LightType.Green, doGreen);
            }

            DOAccessor doBlue = ParseDoNode("doBlue", node, ioModule);
            if (doBlue != null)
            {
                _lightDoDic.Add(LightType.Blue, doBlue);
            }

            DOAccessor doWhite = ParseDoNode("doWhite", node, ioModule);
            if (doWhite != null)
            {
                _lightDoDic.Add(LightType.White, doWhite);
            }

            DOAccessor doBuzzer = ParseDoNode("doBuzzer", node, ioModule);
            if (doBuzzer != null)
            {
                _lightDoDic.Add(LightType.Buzzer, doBuzzer);
            }

            DOAccessor doBuzzer1 = ParseDoNode("doBuzzer1", node, ioModule);
            if (doBuzzer1 != null)
            {
                _lightDoDic.Add(LightType.Buzzer1, doBuzzer1);
            }

            DOAccessor doBuzzer2 = ParseDoNode("doBuzzer2", node, ioModule);
            if (doBuzzer2 != null)
            {
                _lightDoDic.Add(LightType.Buzzer2, doBuzzer2);
            }
        }

        public override bool Initialize()
        {
            return base.Initialize();
        }

        public override SignalLightBase CreateLight(LightType type)
        {
            if (!_lightDoDic.ContainsKey(type))
            {
                return null;
            }
            return new IoSingalLight(ModuleName.System.ToString(), $"SignalLight{type}") { Type = type, LightDO = _lightDoDic[type] };
        }
        public IoSingalLight CreateLight(LightType type, TowerLightStatus setpoint)
        {
            if (!_lightDoDic.ContainsKey(type))
            {
                return null;
            }
            IoSingalLight slb = new IoSingalLight(ModuleName.System.ToString(), $"SignalLight{type}") { Type = type, LightDO = _lightDoDic[type] };
            slb.StateSetPoint = setpoint;
            //
            return slb;
        }

        public void SwitchOffBuzzerEx(int iCount, int iInterval = 500, int iRemainTime = 1000)
        {
            SwitchOffBuzzer(true);
            for (int i = 0; i < iCount; i++)
            {
                SwitchOffBuzzer(false);
                System.Threading.Thread.Sleep(iRemainTime);
                SwitchOffBuzzer(true);
                System.Threading.Thread.Sleep(iInterval);
            }
        }
    }
}
