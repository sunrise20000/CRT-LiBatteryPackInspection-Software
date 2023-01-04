using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.Common.Event;

namespace SicPM.Devices
{
    public partial class IoPTOffsetAndK : BaseDevice, IDevice, IPressureMeter
    {        
        public double PTOffset
        {
            get
            {
                return _aoPTOffset == null ? 0 : (_isFloatAioType ? _aoPTOffset.FloatValue : _aoPTOffset.Value);                
            }
            set
            {
                if (_aoPTOffset != null)
                {
                    if (_isFloatAioType)
                        _aoPTOffset.FloatValue = (float)value;
                    else
                        _aoPTOffset.Value = (short)value;
                }
            }
        }

        public double PTK
        {
            get
            {
                return _aoPTK == null ? 0 : (_isFloatAioType ? _aoPTK.FloatValue : _aoPTK.Value);
            }
            set
            {
                if (_aoPTK != null)
                {
                    if (_isFloatAioType)
                        _aoPTK.FloatValue = (float)value;
                    else
                        _aoPTK.Value = (short)value;
                }
            }
        }


        public string Unit { get; set; }

        private AOAccessor _aoPTOffset = null;
        private AOAccessor _aoPTK= null;        
        private bool _isFloatAioType = false;

        private string _offsetName;
        private string _kName;
        
        public IoPTOffsetAndK(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");
            Unit = node.GetAttribute("unit");
            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _aoPTOffset = ParseAoNode("aoOffset", node, ioModule);
            _aoPTK = ParseAoNode("aoK", node, ioModule);

            _offsetName = Display + "_Offset";
            _kName = Display + "_K";
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Display}.PTOffset", () => PTOffset);
            DATA.Subscribe($"{Module}.{Display}.PTK", () => PTK);

            //OP.Subscribe($"{Module}.{Name}.GetSCPTOffset", (function, args) =>
            //{                
            //    GetSCPTOffset();
            //    return true;
            //});
            OP.Subscribe($"{Module}.{Display}.SetPTOffset", (function, args) =>
            {
                SetPTOffset();
                return true;            
            });

            OP.Subscribe($"{Module}.{Display}.SetPTK", (function, args) =>
            {
                SetPTK();
                return true;
            });

            return true;
        }

        //public void GetSCPTOffset()
        //{
        //    try
        //    {
        //        PTOffset = SC.GetValue<double>($"PM.{Module}.PT.{Name}");
        //    }
        //    catch (Exception ex)
        //    {
        //        EV.PostAlarmLog(Module, $"Get PM..{Module}.PT.{Name} Error:" + ex.Message);
        //    }
        //}

        public void SetPTOffset()
        {
            try
            {
                PTOffset = SC.GetValue<double>($"PM.{Module}.PT.{Display}.{_offsetName}");

                EV.PostInfoLog(Module, $"Set PM..{Module}.PT.{Display}.{_offsetName} OK.");
            }
            catch(Exception ex)
            {
                EV.PostAlarmLog(Module, $"Set PM..{Module}.PT.{Display}.{_offsetName} Error:"+ex.Message);
            }
        }

        public void SetPTK()
        {
            try
            {
                PTK = SC.GetValue<double>($"PM.{Module}.PT.{Display}.{_kName}");

                EV.PostInfoLog(Module, $"Set PM..{Module}.PT.{Display}.{_kName} OK.");
            }
            catch (Exception ex)
            {
                EV.PostAlarmLog(Module, $"Set PM..{Module}.PT.{Display}.{_kName} Error:" + ex.Message);
            }
        }


        public void Terminate()
        {
            
        }
        public void Monitor()
        {
           
        }
        public void Reset()
        {

        }
    }
}
