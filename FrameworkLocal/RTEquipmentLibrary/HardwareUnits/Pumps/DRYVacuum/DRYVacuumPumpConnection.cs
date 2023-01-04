using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.DRYVacuum
{
    public class DRYVacuumPumpMessage : BinaryMessage
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte Length { get; set; }
        public byte[] Data { get; set; }
    }
    public class DRYVacuumPumpConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        public DRYVacuumPumpConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits," ", false)
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

            DRYVacuumPumpMessage msg = new DRYVacuumPumpMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length < 4) return msg;

            //msg.DeviceAddress = temps[0];
            //msg.FunctionCode = temps[1];
            var Length = _lstCacheBuffer.Count;
            msg.Data = _lstCacheBuffer.Take(Length - 4).Skip(4).ToArray(); 


            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        
        }
    }
}
