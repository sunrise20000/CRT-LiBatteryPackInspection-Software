using Aitex.Sorter.Common;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Errors;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobot
{
    public abstract class HwinRobotBHandler : HandlerBase
    {
        public HwinRobotB Device { get; }
        public bool HasResponse { get; set; } = true;

        protected string _command;
        protected string _parameter;
        protected string _target;
        protected RobotArmEnum _blade;


        protected string _requestResponse = "";

        protected HwinRobotBHandler(HwinRobotB device, string command, string parameter = null)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        private static string BuildMessage(string command, string parameter)
        {
            string msg = parameter == null ? $"{command}" : $"{command} {parameter}";

            return msg + "\r\n";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HwinRobotBMessage response = msg as HwinRobotBMessage;
            ResponseMessage = msg;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }
            if (response.IsResponse)
            {
                _requestResponse = response.Data;
            }

            transactionComplete = false;
            return false;
        }
    }


    public class HwinRobotBSTATHandler : HwinRobotBHandler
    {
        // STAT
        public HwinRobotBSTATHandler(HwinRobotB device, int timeout = 5)
            : base(device, $"STAT")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot STAT Timeout");
                return true;
            }

            if (result.Data == "?")
            {
                Device.NoteError("Robot STAT Response Error");
                return false;
            }

            if (int.TryParse(result.Data, out var stat))
            {
                //Wafer位置从机械手和Station之间转换
                Device.ParseStatusData(stat);
            }
            else
                ResponseMessage = msg;

            handled = true;
            return true;
        }
    }

    public class HwinRobotBERRHandler : HwinRobotBHandler
    {
        // STAT
        public HwinRobotBERRHandler(HwinRobotB device, int timeout = 5)
            : base(device, $"ERR 0")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot ERR Timeout");
                return true;
            }

            if (result.Data == "?")
            {
                Device.NoteError("Robot ERR Response Error");
                return false;
            }

            if (HiwinRobotAggregatedErrors.IsErrResponse(result.Data))
            {
                return Device.ParseErrData(result.Data);
            }
            else
                ResponseMessage = msg;

            handled = true;
            return true;
        }
    }

    public class HwinRobotBGETBHandler : HwinRobotBHandler
    {
        //GETB [module],[slot]
        string cModule = "";

        public HwinRobotBGETBHandler(HwinRobotB device, string module, int slot, int timeout = 60)
            : base(device, $"GETB {device.ModuleAssociateStationDic[module]} {slot + 1}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            cModule = module;

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = module,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot GETB Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                Device.GetWaferData();
                Device.NotePickCompleted();

                Device.MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = RobotArm.ArmA,
                    BladeTarget = ModuleName.System.ToString(),
                };
            }
            else if (result.Data == "?")
            {
                Device.RobotCheckError();
                //Device.NoteError("Robot GETB Response Error");
                return true;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class HwinRobotBPUTBHandler : HwinRobotBHandler
    {
        //PUTA [module],[slot]
        string cModule = "";

        public HwinRobotBPUTBHandler(HwinRobotB device, string module, int slot, int timeout = 60)
            : base(device, $"PUTB {device.ModuleAssociateStationDic[module]} {slot + 1}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            cModule = module;

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Placing,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = module,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot PUTB Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                Device.PutWaferData();
                Device.NotePlaceCompleted();

                Device.MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = RobotArm.ArmA,
                    BladeTarget = ModuleName.System.ToString(),
                };

            }
            else if (result.Data == "?")
            {
                Device.NoteError("Robot PUTB Response Error");
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }


    public class HwinRobotBMapHandler : HwinRobotBHandler
    {
        //MAP [module]
        string cModule = "";

        public HwinRobotBMapHandler(HwinRobotB device, string module, int timeout = 60)
            : base(device, $"MAP {device.ModuleAssociateStationDic[module].ToLower()}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = module,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot MAP Timeout");
                return true;
            }

            if (result.Data == ">")
            {
                Device.MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = RobotArm.ArmA,
                    BladeTarget = ModuleName.System.ToString(),
                };
            }
            else if (result.Data == "?")
            {
                Device.NoteError("Robot MAP Response Error");
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }


    public class HwinRobotBRSRHandler : HwinRobotBHandler
    {
        //RSR
        public HwinRobotBRSRHandler(HwinRobotB device, int timeout = 60)
            : base(device, $"RSR")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot RSR Timeout");
                return true;
            }
            if (result.Data.Length > 5 )
            {
                Device.SetWaferData(result.Data);
            }
            else if (result.Data == "?")
            {
                Device.NoteError("Robot RSR Response Error");
                return false;
            }

            Device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = ModuleName.System.ToString(),
            };

            ResponseMessage = msg;
            Device.NoteActionCompleted();
            handled = true;
            return true;
        }
    }




    public class HwinRobotBINPUTHandler : HwinRobotBHandler
    {
        // INPUT A 3
        public HwinRobotBINPUTHandler(HwinRobotB device, int timeout = 5)
            : base(device, $"INPUT A 6")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot INPUT Timeout");
                return true;
            }
            if (result.Data.Length == 1)
            {
                Device.SetWaferData(result.Data);
            }
            else if (result.Data == "?")
            {
                Device.NoteError("Robot INPUT Response Error");
                return false;
            }

            Device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = ModuleName.System.ToString(),
            };

            ResponseMessage = msg;
            Device.NoteActionCompleted();
            handled = true;
            return true;
        }
    }

    public class HwinRobotBRespHandler : HwinRobotBHandler
    {
        //RESP 确认控制器是否成功建立与主控电脑的通信
        public HwinRobotBRespHandler(HwinRobotB device, int timeout = 60)
            : base(device, $"RESP")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot RESP Timeout");
                return true;
            }
            if (result.Data == ">")
            {

            }
            else if (result.Data == "?")
            {
                Device.NoteError("Robot RESP Response Error");
                return false;
            }
            Device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = ModuleName.System.ToString(),
            };
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class HwinRobotBRemsHandler : HwinRobotBHandler
    {
        //RESP 确认控制器是否成功建立与主控电脑的通信
        public HwinRobotBRemsHandler(HwinRobotB device, int timeout = 60)
            : base(device, $"REMS")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot REMS Timeout");
                return true;
            }
            if (result.Data == ">")
            {
            }
            else if (result.Data == "?")
            {
                Device.NoteError("Robot REMS Response Error");
                return false;
            }
            Device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = ModuleName.System.ToString(),
            };
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class HwinRobotBSVONHandler : HwinRobotBHandler
    {
        //SVON 用以激磁位于机械手臂内之伺服马达
        public HwinRobotBSVONHandler(HwinRobotB device, int timeout = 60)
            : base(device, $"SVON")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot SVON Timeout");
                return true;
            }
            if (result.Data == ">")
            {
            }
            else if (result.Data == "?")
            {
                Device.NoteError("Robot SVON Response Error");
                return false;
            }
            Device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = ModuleName.System.ToString(),
            };
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class HwinRobotBHomeHandler : HwinRobotBHandler
    {
        //HOM
        public HwinRobotBHomeHandler(HwinRobotB device, int timeout = 60)
            : base(device, $"HOM")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.Both,
                BladeTarget = "ArmA.System",
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot HOM Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                Device.IsBusy = false;
            }
            else if (result.Data == "?")
            {
                //Device.ContineHome();
                return false;
            }
            Device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = ModuleName.System.ToString(),
            };
            ResponseMessage = msg;
            handled = true;
            //Device.NoteActionCompleted();
            return true;
        }
    }







    public class HwinRobotBOPTClosePHandler : HwinRobotBHandler
    {
        //打开夹爪
        public HwinRobotBOPTClosePHandler(HwinRobotB device, int timeout = 60)
            : base(device, $"OUTP 1 1")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot OUTP Timeout");
                return true;
            }
            if (result.Data == ">")
            {

            }
            else if (result.Data == "?")
            {
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }



    public class HwinRobotBOPTPHandler : HwinRobotBHandler
    {
        //打开夹爪
        public HwinRobotBOPTPHandler(HwinRobotB device, int timeout = 60)
            : base(device, $"OUTP 1 0")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot OUTP Timeout");
                return true;
            }
            if (result.Data == ">")
            {

            }
            else if (result.Data == "?")
            {
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }


    public class HwinRobotMTCSPHandler : HwinRobotBHandler
    {
        //前往位置
        public HwinRobotMTCSPHandler(HwinRobotB device, string module,string strAction, int timeout = 60)
            : base(device, $"MTCS {device.ModuleAssociateStationDic[$"{module}.{strAction}"].ToUpper()}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = module,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot MTCS Timeout");
                return true;
            }
            if (result.Data == ">")
            {

            }
            else if (result.Data == "?")
            {
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }


    public class HwinRobotMOVRPHandler : HwinRobotBHandler
    {
        //抬升或下降 一定距离
        public HwinRobotMOVRPHandler(HwinRobotB device, int rDistance, int timeout = 60)
            : base(device, $"MOVR Z {rDistance}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot MOVR Z Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                Device.PutWaferData();
            }
            else if (result.Data == "?")
            {
                Device.NoteError("Robot MOVR Z Response Error");
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }    
    }

    public class HwinRobotMOVRGHandler : HwinRobotBHandler
    {
        //抬升或下降 一定距离
        public HwinRobotMOVRGHandler(HwinRobotB device, int rDistance, int timeout = 60)
            : base(device, $"MOVR Z {rDistance}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot MOVR Z Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                Device.GetWaferData();
            }
            else if (result.Data == "?")
            {
                Device.NoteError("Robot MOVR Z Response Error");
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class HwinRobotRETHHandler : HwinRobotBHandler
    {
        //缩回
        public HwinRobotRETHHandler(HwinRobotB device, int timeout = 60)
            : base(device, $"RETH")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = "ArmA.System",
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotBMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot RETH Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                Device.NotePickCompleted();
            }
            else if (result.Data == "?")
            {
                return false;
            }
            Device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = ModuleName.System.ToString(),
            };
            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();
            return true;
        }
    }



}



