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
    public abstract class HwinRobotHandler : HandlerBase
    {
        public HwinRobot Device { get; }
        public bool HasResponse { get; set; } = true;

        protected string _command;
        protected string _parameter;
        protected string _target;
        protected RobotArmEnum _blade;


        protected string _requestResponse = "";

        protected HwinRobotHandler(HwinRobot device, string command, string parameter = null)
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
            HwinRobotMessage response = msg as HwinRobotMessage;
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


    public class HwinRobotSTATHandler : HwinRobotHandler
    {
        // STAT
        public HwinRobotSTATHandler(HwinRobot device, int timeout = 5)
            : base(device, $"STAT")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
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

    public class HwinRobotERRHandler : HwinRobotHandler
    {
        // STAT
        public HwinRobotERRHandler(HwinRobot device, int timeout = 5)
            : base(device, $"ERR 0")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
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


    public class HwinRobotGETAHandler : HwinRobotHandler
    {
        //GETA [module],[slot]
        public HwinRobotGETAHandler(HwinRobot device, string module, int slot, int timeout = 60)
            : base(device, $"GETA {device.ModuleAssociateStationDic[module]} {slot+1}")
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
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot GETA Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                //Wafer位置从机械手和Station之间转换
                Device.PrasePutWaferData();
            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot GETA Response Error");
                Device.RobotCheckError();
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class HwinRobotGETBHandler : HwinRobotHandler
    {
        //GETB [module],[slot]
        public HwinRobotGETBHandler(HwinRobot device, string module, int slot, int timeout = 60)
            : base(device, $"GETB {device.ModuleAssociateStationDic[module]} {slot + 1}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget =  module,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot GETB Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                //Wafer位置从机械手和Station之间转换
                Device.PraseGetWaferData();
            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot GETB Response Error");
                Device.RobotCheckError();
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }


    public class HwinRobotPUTAHandler : HwinRobotHandler
    {
        //PUTA [module],[slot]
        public HwinRobotPUTAHandler(HwinRobot device, string module, int slot, int timeout = 60)
            : base(device, $"PUTA {device.ModuleAssociateStationDic[module]} {slot + 1}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Placing,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = module,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot PUTA Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                //Wafer位置从机械手和Station之间转换
                //Device.PraseWaferData(result.Data);
            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot PUTA Response Error");
                Device.RobotCheckError();
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class HwinRobotPUTBHandler : HwinRobotHandler
    {
        //PUTA [module],[slot]
        public HwinRobotPUTBHandler(HwinRobot device, string module, int slot, int timeout = 60)
            : base(device, $"PUTB {device.ModuleAssociateStationDic[module]} {slot + 1}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget =  module,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot PUTB Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                //Wafer位置从机械手和Station之间转换
                //Device.PraseWaferData(result.Data);
            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot PUTB Response Error");
                Device.RobotCheckError();
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }


    public class HwinRobotPUTSPHandler : HwinRobotHandler
    {
        //PUTSP [module],[slot],[step]
        //step:1:初始化位置  2:伸出  3:开真空并抬升  4:到位回缩
        string cModule = "";
        int cSlot = 0;
        int cStep = 0;
        public HwinRobotPUTSPHandler(HwinRobot device, string module, int slot,int step, int timeout = 60)
            : base(device, $"PUTSP {device.ModuleAssociateStationDic[module]} {slot + 1} {step}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            cModule = module;
            cSlot = slot;
            cStep = step;
            if (cStep == 2)
            {
                device.MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Placing,
                    ArmTarget = RobotArm.ArmA,
                    BladeTarget = module,
                };
            }
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot PUTSP Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                if (cStep == 2)
                {
                    Device.MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Placing,
                        ArmTarget = _blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = cModule,
                    };
                }
                else if (cStep == 3)
                {
                    Device.NoteActionCompleted();
                }
                else if (cStep == 4)
                {
                    Device.NotePlaceCompleted();
                    Device.MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Moving,
                        ArmTarget = RobotArm.ArmA,
                        BladeTarget = ModuleName.System.ToString(),
                    };
                }
            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot PUTSP Response Error");
                Device.RobotCheckError();
                return false;
            }       
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class HwinRobotGETSPHandler : HwinRobotHandler
    {
        //PUTSP [module],[slot],[step]
        //step:1:初始化位置  2:伸出  3:关真空并抬升  4:到位回缩        
        string cModule = "";
        int cSlot = 0; 
        int cStep = 0;
        public HwinRobotGETSPHandler(HwinRobot device, string module, int slot, int step, int timeout = 60)
            : base(device, $"GETSP {device.ModuleAssociateStationDic[module]} {slot + 1} {step}")
        {
            cModule = module;
            cSlot = slot;
            cStep = step;
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            if (cStep == 2)
            {
                device.MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Picking,
                    ArmTarget = RobotArm.ArmA,
                    BladeTarget = module,
                };
            }
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot GETSP Timeout");
                return true;
            }
            if (result.Data == ">")
            {
                if (cStep == 2)
                {
                    Device.MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Picking,
                        ArmTarget = _blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = cModule,
                    };
                    Device.NoteActionCompleted();
                }
                else if (cStep == 3)
                {
                    Device.NotePickCompleted();
                }
                else if (cStep == 4)
                {                    
                    Device.NoteActionCompleted(); 
                    Device.MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Moving,
                        ArmTarget = RobotArm.ArmA,
                        BladeTarget = ModuleName.System.ToString(),
                    };
                }
            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot GETSP Response Error");
                Device.RobotCheckError();
                return false;
            }
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }



    public class HwinRobotINPUTHandler : HwinRobotHandler
    {
        // INPUT A 3
        public HwinRobotINPUTHandler(HwinRobot device, int timeout = 5)
            : base(device, $"INPUT A 1")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
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
                //Device.NoteError("Robot INPUT Response Error");
                Device.RobotCheckError();
                return false;
            }

            Device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = ModuleName.System.ToString(),
            };

            ResponseMessage = msg;
            //Device.NoteActionCompleted();
            handled = true;
            return true;
        }
    }


    public class HwinRobotOutpOpenHandler : HwinRobotHandler
    {
        //开真空
        public HwinRobotOutpOpenHandler(HwinRobot device, int timeout = 5)
            : base(device, $"OUTP 1 1")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot OutP Timeout");
                return true;
            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot OutP Response Error");
                Device.RobotCheckError();
                return false;
            }

            Device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.ArmA,
                BladeTarget = ModuleName.System.ToString(),
            };

            ResponseMessage = msg;
            //Device.NoteActionCompleted();
            handled = true;
            return true;
        }
    }

    public class HwinRobotOutpCloseHandler : HwinRobotHandler
    {
        //关真空
        public HwinRobotOutpCloseHandler(HwinRobot device, int timeout = 5)
            : base(device, $"OUTP 1 0")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot OutP Timeout");
                return true;
            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot OutP Response Error");
                Device.RobotCheckError();
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

    public class HwinRobotMapHandler : HwinRobotHandler
    {
        //MAP [module]
        public HwinRobotMapHandler(HwinRobot device, string module, int timeout = 60)
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
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot MAP Timeout");
                return true;
            }
            if (result.Data == ">")
            {

            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot MAP Response Error");
                Device.RobotCheckError();
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

    public class HwinRobotRSRHandler : HwinRobotHandler
    {
        //RSR
        public HwinRobotRSRHandler(HwinRobot device, int timeout = 60)
            : base(device, $"RSR")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
            handled = false;
            if (!result.IsResponse)
            {
                Device.NoteError("Robot RSR Timeout");
                return true;
            }
            if (result.Data.Length == 25 )
            {
                Device.SetWaferData(result.Data);
            }
            else if (result.Data == "?")
            {
                //Device.NoteError("Robot RSR Response Error");
                Device.RobotCheckError();
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

    public class HwinRobotRespHandler : HwinRobotHandler
    {
        //RESP 确认控制器是否成功建立与主控电脑的通信
        public HwinRobotRespHandler(HwinRobot device, int timeout = 60)
            : base(device, $"RESP")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
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
                //Device.NoteError("Robot RESP Response Error");
                Device.RobotCheckError();
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

    public class HwinRobotRemsHandler : HwinRobotHandler
    {
        //RESP 确认控制器是否成功建立与主控电脑的通信
        public HwinRobotRemsHandler(HwinRobot device, int timeout = 60)
            : base(device, $"REMS")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
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
                //Device.NoteError("Robot REMS Response Error");
                Device.RobotCheckError();
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

    public class HwinRobotSVONHandler : HwinRobotHandler
    {
        //SVON 用以激磁位于机械手臂内之伺服马达
        public HwinRobotSVONHandler(HwinRobot device, int timeout = 60)
            : base(device, $"SVON")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwinRobotMessage;
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
                //Device.NoteError("Robot SVON Response Error");
                Device.RobotCheckError();
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

    public class HwinRobotHomeHandler : HwinRobotHandler
    {
        //HOM
        public HwinRobotHomeHandler(HwinRobot device, int timeout = 60)
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
            var result = msg as HwinRobotMessage;
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
                Device.RobotCheckError();
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



