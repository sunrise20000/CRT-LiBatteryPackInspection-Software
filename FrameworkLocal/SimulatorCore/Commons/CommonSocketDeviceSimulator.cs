using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class SocketDeviceSimulatoFactory
    {
        public static CommonSocketDeviceSimulator GetCommonSocketDeviceSimulator(int port, string deviceName)
        {
            if (deviceName == "Hanbell")
            {
                return new HanbellPumpSocketSimulator(port, deviceName);
            }
            else if (deviceName == "SiasunPhoenixB")
            {
                return new SiasunPhoenixBSocketSimulator(port, deviceName);
            }
            else if (deviceName == "Siasun1500C800C")
            {
                return new Siasun1500C800CSocketSimulator(port, deviceName);
            }
            return null;
        }
    }


    public class CommonSocketDeviceSimulator : SimpleSocketDeviceSimulator
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
        public CommonSocketDeviceSimulator(int port, string deviceName, bool isAscii = true, string newLine = "\r")
            : base(port, -1, newLine, ',', isAscii)
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


        protected override void ProcessUnsplitMessage(byte[] binaryMessage)
        {
            lock (_locker)
            {
                var activeSimulatorItem = GetActiveIOSimulatorItemViewModel(binaryMessage);
                if (activeSimulatorItem == null) return;

                activeSimulatorItem.CommandContent = string.Join(",", binaryMessage.Select(bt => bt.ToString("X2")).ToArray());
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

                if (AutoReply)
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

    public class SiasunPhoenixBSocketSimulator : CommonSocketDeviceSimulator
    {
        private bool _isWaferPresent = false;
        private string _endline = "\r\n";
        private readonly Dictionary<string, int> _timeConfigs = new Dictionary<string, int>();

        public SiasunPhoenixBSocketSimulator(int port, string deviceName) : base(port, deviceName)
        {
            //try
            //{
            //    Hashtable timeSim = (Hashtable)ConfigurationManager.GetSection("VacRobotSim");

            //    _timeConfigs.Add("PICK", int.Parse(timeSim["PICK"].ToString()) * 1000);
            //    _timeConfigs.Add("PLACE", int.Parse(timeSim["PLACE"].ToString()) * 1000);
            //    _timeConfigs.Add("GOTO", int.Parse(timeSim["GOTO"].ToString()) * 1000);
            //    _timeConfigs.Add("RQLOAD", int.Parse(timeSim["RQLOAD"].ToString()) * 1000);
            //    _timeConfigs.Add("CHECKLOAD", int.Parse(timeSim["CHECKLOAD"].ToString()) * 1000);
            //    _timeConfigs.Add("HOME", int.Parse(timeSim["HOME"].ToString()) * 1000);
            //}
            //catch (ConfigurationErrorsException ex)
            //{
            //    throw ex;
            //}
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

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeItem)
        {
            string cmdName = activeItem.SourceCommandName;
            if (cmdName.StartsWith("RQ"))
            {
                if (cmdName.StartsWith("RQ LOAD"))
                {
                    //Thread.Sleep(_timeConfigs["RQLOAD"]);
                    string response = $"{activeItem.Response} {(activeItem.CommandContent.Contains(" A") ? "A" : "B")} {(_isWaferPresent ? "ON" : "OFF")}";
                    OnWriteMessage(response + _endline + "_RDY" + _endline);
                }
                else
                {
                    OnWriteMessage(activeItem.Response + _endline + "_RDY" + _endline);
                }

                //OnWriteMessage("_RDY" + _endline);
                return;
            }
            else if (cmdName.StartsWith("CHECK LOAD"))
            {
                Thread.Sleep(1000);
            }
            else if (cmdName.StartsWith("HOME"))
            {
                Thread.Sleep(2000);
            }
            else if (cmdName.StartsWith("RESET"))
            {
                return;
            }
            else if (cmdName.StartsWith("PICK"))
            {
                Thread.Sleep(2000);
            }
            else if (cmdName.StartsWith("PLACE"))
            {
                Thread.Sleep(2000);
            }
            else if (cmdName.StartsWith("GOTO"))
            {
                Thread.Sleep(1500);
            }

            OnWriteMessage(activeItem.Response + _endline);
        }
    }

    public class Siasun1500C800CSocketSimulator : CommonSocketDeviceSimulator
    {
        public Siasun1500C800CSocketSimulator(int port, string deviceName) : base(port, deviceName)
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

    internal class HanbellPumpSocketSimulator : CommonSocketDeviceSimulator
    {
        private List<byte> _msgBuffer = new List<byte>();
        private bool _isPumpOn;
        private List<byte> statusArray;
        public HanbellPumpSocketSimulator(int port, string deviceName) : base(port, deviceName, false)
        {
            statusArray = Enumerable.Repeat((byte)0x0,90).ToList();
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            _msgBuffer.AddRange(msg);
            if (_msgBuffer.Count < 12)
            {
                return null;
            }
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg[7] == byte.Parse(simulatorItem.SourceCommand))
                {
                    //action
                    if (msg[7] == 5)
                    {
                        simulatorItem.Response = string.Join(",", msg.Take(12).Select(bt => bt.ToString("X2")).ToArray());
                    }

                    _msgBuffer.RemoveRange(0, 12);
                    return simulatorItem;
                }
            }
            return null;
        }
        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            var responseArray = activeSimulatorItem.Response.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray();
            if(activeSimulatorItem.SourceCommandName == "OperatePump")
            {
                var response = activeSimulatorItem.Response.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray();
                _isPumpOn = response[10] == 0xFF;
                OnWriteMessage(response);
            }
            if (activeSimulatorItem.SourceCommandName == "RequestRegisters")
            {
                List<byte> buffer = new List<byte>();
                buffer.AddRange(activeSimulatorItem.Response.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
                statusArray[61] = (byte)(_isPumpOn ? 0xdc : 0x7f);
                buffer.AddRange(statusArray);
                OnWriteMessage(buffer.ToArray());
            }
        }
    }

}
