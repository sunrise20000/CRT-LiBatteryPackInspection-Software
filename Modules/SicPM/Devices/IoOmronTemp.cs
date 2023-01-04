using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using System;
using System.Xml;

namespace SicPM.Devices
{
    public class IoOmronTemp : BaseDevice, IDevice
    {
        public float CH1_TMATemp
        {
            get
            {
                return _aoCH1 == null ? 0 : (_isFloatAioType ? _aoCH1.FloatValue : _aoCH1.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH1.FloatValue = value;
                }
                else
                {
                    _aoCH1.Value = (short)value;
                }
            }
        }

        public float CH2_TCSTemp
        {
            get
            {
                return _aoCH2 == null ? 0 : (_isFloatAioType ? _aoCH2.FloatValue : _aoCH2.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH2.FloatValue = value;
                }
                else
                {
                    _aoCH2.Value = (short)value;
                }
            }
        }

        public float CH3_PC6Temp
        {
            get
            {
                return _aoCH3 == null ? 0 : (_isFloatAioType ? _aoCH3.FloatValue : _aoCH3.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH3.FloatValue = value;
                }
                else
                {
                    _aoCH3.Value = (short)value;
                }
            }
        }

        public float CH4_PC6Temp
        {
            get
            {
                return _aoCH4 == null ? 0 : (_isFloatAioType ? _aoCH4.FloatValue : _aoCH4.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH4.FloatValue = value;
                }
                else
                {
                    _aoCH4.Value = (short)value;
                }
            }
        }

        public float CH5_InnerPanelTemp
        {
            get
            {
                return _aoCH5 == null ? 0 : (_isFloatAioType ? _aoCH5.FloatValue : _aoCH5.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH5.FloatValue = value;
                }
                else
                {
                    _aoCH5.Value = (short)value;
                }
            }
        }

        public float CH6_OutMidPanelTemp
        {
            get
            {
                return _aoCH6 == null ? 0 : (_isFloatAioType ? _aoCH6.FloatValue : _aoCH6.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH6.FloatValue = value;
                }
                else
                {
                    _aoCH6.Value = (short)value;
                }
            }
        }

        public float CH7_ShowerOuterTemp
        {
            get
            {
                return _aoCH7 == null ? 0 : (_isFloatAioType ? _aoCH7.FloatValue : _aoCH7.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH7.FloatValue = value;
                }
                else
                {
                    _aoCH7.Value = (short)value;
                }
            }
        }

        public float CH8_ShowerMidTemp
        {
            get
            {
                return _aoCH8 == null ? 0 : (_isFloatAioType ? _aoCH8.FloatValue : _aoCH8.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH8.FloatValue = value;
                }
                else
                {
                    _aoCH8.Value = (short)value;
                }
            }
        }

        public float CH9_DptPanelTemp
        {
            get
            {
                return _aoCH9 == null ? 0 : (_isFloatAioType ? _aoCH9.FloatValue : _aoCH9.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH9.FloatValue = value;
                }
                else
                {
                    _aoCH9.Value = (short)value;
                }
            }
        }

        public float CH10_LeakSourceTemp
        {
            get
            {
                return _aoCH10 == null ? 0 : (_isFloatAioType ? _aoCH10.FloatValue : _aoCH10.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH10.FloatValue = value;
                }
                else
                {
                    _aoCH10.Value = (short)value;
                }
            }
        }

        public float CH11_LeakBypassTemp
        {
            get
            {
                return _aoCH11 == null ? 0 : (_isFloatAioType ? _aoCH11.FloatValue : _aoCH11.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH11.FloatValue = value;
                }
                else
                {
                    _aoCH11.Value = (short)value;
                }
            }
        }

        public float CH12_DptGateTemp
        {
            get
            {
                return _aoCH12 == null ? 0 : (_isFloatAioType ? _aoCH12.FloatValue : _aoCH12.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoCH12.FloatValue = value;
                }
                else
                {
                    _aoCH12.Value = (short)value;
                }
            }
        }

