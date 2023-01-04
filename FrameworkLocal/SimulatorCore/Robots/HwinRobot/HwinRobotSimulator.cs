using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Timers;
using MECF.Framework.Simulator.Core.SubstrateTrackings;

namespace MECF.Framework.Simulator.Core.Robots
{

    public class HwinRobotSimulator : RobotSimulator
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

        private static string msgDone =  ">";
        private static string msgError = "?";
        private static string msgRSRCassA = "0022133000011111111211541"; //cd
        private static string msgRSRCassB = "1111111111111"; //f
        private static string msgRSR = "1";

        private string lastMsg = "";
        private System.Timers.Timer timer;


        OperateEnum operateEnum = OperateEnum.Null;

        public string CassetteResult { get; set; }


        public HwinRobotSimulator() : base(1103, 0, "\r\n", ' ', 5)
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
            AddCommandHandler("STAT", HandleStat);
            AddCommandHandler("OUTP", HandleHome);
            AddCommandHandler("MTCS", HandleHome);
            AddCommandHandler("MOVR", HandleHome);
            AddCommandHandler("RETH", HandleHome);
            AddCommandHandler("INPUT", HandleInput);
            AddCommandHandler("OUPT", HandleHome);
            //AddCommandHandler("STAT", HandleSTAT);


            timer = new System.Timers.Timer();
            timer.Enabled = false;
            timer.AutoReset = false;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }

        internal void HandleStat(string msg)
        {
            lastMsg = msg;
            OnWriteMessage("2048");
        }

        internal void HandleHome(string msg)
        {
            lastMsg = msg;
            operateEnum = OperateEnum.Null;
            HandleMove(RobotStateEnum.Homing, new string[] { ""});
        }

        internal void HandleInput(string msg)
        {
            lastMsg = msg;
            operateEnum =  OperateEnum.INPUT;
            HandleInput(RobotStateEnum.Input, new string[] { "" });
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
        internal void HandleSTAT(string msg)
        {
            operateEnum = OperateEnum.STAT;
            HandleSTAT(RobotStateEnum.STAT, new string[] { "" });
        }
        internal void SetResult()
        {
            msgRSRCassA = CassetteResult;
        }

        private bool HandleRSR(RobotStateEnum action, string[] cmdComponents)
        {
            if (robotStateArgs.State != RobotStateEnum.Idle &&
                    (action == RobotStateEnum.Homing && robotStateArgs.State != RobotStateEnum.Errored))  // allow homes when in error, but not other moves
            {

                HandleError("Already moving");
                return false;
            }


            double delay = 1;
            timer.Interval = delay * 1000;
            timer.Enabled = true;
            return true;
        }

        private bool HandleSTAT(RobotStateEnum action, string[] cmdComponents)
        {
            if (robotStateArgs.State != RobotStateEnum.Idle &&
                    (action == RobotStateEnum.Homing && robotStateArgs.State != RobotStateEnum.Errored))  // allow homes when in error, but not other moves
            {

                HandleError("Already moving");
                return false;
            }


            double delay = 1;
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
          

            double delay = 1;
            timer.Interval = delay * 1000;
            timer.Enabled = true;
            return true;
        }

        private bool HandleInput(RobotStateEnum action, string[] cmdComponents)
        {

            if (robotStateArgs.State != RobotStateEnum.Idle &&
                (action == RobotStateEnum.Homing && robotStateArgs.State != RobotStateEnum.Errored))  // allow homes when in error, but not other moves
            {

                HandleError("Already moving");
                return false;
            }

            double delay = 1;
            timer.Interval = delay * 1000;
            timer.Enabled = true;
            return true;
        }
        


        private bool HandleMap(RobotStateEnum action, string[] cmdComponents)
        {

            if (robotStateArgs.State != RobotStateEnum.Idle 
                &&(action == RobotStateEnum.Homing && robotStateArgs.State != RobotStateEnum.Errored)
                )  // allow homes when in error, but not other moves
            {

                HandleError("Already moving");
                return false;
            }


            double delay = 1;
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

            this.CassetteResult = msgRSRCassA;

            if (operateEnum == OperateEnum.STAT )
            {
                OnWriteMessage("19568");
            }
            else if (operateEnum == OperateEnum.RSR)
            {
                OnWriteMessage(msgRSRCassA);
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
