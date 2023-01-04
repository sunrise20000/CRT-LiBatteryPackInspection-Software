﻿using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temps.AE
{
   public class AETempHandler : HandlerBase
    {
        public AETemp Device { get; }

        public string _command;
        protected string _parameter;


        protected AETempHandler(AETemp device, string command, string parameter)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        private static string _CH = "\t";
        private static string _endLine = "\r";
        private static string BuildMessage(string command, string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
                return command + _endLine;
            else
                return command +_CH + parameter + _endLine;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            AETempAsciiMessage response = msg as AETempAsciiMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);

                if (msg.IsError)
                {
                    //Device.NoteError($"Command '{_command}' Error: {response.Data}:{ErrorString(response.ErrorText)}");
                }
                else
                {
                    SetState(EnumHandlerState.Completed);
                    handled = true;
                    //Device.NoteError(null);
                    return true;
                }
            }

            handled = false;
            return false;
        }

        private static Dictionary<string, string> ErrorDict = new Dictionary<string, string>()
        {
            {"1","Invalid message" },
            {"2","Number not found" },
            {"3","Number Invalid" },
            {"4","Parameter’s value not received" },
            {"5","Command not possible" }
        };
        private static string ErrorString(string errorCode)
        {
            if (ErrorDict.ContainsKey(errorCode))
                return ErrorDict[errorCode];
            else
                return "NotDefined error";
        }

    }
    public class AETempReadCommandHandler : AETempHandler
    {
        public AETempReadCommandHandler(AETemp device, string command, string parameter)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as AETempAsciiMessage;
                Device.ParseCommandInfo(_command, result.Data);
            }
            return true;
        }
    }
}
