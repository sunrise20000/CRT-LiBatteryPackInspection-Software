using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Breakers
{
    class NSXBreaker : SerialPortDeviceSimulator
    {
        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        public event Action<string> receiveMsg;

        public bool IsACurrent { get; set; }
        public bool IsBCurrent { get; set; }
        public bool IsCCurrent { get; set; }
        public bool IsAActive{ get; set; }
        public bool IsBActive { get; set; }
        public bool IsCActive { get; set; }

        public bool IsAReactive { get; set; }
        public bool IsBReactive { get; set; }
        public bool IsCReactive { get; set; }
        public int APhaseCurrent { get; set; }
        public int BPhaseCurrent { get; set; }
        public int CPhaseCurrent { get; set; }
        public int AActivePower { get; set; }
        public int BActivePower { get; set; }
        public int CActivePower { get; set; }
        public int AReactivePower { get; set; }
        public int BReactivePower { get; set; }
        public int CReactivePower { get; set; }

        private object _locker = new object();

        public NSXBreaker(string port)
          : base(port, -1, "\r", ' ', false,"")
        {
            // ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

            IsACurrent = false;
            IsBCurrent = false;
            IsCCurrent = false;

            IsAActive = false;
            IsBActive = false;
            IsCActive = false;

            IsAReactive = false;
            IsBReactive = false;
            IsCReactive = false;

            APhaseCurrent = 500;
            BPhaseCurrent = 600;
            CPhaseCurrent = 555;
            AActivePower = 700;
            BActivePower = 650;
            CActivePower = 680;
            AReactivePower = 680;
            BReactivePower = 660;
            CReactivePower = 540;

        }
        private byte GetRandomValue()
        {
            Thread.Sleep(30);
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
            switch (command)
            {
                case 0x03:
                    {
                        byte[] speedmsg = new byte[53];
                        speedmsg[0] = 0x01;
                        speedmsg[1] = 0x03;
                        speedmsg[2] = 0x30;
                        if (IsACurrent)
                            APhaseCurrent = GetRandomValue();
                        speedmsg[3] = (byte)(APhaseCurrent >> 8 & 0xff);
                        speedmsg[4] = (byte)(APhaseCurrent & 0xff);

                        if (IsBCurrent)
                            BPhaseCurrent = GetRandomValue();
                        speedmsg[5] = (byte)(BPhaseCurrent >> 8 & 0xff);
                        speedmsg[6] = (byte)(BPhaseCurrent & 0xff);

                        if (IsCCurrent)
                            CPhaseCurrent = GetRandomValue()+50;
                        speedmsg[7] = (byte)(CPhaseCurrent >> 8 & 0xff);
                        speedmsg[8] = (byte)(CPhaseCurrent & 0xff);

                        if (IsAActive)
                            AActivePower = GetRandomValue()+80;
                        speedmsg[39] = (byte)(AActivePower >> 8 & 0xff);
                        speedmsg[40] = (byte)(AActivePower & 0xff);

                        if (IsBActive)
                            BActivePower = GetRandomValue();
                        speedmsg[41] = (byte)(BActivePower >> 8 & 0xff);
                        speedmsg[42] = (byte)(BActivePower & 0xff);

                        if (IsCActive)
                            CActivePower = GetRandomValue();
                        speedmsg[43] = (byte)(CActivePower >> 8 & 0xff);
                        speedmsg[44] = (byte)(CActivePower & 0xff);

                        if (IsAReactive)
                            AReactivePower = GetRandomValue();
                        speedmsg[45] = (byte)(AReactivePower >> 8 & 0xff);
                        speedmsg[46] = (byte)(AReactivePower & 0xff);

                        if (IsBReactive)
                            BReactivePower = GetRandomValue();
                        speedmsg[47] = (byte)(BReactivePower >> 8 & 0xff);
                        speedmsg[48] = (byte)(BReactivePower & 0xff);

                        if (IsCReactive)
                            CReactivePower = GetRandomValue();
                        speedmsg[49] = (byte)(CReactivePower >> 8 & 0xff);
                        speedmsg[50] = (byte)(CReactivePower & 0xff);


                        speedmsg = ModRTU_CRC(speedmsg);
                        OnWriteMessage(speedmsg);
                        //ProcessWriteMessage(speedmsg);
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
