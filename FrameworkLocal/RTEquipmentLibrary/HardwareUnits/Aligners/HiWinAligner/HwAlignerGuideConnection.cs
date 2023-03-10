using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System.Collections.Generic;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.HwAligner
{
    public class HwAlignerGuideMessage : BinaryMessage
    {
        //public bool IsResponse
        //{
        //    get;
        //    set;
        //}

        //public bool IsAck
        //{
        //    get;
        //    set;
        //}

        //public bool IsError
        //{
        //    get;
        //    set;
        //}

        //public bool IsEvent
        //{
        //    get;
        //    set;
        //}

        //public bool IsComplete
        //{
        //    get;
        //    set;
        //}

        //public bool IsFormatError
        //{
        //    get;
        //    set;
        //}

        //public bool IsBusy
        //{
        //    get;
        //    set;
        //}

        //public bool IsNak
        //{
        //    get;
        //    set;
        //}

        //public int MutexTag
        //{
        //    get;
        //    set;
        //}
        //public byte[] RawMessage
        //{
        //    get;
        //    set;
        //}

        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte Length { get; set; }
        public byte[] Data { get; set; }



    }
    public class HwAlignerGuideConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        private object _lockerActiveHandler = new object();
        public HwAlignerGuideConnection(string portName)
            : base(portName, 115200, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One, "\r", false)
        {   
        }
        public override bool SendMessage(byte[] message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
         
            _lstCacheBuffer.AddRange(rawMessage);
            byte[] temps = _lstCacheBuffer.ToArray();

            HwAlignerGuideMessage msg = new HwAlignerGuideMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            //if (temps.Length < 4) return msg;
            /*
            msg.DeviceAddress = temps[0];
            msg.FunctionCode = temps[1];
            */
            //if (msg.FunctionCode == 3)
            //{
            //    msg.Length = temps[2];

            //    if (temps.Length < msg.Length + 5) return msg;

            //    msg.Data = _lstCacheBuffer.Skip(3).Take(msg.Length).ToArray();
            //}else
            msg.Data = _lstCacheBuffer.ToArray();
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;
            //msg.IsEvent = false;
            //msg.IsResponse = false;

            return msg;
        }

        private static byte ModRTU_CRC(byte[] buffer)
        {
            //ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;
            byte temp = buffer[0];

            for (int i = 1; i < buffer.Length; i++)
            {
                temp = (byte)(temp ^ buffer[i]);
            }
            return (byte)~temp;
        }

        //public new void Execute(HandlerBase handler)
        //{
        //    if (_activeHandler != null || handler == null) //|| !_port.IsOpen())
        //    {
        //        return;
        //    }

        //    lock (_lockerActiveHandler)
        //    {
        //        retryTime = 0;
        //        _activeHandler = handler;
        //        _activeHandler.SetState(EnumHandlerState.Sent);

        //        SendMessage(handler.SendBinary);
        //        //

        //    }
        //}
    }

}
