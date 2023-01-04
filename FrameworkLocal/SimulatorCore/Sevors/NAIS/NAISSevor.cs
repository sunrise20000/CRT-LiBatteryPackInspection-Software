using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Sevors.NAIS
{
    class NAISSevor : SerialPortDeviceSimulator
    {
        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        public event Action<string> receiveMsg;

        //public bool IsSetting { get; set; }

        //public bool IsActual { get; set; }
        //public int ActualTemp { get; set; }
        //public int SettingTemp { get; set; }

        private object _locker = new object();

        public NAISSevor(string port)
          : base(port, -1, "\r", ' ', false)
        {
            // ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

            //IsActual = false;
            //IsSetting = false;
            //SettingTemp = 50;
            //ActualTemp = 60;

        }
        private byte GetActualTempValue()
        {
            Random random = new Random();
            return (byte)random.Next(0, 10000);
        }
        private byte GetSettingTempValue()
        {
            Random random = new Random();
            return (byte)random.Next(0, 10000);
        }
        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();

                    //IsAtSpeed = true;
                }
            }
        }
        protected override void ProcessUnsplitMessage(byte[] message)
        {
            if (message.Length != 8)
            { return; }
            var arry = ModRTU_CRC(message.Take(6).ToArray()).Skip(2).ToArray();
            if (CheckModRTU_CRC(arry) != CheckModRTU_CRC(message.Skip(2).ToArray()))
            { return; }
            base.ProcessUnsplitMessage(message);
            byte prefix = message[0];
            byte command = (byte)message[1];
            byte AddressHigh = (byte)(message[2]);
            byte AddressLow = message[3];
            byte value = message[4];
            switch (command)
            {
                case 0x01:
                    {
                        switch (AddressLow)
                        {
                            case 0xA1:
                                {
                                    var buffer = new byte[] { 0x01, 0x01, 0x01, 0x00 };
                                    var mess = ModRTU_CRC(buffer);
                                    OnWriteMessage(mess);
                                }
                                break;
                            case 0xA2:
                                {
                                    var buffer = new byte[] { 0x01, 0x01, 0x01, 0x01 };
                                    var mess = ModRTU_CRC(buffer);
                                    OnWriteMessage(mess);
                                }
                                break;
                            case 0x40:
                                {
                                    var buffer = new byte[] { 0x01, 0x01, 0x01, 0x00 };
                                    var mess = ModRTU_CRC(buffer);
                                    OnWriteMessage(mess);
                                }
                                break;
                        }
                    }
                    break;
                case 0x05:
                    {
                        switch (AddressLow)
                        {
                            case 0x60:
                                {

                                    OnWriteMessage(message);
                                    
                                    receiveMsg("set Sevor " +(value==0xff ?"ON":"OFF"));
                                }
                                break;
                            case 0x20:
                                {
                                    OnWriteMessage(message);
                                    receiveMsg("set Stb " + (value == 0xff ? "ON" : "OFF"));
                                }
                                break;

                        }
                        break;
                    }
                case 0x06:
                    {
                        OnWriteMessage(message);
                        receiveMsg("set blocl no " + message[5]);
                    }
                    break;
            }
        }
        public byte[] ModRTU_CRC(byte[] value)
        {
            var val = 0xFFFF;

            for (int i = 0; i < value.Length; i++)
            {
                val ^= value[i];
                for (int j = 0; j < 8; j++)
                {
                    var xdabit = (int)(val & 0x01);
                    val >>= 1;
                    if (xdabit == 1)
                        val ^= 0xA001;
                }
            }

            if (val == 0x0000)
                return value;
            var arg = value.ToList();
            arg.Add((byte)(val & 0xFF));
            arg.Add((byte)(val >> 8));
            return arg.ToArray();
        }
        private static byte CheckModRTU_CRC(byte[] buffer)
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
            return (byte)temp;
        }
    }
}
