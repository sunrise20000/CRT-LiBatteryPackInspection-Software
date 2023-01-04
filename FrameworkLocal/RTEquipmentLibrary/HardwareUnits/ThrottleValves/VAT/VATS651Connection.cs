using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.ThrottleValves.VAT
{
    public class VATS651Message : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class VATS651Connection : SerialPortConnectionBase
    {
        private static string _endLine = "\r\n";

        private string _cachedBuffer = string.Empty;

        public VATS651Connection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, _endLine, true)
        {

        }


        protected override MessageBase ParseResponse(string rawText)
        {
            VATS651Message msg = new VATS651Message();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }
            if (rawText.Length <= 1)
            {
                LOG.Error($"too short response,");
                msg.IsFormatError = true;
                return msg;
            }
            rawText = rawText.Substring(0, rawText.Length - 2);//remove end char

            if (rawText.Contains("E:"))
            {
                msg.IsError = true;
                msg.Data = rawText.Substring(2, rawText.Length - 2); ;
            }
            else
            {
                msg.Data = rawText;
                msg.IsAck = true;
            }
            return msg;
        }

    }
}