        public bool Enable
        {
            get
            {
                return _doLineHeaterEnable == null ? false : _doLineHeaterEnable.Value;
                //return true;
            }
            set
            {
                _doLineHeaterEnable.Value = value;
            }
        }

        private AOAccessor _aoCH1 = null;
        private AOAccessor _aoCH2 = null;
        private AOAccessor _aoCH3 = null;
        private AOAccessor _aoCH4 = null;
        private AOAccessor _aoCH5 = null;
        private AOAccessor _aoCH6 = null;
        private AOAccessor _aoCH7 = null;
        private AOAccessor _aoCH8 = null;
        private AOAccessor _aoCH9 = null;
        private AOAccessor _aoCH10 = null;
        private AOAccessor _aoCH11 = null;
        private AOAccessor _aoCH12 = null;
        private DOAccessor _doLineHeaterEnable = null; //温控器使能（通断电）
        

        private bool _isFloatAioType = false;

        private string reason = string.Empty;

        private DeviceTimer _timer = new DeviceTimer();

        public IoOmronTemp(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _aoCH1 = ParseAoNode("aoCH1", node, ioModule);
            _aoCH2 = ParseAoNode("aoCH2", node, ioModule);
            _aoCH3 = ParseAoNode("aoCH3", node, ioModule);
            _aoCH4 = ParseAoNode("aoCH4", node, ioModule);
            _aoCH5 = ParseAoNode("aoCH5", node, ioModule);
            _aoCH6 = ParseAoNode("aoCH6", node, ioModule);
            _aoCH7 = ParseAoNode("aoCH7", node, ioModule);
            _aoCH8 = ParseAoNode("aoCH8", node, ioModule);
            _aoCH9 = ParseAoNode("aoCH9", node, ioModule);
            _aoCH10 = ParseAoNode("aoCH10", node, ioModule);
            _aoCH11 = ParseAoNode("aoCH11", node, ioModule);
            _aoCH12 = ParseAoNode("aoCH12", node, ioModule);
            _doLineHeaterEnable = ParseDoNode("doLineHeaterEnable", node, ioModule);


            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");
        }

        
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.Enable", () => Enable);
            OP.Subscribe($"{Module}.{Name}.SetEnable", (function, args) =>
            {
                bool isTrue = Convert.ToBoolean(args[0]);
                SetEnable(isTrue, out reason);
                return true;
            });

            return true;
        }


        

        public void Terminate()
        {
        }

        public void Monitor()
        {
            MonitorActualTemp();
        }

        private void MonitorActualTemp()
        {
            if (omronTemp != null && omronTemp.Length > 11)
            {
                CH1_TMATemp = omronTemp[0];
                CH2_TCSTemp = omronTemp[1];
                CH3_PC6Temp = omronTemp[2];
                CH4_PC6Temp = omronTemp[3];
                CH5_InnerPanelTemp = omronTemp[4];
                CH6_OutMidPanelTemp = omronTemp[5];
                CH7_ShowerOuterTemp = omronTemp[6];
                CH8_ShowerMidTemp = omronTemp[7];
                CH9_DptPanelTemp = omronTemp[8];
                CH10_LeakSourceTemp = omronTemp[9];
                CH11_LeakBypassTemp = omronTemp[10];
                CH12_DptGateTemp = omronTemp[11];

            }

        }

        public void Reset()
        {

        }

        public float[] omronTemp
        {
            get
            {
                return (float[])DATA.Poll($"{Module}.TempOmron.ActualTemp");
            }

        }

        public bool SetEnable(bool setValue, out string reason)
        {
            reason = "";
            //if (FuncCheckInterLock != null)
            //{
            //    if (!FuncCheckInterLock(setValue))
            //    {
            //        EV.PostInfoLog(Module, $"Set PSU Enable fialed for Interlock!");
            //        return false;
            //    }
            //}

            if (!_doLineHeaterEnable.Check(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doLineHeaterEnable.SetValue(setValue, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            return true;
        }

    }
}
