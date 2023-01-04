using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.TruPlasmaRF
{

    public class TruPlasmaRFMessage : BinaryMessage
    {
        public string Command { get; set; }
        public string ErrorCode { get; set; }
        public byte[] Data { get; set; }
    }

    public class TruPlasmaRFConnection : SerialPortConnectionBase
    {

        private static byte _start = 0xAA;
        //private static byte _address = 0x02;
        //private static byte _GS = 0x00;
        private static byte _stop = 0x55;
        private static byte _ack = 0x06;


        public TruPlasmaRFConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(byte[] rawMessage)
        {

            TruPlasmaRFMessage msg = new TruPlasmaRFMessage();
            msg.RawMessage = rawMessage;
            //msg.Data = rawMessage;


            if (rawMessage.Length == 1 && rawMessage[0] == _ack)
            {
                msg.IsResponse = true;
                msg.IsAck = true;
                return msg;
            }

            if (rawMessage.Length < 11)
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
            if (rawMessage.Last() != _stop)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid stop byte");
                return msg;
            }

            var contentBuffer = rawMessage.Skip(3).Take(rawMessage.Length - 6).ToArray();

            var checkSum = Crc16.Crc16Ccitt(contentBuffer);
            var recSum = rawMessage[rawMessage.Length - 3] * 256 + rawMessage[rawMessage.Length - 2];
            //if(recSum != checkSum)
            //{
            //    LOG.Error($"check sum failed,");
            //    msg.IsFormatError = true;
            //    return msg;
            //}

            if (rawMessage[8] != 0xFF)
            {
                msg.IsError = true;
                msg.ErrorCode = rawMessage[8].ToString("X2");
            }

            //var msgArray = rawMessage.Select(bt => bt.ToString("X2")).ToArray();
            //string msgCommand = string.Join(",", msgArray, 4, 4);
            //msg.Command = msgCommand;

            //if(rawMessage.Length > 11)
            //    msg.Data = rawMessage.Skip(8).Take(rawMessage.Length - 11).ToArray();
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
    }
}
