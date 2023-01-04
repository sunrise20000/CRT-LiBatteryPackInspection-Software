using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Vacuometer
{
    public class VacuometerMessage : BinaryMessage
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte Length { get; set; }
        public byte[] Data { get; set; }

    }
    class VacuometerConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        public VacuometerConnection(string portName) : base(portName, 9600, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One, "\n", false)
        {

        }
        public override bool SendMessage(byte[] message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            _lstCacheBuffer.Clear();
            _lstCacheBuffer.AddRange(rawMessage);
            byte[] temps = _lstCacheBuffer.ToArray();
           // EV.PostInfoLog("VCE","ARR1");
            VacuometerMessage msg = new VacuometerMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length > 9)
            {
                for(int i= 0;i < temps.Length;i++)
                {
                    if (i + 3 < temps.Length)
                    {
                        if (temps[i] == 0x07 && temps[i + 1] == 0x02 && temps[i + 2] == 0x10 && temps[i + 3] == 0x01)
                        {

                            temps = temps.Skip(i).Take(9).ToArray();//.Skip(9).ToArray();
                            break;
                        }
                    }
                }
            }
            if(temps.Length<9) return msg;
            if (temps[0] != 0x7) return msg;
            if(temps[8]!= CheckSum(temps)) return msg;
            //msg.DeviceAddress = temps[0];
            //msg.FunctionCode = temps[1];
            //msg.Length = temps[2];

            // if (temps.Length < msg.Length + 5) return msg;

            msg.Data = temps; //_lstCacheBuffer.ToArray();  //_lstCacheBuffer.Skip(3).Take(msg.Length).ToArray();
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }

        public byte CheckSum(byte[] buffer)
        {
            int Sum = 0;
            var arry = buffer.ToList().Skip(1).Take(7).ToArray();
            foreach (var data in arry)
            {
                Sum += data;
            }
            return Convert.ToByte(Sum & 0xff);
        }
    }
    public abstract class VacuometerHandler : HandlerBase
    {
        public Vacuometer Device { get; }

        protected VacuometerHandler(Vacuometer device, byte[] commandvalue)
            : base(BuildMessage(commandvalue))
        {
            Device = device;

        }

        private static byte[] BuildMessage(byte[] commandvalue)
        {
            if (commandvalue == null) return null;
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
            var result = msg as VacuometerMessage;

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
    public class VacuometerReciveHandler : VacuometerHandler
    {
        public VacuometerReciveHandler(Vacuometer device, string name)
            : base(device,null)
        {
            Name = name;//"Query Temp";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as VacuometerMessage;
            handled = false;
            if (!result.IsResponse) return true;
            Device.ReadData(result.Data);
            ResponseMessage = msg;
            SetState(EnumHandlerState.Acked);
            handled = true;
            Thread.Sleep(10);
            //EV.PostInfoLog("VCE", "ARR2");
            return true;
        }
    }
 }
