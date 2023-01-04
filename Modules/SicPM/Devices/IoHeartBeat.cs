using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.Devices
{
    class IoHeartBeat : BaseDevice, IDevice
    {
        public float Feedback
        {
            get
            {
                if (_ai != null)
                {
                    return _isFloatAioType ? _ai.FloatValue : _ai.Value;
                }

                return 0;
            }
        }

        public float SetPoint
        {
            get
            {
                if (_ao != null)
                {
                    return _isFloatAioType ? _ao.FloatValue : _ao.Value;
                }

                return 0;
            }
            set
            {
                if (_ao != null)
                {
                    if (_isFloatAioType)
                        _ao.FloatValue = value;
                    else
                        _ao.Value = (short)value;
                }
            }
        }

        //IO
        private AIAccessor _ai = null;
        private AOAccessor _ao = null;

        private bool _isFloatAioType = false;

        private short _counter = 0;
        private bool _isErrorShowed;
        private PeriodicJob _thread;
        private PeriodicJob _check;

        private List<int> _plcValue = new List<int>();

        public IoHeartBeat(string module, XmlElement node, string ioModule = "")
        {

            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _ai = ParseAiNode("ai", node, ioModule);
            _ao = ParseAoNode("ao", node, ioModule);

            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");
        }

        public bool Initialize()
        {
            _thread = new PeriodicJob(1000, OnTimer, "PLC Write Thread", false);
            _check = new PeriodicJob(10000, CheckHeartBeat, "PLC Check Thread", false);

            //防止UI没启动，就进行EV.Post
            Task.Delay(40000).ContinueWith((a) => _thread.Start());
            Task.Delay(60000).ContinueWith((a) => _check.Start());

            return true;
        }

        public bool OnTimer()
        {
            try
            {
                _counter++;

                if (_counter > 1000)
                {
                    _counter = 0;
                }

                SetPoint = _counter;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public bool CheckHeartBeat()
        {
            try
            {
                if (_plcValue.Count <= 1)
                {
                    if (!_isErrorShowed)
                    {
                        EV.PostAlarmLog(Module, $"Alarm:PLC heartbeat error");

                        //if (FireEvent != null)
                        //    FireEvent(this);
                    }
                    _isErrorShowed = true;
                }

                _plcValue.Clear();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            if (!_plcValue.Contains((int)Feedback))
            {
                _plcValue.Add((int)Feedback);
            }
        }

        public void Reset()
        {
            _isErrorShowed = false;
            _check.Stop();
            _check.Start();
        }
    }
}
