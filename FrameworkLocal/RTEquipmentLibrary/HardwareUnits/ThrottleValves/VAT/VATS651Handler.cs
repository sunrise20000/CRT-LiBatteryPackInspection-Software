using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.ThrottleValves.VAT
{
    public abstract class VATS651Handler : HandlerBase
    {
        public VATS651 Device { get; }

        public string _command;
        protected string _parameter;


        protected VATS651Handler(VATS651 device, string command, string parameter = null)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        private static string _endLine = "\r\n";
        private static string BuildMessage( string command, string parameter)
        {
            string commandContent = parameter == null ? $"{command}" : $"{command}{parameter}";
            return commandContent + _endLine.ToString();
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            VATS651Message response = msg as VATS651Message;
            ResponseMessage = msg;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
                transactionComplete = true;
                return false;
            }
            Device.NoteError(null);

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                if(response.Data.IndexOf(_command)==0)
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                    return true;
                }
            }
            transactionComplete = false;
            return false;
        }

    }

    public class VATS651RawCommandHandler : VATS651Handler
    {
        public VATS651RawCommandHandler(VATS651 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                var result = msg as VATS651Message;
                Device.NoteRawCommandInfo(_command, result.RawMessage);
            }
            return true;
        }
    }

    public class VATS651SimpleActionHandler : VATS651Handler
    {
        public VATS651SimpleActionHandler(VATS651 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteActionCompleted(_command);
            }
            return true;
        }
    }
    public class VATS651SimpleSetHandler : VATS651Handler
    {
        public VATS651SimpleSetHandler(VATS651 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetCompleted(_command, _parameter);
            }
            return true;
        }
    }
    public class VATS651SimpleQueryHandler : VATS651Handler
    {
        public VATS651SimpleQueryHandler(VATS651 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                Device.NoteQueryResult(_command, result);
            }
            return true;
        }

    }

}
