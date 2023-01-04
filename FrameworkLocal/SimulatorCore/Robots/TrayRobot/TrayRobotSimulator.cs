using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Timers;
using MECF.Framework.Simulator.Core.SubstrateTrackings;

namespace MECF.Framework.Simulator.Core.Robots
{

    public class TrayRobotSimulator : RobotSimulator
    {
        public enum OperateEnum
        {
            Null,
            RSR,
            INPUT,
            STAT,
        }

        protected Random _rd = new Random();
        public bool Failed { get; set; }
        public string ErrorCode { get; set; }

        public bool EventChecked { get; set; }
        public string EventCode { get; set; }

        private static string msgDone = ">";
        private static string msgError = "?";
        private static string msgRSRCassB = "1001311011110000000000101"; //f
        private static string msgRSR = "1";

        private string lastMsg = "";
        private System.Timers.Timer timer;

        private bool IsRSR = false;


        public TrayRobotSimulator() : base(1104, 0, "\r\n", ' ', 5)
        {
            AddCommandHandler("HOM", HandleHome);
            AddCommandHandler("GETA", HandleHome);
            AddCommandHandler("PUTA", HandleHome);
            AddCommandHandler("GETB", HandleHome);
            AddCommandHandler("PUTB", HandleHome);
            AddCommandHandler("GETSP", HandleHome);
            AddCommandHandler("PUTSP", HandleHome);
            AddCommandHandler("MAP", HandleMap);
            AddCommandHandler("RSR", HandleRSR);
            AddCommandHandler("RESP", HandleHome);
            AddCommandHandler("REMS", HandleHome);
            AddCommandHandler("SVON", HandleHome);
            AddCommandHandler("OUTP", HandleHome);
            AddCommandHandler("MTCS", HandleHome);
            AddCommandHandler("MOVR", HandleHome);
            AddCommandHandler("RETH", HandleHome);

            AddCommandHandler("STAT", HandleStat);
            AddCommandHandler("INPUT", HandleInput);

            timer = new System.Timers.Timer();
            timer.Enabled = false;
            timer.AutoReset = false;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }


        OperateEnum operateEnum = OperateEnum.Null;

        internal void HandleStat(string msg)
        {
            lastMsg = msg; 
            operateEnum = OperateEnum.STAT;
            OnWriteMessage("2048");
        }

        internal void HandleHome(string msg)
        {
            operateEnum = OperateEnum.Null;
            lastMsg = msg;
            HandleMove(RobotStateEnum.Homing, new string[] { "" });
        }
        internal void HandleMap(string msg)
        {
            lastMsg = msg;
            HandleMap(RobotStateEnum.MAP, new string[] { "" });
        }
        internal void HandleRSR(string msg)
        {
            operateEnum = OperateEnum.RSR;
            HandleRSR(RobotStateEnum.RSR, new string[] { "" });
        }

        internal void HandleInput(string msg)
        {
            lastMsg = msg;
            operateEnum = OperateEnum.INPUT;
            HandleInput(RobotStateEnum.Input, new string[] { "" });
        }


        private bool HandleInput(RobotStateEnum action, string[] cmdComponents)
        {

            if (robotStateArgs.State != RobotStateEnum.Idle &&
                (action == RobotStateEnum.Homing && robotStateArgs.State != RobotStateEnum.Errored))  // allow homes when in error, but not other moves
            {

                HandleError("Already moving");
                return false;
            }

            double delay = 2;
            timer.Interval = delay * 1000;
            timer.Enabled = true;
            return true;
        }

        private bool HandleRSR(RobotStateEnum action, string[] cmdComponents)
        {
            if (robotStateArgs.State != RobotStateEnum.Idle &&
                    (action == RobotStateEnum.Homing && robotStateArgs.State != RobotStateEnum.Errored))  // allow homes when in error, but not other moves
            {

                HandleError("Already moving");
                return false;
            }

            double delay = 2;
            timer.Interval = delay * 1000;
            timer.Enabled = true;
            return true;
        }

        internal void HandleUnknown(string msg)
        {
            OnWriteMessage(msgError);
        }


        private bool HandleMove(RobotStateEnum action, string[] cmdComponents)
        {

            if (robotStateArgs.State != RobotStateEnum.Idle &&
                (action == RobotStateEnum.Homing && robotStateArgs.State != RobotStateEnum.Errored))  // allow homes when in error, but not other moves
            {

                HandleError("Already moving");
                return false;
            }

            IsRSR = false;

            double delay = 2;
            timer.Interval = delay * 1000;
            timer.Enabled = true;
            return true;
        }

        private bool HandleMap(RobotStateEnum action, string[] cmdComponents)
        {

            if (robotStateArgs.State != RobotStateEnum.Idle &&
                (action == RobotStateEnum.Homing && robotStateArgs.State != RobotStateEnum.Errored))  // allow homes when in error, but not other moves
            {

                HandleError("Already moving");
                return false;
            }

            IsRSR = false;

            double delay = 2;
            timer.Interval = delay * 1000;
            timer.Enabled = true;
            return true;
        }



        private void HandleError(string msg)
        {
            OnWriteMessage(msgError);
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SetRobotState(RobotStateEnum.Idle);
            timer.Enabled = false;
    
            if (operateEnum == OperateEnum.STAT)
            {
                OnWriteMessage(msgRSRCassB);
            }
            else if (operateEnum == OperateEnum.RSR)
            {
                OnWriteMessage(msgRSRCassB);
            }
            else if (operateEnum == OperateEnum.INPUT)
            {
                OnWriteMessage("1");
            }
            else
            {
                OnWriteMessage(msgDone);
            }
        }





    }
}
