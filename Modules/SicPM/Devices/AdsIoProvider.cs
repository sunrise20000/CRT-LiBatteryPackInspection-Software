using System;
using System.Collections.Generic;
using System.Xml;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.IOCore;
using MECF.Framework.Common.PLC;
using MECF.Framework.RT.Core.IoProviders;

namespace SicPM.Devices
{
    public class AdsIoProvider : IoProvider
    {
        private string _diVariable;
        private string _doVariable;
        private string _aiVariable;
        private string _aoVariable;

        private IAdsPlc _ads;

        private R_TRIG _trigConnected = new R_TRIG();

        protected override void SetParameter(XmlElement nodeParameter)
        {
            _diVariable = nodeParameter.GetAttribute("diVariable");
            _doVariable = nodeParameter.GetAttribute("doVariable");
            _aiVariable = nodeParameter.GetAttribute("aiVariable");
            _aoVariable = nodeParameter.GetAttribute("aoVariable");
        }

        protected override void Open()
        {

        }

        protected override void Close()
        {

        }

        public override void Initialize(string module, string name, List<IoBlockItem> lstBuffers, IIoBuffer buffer, XmlElement nodeParameter, string ioMappingPathFile, string ioModule)
        {
            base.Initialize(module, name, lstBuffers, buffer, nodeParameter, ioMappingPathFile, ioModule);
        }

        private bool MonitorAdsConnection()
        {
            if (_ads == null)
            {
                _ads = DEVICE.GetOptionDevice($"{Module}.MainPLC", typeof(WcfPlc)) as IAdsPlc;
            }

            return _ads != null && _ads.CheckIsConnected();
        }

        
        protected override bool OnTimer()
        {
            _trigConnected.CLK = MonitorAdsConnection();

            if (!_trigConnected.M)
                return true;

            try
            {
                foreach (var bufferSection in _blockSections)
                {
                    if (bufferSection.Type == IoType.DI)
                    {
                        bool[] diBuffer = ReadDi(bufferSection.Offset, bufferSection.Size);
                        if (diBuffer != null)
                        {
                            _buffer.SetDiBuffer(_source, bufferSection.Offset, diBuffer);
                        }
                    }
                    else if (bufferSection.Type == IoType.DO)
                    {
                        bool[] doBuffer = ReadDo(bufferSection.Offset, bufferSection.Size);
                        if (doBuffer != null)
                        {
                            _buffer.SetDoBuffer(_source, bufferSection.Offset, doBuffer);
                        }
                    }
                    else if (bufferSection.Type == IoType.AI)
                    {
                        float[] aiBuffer = ReadAiFloat(bufferSection.Offset, bufferSection.Size);
                        if (aiBuffer != null)
                        {
                            _buffer.SetAiBufferFloat(_source, bufferSection.Offset, aiBuffer);
                        }
                    }
                    else if (bufferSection.Type == IoType.AO)
                    {
                        float[] aoBuffer = ReadAoFloat(bufferSection.Offset, bufferSection.Size);
                        if (aoBuffer != null)
                        {
                            _buffer.SetAoBufferFloat(_source, bufferSection.Offset, aoBuffer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }


            return true;
        }

        protected override bool[] ReadDi(int offset, int size)
        {
            if (!_trigConnected.M)
                return null;

            if (_ads.Read(_diVariable, out object data, typeof(bool[]).ToString(), 100, out string _))
                return (bool[])data;
            return null;
        }

        private bool[] ReadDo(int offset, int size)
        {
            if (!_trigConnected.M)
                return null;

            if (_ads.Read(_doVariable, out object data, typeof(bool[]).ToString(), 100, out string _))
                return (bool[])data;
            return null;
        }

        protected override float[] ReadAiFloat(int offset, int size)
        {
            if (!_trigConnected.M)
                return null;

            if (_ads.Read(_aiVariable, out object data, typeof(float[]).ToString(), 100, out string _))
                return (float[])data;
            return null;
        }

        private float[] ReadAoFloat(int offset, int size)
        {
            if (!_trigConnected.M)
                return null;

            if (_ads.Read(_aoVariable, out object data, typeof(float[]).ToString(), 100, out string _))
                return (float[])data;
            return null;
        }

        protected override short[] ReadAi(int offset, int size)
        {
            return null;
        }

        protected short[] ReadAo(int offset, int size)
        {
            return null;
        }

        protected override void WriteDo(int offset, bool[] data)
        {
        }

        protected override void WriteAo(int offset, short[] data)
        {
        }

        public override bool SetValue(AOAccessor aoItem, short value)
        {
            if (!_trigConnected.M)
                return false;

            return true;
        }

        public override bool SetValueFloat(AOAccessor aoItem, float value)
        {
            if (!_trigConnected.M)
                return false;

            return _ads.WriteArrayElement(_aoVariable, aoItem.Index, value, out _);
        }

        public override bool SetValue(DOAccessor doItem, bool value)
        {
            if (!_trigConnected.M)
                return false;

            if (!IO.CanSetDO(doItem.Name, value, out string reason))
            {
                LOG.Write(reason);
                return false;
            }

            return _ads.WriteArrayElement(_doVariable, doItem.Index, value, out _);
        }

    }
}

