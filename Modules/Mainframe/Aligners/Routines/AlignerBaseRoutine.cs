using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.HwAligner;

namespace Mainframe.Aligners.Routines
{
     public class AlignerBaseRoutine : ModuleRoutine, IRoutine
    {

        private HwAlignerGuide _alignerDevice = null;

        protected AlignerBaseRoutine()
        {
            Module = ModuleName.Aligner.ToString();
            _alignerDevice = DEVICE.GetDevice<HwAlignerGuide>($"TM.HiWinAligner");
        }


        public virtual Result Monitor()
        {
            return Result.DONE;
        }

        public virtual Result Start(params object[] objs)
        {
            return Result.RUN;
        }

        public virtual  void Abort()
        {
        }



        protected void AlignerSME(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Aligner SME");
                _alignerDevice.InitMachine();
                return true;
            }, () =>
            {
                return !_alignerDevice.IsBusy;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner SME Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        protected void MsgCheckHaveWafer(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"check  wafer exist or not");
                _alignerDevice.CheckWaferLoad();
                return true;
            }, () =>
            {
                if (!_alignerDevice.IsBusy)
                {
                    if (_alignerDevice.HaveWafer)
                    {
                        if (WaferManager.Instance.CheckNoWafer(Module, 0))
                        {
                            WaferManager.Instance.CreateWafer(ModuleHelper.Converter(Module), 0,Aitex.Core.Common.WaferStatus.Normal);
                        }
                        return true;
                    }
                    else
                    {
                        if (WaferManager.Instance.CheckHasWafer(Module, 0))
                        {
                            WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Module), 0);
                        }
                        return true;
                    }
                }
                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner Move to Robot Put Place Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void Home(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Aligner Home Msg");
                _alignerDevice.MsgHome();
                return true;
            }, () =>
            {
                return !_alignerDevice.IsBusy;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner home Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void CheckWafer(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Aligner Check Wafer");
                _alignerDevice.CheckWaferLoad();
                return true;
            }, () =>
            {
                if (!_alignerDevice.IsBusy)
                {
                    if (_alignerDevice.HaveWafer)
                    {
                        return true;
                    }
                    else
                    {
                        EV.PostAlarmLog(Module, $"Aligner Check No Wafer");
                        return null;
                    }
                }
                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner Check Wafer Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void OpenVacuum(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open Vacuum");
                _alignerDevice.OpenVacuum();
                return true;
            }, () =>
            {
                return !_alignerDevice.IsBusy;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner Open Vacuum Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void CloseVacuum(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Close Vacuum");
                _alignerDevice.CloseVaccum();
                return true;
            }, () =>
            {
                return !_alignerDevice.IsBusy;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner Close Vacuum Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void AlignerMove(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Alinger");
                _alignerDevice.DoAliger();
                return true;
            }, () =>
            {
                return !_alignerDevice.IsBusy;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner Alinger Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void AlignerMoveToRobotPutPalce(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Move to Robot Put Place");
                _alignerDevice.MoveToRobotPutPlace();
                return true;
            }, () =>
            {
                return !_alignerDevice.IsBusy;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner Move to Robot Put Place Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
    }
}
