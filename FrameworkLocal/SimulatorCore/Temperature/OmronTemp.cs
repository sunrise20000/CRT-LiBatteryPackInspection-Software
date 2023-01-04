using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Temperature
{
    class OmronTemp : SerialPortDeviceSimulator
    {
        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        public event Action<string> receiveMsg;

        public bool IsSetting { get; set; }

        public bool IsActual { get; set; }      
        public int ActualTemp { get; set; }
        public int SettingTemp { get; set; }

        private object _locker = new object();

        public OmronTemp(string port)
          : base(port, -1, "\r", ' ', false)
        {
           // ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

            IsActual = false;
            IsSetting = false;
            SettingTemp = 50;
            ActualTemp = 60;

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
            //if (message.Length != 8)
            //{ return; }
            //var arry = ModRTU_CRC(message.Take(6).ToArray()).Skip(2).ToArray();
            //if (CheckModRTU_CRC(arry) != CheckModRTU_CRC(message.Skip(2).ToArray()) )
            //{ return; }
            base.ProcessUnsplitMessage(message);
            byte deviceId = message[0];
            byte command = (byte)message[1];
            byte AddressHigh = (byte)(message[2]);
            byte AddressLow = message[3];
            switch (command)
            {         
                case 0x03:
                    switch (AddressLow)
                    {
                        case 0x00:
                            {

                                // if(IsActual)
                                var index =(deviceId-1)*4+ (AddressHigh /2-1);
                                ActualTemp = Act[index] +5;
                                byte high = (byte)(ActualTemp >> 8 & 0xff);
                                byte low = (byte)(ActualTemp & 0xff);
                                byte[] speedmsg = new byte[] { deviceId, 0x03, 0x02, high, low};
                                speedmsg = ModRTU_CRC(speedmsg);
                                OnWriteMessage(speedmsg);
                            }
                            break;
                        case 0x40:
                            {
                                var index = (deviceId - 1) * 4 + (AddressHigh / 2 - 1);
                                SettingTemp = Setting[index]; ;
                                byte high = (byte)(SettingTemp >> 8 & 0xff);
                                byte low = (byte)(SettingTemp & 0xff);
                                byte[] speedmsg = new byte[] { deviceId, 0x03, 0x02, high, low };
                                speedmsg = ModRTU_CRC(speedmsg);
                                OnWriteMessage(speedmsg);
                            }
                            break;

                    }
                    break;
                case 0x10:
                    {
                        var index = (deviceId - 1) * 4 + (AddressHigh / 2 - 1);
                        var value = BitConverter.ToInt16(new byte[] {message[8],message[7] },0);
                        
                        Act[index]= value;
                        Setting[index] = value;
                        OnWriteMessage(ModRTU_CRC(message.Take(6).ToArray()));
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
        public int[] Act = new int[12] {30,30,30,40,50,30,45,36,48,90,20,45};
        public int[] Setting= new int[12] { 30, 30, 30, 40, 50, 30, 45, 36, 48, 90, 20, 45 };
    }
}
