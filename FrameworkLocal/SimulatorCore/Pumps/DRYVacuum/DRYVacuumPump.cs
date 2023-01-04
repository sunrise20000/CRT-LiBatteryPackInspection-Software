using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Pumps.DRYVacuum
{
    class DRYVacuumPump : SerialPortDeviceSimulator
    {
        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        public event Action<string> receiveMsg;

        public bool IsRunS { get; set; }
        public bool IsMPS { get; set; }
        public bool IsBPS { get; set; }
        public bool IsAlarmRondom { get; set; }
        public bool IsAlarm { get; set; }


        private object _locker = new object();

        public DRYVacuumPump(string port)
          : base(port, -1, "\r", ' ', false)
        {
            // ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();
            IsRunS = false;
            IsMPS = false;
            IsBPS = false;
            IsAlarmRondom = false;
            IsAlarm = false;
        }
        private byte[] GetRandomValue()
        {
            Thread.Sleep(20);
            Random random = new Random();
            return Encoding.ASCII.GetBytes(random.Next(16).ToString());
        }
        private byte GetValue()
        {
            Random random = new Random();
            return (byte)random.Next(16);
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
            //var arry = ModRTU_CRC(message.Take(6).ToArray()).Skip(2).ToArray();
            //if (CheckModRTU_CRC(arry) != CheckModRTU_CRC(message.Skip(2).ToArray()))
            //{ return; }
            base.ProcessUnsplitMessage(message);
            byte prefix = message[0];
            var mw = message.Take(4);
            var mws = mw.Skip(1);
            string command = Encoding.ASCII.GetString(message.Take(4).Skip(1).ToArray());
            byte AddressHigh = (byte)(message[2]);
            byte AddressLow = message[3];
            switch (command)
            {
                case "M21":
                    {
                        byte[] speedmsg = new byte[27];
                        speedmsg[0] = message[0];
                        speedmsg[1] = message[1];
                        speedmsg[2] = message[2];
                        speedmsg[3] = message[3];
                        if (IsRunS)
                            speedmsg[4] = Encoding.ASCII.GetBytes("S")[0];
                        else speedmsg[4] = Encoding.ASCII.GetBytes("N")[0];

                        if (IsMPS)
                            speedmsg[5] = Encoding.ASCII.GetBytes("S")[0];
                        else speedmsg[5] = Encoding.ASCII.GetBytes("R")[0];
                        if (IsBPS)
                            speedmsg[6] = Encoding.ASCII.GetBytes("S")[0];
                        else speedmsg[6] = Encoding.ASCII.GetBytes("R")[0];
                        if (IsAlarmRondom)
                        {
                            speedmsg[7] = GetRandomValue()[0];
                            speedmsg[8] = GetRandomValue()[0];
                            speedmsg[9] = GetRandomValue()[0];
                            speedmsg[10] = GetRandomValue()[0];
                            speedmsg[11] = GetRandomValue()[0];
                            speedmsg[12] = GetRandomValue()[0];

                            speedmsg[13] = GetRandomValue()[0];

                            speedmsg[14] = GetRandomValue()[0];

                            speedmsg[15] = GetRandomValue()[0];

                            speedmsg[16] = GetRandomValue()[0];

                            speedmsg[17] = GetRandomValue()[0];

                            speedmsg[18] = GetRandomValue()[0];

                            speedmsg[19] = GetRandomValue()[0];

                            speedmsg[20] = GetRandomValue()[0];

                            speedmsg[21] = GetRandomValue()[0];

                            speedmsg[22] = GetRandomValue()[0];
                        }
                        else
                        {
                            if (IsAlarm)
                            {
                                speedmsg[7] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[8] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[9] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[10] = Encoding.ASCII.GetBytes("F")[0];
                                speedmsg[11] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[12] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[13] = Encoding.ASCII.GetBytes("2")[0];
                                speedmsg[14] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[15] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[16] = Encoding.ASCII.GetBytes("0")[0]; ;
                                speedmsg[17] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[18] = Encoding.ASCII.GetBytes("4")[0];
                                speedmsg[19] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[20] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[21] = Encoding.ASCII.GetBytes("2")[0];
                                speedmsg[22] = Encoding.ASCII.GetBytes("3")[0];
                            }
                            else
                            {
                                speedmsg[7] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[8] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[9] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[10] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[11] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[12] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[13] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[14] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[15] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[16] = Encoding.ASCII.GetBytes("0")[0]; ;
                                speedmsg[17] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[18] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[19] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[20] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[21] = Encoding.ASCII.GetBytes("0")[0];
                                speedmsg[22] = Encoding.ASCII.GetBytes("0")[0];
                            }
                        }

                        speedmsg[23] =0x03;
                        var CheckLow = SUMCHECK(speedmsg.Take(24).ToArray());
                        speedmsg[24] = Convert.ToByte(CheckLow[0]); ;
                        speedmsg[25] = Convert.ToByte(CheckLow[1]); ;
                        speedmsg[26] =0x0D;
                        OnWriteMessage(speedmsg);
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

        public static string SUMCHECK(byte[] buffer)
        {
           // byte[] buffer = Encoding.ASCII.GetBytes(command);
            int sum = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                sum += Convert.ToInt16(buffer[i]);
            }
            var SUM = 0x02 + sum + 0x03;

            return (SUM & 0xff).ToString("X2");
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
