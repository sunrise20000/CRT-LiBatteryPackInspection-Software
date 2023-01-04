using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.PfeifferPumpA100
{
    public abstract class PfeifferPumpA100Handler : HandlerBase
    {
        public PfeifferPumpA100 Device { get; }

        public string _command;
        protected string _parameter;


        protected PfeifferPumpA100Handler(PfeifferPumpA100 device, string command,string parameter)
            : base(BuildMessage(device.Address, command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        private static string _startLine = "#";
        private static string _endLine = "\r";
        private static string BuildMessage(string address, string command, string parameter)
        {
            if(string.IsNullOrEmpty(parameter))
                return _startLine + address + command + _endLine;
            else
                return _startLine + address + command + parameter + _endLine;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            PfeifferPumpA100Message response = msg as PfeifferPumpA100Message;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);

                if (response.Address != Device.Address)
                {
                    Device.NoteError("Invalid address");
                    msg.IsFormatError = true;
                }
                else if (response.Data.Contains("ERR"))
                {
                    Device.NoteError($"Command '{_command}' Error: {response.Data}");
                    msg.IsError = true;
                }
                else
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                    Device.NoteError(null);
                    return true;
                }
            }

            transactionComplete = false;
            return false;
        }

    }

    public class PfeifferPumpA100RawCommandHandler : PfeifferPumpA100Handler
    {
        public PfeifferPumpA100RawCommandHandler(PfeifferPumpA100 device, string command, string parameter)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                var result = msg as PfeifferPumpA100Message;
                Device.NoteRawCommandInfo(_command, result.RawMessage);
            }
            return true;
        }


    }

    public class PfeifferPumpA100SimpleSwitchHandler : PfeifferPumpA100Handler
    {
        public PfeifferPumpA100SimpleSwitchHandler(PfeifferPumpA100 device, string command, string parameter)
            : base(device, command, parameter)
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                Device.NoteSwitchCompleted(_command, _parameter.Contains("ON"));
            }
            return true;
        }
    }

    public class PfeifferPumpSetPumpParameterHandler : PfeifferPumpA100Handler
    {
        public PfeifferPumpSetPumpParameterHandler(PfeifferPumpA100 device, string parameter)
            : base(device, "SET", parameter)
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetParaCompleted();
            }
            return true;
        }
    }

    public class PfeifferPumpA100ReadPumpStatusHandler : PfeifferPumpA100Handler
    {
        public PfeifferPumpA100ReadPumpStatusHandler(PfeifferPumpA100 device)
            : base(device, "STA", null)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as PfeifferPumpA100Message;

                if(result.Data.Length != 68)
                {
                    Device.NoteError("Invalid 'RunParameter'response, the length is not 68");
                }
                else
                {
                    Device.NotePumpStatus(result.Data);
                }
            }
            return true;

        }

    }

    public class PfeifferPumpA100ReadSetStateHandler : PfeifferPumpA100Handler
    {
        public PfeifferPumpA100ReadSetStateHandler(PfeifferPumpA100 device)
            : base(device, "LEV", null)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as PfeifferPumpA100Message;

                if (result.Data.Length != 136)
                {
                    Device.NoteError("Invalid 'RunParameter'response, the length is not 67");
                }
                else
                {
                    Device.NoteReadSetState(result.Data);
                }
            }
            return true;

        }

    }
}
