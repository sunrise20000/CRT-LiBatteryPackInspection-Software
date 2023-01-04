using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temps.Omron
{
    public class TempOmronMessage : BinaryMessage
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte Length { get; set; }
        public byte[] Data { get; set; }

    }
    public class TempOmronConnection:SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        public TempOmronConnection(string portName) : base(portName, 9600, 8, System.IO.Ports.Parity.Even, System.IO.Ports.StopBits.One, "\n", false)
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

            TempOmronMessage msg = new TempOmronMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length < 4) return msg;

            msg.DeviceAddress = temps[0];
            msg.FunctionCode = temps[1];
            if (msg.FunctionCode !=0x03)
            {  
                msg.Data = _lstCacheBuffer.ToArray();
            }
            else
            {
                msg.Length = temps[2];

                if (temps.Length < msg.Length + 5)
                {
                   //msg.IsFormatError = true; 
                    return msg;
                }

                msg.Data = _lstCacheBuffer.Skip(3).Take(msg.Length).ToArray();
            }
            
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
    public abstract class TempOmronHandler : HandlerBase
    {
        public TempOmron Device { get; }

        protected TempOmronHandler(TempOmron device, byte[] commandvalue)
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
            var result = msg as TempOmronMessage;

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

    public class TempOmronQueryHandler : TempOmronHandler
    {
        public TempOmronQueryHandler(TempOmron device,string name, byte groupAddress,byte offerHigh,byte offerLow,byte dataHigh,byte dataLow)
            : base(device, new byte[] { groupAddress, 3, offerHigh, offerLow, dataHigh, dataLow })
        {
            Name = name;//"Query Temp";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as TempOmronMessage;
            handled = false;
            if (!result.IsResponse) return true;

            short rvalue = 0;
            rvalue = (short)(rvalue ^ result.Data[0]);
            rvalue = (short)(rvalue << 8);
            rvalue = (short)(rvalue ^ result.Data[1]);

            //if(Name.Contains("Actual"))
            //Device.ActualTemp = rvalue;
            //else
            //Device.SettingTemp = rvalue;
            if (Name.Contains("Actual"))
            {   var chel =Convert.ToInt16(Name.Split(' ')[1].Substring(7,1));
                var count = Convert.ToInt16(Name.Split(' ')[2].Substring(6, 1));
                var index = (chel-1) * 4 + count - 1;
                Device.ActualTemp[index] =Convert.ToSingle(rvalue)/10;
            }
            else
            {
                var chel = Convert.ToInt16(Name.Split(' ')[1].Substring(7, 1));
                var count = Convert.ToInt16(Name.Split(' ')[2].Substring(7, 1));
                var index = (chel - 1) * 4 + count - 1;
                Device.SettingTemp[index] = Convert.ToSingle(rvalue)/10;
            }
            Device._connecteTimes = 0;
            ResponseMessage = msg;
            handled = true;
            Thread.Sleep(1000);
            return true;
        }
    }
    public class TempOmronWriteSingleHandler : TempOmronHandler
    {
        public TempOmronWriteSingleHandler(TempOmron device, string name, byte groupAddress, byte offerHigh, byte offerLow, byte dataHigh, byte dataLow)
            : base(device, new byte[] { groupAddress, 0x06, offerHigh, offerLow, dataHigh, dataLow })
        {
            Name = name;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as TempOmronMessage;
            handled = false;
            if (!result.IsResponse) return true;
            if (result.FunctionCode == 0x86)
            {
               Device.RespondAbnormal(result.FunctionCode, result.Data[2]);
            }
            ResponseMessage = msg;
            handled = true;
            Device.IsConfig = false;
            Thread.Sleep(1000);
            return true;
        }
    }
    public class TempOmronWriteHandler : TempOmronHandler
    {
        public TempOmronWriteHandler(TempOmron device, string name,byte groupAddress, byte offerHigh, byte offerLow,byte unitH,byte unitL,byte length, byte dataHigh, byte dataLow)
            : base(device, new byte[] { groupAddress, 0x10, offerHigh, offerLow, unitH, unitL, length, dataHigh, dataLow })
        {
            Name = name;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as TempOmronMessage;
            handled = false;
            if (!result.IsResponse) return true;
            if (result.FunctionCode == 0x90)
            {
                Device.RespondAbnormal(result.FunctionCode, result.Data[2]);
            }
            ResponseMessage = msg;
            handled = true;
            Device.IsConfig = false;
            Thread.Sleep(1000);
            return true;
        }
    }
}
