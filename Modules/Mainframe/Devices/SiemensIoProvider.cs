using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.IOCore;
using MECF.Framework.Common.PLC;
using MECF.Framework.RT.Core.IoProviders;
using MECF.Framework.RT.Core.IoProviders.Siemens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Mainframe.Devices
{
    public partial class SiemensIoProvider : IoProvider
    {
        private string _diVariable;
        private string _doVariable;
        private string _aiVariable;
        private string _aoVariable;

        private ushort diLength;
        private ushort doLength;
        private ushort aiLength;
        private ushort aoLength;

        private IAdsPlc _ads;
        private static object _locker = new object();

        private R_TRIG _trigConnected = new R_TRIG();
        protected override void SetParameter(XmlElement nodeParameter)
        {
            _diVariable = nodeParameter.GetAttribute("diVariable");
            _doVariable = nodeParameter.GetAttribute("doVariable");
            _aiVariable = nodeParameter.GetAttribute("aiVariable");
            _aoVariable = nodeParameter.GetAttribute("aoVariable");

            ushort.TryParse(nodeParameter.GetAttribute("diLength"), out diLength);
            ushort.TryParse(nodeParameter.GetAttribute("doLength"), out doLength);
            ushort.TryParse(nodeParameter.GetAttribute("aiLength"), out aiLength);
            ushort.TryParse(nodeParameter.GetAttribute("aoLength"), out aoLength);
        }
        private bool MonitorAdsConnection()
        {

            //if (_SiemensClient == null)
            //{
            //    _SiemensClient = DEVICE.GetDevice<PMModule>(Module + "." + Module)?.Siemens;
            //}
            //if (_SiemensClient != null) return _SiemensClient.IsTrue;
            //return false;

            if (_ads == null)
            {
                _ads = DEVICE.GetOptionDevice($"{Module}.MainPLC", typeof(SicAds)) as IAdsPlc;
            }

            return _ads != null && _ads.CheckIsConnected();
        }

        //public bool IsCommunicationError
        //{
        //    get { return _ads.IsCommunicationError; }
        //}
        protected override void Close()
        {

        }
        protected override void Open()
        {

        }
        public override void Initialize(string module, string name, List<IoBlockItem> lstBuffers, IIoBuffer buffer, XmlElement nodeParameter, string ioMappingPathFile, string ioModule)
        {
            base.Initialize(module, name, lstBuffers, buffer, nodeParameter, ioMappingPathFile, ioModule);
        }

        protected override bool OnTimer()
        {
            _trigConnected.CLK = MonitorAdsConnection();

            if (!_trigConnected.M)
                return true;

            lock (_locker)
            {
                // PLC数据以32位二级制存储，软件读取的值与DINT相同
                // 如果是DINT类型数据，直接使用，无需转换；
                // 如果是REAL类型数据，先使用BitConverter.GetBytes获取有4个元素的字节数组，再使用BitConverter.ToSingle，得到float类型值；
                // 如果是DWORD类型数据，先使用BitConverter.GetBytes获取有4个元素的字节数组，再进行位运算，得到每位的值

                //int[] input = ReadInput();
                //Console.WriteLine(input[0]);
                //byte[] byteArr = BitConverter.GetBytes(input[0]);
                //float fValve = BitConverter.ToSingle(byteArr, 0);





                byte[] DI = ReadDIn();
                //if (IsCommunicationError) return CommunicationError();
                if (DI != null)
                {
                    bool[] diBuffer = ParseDI(DI);
                    if (diBuffer != null)
                    {
                        _buffer.SetDiBuffer(_source, 0, diBuffer);
                    }
                }

                byte[] DO = ReadDOut();
                //if (IsCommunicationError) return CommunicationError();
                if (DO != null)
                {
                    bool[] doBuffer = ParseDO(DO);
                    if (doBuffer != null)
                    {
                        _buffer.SetDoBuffer(_source, 0, doBuffer);
                    }
                }

                byte[] AI = ReadAIn();
                //if (IsCommunicationError) return CommunicationError();
                if (AI != null)
                {
                    float[] aiBuffer = ParseAI(AI);
                    if (aiBuffer != null)
                    {
                        _buffer.SetAiBufferFloat(_source, 0, aiBuffer);
                    }
                }

                byte[] AO = ReadAOut();
                //if (IsCommunicationError) return CommunicationError();
                if (AO != null)
                {
                    float[] aoBuffer = ParseAO(AO);
                    if (aoBuffer != null)
                    {
                        _buffer.SetAoBufferFloat(_source, 0, aoBuffer);
                    }
                }

            }

            return true;
        }
        private bool CommunicationError()
        {
            //_buffer.SetDiBuffer(_source, 0, new bool[1000]);
            //_buffer.SetDoBuffer(_source, 0, new bool[1000]);
            //_buffer.SetAiBufferFloat(_source, 0, new float[500]);
            //_buffer.SetAoBufferFloat(_source, 0, new float[500]);

            return true;
        }
        protected byte[] ReadDIn()
        {
            if (!_trigConnected.M)
                return null;

            if (_ads.BulkReadRenderResult(_diVariable, diLength, out byte[] data))
                return data;

            return null;
        }
        private byte[] ReadDOut()
        {
            if (!_trigConnected.M)
                return null;

            if (_ads.BulkReadRenderResult(_doVariable, doLength, out byte[] data))
                return (byte[])data;

            return null;
        }

        protected byte[] ReadAIn()
        {
            if (!_trigConnected.M)
                return null;

            if (_ads.BulkReadRenderResult(_aiVariable, aiLength, out byte[] data))
                return data;

            return null;
        }
        private byte[] ReadAOut()
        {
            if (!_trigConnected.M)
                return null;

            if (_ads.BulkReadRenderResult(_aoVariable, aoLength, out byte[] data))
                return (byte[])data;

            return null;
        }

        private bool[] ParseDI(byte[] inputValue)
        {
            List<DIAccessor> diList = Singleton<IoManager>.Instance.GetDIList($"{Module}.io");

            bool[] result = new bool[1000];
            foreach (var diAccessor in diList)
            {
                var addr = diAccessor.Addr;
                int index = int.Parse(addr);
                int Offset = index % 8;
                int val = index % 8 == 0 ? index / 8 : (index - index % 8) / 8;
                result[diAccessor.Index] = ((inputValue[val] >> Offset) & 0x1) == 1;
            }
            return result;
        }


        private bool[] ParseDO(byte[] outputValue)
        {
            //Addr="OUTPUT[0].4"
            List<DOAccessor> doList = Singleton<IoManager>.Instance.GetDOList($"{Module}.io");

            bool[] result = new bool[1000];
            foreach (var doAccessor in doList)
            {
                var addr = doAccessor.Addr;
                int index = int.Parse(addr);
                int Offset = index % 8;
                int val = index % 8 == 0 ? index / 8 : (index - index % 8) / 8;

                result[doAccessor.Index] = ((outputValue[val] >> Offset) & 0x1) == 1;
            }
            return result;
        }


        private float[] ParseAI(byte[] inputValue)
        {
            List<AIAccessor> aiList = Singleton<IoManager>.Instance.GetAIList($"{Module}.io");
            float[] result = new float[500];
            foreach (var aiAccessor in aiList)
            {
                var addr = aiAccessor.Addr;
                int index = int.Parse(addr);
                float valve = BitConverter.ToSingle(new byte[] { inputValue[index * 4 + 3], inputValue[index * 4 + 2], inputValue[index * 4 + 1], inputValue[index * 4] }, 0);
                result[aiAccessor.Index] = valve;
            }
            return result;
        }


        private float[] ParseAO(byte[] outputValue)
        {
            //Addr="OUTPUT[30]"
            List<AOAccessor> aoList = Singleton<IoManager>.Instance.GetAOList($"{Module}.io");

            float[] result = new float[500];
            foreach (var aoAccessor in aoList)
            {
                var addr = aoAccessor.Addr;
                int index = int.Parse(addr);
                float valve = BitConverter.ToSingle(new byte[] { outputValue[index * 4 + 3], outputValue[index * 4 + 2], outputValue[index * 4 + 1], outputValue[index * 4] }, 0);
                result[aoAccessor.Index] = valve;
                //uint valve = BitConverter.ToUInt32(outputValue, index * 4);
                //result[aoAccessor.Index] = valve;
            }

            return result;
        }
        protected override short[] ReadAi(int offset, int size)
        {
            throw new NotImplementedException();
        }

        protected override bool[] ReadDi(int offset, int size)
        {
            throw new NotImplementedException();
        }



        protected override void WriteAo(int offset, short[] buffer)
        {
            throw new NotImplementedException();
        }

        protected override void WriteDo(int offset, bool[] buffer)
        {
            throw new NotImplementedException();
        }

        public override bool SetValue(AOAccessor aoItem, short value)
        {
            if (!_trigConnected.M)
                return false;
            //....
            return true;
        }

        public override bool SetValueFloat(AOAccessor aoItem, float value)
        {
            if (!_trigConnected.M)
                return false;

            //// 获取下标，转换成16进制 IEEE 754浮点数，然后整个写下去
            var addr = aoItem.Addr;
            int index = int.Parse(addr);
            string address = _aoVariable.Substring(0, 9) + (int.Parse(_aoVariable.Substring(9, 3)) + index * 4);
            byte[] data = new byte[4];

            if (index <= 48)
            {
                data = BitConverter.GetBytes(value);
            }
            else if (index >= 49)
            {
                data = BitConverter.GetBytes((int)value);
            }

            Array.Reverse(data);

            return _ads.BulkWriteByteRenderResult(address, data);
        }

        public override bool SetValue(DOAccessor doItem, bool value)
        {
            if (!_trigConnected.M)
            {
                return false;
            }

            if (!IO.CanSetDO(doItem.Name, value, out string reason))
            {
                LOG.Write(reason);
                return false;
            }

            lock (_locker)
            {
                ////获取单个DWORD数据，修改其中一位，然后整个写下去
                var addr = doItem.Addr;
                int add = int.Parse(addr);
                int indexByte = add % 8;
                int index = add % 8 == 0 ? add / 8 : (add - add % 8) / 8;
                var val = 1 << indexByte;
                byte[] output = ReadDOut();
                if (output == null)
                {
                    LOG.Write("ReadDOut = null , in SetValue()");
                    return false;
                }

                output[index] = value ? Convert.ToByte(Convert.ToUInt16(output[index]) | val) : Convert.ToByte((Convert.ToUInt16(output[index]) & ~val));

                string[] stringSeparators = new string[] { "DBB" };
                string[] result = _doVariable.Split(stringSeparators, StringSplitOptions.None);
                int.TryParse(result[1], out int startIndex);
                int finalIndex = startIndex + index;
                string address = $"{result[0]}DBB{finalIndex}";
                byte[] data = new byte[] { output[index] };

                return _ads.BulkWriteByteRenderResult(address, data);
            }
        }
    }
}
