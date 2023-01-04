using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Comet
{

    public enum RFMatchResponseResult
    {
        InvalidResonse,
        TriggerAck,
        TriggerNAK,
        CheckInProcess,
        CheckCompleted
    }
    public abstract class CometRFMatchHandler : HandlerBase
    {
        public CometRFMatch Device { get; }

        public string _command;
        protected string _parameter;
        protected string _completeEvent;



        protected CometRFMatchHandler(CometRFMatch device, string command, string completeEvent, string parameter)
            : base(BuildMessage(StringToByteArray(command), StringToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _completeEvent = completeEvent;
            _parameter = parameter;
            _address = Convert.ToByte(Device.Address, 16);
            Name = command;
        }

        protected CometRFMatchHandler(CometRFMatch device, string command)
            : base(BuildMessage(StringToByteArray(command)))
        {
            Device = device;
            _command = command;
            _address = Convert.ToByte(Device.Address, 16);
            Name = command;
        }


        protected CometRFMatchHandler(CometRFMatch device, string command, string parameter)
            : base(BuildMessage(StringToByteArray(command), StringToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            _address = Convert.ToByte(Device.Address, 16);
            Name = command;
        }

        protected CometRFMatchHandler(CometRFMatch device, string command, byte[] parameter)
        : base(BuildMessage(StringToByteArray(command), parameter))
        {
            Device = device;
            _command = command;
            //_parameter = parameter;
            Name = command;
        }

        private static byte _start = 0xAA;
        private static byte _address = 0x21;

        protected static byte[] BuildMessage(byte[] commandArray, byte[] argumentArray = null)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(_start);
            buffer.Add(_address);

            if (commandArray != null && commandArray.Length > 0)
            {
                buffer.AddRange(commandArray);
            }
            if (argumentArray != null && argumentArray.Length > 0)
            {
                buffer.AddRange(argumentArray);
            }
            byte checkSum = 0;
            for(int i = 0; i < buffer.Count; i++)
            {
                checkSum += buffer[i];
            }
            buffer.Add(checkSum);

            return buffer.ToArray();
        }

        protected static byte[] BuildMessage(string commandStr, byte[] argumentArray = null)
        {
            byte[] commandArray = StringToByteArray(commandStr);
            List<byte> buffer = new List<byte>();

            buffer.Add(_start);
            buffer.Add(_address);

            if (commandArray != null && commandArray.Length > 0)
            {
                buffer.AddRange(commandArray);
            }
            if (argumentArray != null && argumentArray.Length > 0)
            {
                buffer.AddRange(argumentArray);
            }
            byte checkSum = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                checkSum += buffer[i];
            }
            buffer.Add(checkSum);

            return buffer.ToArray();
        }


        protected static byte[] BuildByteArray(byte para1, short para2)
        {
            List<byte> buffer = new List<byte>();
            buffer.Add(para1);
            buffer.AddRange(IntToByteArray(para2));
            return buffer.ToArray();
        }

        //public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        //{
        //    CometRFMatchMessage response = msg as CometRFMatchMessage;
        //    ResponseMessage = msg;

        //    string reponseData = string.Join(",", response.Data.Select(bt => bt.ToString("X2")).ToArray());
        //    if(_completeEvent.Contains("|"))
        //    {
        //        var eventArray = _completeEvent.Split('|');
        //        var subRes = reponseData.Substring(0, eventArray[0].Length);
        //        if(!eventArray.Contains(subRes))
        //        {
        //            Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
        //            msg.IsFormatError = true;

        //            transactionComplete = false;
        //            return false;
        //        }
        //    }
        //    else if (reponseData.IndexOf(_completeEvent) == -1)
        //    {
        //        Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
        //        msg.IsFormatError = true;

        //        transactionComplete = false;
        //        return false;
        //    }


        //    if (response.IsAck)
        //    {
        //        SetState(EnumHandlerState.Acked);
        //    }
        //    if (this.IsAcked && response.IsComplete)
        //    {
        //        SetState(EnumHandlerState.Completed);
        //        transactionComplete = true;
        //        return true;
        //    }

        //    transactionComplete = false;
        //    return false;
        //}


        protected ResponseNAK CheckNak(string reponseData)
        {
            if (reponseData.IndexOf("90") >= 0)
            {
                return ResponseNAK.CommandNotDefined;
            }
            if (reponseData.IndexOf("92") >= 0)
            {
                return ResponseNAK.CheckSumFailed;
            }
            if (reponseData.IndexOf("93") >= 0)
            {
                return ResponseNAK.PositionOverLimit;
            }
            if (reponseData.IndexOf("93") >= 0)
            {
                return ResponseNAK.IndexOutOfRange;
            }

            return ResponseNAK.NotNak;
        }
        bool _startWaitCheckResonse = false;
        protected RFMatchResponseResult HandleTriggerMessage(MessageBase msg)
        {
            CometRFMatchMessage response = msg as CometRFMatchMessage;

            string triggerAck = "50";
            string[] checkResponseArray = new string[] { "53", "52", "51" };

            string reponseData = string.Join(",", response.Data.Select(bt => bt.ToString("X2")).ToArray());

            if (!_startWaitCheckResonse)
            {
                if (reponseData.IndexOf(triggerAck) >= 0)
                {
                    _startWaitCheckResonse = true;
                    return RFMatchResponseResult.TriggerAck;
                }
                else if (CheckNak(reponseData) != ResponseNAK.NotNak)
                {
                    Device.NoteNAK(CheckNak(reponseData));
                    return RFMatchResponseResult.TriggerNAK;
                }
                else
                {
                    Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                    msg.IsFormatError = true;
                    return RFMatchResponseResult.InvalidResonse;
                }
            }
            else
            {
                var subRes = reponseData.Substring(0, checkResponseArray[0].Length);
                if (checkResponseArray.Contains(subRes))
                {
                    if(subRes == checkResponseArray[2])
                    {
                        return RFMatchResponseResult.CheckCompleted;
                    }
                    else
                    {
                        return RFMatchResponseResult.CheckInProcess;
                    }
                }
                else
                {
                    Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                    msg.IsFormatError = true;
                    return RFMatchResponseResult.InvalidResonse;
                }

            }

        }

        protected bool HandleSetMessage(MessageBase msg)
        {
            CometRFMatchMessage response = msg as CometRFMatchMessage;

            string setOK = _command;
            string[] checkResponseArray = new string[] { "53", "52", "51" };

            string reponseData = string.Join(",", response.Data.Select(bt => bt.ToString("X2")).ToArray());
            if (reponseData.IndexOf(setOK) >= 0)
            {
                return true;
            }
            else if (CheckNak(reponseData) != ResponseNAK.NotNak)
            {
                Device.NoteNAK(CheckNak(reponseData));
                return false;
            }
            else
            {
                Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                msg.IsFormatError = true;
                return false;
            }
        }

        protected bool HandleGetMessage(MessageBase msg)
        {
            CometRFMatchMessage response = msg as CometRFMatchMessage;

            string setOK = _completeEvent;
            string reponseData = string.Join(",", response.Data.Select(bt => bt.ToString("X2")).ToArray());
            if (reponseData.IndexOf(setOK) >= 0)
            {
                return true;
            }
            else
            {
                Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                msg.IsFormatError = true;
                return false;
            }
        }



        protected static byte[] StringToByteArray(string parameter)
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

        protected static byte[] ShortToByteArray(short num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }

        protected uint ByteArrayToUInt(byte[] bytes)
        {
            uint temp = BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
            return temp;
        }
        protected short ByteArrayToShort(byte[] bytes)
        {
            short temp = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0);
            return temp;
        }

        protected void SetDevicePropValue<T>(string propName, T value)
        {
            try
            {
                Type objType = Device.GetType();
                objType.GetProperty(propName).SetValue(Device, value, null);
            }
            catch
            {
            }
        }

        public string DevicePropName;
    }

    public class CommetRFMatchRawCommandHandler : CometRFMatchHandler
    {
        public CommetRFMatchRawCommandHandler(CometRFMatch device, string command, string completeEvent, string parameter)
            : base(device, command, completeEvent, parameter)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            CometRFMatchMessage response = msg as CometRFMatchMessage;
            ResponseMessage = msg;

            string reponseData = string.Join(",", response.Data.Select(bt => bt.ToString("X2")).ToArray());
            if (_completeEvent.Contains("|"))
            {
                var eventArray = _completeEvent.Split('|');
                var subRes = reponseData.Substring(0, eventArray[0].Length);
                if (!eventArray.Contains(subRes))
                {
                    Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                    msg.IsFormatError = true;

                    transactionComplete = false;
                    return false;
                }
            }
            else if (reponseData.IndexOf(_completeEvent) == -1)
            {
                Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                msg.IsFormatError = true;

                transactionComplete = false;
                return false;
            }

            else if (CheckNak(reponseData) != ResponseNAK.NotNak)
            {
                Device.NoteNAK(CheckNak(reponseData));

                transactionComplete = false;
                return false;
            }

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }
            if (this.IsAcked && response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                Device.NoteRawCommandInfo(_command, string.Join(",", response.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                return true;
            }

            transactionComplete = false;
            return false;
        }
    }

    public class CommetRFMatchTriggerHandler : CometRFMatchHandler
    {
        string _checkCommand;
        public CommetRFMatchTriggerHandler(CometRFMatch device, string triggerCommand, string checkCommand, string propName)
            : base(device, triggerCommand)
        {
            _checkCommand = checkCommand;
            DevicePropName = propName;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var resonseResult = HandleTriggerMessage(msg);
            if (resonseResult == RFMatchResponseResult.TriggerAck || resonseResult == RFMatchResponseResult.CheckInProcess)
            {
                Device.Connection.SendMessage(BuildMessage(_checkCommand, null));
            }
            else if (resonseResult == RFMatchResponseResult.CheckCompleted)
            {
                SetDevicePropValue<bool>(DevicePropName, true);
                handled = true;
                return true;
            }
            else if (resonseResult == RFMatchResponseResult.TriggerNAK || resonseResult == RFMatchResponseResult.InvalidResonse)
            {
                handled = true;
                return true;
            }
            handled = false;
            return false;
        }
    }



    public class CommetRFMatchSetHandler : CometRFMatchHandler
    {
        public CommetRFMatchSetHandler(CometRFMatch device,string command, string parameter)
            : base(device, command, parameter)
        {
        }

        public CommetRFMatchSetHandler(CometRFMatch device, string command, short parameter)
        : base(device, command, ShortToByteArray(parameter))
        {
        }
        public CommetRFMatchSetHandler(CometRFMatch device, string command, byte para1, short para2)
        : base(device, command, BuildByteArray(para1, para2))
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleSetMessage(msg))
            {
                Device.NoteSetComplted();
                handled = true;
                return true;
            }
            handled = false;
            return false;
        }
    }


    public class CommetRFMatchGet3ByteHandler : CometRFMatchHandler
    {
        public CommetRFMatchGet3ByteHandler(CometRFMatch device, string command, string completeEvent, string propName, string parameter)
            : base(device, command, completeEvent, parameter)
        {
            DevicePropName = propName;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;

                if (result.Data[3].ToString("X2") != _parameter)
                {
                    handled = false;
                    return false;
                }
                short oriValue = ByteArrayToShort(result.Data.Skip(3).ToArray());
                SetDevicePropValue<short>(DevicePropName, oriValue);
                handled = true;
                return true;
            }
            handled = false;
            return false;
        }
    }


    public class CommetRFMatchGetLimitHandler : CometRFMatchHandler
    {
        public CommetRFMatchGetLimitHandler(CometRFMatch device, string command, string completeEvent, string propName, string parameter)
            : base(device, command, completeEvent, parameter)
        {
            DevicePropName = propName;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;

                if (result.Data[3].ToString("X2") != _parameter)
                {
                    handled = false;
                    return false;
                }
                short oriValue = ByteArrayToShort(result.Data.Skip(3).ToArray());
                SetDevicePropValue<double>(DevicePropName, oriValue * 0.1);
                handled = true;
                return true;
            }
            handled = false;
            return false;
        }
    }


    public class CommetRFMatchGetDoubleValueHandler : CometRFMatchHandler
    {
        double _factor = 1.0f;
        public CommetRFMatchGetDoubleValueHandler(CometRFMatch device, string command, string completeEvent, string propName, double factor)
            : base(device, command, completeEvent, null)
        {
            DevicePropName = propName;
            _factor = factor;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;
                short oriValue = ByteArrayToShort(result.Data.Skip(2).ToArray());
                SetDevicePropValue<double>(DevicePropName, oriValue * _factor);
                handled = true;
                return true;
            }
            handled = false;
            return false;
        }
    }

    public class CommetRFMatchGetIntValueHandler : CometRFMatchHandler
    {
        public CommetRFMatchGetIntValueHandler(CometRFMatch device, string command, string completeEvent, string propName)
            : base(device, command, completeEvent, null)
        {
            DevicePropName = propName;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;
                uint oriValue = ByteArrayToUInt(result.Data.Skip(2).ToArray());
                SetDevicePropValue<uint>(DevicePropName, oriValue);
                handled = true;
                return true;
            }
            handled = false;
            return false;
        }
    }

    public class CommetRFMatchGetShortValueHandler : CometRFMatchHandler
    {
        public CommetRFMatchGetShortValueHandler(CometRFMatch device, string command, string completeEvent, string propName)
            : base(device, command, completeEvent, null)
        {
            DevicePropName = propName;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;
                short oriValue = ByteArrayToShort(result.Data.Skip(2).ToArray());
                SetDevicePropValue<short>(DevicePropName, oriValue);
                handled = true;
                return true;
            }
            handled = false;
            return false;
        }
    }

    public class CommetRFMatchGetByteValueHandler : CometRFMatchHandler
    {
        public CommetRFMatchGetByteValueHandler(CometRFMatch device, string command, string completeEvent, string propName)
            : base(device, command, completeEvent, null)
        {
            DevicePropName = propName;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;
                SetDevicePropValue<byte>(DevicePropName, result.Data[2]);
                handled = true;
                return true;
            }
            handled = false;
            return false;
        }
    }

}
