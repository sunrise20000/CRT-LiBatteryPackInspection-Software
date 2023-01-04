using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Comet
{

    public class CometRFMatchMessage : BinaryMessage
    {
        public string Command { get; set; }
        public string ErrorCode { get; set; }
        public byte[] Data { get; set; }
    }

    public class CometRFMatchConnection : SerialPortConnectionBase
    {

        private static byte _start = 0xAA;
        private static byte _address = 0x21;



        public CometRFMatchConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(byte[] rawMessage)
        {

            CometRFMatchMessage msg = new CometRFMatchMessage();
            msg.RawMessage = rawMessage;
            //msg.Data = rawMessage;

            if (rawMessage.Length < 4)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid response");
                return msg;
            }

            if (rawMessage[0] != _start )
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid start byte");
                return msg;
            }
            if (rawMessage[1] != _address)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid address byte");
                return msg;
            }

            byte checkSum = 0;
            for (int i = 0; i < rawMessage.Length-1; i++)
            {
                checkSum += rawMessage[i];
            }
            var recSum = rawMessage[rawMessage.Length - 1];
            if (recSum != checkSum)
            {
                LOG.Error($"check sum failed,");
                msg.IsFormatError = true;
                return msg;
            }

            //command&data
            msg.Data = rawMessage.Skip(2).Take(rawMessage.Length - 3).ToArray();
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
    }
}
