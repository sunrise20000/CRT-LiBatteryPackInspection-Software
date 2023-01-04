// /************************************************************************
// * @file         BaseRoutineWithDeviceLocker.cs
// * @author   Su Liang
// * @date       2022/11/18
// *
// * @copyright &copy Sicentury Inc.
// *
// * @brief
// *
// * @details
// *
// *
// * *****************************************************************************/

using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Sicentury.Core;

namespace MECF.Framework.RT.EquipmentLibrary.Core
{
    public class BaseRoutineWithDeviceLocker : ModuleRoutine
    {
        private readonly DeviceLocker _lockerPump2;

        public BaseRoutineWithDeviceLocker()
        {
            _lockerPump2 = DeviceLockerManager.Instance.GetLocker(DeviceLockerManager.LockerNames.Pump2);
        }

        protected bool LockPump2(out string reason, int timeoutMs = 30*60*1000)
        {
            reason = "";
            EV.PostInfoLog(Module, $"Routine {Name} is locking the Pump2");
            var ret = _lockerPump2?.TryLock(Module, out reason, timeoutMs);
            if (ret.HasValue == false || ret == DeviceLocker.Results.Ok ||
                ret == DeviceLocker.Results.IHaveLocker) // 不需要锁定、已锁定成功、或已占有该锁
            {
                EV.PostInfoLog(Module, $"Routine {Name} locked the Pump2, Result: {ret}");
                return true;
            }

            // 无法锁定设备。
            EV.PostWarningLog(Module, $"Routine {Name} was unable to lock Pump2, {reason}");
            return false;
        }

        protected bool UnlockPump2(out string reason)
        {
            reason = "";
            EV.PostInfoLog(Module, $"Routine {Name} is unlocking the Pump2");
            var ret = _lockerPump2?.TryUnlock(Module, out reason);
            if (ret.HasValue == false || ret == DeviceLocker.Results.Ok) // 不需要锁定、解锁成功
            {
                EV.PostInfoLog(Module, $"Routine {Name} unlock the Pump2, Result: {ret}");
                return true;
            }

            EV.PostWarningLog(Module, $"Routine {Name} was unable to unlock pump2, {reason}");
            return false;
        }

        public void ResetLocker()
        {
            _lockerPump2?.Reset();
            EV.PostInfoLog(Module, $"Routine {Name} Pump2 Locker reset");
        }

        public virtual void Abort()
        {
            CancelLockWaiting();
            UnlockPump2(out _);
        }

        /// <summary>
        /// 取消等待锁定Pump2。
        /// </summary>
        public void CancelLockWaiting()
        {
            _lockerPump2?.CancelLockWaiting();
        }
    }
}