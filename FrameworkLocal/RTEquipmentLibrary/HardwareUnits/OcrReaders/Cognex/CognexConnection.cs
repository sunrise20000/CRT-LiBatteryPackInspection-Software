using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.Cognex
{
    public class CognexOCRMessage : AsciiMessage
    {
        public string Data { get; set; }

    }
    public class CognexOCRConnection : TCPPortConnectionBase
    {
        private CognexWaferIDReader _reader;
        public CognexOCRConnection(string addr,CognexWaferIDReader reader) : base(addr,"")
        {
            _reader = reader;
        }
         

        protected override MessageBase ParseResponse(string rawText)
        {
            if (rawText.Contains("User:") && _reader.DeviceState != OCRReaderStateEnum.Error)
            {
                SendMessage("admin\r\n");

            }
            if (rawText.Contains("Password:") && _reader.DeviceState != OCRReaderStateEnum.Error)
            {
                SendMessage("\r\n");
            }
            if (rawText.Contains("User Logged In") && _reader.DeviceState != OCRReaderStateEnum.Error)
            {
                _reader.IsLogined = true;
            }
            if (rawText.Contains("Invalid Password") && _reader.DeviceState != OCRReaderStateEnum.Error)
            {
                _reader.IsLogined = false;
                _reader.OnError(rawText);
            }

            CognexOCRMessage msg = new CognexOCRMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            string tempdata = rawText.Replace("\r", "").Replace("\n", "");
            msg.RawMessage = rawText;
            msg.Data = rawText;
            if (tempdata.Contains("Welcome")) msg.IsAck = true;
            if (tempdata == "1") msg.IsAck = true;

            //else if (tempdata.Length == 1)
            //{
            //    msg.IsNak = true;
            //    if(IsBusy)
            //        _reader.OnError("Received response:" + rawText);
            //}
            else
            {
                msg.IsNak = true;
                msg.IsResponse = true;
                msg.Data = rawText;
            }
            return msg;
        }
        




    }


    
}
