using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.TazmoRobot
{
    public abstract class TazmoRobotHandler : HandlerBase
    {
        public TazmoRobot Device { get; }

        public string Command;

        public string Parameter;

        protected TazmoRobotHandler(TazmoRobot device, string command, string para): base(BuildMessage(command, para))
        {
            Device = device;            
            Command = command;
            Name = command;           
        }
        private static byte[] BuildMessage(string command, string para)
        {
            List<byte> ret = new List<byte>();
            foreach (char c in command)
            {
                ret.Add((byte)c);
            }
            if (!string.IsNullOrEmpty(para))
            {
                ret.Add((byte)0x2C);
                foreach (char b in para) ret.Add((byte)b);
            }

            ret.Add(0x0D);
            return ret.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoRobotMessageBin response = msg as TazmoRobotMessageBin;
            ResponseMessage = msg;
            transactionComplete = false;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = true;
            }
            if (response.IsBusy)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsError)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsEvent)
            {
                SendAck();
                if (response.CMD == Encoding.ASCII.GetBytes(Command))
                {

                }
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsResponse)
            {
                SendAck();
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            return true;
        }
        public void SendAck()
        {
            Device.Connection.SendMessage(new byte[] { 0x06 });
        }
    }

    public class TazmoRobotSingleTransactionHandler : TazmoRobotHandler
    {
        public TazmoRobotSingleTransactionHandler(TazmoRobot device, string command, string para) : base(device, BuildData(command), para)
        {

        }

        private static string BuildData(string command)
        {
            return command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoRobotMessageBin response = msg as TazmoRobotMessageBin;
            ResponseMessage = msg;
            Device.TaExecuteSuccss = true;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;

            }
            if (response.IsBusy)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = false;
                transactionComplete = true;
                //ParseError(response.Data);
            }
            if (response.IsError)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = false;
                transactionComplete = true;
                Device.ParseStatus(Encoding.ASCII.GetString(response.Data));
            }
            if (response.IsResponse)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = true;
                transactionComplete = true;
                if (Encoding.Default.GetString(response.CMD) == "STS") Device.ParseStatus(Encoding.ASCII.GetString(response.Data));
                if (Encoding.Default.GetString(response.CMD) == "STA") Device.ParseStatusAndPostion(Encoding.ASCII.GetString(response.Data));
            }

            return true;
        }
    }
    public class TazmoRobotTwinTransactionHandler : TazmoRobotHandler
    {
        public TazmoRobotTwinTransactionHandler(TazmoRobot device, string command, string para) : base(device, BuildData(command), para)
        {
        }
        private static string BuildData(string command)
        {
            return command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoRobotMessageBin response = msg as TazmoRobotMessageBin;
            ResponseMessage = msg;
            Device.TaExecuteSuccss = true;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = false;
            }
            if (response.IsBusy)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = false;
                transactionComplete = true;
                //ParseError(response.Data);
            }
            if (response.IsError)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = false;
                transactionComplete = true;
                Device.ParseStatus(Encoding.ASCII.GetString(response.Data));
            }
            if (response.IsResponse)
            {
                string command = Encoding.Default.GetString(response.CMD);
                //if (command == "RST" || command == "HOM")
                //    Device.Initalized = true;
                SetState(EnumHandlerState.Completed);
                SendAck();
                Device.TaExecuteSuccss = true;
                transactionComplete = true;
            }
            return true;
        }
    }

}
