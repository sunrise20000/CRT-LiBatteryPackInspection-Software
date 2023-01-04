using Aitex.Core.Util;
using MECF.Framework.Common.Utilities;
using MECF.Framework.Simulator.Core.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class SerialPortDeviceSimulatorFactory
    { 
        public static CommonSerialPortDeviceSimulator GetCommonSerialPortDeviceSimulator(string port, string deviceName)
        {
            if(deviceName == "SiasunPhoenixB")
            {
                return new SiasunPhoenixBSimulator(port, deviceName);
            }
            else if (deviceName == "Siasun1500C800C")
            {
                return new Siasun1500C800CSimulator(port, deviceName);
            }
            else if (deviceName == "BrooksVCE")
            {
                return new BrooksVCESimulator(port, deviceName);
            }
            else if (deviceName == "Hanbell")
            {
                return new HanbellPumpSimulator(port, deviceName);
            }
            else if (deviceName == "HirataR4")
            {
                return new HirataR4Simulator(port, deviceName);
            }
            else if (deviceName == "BrooksSMIF")
            {
                return new BrooksSMIFSimulator(port, deviceName);
            }
            else if (deviceName == "SiasunAligner")
            {
                return new SiasunAlignerSimulator(port, deviceName);
            }
            else if (deviceName == "RisshiChiller")
            {
                return new RisshiChillerSimulator(port, deviceName);
            }
            else if (deviceName == "SiasunVCE")
            {
                return new SiasunVCESimulator(port, deviceName);
            }
            else if (deviceName == "TazmoRobot")
            {
                return new TazmoRobotSimulator(port, deviceName);
            }
            else if (deviceName == "VATS651")
            {
                return new VATS651Simulator(port, deviceName);
            }
            else if (deviceName == "TruPlasmaRF")
            {
                return new TruPlasmaRFSimulator(port, deviceName);
            }
            else if (deviceName == "SkyPump")
            {
                return new SkyPumpSimulator(port, deviceName);
            }
            else if (deviceName == "CommetRFMatch")
            {
                return new CommetRFMatchSimulator(port, deviceName);
            }
            else if (deviceName == "FujikinMFC")
            {
                return new FujikinMFCSimulator(port, deviceName);
            }
            else if (deviceName == "PfeifferPumpA603")
            {
                return new PfeifferPumpA603Simulator(port, deviceName); 
            }
            else if (deviceName == "PfeifferPumpA100")
            {
                return new PfeifferPumpA100Simulator(port, deviceName);
            }
            else if (deviceName == "EdwardsPump") 
            {
                return new EdwardsPumpSimulator(port, deviceName);
            }
            else if (deviceName == "KaimeiRFMatch")
            {
                return new KaimeiRFMatchSimulator(port, deviceName);
            }
            return null;
        }
    }



    public class CommonSerialPortDeviceSimulator : SerialPortDeviceSimulator
    {
        public bool Failed { get; set; }
        public bool AutoReply { get; set; } = true;

        public bool IsAtSpeed { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;

        private object _locker = new object();

        public string ResultValue { get; set; }

        public List<IOSimulatorItemViewModel> IOSimulatorItemList { get; set; }

        public event Action<IOSimulatorItemViewModel> SimulatorItemActived;

        string _deviceName;
        public CommonSerialPortDeviceSimulator(string port, string deviceName, bool isAscii = false, string newLine = "\r")
            : base(port, -1, newLine, ' ', isAscii)
        {
            _deviceName = deviceName;
            ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

            IsAtSpeed = true;
        }

        public CommonSerialPortDeviceSimulator(string port, string deviceName, bool isAscii, string newLine, int dataBits)
            : base(port, -1, newLine, ' ', isAscii, dataBits)
        {
            _deviceName = deviceName;
            ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

            IsAtSpeed = true;
        }

        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();

                    IsAtSpeed = true;
                }
            }
        }

        //private string GetCharsFormFromBinaryString(String binary)
        //{
        //    return Encoding.ASCII.GetString(GetBytesFromBinaryString(binary));
        //}

        private string GetCharsFormFromBinaryString(byte[] binary)
        {
            return Encoding.ASCII.GetString(binary);
        }

        protected override void ProcessUnsplitMessage(byte[] binaryMessage)
        {
            lock (_locker)
            {
                var activeSimulatorItem = GetActiveIOSimulatorItemViewModel(binaryMessage);
                if (activeSimulatorItem == null) return;

                activeSimulatorItem.CommandContent = string.Join("," , binaryMessage.Select(bt=>bt.ToString("X2")).ToArray());
                activeSimulatorItem.CommandRecievedTime = DateTime.Now;

                if (SimulatorItemActived != null)
                    SimulatorItemActived(activeSimulatorItem);

                if (AutoReply)
                {
                    OnWriteSimulatorItem(activeSimulatorItem);
                } 
            }
        }

        protected override void ProcessUnsplitMessage(string msg)
        {
            lock (_locker)
            {
                var activeSimulatorItem = GetActiveIOSimulatorItemViewModel(msg);
                if (activeSimulatorItem == null) return;

                activeSimulatorItem.CommandContent = msg;
                activeSimulatorItem.CommandRecievedTime = DateTime.Now;

                if (SimulatorItemActived != null)
                    SimulatorItemActived(activeSimulatorItem);

                if(AutoReply)
                {
                    OnWriteSimulatorItem(activeSimulatorItem);
                }
            }
        }

        protected virtual IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            return null;
        }
        protected virtual IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            return null;
        }
        protected virtual void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
        }


        public void ManualWriteMessage(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteSimulatorItem(activeSimulatorItem);
        }

    }


    public class SiasunPhoenixBSimulator : CommonSerialPortDeviceSimulator
    {
        public SiasunPhoenixBSimulator(string port, string deviceName):base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommandName))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if(activeSimulatorItem.SourceCommandName.StartsWith("RQ"))
            {
                OnWriteMessage(activeSimulatorItem.Response + "\r");
                Thread.Sleep(1000);
                OnWriteMessage("_RDY" + "\r");
            }
            else
            {
                OnWriteMessage(activeSimulatorItem.Response + "\r");
            }
        }
    }

    public class Siasun1500C800CSimulator : CommonSerialPortDeviceSimulator
    {
        public Siasun1500C800CSimulator(string port, string deviceName) : base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommandName))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName.StartsWith("RQ"))
            {
                OnWriteMessage(activeSimulatorItem.Response + "\r");
                Thread.Sleep(1000);
                OnWriteMessage("_RDY" + "\r");
            }
            else
            {
                OnWriteMessage(activeSimulatorItem.Response + "\r");
            }
        }
    }

    public class RisshiChillerSimulator : CommonSerialPortDeviceSimulator
    {
        private static char _startLine = (char)1;
        private static char _endLine = (char)3;
        public RisshiChillerSimulator(string port, string deviceName) : base(port, deviceName, true, _endLine.ToString())
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }


        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(BuildMessage(activeSimulatorItem.Response));
        }

        private string BuildMessage(string response)
        {
            string sLength = response.Length.ToString("D2");
            var charArray = (sLength + response).ToArray();
            int sum = charArray.Sum(chr => (int)chr);
            string sSum = sum.ToString("X");

            if (sSum.Length < 2)
                return _startLine + sLength + response + "0" + sSum + _endLine.ToString();
            else
                return _startLine + sLength + response + sSum.Substring(sSum.Length - 2) + _endLine.ToString();
        }
    }
    public class SiasunAlignerSimulator : CommonSerialPortDeviceSimulator
    {
        private static char _startLine = (char)1;
        private static char _endLine = (char)3;
        public SiasunAlignerSimulator(string port, string deviceName) : base(port, deviceName, true, _endLine.ToString())
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }


        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(BuildMessage(activeSimulatorItem.Response));
        }

        private string BuildMessage(string response)
        {
            string sLength = response.Length.ToString("D2");
            var charArray = (sLength + response).ToArray();
            int sum = charArray.Sum(chr=>(int)chr);
            string sSum = sum.ToString("X");

            if(sSum.Length < 2)
                return _startLine + sLength  + response + "0" + sSum + _endLine.ToString();
            else
                return _startLine + sLength + response + sSum.Substring(sSum.Length-2) + _endLine.ToString();
        }
    }


    public class TruPlasmaRFResponse
    {
        public string GS { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Data { get; set; }
    }
    public class TruPlasmaRFSimulator : CommonSerialPortDeviceSimulator
    {
        private static byte _start = 0xAA;
        private static byte _address = 0x01;
        private static byte _stop = 0x55;
        private static byte _ack = 0x06;
        public TruPlasmaRFSimulator(string port, string deviceName) : base(port, deviceName, false, "",8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            if(msg.Length < 8)
                return null;
            var msgArray = msg.Select(bt => bt.ToString("X2")).ToArray();
            string msgCommand = string.Join(",", msgArray, 4, 4);
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msgCommand == simulatorItem.SourceCommand)
                {
                    return simulatorItem;
                }
            }

            return IOSimulatorItemList.Find(item=>item.SourceCommandName == "ExecuteAnyCommand");
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(new byte[] { _ack });
            Thread.Sleep(1000);
            OnWriteMessage(BuildMessage(activeSimulatorItem));
        }

        private static byte[] BuildMessage(IOSimulatorItemViewModel activeSimulatorItem)
        {
            TruPlasmaRFResponse rfResponse = JsonConvert.DeserializeObject<TruPlasmaRFResponse>(activeSimulatorItem.Response);

            List<byte> buffer = new List<byte>();

            buffer.Add(_start);
            buffer.Add(_address);
            int length = 4;
            if (rfResponse.Status != null)
                length++;
            if (rfResponse.Type != null)
                length++;
            if (rfResponse.Data != null)
            {
                length += rfResponse.Data.Split(',').Length;
            }

            buffer.Add((byte)length);
            buffer.Add(Convert.ToByte(rfResponse.GS, 16));

            buffer.AddRange(activeSimulatorItem.SourceCommand.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());


            if (rfResponse.Status != null)
                buffer.Add(Convert.ToByte(rfResponse.Status, 16));
            if (rfResponse.Type != null)
                buffer.Add(Convert.ToByte(rfResponse.Type, 16));
            if (rfResponse.Data != null)
            {
                buffer.AddRange(rfResponse.Data.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
            }
            var contentBuffer = buffer.Skip(3).Take(buffer.Count - 3).ToArray();
            buffer.AddRange(BitConverter.GetBytes(Crc16.Crc16Ccitt(contentBuffer)));
            buffer.Add(_stop);

            return buffer.ToArray();
        }
    }
    public class CommetRFMatchSimulator : CommonSerialPortDeviceSimulator
    {
        private static byte _start = 0xAA;
        private static byte _address = 0x21;
        public CommetRFMatchSimulator(string port, string deviceName) : base(port, deviceName, false, "", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            var msgArray = msg.Select(bt => bt.ToString("X2")).ToArray();
            string msgCommand = string.Join(",", msgArray, 2, 2);
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msgCommand == simulatorItem.SourceCommand)
                {
                    return simulatorItem;
                }
            }

            return null;
        }

        private static int iCount = 0;
        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {

            if (activeSimulatorItem.Response.Contains("|"))
            {
                var resArray = activeSimulatorItem.Response.Split('|');
                int length = resArray.Length;
                OnWriteMessage(BuildMessage(resArray[iCount%length]));
                iCount++;
                if (iCount > 100) 
                    iCount = 0;
            }
            else
            {
                OnWriteMessage(BuildMessage(activeSimulatorItem.Response));
            }
        }

        private static byte[] BuildMessage(string reponse)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(_start);
            buffer.Add(_address);

            buffer.AddRange(reponse.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());

            byte checkSum = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                checkSum += buffer[i];
            }
            buffer.Add(checkSum);

            return buffer.ToArray();
        }
    }

    public class KaimeiRFMatchSimulator : CommonSerialPortDeviceSimulator
    {
        //private byte _address = 0x01;
        private string _load = "01,F4";
        private string _tune = "01,F4";
        private string _mode = "01,00";
        private List<byte> _msgBuffer = new List<byte>();
        public KaimeiRFMatchSimulator(string port, string deviceName) : base(port, deviceName, false, "", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            _msgBuffer.AddRange(msg);
            if(_msgBuffer.Count < 8)
            {
                return null;
            }
            if(_msgBuffer[1] == 0x03)
            {
                if(_msgBuffer.Count >= 8)
                {
                    var msgArray = _msgBuffer.Select(bt => bt.ToString("X2")).ToArray();
                    string msgCommand = string.Join(",", msgArray, 0, 4);
                    _msgBuffer.RemoveRange(0, msgArray.Length);
                    foreach (var simulatorItem in IOSimulatorItemList)
                    {
                        if (msgCommand == simulatorItem.SourceCommand)
                        {
                            return simulatorItem;
                        }
                    }
                }
            }
            if(_msgBuffer[1] == 0x10)
            {
                var length = _msgBuffer[6];
                if (_msgBuffer.Count >= 7 + length + 2)
                {
                    var msgArray = _msgBuffer.Select(bt => bt.ToString("X2")).ToArray();
                    string msgCommand = string.Join(",", msgArray, 0, 4);
                    _msgBuffer.RemoveRange(0, msgArray.Length);
                    foreach (var simulatorItem in IOSimulatorItemList)
                    {
                        if (msgCommand == simulatorItem.SourceCommand)
                        {
                            return simulatorItem;
                        }
                    }
                }
            }

            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if(activeSimulatorItem.SourceCommandName == "SetPresetAbsoluteMode")
            {
                var contentArray = activeSimulatorItem.CommandContent.Split(',');
                if (contentArray.Length >= 15)
                {
                    var modeArray = contentArray.Skip(7).Take(2).ToArray();
                    var tuneArray = contentArray.Skip(9).Take(2).ToArray();
                     var loadArray = contentArray.Skip(11).Take(2);
                    _tune = $"0{string.Join(",", tuneArray).Substring(1)}";
                    _load = $"0{string.Join(",", loadArray).Substring(1)}";
                    _mode = string.Join(",", modeArray.Reverse());
                }
            }

            if (activeSimulatorItem.SourceCommandName == "GetStatus")
            {
                OnWriteMessage(BuildMessage($"{activeSimulatorItem.Response},{_tune},{_load},{_mode}"));
            }
            else
            {
                OnWriteMessage(BuildMessage(activeSimulatorItem.Response));
            }
        }

        private byte[] BuildMessage(string reponse)
        {
            List<byte> buffer = new List<byte>();

            buffer.AddRange(reponse.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());

            var checkSum = Crc16.CRC16_ModbusRTU(buffer.ToArray());
            var ret = BitConverter.GetBytes(checkSum);
            buffer.AddRange(ret);

            return buffer.ToArray();
        }
    }

    public class FujikinMFCSimulator : CommonSerialPortDeviceSimulator
    {
        private static byte _start = 0x02;
        private static byte _end = 0x00;
        private static byte _address = 0x00;
        private static byte _ack = 0x06;
        private static byte _nak = 0x16;
        public FujikinMFCSimulator(string port, string deviceName) : base(port, deviceName, false, "", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            var msgArray = msg.Select(bt => bt.ToString("X2")).ToArray();
            string commandType = msgArray[2];
            string msgCommand = string.Join(",", msgArray, 4, 3);
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (commandType == simulatorItem.SourceCommandType && msgCommand == simulatorItem.SourceCommand)
                {
                    return simulatorItem;
                }
            }

            return null;
        }

        
        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if(activeSimulatorItem.Response == activeSimulatorItem.SuccessResponse.ToString())
            {
                OnWriteMessage(new byte[] {0x06});
                Thread.Sleep(1000);

                var resArray = activeSimulatorItem.Response.Split('|');
                int iIndex = new Random().Next(0, resArray.Length - 1);
                string responseData = resArray[iIndex];
                if(responseData == "06" )
                {
                    OnWriteMessage(new byte[] { _ack });
                }
                else if (responseData == "16")
                {
                    OnWriteMessage(new byte[] { _nak });
                }
                else
                {
                    OnWriteMessage(BuildMessage(activeSimulatorItem, responseData));
                }
            }
            else
            {
                OnWriteMessage(new byte[] { _nak });
            }
        }

        private static byte[] BuildMessage(IOSimulatorItemViewModel activeSimulatorItem, string responseData)
        {
            string reponse = activeSimulatorItem.Response;
            List<byte> buffer = new List<byte>();
            var responseArray = responseData.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray();
            buffer.Add(_address);
            buffer.Add(_start);
            buffer.Add(Convert.ToByte(activeSimulatorItem.SourceCommandType, 16));
            buffer.Add((byte)(3+ responseArray.Length));
            buffer.AddRange(activeSimulatorItem.SourceCommand.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
            buffer.AddRange(responseArray);
            buffer.Add(_end);

            byte checkSum = 0;
            for (int i = 1; i < buffer.Count-1; i++)
            {
                checkSum += buffer[i];
            }
            buffer.Add(checkSum);

            return buffer.ToArray();
        }
    }

    
    public class PfeifferPumpA603Simulator : CommonSerialPortDeviceSimulator
    {
        private static string _start = "#";
        private static string _address = "005";
        private static string _end = "\r";
        public PfeifferPumpA603Simulator(string port, string deviceName) : base(port, deviceName, true, "\r", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(_start + _address + activeSimulatorItem.Response + _end);
        }
    }

    public class PfeifferPumpA100Simulator : CommonSerialPortDeviceSimulator
    {
        private static string _start = "#";
        private static string _address = "005";
        private static string _end = "\r";
        public PfeifferPumpA100Simulator(string port, string deviceName) : base(port, deviceName, true, "\r", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(_start + _address + "," + activeSimulatorItem.Response + _end);
        }
    }
    public class EdwardsPumpSimulator : CommonSerialPortDeviceSimulator
    {
        private static string _end = "\r\n";
        private bool _isPumpOn;
        public EdwardsPumpSimulator(string port, string deviceName) : base(port, deviceName, true, "\r", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName == "SwitchPump")
            {
                if (activeSimulatorItem.CommandContent.Contains("0"))
                    _isPumpOn = false;
                else
                    _isPumpOn = true;
            }
                
            if (activeSimulatorItem.SourceCommandName == "ReadPumpStatus")
            {
                OnWriteMessage($"{(_isPumpOn?"4":"0")},{activeSimulatorItem.Response}{_end}");
            }
            else
                OnWriteMessage(activeSimulatorItem.Response + _end);
        }
    }

    public class SkyPumpSimulator : CommonSerialPortDeviceSimulator
    {
        private static string _start = "@";
        private static string _address = "00";
        private static string _end = "\0\r\n";
        public SkyPumpSimulator(string port, string deviceName) : base(port, deviceName, true, "\r", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(_start + _address + " " + activeSimulatorItem.Response + _end);
        }
    }

    public class VATS651Simulator : CommonSerialPortDeviceSimulator
    {
        static string _endLine = "\r\n";
        public VATS651Simulator(string port, string deviceName) : base(port, deviceName, true, _endLine.ToString())
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.IndexOf(simulatorItem.SourceCommand) == 0)
                {
                    return simulatorItem;
                }
            }
            return null;
        }


        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if(activeSimulatorItem.Response == activeSimulatorItem.SuccessResponseStr)
            {
                OnWriteMessage(activeSimulatorItem.SourceCommand + activeSimulatorItem.Response + _endLine);
            }
            else
            {
                OnWriteMessage("E" + activeSimulatorItem.Response + _endLine);
            }
        }
    }

    public class TazmoRobotSimulator : CommonSerialPortDeviceSimulator
    {
        static string _enq = ((char)0x05).ToString();
        static string _ack = ((char)0x06).ToString();
        static string _nak = ((char)0x15).ToString();
        static string _busy = ((char)0x11).ToString();
        static string _cr = ((char)0x0D).ToString();
        static string _endLine = ((char)0x0A).ToString();

        public TazmoRobotSimulator(string port, string deviceName) : base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            string content = msg.Remove(msg.Length - 1);
            var contentArray = content.Split(',');
            if (contentArray.Length > 0)
            {
                foreach (var simulatorItem in IOSimulatorItemList)
                {
                    if (simulatorItem.SourceCommand != null && contentArray[0] == simulatorItem.SourceCommand)
                    {
                        return simulatorItem;
                    }
                }
            }
            return null;
        }


        //1) action:  ack & completeEvent==command      +  send ack after complete process
        //2) query:  command,parameterlist,,,respone
        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName == "SendError")
            {
                OnWriteMessage("ERR," + activeSimulatorItem.Response + _endLine);
                return;
            }
            if (activeSimulatorItem.SourceCommandName.Contains("Read"))
            {
                if (activeSimulatorItem.Response != null)
                    OnWriteMessage(activeSimulatorItem.SourceCommand + "," + activeSimulatorItem.Response + _endLine);
                else
                    OnWriteMessage(activeSimulatorItem.SourceCommand + _endLine);
            }
            else
            {
                OnWriteMessage(_ack);
                if (activeSimulatorItem.Response == "CompleteEvent")
                {
                    Thread.Sleep(1000);
                    OnWriteMessage(activeSimulatorItem.SourceCommand + _endLine);
                }
            }
        }
    }

    public class SiasunVCESimulator : CommonSerialPortDeviceSimulator
    {
        public SiasunVCESimulator(string port, string deviceName) : base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            //id,E
            //id,A,DC
            string content = msg.Remove(msg.Length - 1);
            var contentArray = content.Split(',');

            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (contentArray.Length == 2)
                {
                    if (simulatorItem.SourceCommandType != null && contentArray[1] == simulatorItem.SourceCommandType)
                    {
                        return simulatorItem;
                    }
                }
                else if (contentArray.Length > 2)
                {
                    if (simulatorItem.SourceCommandType != null && contentArray[1] == simulatorItem.SourceCommandType &&
                        simulatorItem.SourceCommand != null && contentArray[2] == simulatorItem.SourceCommand)
                    {
                        return simulatorItem;
                    }
                }
            }

            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            //if (activeSimulatorItem.Response.First() == '{')
            //{
            //    var response = activeSimulatorItem.Response.Substring(1, activeSimulatorItem.Response.Length - 2);
            //    OnWriteMessage("00," + response + "\r");
            //}
            //else
            //{
            //    var responseArray = activeSimulatorItem.Response.Split(',');
            //    foreach (var response in responseArray)
            //    {
            //        OnWriteMessage("00," + response + "\r");
            //        Thread.Sleep(1000);
            //    }
            //}

            if(activeSimulatorItem.SourceCommandType == "A" || activeSimulatorItem.SourceCommandType == "S" || activeSimulatorItem.SourceCommandType == "P")
            {
                OnWriteMessage("M," + activeSimulatorItem.Response + "\r");
            }
            else if (activeSimulatorItem.SourceCommandType == "R")
            {
                OnWriteMessage("X," + activeSimulatorItem.Response + "\r");
            }
        }
    }



    public class BrooksVCESimulator : CommonSerialPortDeviceSimulator
    {
        public BrooksVCESimulator(string port, string deviceName) : base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            //id,E
            //id,A,DC
            string content = msg.Remove(msg.Length - 1);
            var contentArray = content.Split(',');

            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (contentArray.Length == 2)
                {
                    if (simulatorItem.SourceCommandType != null && contentArray[1] == simulatorItem.SourceCommandType)
                    {
                        return simulatorItem;
                    }
                }
                else if (contentArray.Length > 2)
                {
                    if (simulatorItem.SourceCommandType != null && contentArray[1] == simulatorItem.SourceCommandType &&
                        simulatorItem.SourceCommand != null && contentArray[2] == simulatorItem.SourceCommand)
                    {
                        return simulatorItem;
                    }
                }
            }

            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.Response.First() == '{')
            {
                var response = activeSimulatorItem.Response.Substring(1, activeSimulatorItem.Response.Length - 2);
                OnWriteMessage("00," + response + "\r");
            }
            else
            {
                var responseArray = activeSimulatorItem.Response.Split(',');
                foreach (var response in responseArray)
                {
                    OnWriteMessage("00," + response + "\r");
                    Thread.Sleep(1000);
                }
            }
        }
    }

    internal class HanbellPumpSimulator : CommonSerialPortDeviceSimulator
    {
        public HanbellPumpSimulator(string port, string deviceName) : base(port, deviceName,false)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg[1] == byte.Parse(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(activeSimulatorItem.Response.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
        }
    }

    public class HirataR4Simulator : CommonSerialPortDeviceSimulator
    {
        private string _address = "002";
        protected static char _stx = '\u0002';
        protected static char _etx = '\u0003';
        protected static char _space = '\u0020';

        private int _lpIndex = 1;

        public HirataR4Simulator(string port, string deviceName) : base(port, deviceName,false,"\0")
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            IOSimulatorItemViewModel theSimulatorItem = null;
            int maxMatachLength = -1;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                int iIndex = msg.IndexOf(simulatorItem.SourceCommand);
                if(iIndex > 0)
                {
                    int matchLength = simulatorItem.SourceCommand.Length;
                    if (matchLength > maxMatachLength)
                    {
                        maxMatachLength = matchLength;
                        theSimulatorItem = simulatorItem;
                    }
                }

            }
            return theSimulatorItem;
        }


        List<byte> _cache = new List<byte>();
        protected override void ProcessUnsplitMessage(byte[] binaryMessage)
        {
            _cache.AddRange(binaryMessage);
            if (!_cache.Contains((byte) (3)))
                return;

            string message = Encoding.ASCII.GetString(_cache.ToArray());

            _cache.Clear();

            OnReadMessage(message);

            if (message.Contains("GP 1"))
                _lpIndex = 1;
            if (message.Contains("GP 2"))
                _lpIndex = 2;
            if (message.Contains("GP 3"))
                _lpIndex = 3;
            if (message.Contains("GP 4"))
                _lpIndex = 4;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(BuildMessage(activeSimulatorItem.SourceCommand, activeSimulatorItem.Response) + "\0");
        }

        private string BuildMessage(string command, string response)
        {
            if (command == "LE 100")
            {
                if (_lpIndex == 1)
                    response = Convert.ToInt32("1111111111111111111111111", 2).ToString();
                if (_lpIndex == 2)
                    response = "0";
                if (_lpIndex == 3)
                    response = Convert.ToInt32("1100000000000000000000000", 2).ToString();
                if (_lpIndex == 4)
                    response = Convert.ToInt32("0000000000000000000000011", 2).ToString();

            }
            if (command == "LE 101")
            {
                if (_lpIndex == 1)
                    response = "0";
                if (_lpIndex == 2)
                    response = "0";
                if (_lpIndex == 3)
                    response = "0";
                if (_lpIndex == 4)
                    response = "0";
            }

            string msg1 = $"{_address} {response}{_etx}";
            if(response.First() == '-')
            {
                msg1 = $"{_address}{response}{_etx}";
            }

            byte lrc = CalcLRC(Encoding.ASCII.GetBytes(msg1).ToList());
            string msg = $"{_stx}{msg1}{(char)(lrc)}";
            return msg;
        }
        private static byte CalcLRC(List<byte> data)
        {
            byte ret = 0x00;
            for (var i = 0; i < data.Count; i++)
            {
                ret ^= data[i];
            }
            return ret;
        }

    }

    public class BrooksSMIFSimulator : CommonSerialPortDeviceSimulator
    {
        private bool _isPodPresent = true;
        private DeviceTimer _timer = new DeviceTimer();
        private int _motionTime = 3000;//ms
        public BrooksSMIFSimulator(string port, string deviceName) : base(port, deviceName, true, "\r\n")
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            string content = msg.Remove(msg.Length - 2);
            var contentArray = content.Split(' ');
            if (contentArray.Length < 2)
                return null;

            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (contentArray[0] == simulatorItem.SourceCommandType)
                {
                    if(simulatorItem.SourceCommand == null)
                    {
                        return simulatorItem;
                    }
                    else if(contentArray[1] == simulatorItem.SourceCommand)
                    {
                        return simulatorItem;
                    }
                }
            }

            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            string newLine = "\r\n";
            string commandType = activeSimulatorItem.SourceCommandType;
            if (activeSimulatorItem.SourceCommandName.ToUpper() == "LOAD")
                _isPodPresent = true;
            if (activeSimulatorItem.SourceCommandName.ToUpper() == "UNLOAD")
                _isPodPresent = true;

            if(activeSimulatorItem.SourceCommandName.ToUpper() == "LOAD" ||
                activeSimulatorItem.SourceCommandName.ToUpper() == "UNLOAD" ||
                activeSimulatorItem.SourceCommandName.ToUpper() == "HOME" ||
                activeSimulatorItem.SourceCommandName.ToUpper() == "RECOVERY")
            {
                if (_timer.IsIdle())
                    _timer.Stop();
                _timer.Start(0);
            }

            if (commandType == "AERS" || commandType == "ARS")
            {
                OnWriteMessage(commandType + " " + activeSimulatorItem.Response + newLine);
            }
            else if (commandType == "ECR")
            {
                OnWriteMessage("ECD " + activeSimulatorItem.Response + newLine);
            }
            else if (commandType == "FSR")
            {
                var response = activeSimulatorItem.Response;
                if (_isPodPresent)
                    response += " PIP=TRUE";
                else
                    response += " PIP=FALSE";

                if(_timer.IsIdle() || _timer.GetElapseTime() >= _motionTime)
                    response += " READY=TRUE";
                else
                    response += " READY=FALSE";
                OnWriteMessage(response + newLine);
            }
            else
            {
                if (activeSimulatorItem.Response == "OK")
                {
                    OnWriteMessage("HCA OK" + newLine);
                }
                else
                {
                    var resArray = activeSimulatorItem.Response.Split('|');
                    int iIndex = new Random().Next(0, resArray.Length - 1);
                    OnWriteMessage("HCA " + resArray[iIndex] + newLine);
                }
            }         
        }

    }

}
