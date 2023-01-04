using System;
using System.Linq;
using System.Xml;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.RT.Core.IoProviders
{
    public class VirtualIoProvider : IoProvider
    {
        private bool[] _innerDi = new bool[128];
        private bool[] _innerDo = new bool[128];
        private float[] _innerAo = new float[128];
        private float[] _innerAi = new float[128];
        
        private readonly R_TRIG _trigConnected = new R_TRIG();

        protected override void SetParameter(XmlElement nodeParameter)
        {

        }

        protected override void Open()
        {

        }

        protected override void Close()
        {

        }
        
        private bool MonitorAdsConnection()
        {
            return true;
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

            return _innerDi.ToList().GetRange(offset, size).ToArray();
        }

        private bool[] ReadDo(int offset, int size)
        {
            if (!_trigConnected.M)
                return null;

            return _innerDo.ToList().GetRange(offset, size).ToArray();
        }

        protected override float[] ReadAiFloat(int offset, int size)
        {
            if (!_trigConnected.M)
                return null;

            return _innerAi.ToList().GetRange(offset, size).ToArray();
        }

        private float[] ReadAoFloat(int offset, int size)
        {
            if (!_trigConnected.M)
                return null;

            return _innerAo.ToList().GetRange(offset, size).ToArray();
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

            _innerAo[aoItem.Index] = value;
            return true;
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

            _innerDo[doItem.Index] = value;
            return true;
        }

    }
}

