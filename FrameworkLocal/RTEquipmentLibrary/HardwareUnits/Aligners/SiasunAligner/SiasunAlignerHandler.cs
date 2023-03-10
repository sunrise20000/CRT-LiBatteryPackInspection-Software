using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.SiasunAligner
{
    public abstract class SiasunAlignerHandler : HandlerBase
    {
        public SiasunAligner Device { get; }

        public string _command;
        protected string _parameter;


        protected SiasunAlignerHandler(SiasunAligner device, string command, string parameter = null)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        private static char _startLine = (char)1;
        private static char _endLine = (char)3;
        private static string BuildMessage( string command, string parameter)
        {
            command = "ALIG " + command;
            string commandContent = parameter == null ? $"{command}" : $"{command} {parameter}";

            string sLength = commandContent.Length.ToString("D2");
            var charArray = (sLength + commandContent).ToArray();
            int sum = charArray.Sum(chr => (int)chr);
            string sSum = sum.ToString("X");

            if (sSum.Length < 2)
                return _startLine + sLength + commandContent + "0" + sSum + _endLine.ToString();
            else
                return _startLine + sLength + commandContent + sSum.Substring(sSum.Length - 2) + _endLine.ToString();
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SiasunAlignerMessage response = msg as SiasunAlignerMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }


            if (response.IsResponse)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

    }

    public class SiasunAlignerRawCommandHandler : SiasunAlignerHandler
    {
        public SiasunAlignerRawCommandHandler(SiasunAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            Device.NoteRawCommandInfo(_command, result.RawMessage);

            ResponseMessage = msg;
            handled = true;

            return true;
        }


    }

    public class SiasunAlignerXferHandler : SiasunAlignerHandler
    {
        public SiasunAlignerXferHandler(SiasunAligner device, string jsonParameter)
            : base(device, "XFER", BuildCommandParameter(jsonParameter))
        {

        }
        static string BuildCommandParameter(string jsonParameter)
        {
            JObject jsonObject = JObject.Parse(jsonParameter);
            StringBuilder paraBuilder = new StringBuilder();
            foreach (var item in jsonObject)
            {
                if (item.Key.Contains("Slot") || item.Key == "Arm")
                    paraBuilder.Append(" " + item.Key.ToUpper() + " " + item.Value);
                else if(item.Value!=null && item.Value.ToString().Length > 0)
                    paraBuilder.Append(" " + item.Value);
            }

            return paraBuilder.Length > 0 ? paraBuilder.ToString() : null;

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteActionCompleted(_command);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }
    public class SiasunAlignerPickHandler : SiasunAlignerHandler
    {
        public SiasunAlignerPickHandler(SiasunAligner device, string jsonParameter)
            : base(device, "PICK", BuildCommandParameter(jsonParameter))
        {
        }

        //N 2 R EX Z DOWN SLOT 1 ARM A
        static string BuildCommandParameter(string jsonParameter)
        {
            JObject jsonObject = JObject.Parse(jsonParameter);
            StringBuilder paraBuilder = new StringBuilder();
            foreach (var item in jsonObject)
            {
                if (paraBuilder.Length == 0)
                {
                    paraBuilder.Append(item.Value);
                }
                else
                {
                    if(item.Key == "Slot" || item.Key == "Arm")
                        paraBuilder.Append(" " + item.Key.ToUpper() + " " + item.Value);
                    else
                        paraBuilder.Append(" " + item.Value);
                }
            }

            return paraBuilder.Length > 0 ? paraBuilder.ToString() : null;

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteLastPickInfo(SendText);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }

    public class SiasunAlignerPlaceHandler : SiasunAlignerHandler
    {
        public SiasunAlignerPlaceHandler(SiasunAligner device, string jsonParameter)
            : base(device, "PLACE", BuildCommandParameter(jsonParameter))
        {
        }

        static string BuildCommandParameter(string jsonParameter)
        {
            JObject jsonObject = JObject.Parse(jsonParameter);
            StringBuilder paraBuilder = new StringBuilder();
            foreach (var item in jsonObject)
            {
                if (paraBuilder.Length == 0)
                {
                    paraBuilder.Append(item.Value);
                }
                else
                {
                    if (item.Key == "Slot" || item.Key == "Arm")
                        paraBuilder.Append(" " + item.Key.ToUpper() + " " + item.Value);
                    else
                        paraBuilder.Append(" " + item.Value);
                }
            }

            return paraBuilder.Length > 0 ? paraBuilder.ToString() : null;

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteLastPlaceInfo(SendText);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }


    public class SiasunAlignerSimpleActionHandler : SiasunAlignerHandler
    {
        public SiasunAlignerSimpleActionHandler(SiasunAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteActionCompleted(_command);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }
    public class SiasunAlignerSimpleSetHandler : SiasunAlignerHandler
    {
        public SiasunAlignerSimpleSetHandler(SiasunAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteSetCompleted(_command,_parameter);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }


    public class SiasunAlignerSevoOnOffHandler : SiasunAlignerHandler
    {
        public SiasunAlignerSevoOnOffHandler(SiasunAligner device, bool isOn)
            : base(device, isOn ? "SVON" : "SVOFF")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteSevoOnOff(_command == "SVON");
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }


    public class SiasunAlignerSimpleQueryHandler : SiasunAlignerHandler
    {
        public SiasunAlignerSimpleQueryHandler(SiasunAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;

            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                var resultArray = result.Data.Split(' ');
                if(resultArray.Count() >= 2 && resultArray[0] == _command)
                {
                    Device.NoteError(null);
                    Device.NoteQueryResult(_command, _parameter, string.Join(",", resultArray, 1, resultArray.Count()-1));
                }
                else
                {
                    Device.NoteError($"Invalid Response '{result.Data}' for command: {_command}");
                }

            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }


}
