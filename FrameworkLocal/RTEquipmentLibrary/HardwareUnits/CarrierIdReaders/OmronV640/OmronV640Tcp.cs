using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.OmronV640
{
    public class OmronV640Tcp:CIDReaderBaseDevice,IConnection
    {
        private string _scRoot;
        private DateTime _startTime;
        public OmronV640Tcp(string module,string name,string scRoot,LoadPortBaseDevice lp =null,int offset=0,int length=16):base(module,name,lp)
        {
            _scRoot = scRoot;
            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            Offset = offset;
            Length = length;

            _socket = new AsyncSocket(_address);
            _socket.OnDataChanged += new AsyncSocket.MessageHandler(OnDataChanged);
            _socket.OnErrorHappened += new AsyncSocket.ErrorHandler(OnErrorHandler);

            ConnectionManager.Instance.Subscribe($"{Name}", this);
            //int connecttime = 0;
            _socket.Connect(_address);
            //while(!_socket.IsConnected && connecttime++<retryMaxtime)
            //{
            //    _socket.Connect(_address);
            //    if (!_socket.IsConnected)
            //        Thread.Sleep(1000);
            //}

        }

        private void OnErrorHandler(ErrorEventArgs args)
        {
            OnError();
        }

        private void OnDataChanged(string package)
        {
            try
            {
                Thread.Sleep(500);
                package = package.ToUpper();
                string[] msgs = Regex.Split(package, "\r");
                foreach (string msg in msgs)
                {
                    if (msg.Length > 0)
                    {
                        string type = msg.Substring(0, 2);
                        if (type != "00")
                        {
                            EV.PostAlarmLog("RFID", $"{Name} occurred error:{getErrMsg(type)}");
                            if (DeviceState == CIDReaderStateEnum.ReadCarrierID)
                            {
                                OnCarrierIDReadFailed(type);
                            }
                            if (DeviceState == CIDReaderStateEnum.WriteCarrierID)
                            {
                                OnCarrierIDWriteFailed(type);
                            }
                            return;
                        }
                        if (DeviceState == CIDReaderStateEnum.ReadCarrierID)
                        {
                            string msgdata = msg.Substring(2, msg.Length - 2);
                            string tempID = HEX2ASCII(msg);
                            CarrierIDBeRead = tempID.Split('\0')[1].Replace("\0","");
                            
                            if (SC.ContainsItem("LoadPort.CarrierIdNeedTrimSpace") && SC.GetValue<bool>("LoadPort.CarrierIdNeedTrimSpace"))
                            {
                                CarrierIDBeRead = CarrierIDBeRead.Trim();
                            }
                            OnCarrierIDRead(CarrierIDBeRead);
                        }
                        if (DeviceState == CIDReaderStateEnum.WriteCarrierID)
                        {
                            OnCarrierIDWrite();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }
        }
        protected override bool fStartReset(object[] param)
        {
            if (_socket.IsConnected)
                return true;
            else
            {
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                _socket.Connect(_address);
                
            }
            return true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            if(_socket.IsConnected)
                return true;
            else
            {
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                _socket.Connect(_address);
                if (_socket.IsConnected)
                    return true;
            }
            return false;
        }
        protected override bool fStartReadCarrierID(object[] param)
        {
            try
            {
                _startTime = DateTime.Now;
                int offset = 0;
                int length = 16;
                if (param != null)
                {
                    offset = (int)param[0];
                    length = (int)param[1];
                }
                string msgdata = string.Format("{0}{1}\r", "0100", GetPage(offset + 1, length));
                return _socket.Write(msgdata);
                
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        protected override bool fStartWriteCarrierID(object[] param)
        {
            try
            {
                _startTime = DateTime.Now;
                CarrierIDToBeWriten = param[0].ToString();
                int offset = 0;
                int length = 16;
                
                if (param.Length == 3)
                {
                    offset = (int)param[1];
                    length = (int)param[2];
                }
                string msgdata = string.Format("{0}{1}{2}\r", "0200", GetPage(offset+1, length), ASCII2HEX(CarrierIDToBeWriten,length));
                return _socket.Write(msgdata);
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        protected override bool fMonitorReadCarrierID(object[] param)
        {
            if (DateTime.Now - _startTime > TimeSpan.FromSeconds(10))
            {
                OnCarrierIDReadFailed("Timeout");
                return true;
            }
            return base.fMonitorReadCarrierID(param);
        }
        protected override bool fMonitorWriteCarrierID(object[] param)
        {
            if (DateTime.Now - _startTime > TimeSpan.FromSeconds(10))
            {
                OnCarrierIDWriteFailed("Timeout");
                return true;
            }
            return base.fMonitorReadCarrierID(param);
        }

        public string Address => _address;
        private string _address;
        private AsyncSocket _socket;
        public bool IsConnected =>_socket.IsConnected;

        public int Offset { get; private set; }
        public int Length { get; private set; }
        public bool Connect()
        {
            _socket.Connect(_address);
            return true;
        }

        public bool Disconnect()
        {
            _socket.Dispose();
            return true;
        }
        private string getErrMsg(string error)
        {
            string msg = "";
            switch (error)
            {
                case "14":
                    msg = "Format error There is a mistake in the command format";
                    break;
                case "70":
                    msg = "Communications error Noise or another hindrance occurs during communications with an ID Tag, and communications cannot be completed normally.";
                    break;
                case "71":
                    msg = "Verification error Correct data cannot be written to an ID Tag";
                    break;
                case "72":
                    msg = "No Tag error Either there is no ID Tag in front of the CIDRW Head, or the CIDRW Head is unable to detect the ID Tag due to environmental factors";
                    break;
                case "7B":
                    msg = "Outside write area error A write operation was not completed normally because the ID Tag was in an area in which the ID Tag could be read but not written";
                    break;
                case "7E":
                    msg = "ID system error (1) The ID Tag is in a status where it cannot execute command processing";
                    break;
                case "7F":
                    msg = "ID system error (2) An inapplicable ID Tag has been used";
                    break;
                case "9A":
                    msg = "Hardware error in CPU An error occurred when writing to EEPROM.";
                    break;
            }
            return msg;
        }
        public string HEX2ASCII(string hex)
        {
            string res = String.Empty;

            try
            {
                for (int a = 0; a < hex.Length; a = a + 2)

                {

                    string Char2Convert = hex.Substring(a, 2);

                    int n = Convert.ToInt32(Char2Convert, 16);

                    char c = (char)n;

                    res += c.ToString();

                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
            return res;
        }
        private string GetPage(int startpage, int length)
        {

            double dpage = 0;
            for (int i = 0; i < length; i++)
            {
                dpage = dpage + Math.Pow(2, startpage + 1 + i);
            }
            string pageret = String.Format("{0:X}", Convert.ToInt32(dpage));

            for (int j = pageret.Length; j < 8; j++)
            {
                pageret = "0" + pageret;
            }
            return pageret;
        }
        private string ASCII2HEX(string src,int length)
        {
            while (src.Length < length * 8)
            {
                src = src + '\0';
            }

            if (src.Length > length * 8)
            {
                src = src.Substring(0, length * 8);
                LOG.Write($"RFID support max {(length * 8).ToString()} characters");
            }
            string res = String.Empty;
            try
            {

                char[] charValues = src.ToCharArray();
                string hexOutput = "";
                foreach (char _eachChar in charValues)
                {
                    // Get the integral value of the character.
                    int value = Convert.ToInt32(_eachChar);
                    // Convert the decimal value to a hexadecimal value in string form.
                    hexOutput += String.Format("{0:X2}", value);
                    // to make output as your eg 
                    //  hexOutput +=" "+ String.Format("{0:X}", value);

                }

                return hexOutput;
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }

            return res;

        }
    }
}
