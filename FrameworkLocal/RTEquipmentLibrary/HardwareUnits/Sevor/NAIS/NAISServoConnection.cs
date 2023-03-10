using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Servo.NAIS
{
    public class NAISServoMessage : BinaryMessage
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte Length { get; set; }
        public byte[] Data { get; set; }

    }
    public class NAISServoConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        public NAISServoConnection(string portName) : base(portName, 9600, 8, System.IO.Ports.Parity.Even, System.IO.Ports.StopBits.One, "\n", false)
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

            NAISServoMessage msg = new NAISServoMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length < 4) return msg;

            msg.DeviceAddress = temps[0];
            msg.FunctionCode = temps[1];
            if (msg.FunctionCode == 3)
            {
                msg.Length = temps[2];

                if (temps.Length < msg.Length + 5) return msg;

                msg.Data = _lstCacheBuffer.Skip(3).Take(msg.Length).ToArray();
            }else msg.Data = _lstCacheBuffer.ToArray();
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

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
    }
    public abstract class NAISServoHandler : HandlerBase
    {
        public NAISServo Device { get; }

        protected NAISServoHandler(NAISServo device, byte[] commandvalue)
            : base(BuildMessage(commandvalue))
        {
            Device = device;

        }

        private static byte[] BuildMessage(byte[] commandvalue)
        {
            byte[] crc = ModRTU_CRC(commandvalue);
            List<byte> result = commandvalue.ToList();
            foreach (byte b in crc)
            {
                result.Add(b);
            }
            return result.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as NAISServoMessage;

            ResponseMessage = msg;
            handled = true;
            return true;
        }


        private static byte[] ModRTU_CRC(byte[] buffer)
        {
            ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;

            for (var pos = 0; pos < len; pos++)
            {
                crc ^= buf[pos]; // XOR byte into least sig. byte of crc

                for (var i = 8; i != 0; i--)
                    // Loop over each bit
                    if ((crc & 0x0001) != 0)
                    {
                        // If the LSB is set
                        crc >>= 1; // Shift right and XOR 0xA001
                        crc ^= 0xA001;
                    }
                    else // Else LSB is not set
                    {
                        crc >>= 1; // Just shift right
                    }
            }

            // Note, this number has low and high bytes swapped, so use it accordingly (or swap bytes)
            return BitConverter.GetBytes(crc);

        }
    }

    public class NAISServoQueryHandler : NAISServoHandler
    {
        public NAISServoQueryHandler(NAISServo device, string name, byte groupAddress, byte functionCode, byte offerHigh, byte offerLow, byte dataHigh, byte dataLow)
            : base(device, new byte[] { groupAddress, functionCode, offerHigh, offerLow, dataHigh, dataLow })
        {
            Name = name;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as NAISServoMessage;
            handled = false;
            if (!result.IsResponse) return true;
            if (result.FunctionCode==0x01)
            {
                Device.PraseData(Name,result.Data);
            }
            Device._connecteTimes = 0;
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }
    public class NAISServoSetHandler : NAISServoHandler
    {
        public NAISServoSetHandler(NAISServo device, string name, byte groupAddress,byte functionCode, byte offerHigh, byte offerLow, byte dataHigh, byte dataLow)
            : base(device, new byte[] { groupAddress, functionCode, offerHigh, offerLow, dataHigh, dataLow })       
        {
            Name =name;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as NAISServoMessage;
            handled = false;
            if (!result.IsResponse) return true;
            if (Name=="Clear Error On")
            { 
                
            }
            ResponseMessage = msg;
            Device.IsBusy = false;
            handled = true;
            return true;
        }
    }
}
