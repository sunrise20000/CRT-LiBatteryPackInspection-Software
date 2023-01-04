using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.DRYVacuum
{
    public class ErrorInfo
    {
        public int Index;
        public string ErrorName;
        public int Level;
        public bool IsError;
        public R_TRIG _trigError = new R_TRIG();
    }
    public class DRYVacuumPumpHandler : HandlerBase
    {
        public DRYVacuumPump Device { get; }

        public string _command;
        protected string _parameter;


        protected DRYVacuumPumpHandler(DRYVacuumPump device, string command, string parameter)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        private static byte STX = 2;//"0X02"
        private static byte ETX = 3;//"0X03"
        private static byte CR = 13;//"0X0D"
        private static byte[] BuildMessage(string command, string parameter)
        {
            var buffer = new byte[8];
            buffer[0] = STX;
            buffer[1] = Convert.ToByte(command[0]);
            buffer[2] = Convert.ToByte(command[1]);
            buffer[3] = Convert.ToByte(command[2]);
            buffer[4] = ETX;
            var CheckLow = SUMCHECK(command);
            buffer[5] = Convert.ToByte(CheckLow[0]);
            buffer[6] = Convert.ToByte(CheckLow[1]);   
            buffer[7] = CR;
            return buffer;
        }
        public static string SUMCHECK(string command)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(command);
            int sum = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                sum += Convert.ToInt16(buffer[i]);
            }
            var SUM = STX + sum + ETX;

            return (SUM & 0xff).ToString("X2");
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            DRYVacuumPumpMessage response = msg as DRYVacuumPumpMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);

                if (msg.IsError)
                {
                    // Device.NoteError($"Command '{_command}' Error: {response.Data}:{ErrorString(response.ErrorText)}");
                }
                else
                {
                    SetState(EnumHandlerState.Completed);
                    handled = true;
                    Device.NoteError(null);
                    return true;
                }
            }

            handled = false;
            return false;
        }  
    }
        public class DRYVacuumPumpQueryStatusHandler : DRYVacuumPumpHandler
        {
            public DRYVacuumPumpQueryStatusHandler(DRYVacuumPump pump)
                : base(pump, "M21", "")
            {
                Name = "Query DRY Pump Status";
            }
            public override bool HandleMessage(MessageBase msg, out bool handled)
            {
                if (base.HandleMessage(msg, out handled))
                {
                    DRYVacuumPumpMessage response = msg as DRYVacuumPumpMessage;
                    var msgArry = response.Data;
                    if (msgArry.Length == 19)
                    {
                        var str = Encoding.ASCII.GetString(msgArry);
                        if (str[0] == 'N')
                            Device.RunMode = "Normal operation mode";
                        else Device.RunMode = " Power-saving operation mode";
                        if (str[1] == 'R')
                        {
                            Device.MPOn = "being operated";
                            Device.IsMPOn = true;
                        }
                        else
                        {
                            Device.MPOn = "being stopped";
                            Device.IsMPOn = false;
                        }
                        if (str[2] == 'R')
                        {
                            Device.BPOn = "being operated";
                            Device.IsBPOn = true;
                        }
                        else
                        {
                            Device.BPOn = "being stopped";
                            Device.IsBPOn = false;
                        }
                        var warn = str.Substring(3, 8);
                        var alarm = str.Substring(11, 8);
                     if (!warn.Equals("00000000"))
                     {
                        Device.IsWarning = true;
                        for (int i = warn.Length / 2; i > 0; i--)
                        {
                            var bt = Convert.ToByte(warn.Substring(i * 2 - 2, 2), 16);
                            for (int j = 0; j < 8; j++)
                            {
                                var error = Device.errorInfo.FirstOrDefault(m => m.Index == j + (warn.Length / 2 - i) * 8);
                                if (error != null)
                                {
                                    var sbt = (byte)((bt >> j) & 0x1) == 1;
                                    if (sbt)
                                    {
                                        error.IsError = true;
                                        error._trigError.CLK = true;
                                        Device.NoteWarning(error);
                                    }
                                    else
                                    {
                                        error.IsError = false;
                                        error._trigError.CLK = false;
                                        Device.NoteWarning(error);
                                    }
                                }
                            }
                        }      
                      }else Device.IsWarning = false;
                    if (!alarm.Equals("00000000"))
                    {
                        Device.IsAlarm = true;
                        for (int i = alarm.Length / 2; i > 0; i--)
                        {
                            var bt = Convert.ToByte(alarm.Substring(i * 2 - 2, 2), 16);
                            for (int j = 0; j < 8; j++)
                            {
                                var error = Device.errorInfo.FirstOrDefault(m => m.Index == j + (alarm.Length / 2 - i) * 8 + 50);
                                if (error != null)
                                {
                                    var sbt = (byte)((bt >> j) & 0x1) == 1;
                                    if (sbt)
                                    {
                                        error.IsError = true;
                                        error._trigError.CLK = true;
                                        Device.NoteAlarm(error);
                                    }
                                    else
                                    {
                                        error.IsError = false;
                                        error._trigError.CLK = false;
                                        Device.NoteAlarm(error);
                                    }
                                }
                            }
                        }
                    }
                    else Device.IsAlarm = false;
                }
                    Device.NoteSetParaCompleted();
                }
                return true;
            }
        }
    }


