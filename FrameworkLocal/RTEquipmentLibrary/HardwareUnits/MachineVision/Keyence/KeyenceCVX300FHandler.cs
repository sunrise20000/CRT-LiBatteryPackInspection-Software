using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MachineVision.Keyence
{
    public abstract class KeyenceCVX300FHandler : HandlerBase
    {
        public KeyenceCVX300F Device { get; }

        protected KeyenceCVX300FHandler(KeyenceCVX300F device, byte[] commandvalue)
            : base(commandvalue) 
        //: base(BuildMessage(commandvalue))
        {
            Device = device;
            //this.AckTimeout = TimeSpan.FromSeconds(10);
            //this.CompleteTimeout = TimeSpan.FromSeconds(10);
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
            var result = msg as KeyenceCVX300FMessage;

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
    //===================================================================================
    public class KeyenceCVX300FQueryHandler : KeyenceCVX300FHandler
    {
        //public HwAlignerGuideQueryHandler(HwAlignerGuide device, string name, byte groupAddress, byte functionCode, byte offerHigh, byte offerLow, byte dataHigh, byte dataLow)
        //    : base(device, new byte[] { groupAddress, functionCode, offerHigh, offerLow, dataHigh, dataLow })
        //{
        //    Name = name;
        //}
        public KeyenceCVX300FQueryHandler(KeyenceCVX300F device, string name, byte[] commandvalue)
           : base(device, commandvalue)
        {
            Name = name;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KeyenceCVX300FMessage;
            handled = false;
            if (!result.IsResponse) return true;
            //if (result.FunctionCode==0x01)
            //{
            //    Device.PraseData(Name,result.Data);
            //}
            //
            Device.PraseData(Name, result.Data);
            //
            Device._connecteTimes = 0;
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }
    public class KeyenceCVX300FSetHandler : KeyenceCVX300FHandler
    {
        public int iIfMe = 12;
        //public HwAlignerGuideSetHandler(HwAlignerGuide device, string name, byte groupAddress,byte functionCode, byte offerHigh, byte offerLow, byte dataHigh, byte dataLow)
        //    : base(device, new byte[] { groupAddress, functionCode, offerHigh, offerLow, dataHigh, dataLow })       
        //{
        //    Name =name;
        //}
        public KeyenceCVX300FSetHandler(KeyenceCVX300F device, string name, byte[] commandvalue)
          : base(device, commandvalue)
        {
            Name = name;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
           
            //
            var result = msg as KeyenceCVX300FMessage;
            handled = false;
            //if (!result.IsResponse)
            //{
            //    return true;
            //}
            //if (Name == "Clear Error On")
            //{

            //}
            //
            handled = Device.PraseData(Name, result.Data);
            //
            ResponseMessage = msg;
            Device.IsBusy = false;
            //handled = true;
            return true;
        }
    }
}
