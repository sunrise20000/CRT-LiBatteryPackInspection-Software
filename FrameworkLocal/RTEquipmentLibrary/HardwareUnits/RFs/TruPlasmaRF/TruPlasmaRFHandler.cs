using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.TruPlasmaRF
{
    public abstract class TruPlasmaRFHandler : HandlerBase
    {
        public TruPlasmaRF Device { get; }

        public string _command;
        protected string _parameter;


        protected TruPlasmaRFHandler(TruPlasmaRF device, string command, string parameter = null)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        protected TruPlasmaRFHandler(TruPlasmaRF device, string command, byte[] parameter)
        : base(BuildMessage(ToByteArray(command), parameter))
        {
            Device = device;
            _command = command;
            //_parameter = parameter;
            Name = command;
        }

        private static byte _start = 0xAA;
        private static byte _address = 0x02;
        private static byte _GS = 0x00;
        private static byte _stop = 0x55;

        private static byte[] BuildMessage(byte[] commandArray, byte[] argumentArray)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(_start);
            buffer.Add(_address);
            int length = 1 + (commandArray != null ? commandArray.Length : 0) + (argumentArray != null ? argumentArray.Length : 0);
            buffer.Add((byte)length);
            buffer.Add(_GS);

            if (commandArray != null && commandArray.Length > 0)
            {
                //skip ManualExecuteCommand
                if (!(commandArray.Length == 1 && commandArray[0] == 0))
                {
                    buffer.AddRange(commandArray);
                }
            }
            if (argumentArray != null && argumentArray.Length > 0)
            {
                buffer.AddRange(argumentArray);
            }
            var contentBuffer = buffer.Skip(3).Take(buffer.Count - 3).ToArray();
            //var checkSum = Crc16.CRC16_CCITT(contentBuffer);
            var checkSum = Crc16.Crc16Ccitt(contentBuffer);

            buffer.AddRange(BitConverter.GetBytes(checkSum));

            buffer.Add(_stop);

            return buffer.ToArray();
        }

        protected static string BuildCommandParameter(string jsonParameter)
        {
            var truArgObject = JsonConvert.DeserializeObject<TruPlasmaRFArugment>(jsonParameter);

            JObject jsonObject = JObject.Parse(jsonParameter);
            List<string> commandList = new List<string>();
            commandList.Add(truArgObject.Command);
            foreach(var dob in truArgObject.DOBList)
            {
                if(dob.Index != null)
                {
                    commandList.Add(dob.Index.Substring(0,2));
                    commandList.Add(dob.Index.Substring(2));
                }
                if (dob.SubIndex != null)
                {
                    commandList.Add(dob.SubIndex);
                }
                if (dob.Status != null)
                {
                    commandList.Add(dob.Status);
                }
                if (dob.Type != null)
                {
                    commandList.Add(dob.Type);
                }
                if (dob.Data != null)
                {
                    var charArray = dob.Data.ToArray();
                    for (int i = 0; i < charArray.Length - 1; i += 2)
                    {
                        commandList.Add(charArray[i].ToString() + charArray[i + 1].ToString());
                    }
                }
            }
            return string.Join(",", commandList.ToArray());
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TruPlasmaRFMessage response = msg as TruPlasmaRFMessage;
            ResponseMessage = msg;

            if (response.IsError)
            {
                Device.NoteError(response.ErrorCode);
            }
            else
            {
                Device.NoteError(null);
            }

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }


            if (this.IsAcked && response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

        protected static byte[] ToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }
        protected static byte[] IntToByteArray(int num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        protected int ByteArrayToInt(byte[] bytes)
        {
            int temp = BitConverter.ToInt32(bytes, 0);
            return temp;
        }
    }


    public class TruPlasmaRFExecuteAnyCommandHandler : TruPlasmaRFHandler
    {
        public TruPlasmaRFExecuteAnyCommandHandler(TruPlasmaRF device, string jsonParameter)
            : base(device, BuildCommandParameter(jsonParameter))
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRFMessage;
                Device.NoteAnyCommandResult(string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRFRawCommandHandler : TruPlasmaRFHandler
    {
        public TruPlasmaRFRawCommandHandler(TruPlasmaRF device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRFMessage;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRFGetControlHandler : TruPlasmaRFHandler
    {
        public TruPlasmaRFGetControlHandler(TruPlasmaRF device)
            : base(device, "05,01,00,00", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRFMessage;
                Device.NoteInterfaceActived(true);
            }
            return true;
        }
    }
    public class TruPlasmaRFReleaseControlHandler : TruPlasmaRFHandler
    {
        public TruPlasmaRFReleaseControlHandler(TruPlasmaRF device)
            : base(device, "05,02,00,00", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRFMessage;
                Device.NoteInterfaceActived(false);
            }
            return true;
        }
    }
    public class TruPlasmaRFPreSetPiValueHandler : TruPlasmaRFHandler
    {
        public TruPlasmaRFPreSetPiValueHandler(TruPlasmaRF device, int piValue)
            : base(device, "02,06,00,01", IntToByteArray(piValue))
        {
        }


        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRFMessage;
                Device.NoteSetPiValueCompleted(true);
            }
            return true;
        }
    }
    public class TruPlasmaRFReadPiValueHandler : TruPlasmaRFHandler
    {
        public TruPlasmaRFReadPiValueHandler(TruPlasmaRF device)
            : base(device, "01,12,00,01", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRFMessage;
                Device.NotePiValue(ByteArrayToInt(result.RawMessage.Skip(10).Take(4).ToArray()));
            }
            return true;
        }
    }
}
