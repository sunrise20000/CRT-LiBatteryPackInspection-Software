// /************************************************************************
// * @file     DeviceLockerManager.cs
// * @author   Su Liang
// * @date     2022/11/18
// *
// * @copyright &copy Sicentury Inc.
// *
// * @brief    设备锁定控制类。
// *
// * @details  系统中的某些设备会被多个模块共同使用，例如Pump2被LoadLock和UnLoad共用，这类设备
// *           无法同时被使用；为提高设备利用率，并解决设备冲突竞争问题，请使用该类对设备进行锁定。
// *
// * *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading;
using Aitex.Core.Util;
using MECF.Framework.RT.EquipmentLibrary.Core.Exceptions;
using Sicentury.Core;

namespace MECF.Framework.RT.EquipmentLibrary.Core
{
    public class DeviceLockerManager : Singleton<DeviceLockerManager>
    {
        public enum LockerNames
        {
            Pump2
        }

        private readonly Dictionary<string, DeviceLocker> _lockerCollection;

        public DeviceLockerManager()
        {
            _lockerCollection = new Dictionary<string, DeviceLocker>();
        }
        
        public void Initialize()
        {
            _lockerCollection.Add(LockerNames.Pump2.ToString(), new DeviceLocker(LockerNames.Pump2.ToString(), 1));
        }

        /// <summary>
        /// 获取指定名称的锁。
        /// <para>通常一个锁对应一个设备，系统中可用的锁放在列表中，锁名称使用<see cref="LockerNames"/>枚举定义。</para>
        /// </summary>
        /// <param name="lockerNames"></param>
        /// <returns></returns>
        public DeviceLocker GetLocker(LockerNames lockerNames)
        {
            if (_lockerCollection.ContainsKey(lockerNames.ToString()) == false)
                return null;

            return _lockerCollection[lockerNames.ToString()];
        }


        /// <summary>
        /// 锁定指定的设备锁。
        /// </summary>
        /// <param name="lockerName">锁名称</param>
        /// <param name="occupier">占用者名称，当前锁被谁占用。</param>
        /// <param name="reason">锁定失败后的错误信息。</param>
        /// <param name="ct">终止等待锁定。<see cref="CancellationToken"/></param>
        /// <param name="timeoutMillisecond">等待锁定的超时时间，超过此时间后如果还未成功锁定，则返回False</param>
        /// <returns>True：锁定成功；False：锁定失败</returns>
        public DeviceLocker.Results TryLock(string lockerName, string occupier, out string reason, int timeoutMillisecond = 50)
        {
            if (_lockerCollection.ContainsKey(lockerName) == false)
                throw new DeviceLockerNotFoundException(lockerName);

            var locker = _lockerCollection[lockerName];
            return locker.TryLock(occupier, out reason, timeoutMillisecond);
        }

        /// <summary>
        /// 锁定指定的设备锁。
        /// </summary>
        /// <param name="lockerName">锁名称</param>
        /// <param name="occupier">占用者名称，当前锁被谁占用。</param>
        /// <param name="reason">锁定失败后的错误信息。</param>
        /// <param name="ct">终止等待锁定。<see cref="CancellationToken"/></param>
        /// <param name="timeoutMillisecond">等待锁定的超时时间，超过此时间后如果还未成功锁定，则返回False</param>
        /// <returns>True：锁定成功；False：锁定失败</returns>
        public DeviceLocker.Results TryLock(LockerNames lockerName, string occupier, out string reason, int timeoutMillisecond = 50)
        {
            return TryLock(lockerName.ToString(), occupier, out reason, timeoutMillisecond);
        }


        /// <summary>
        /// 解除设备锁定。
        /// </summary>
        /// <param name="lockerName">锁名称</param>
        /// <param name="occupier">占用者名称，当前锁被谁解锁。</param>
        /// <para>注意：只能解锁被自己锁定的设备锁！</para>
        /// <param name="reason">解锁失败后的错误信息</param>
        /// <returns>True：解锁成功；False：解锁失败</returns>
        public DeviceLocker.Results TryUnLock(string lockerName, string occupier, out string reason)
        {
            if (_lockerCollection.ContainsKey(lockerName) == false)
                throw new DeviceLockerNotFoundException(lockerName);
            
            var locker = _lockerCollection[lockerName];
            return locker.TryUnlock(occupier, out reason);
        }

        /// <summary>
        /// 解除设备锁定。
        /// </summary>
        /// <param name="lockerName">锁名称</param>
        /// <param name="occupier">占用者名称，当前锁被谁解锁。</param>
        /// <para>注意：只能解锁被自己锁定的设备锁！</para>
        /// <param name="reason">解锁失败后的错误信息</param>
        /// <returns>True：解锁成功；False：解锁失败</returns>
        public DeviceLocker.Results TryUnLock(LockerNames lockerName, string occupier, out string reason)
        {
            return TryUnLock(lockerName.ToString(), occupier, out reason);
        }
        
        /// <summary>
        /// 取消等待可用的锁。
        /// </summary>
        /// <param name="lockerName">锁名称</param>
        /// <exception cref="DeviceLockerNotFoundException"></exception>
        public void CancelLockWaiting(string lockerName)
        {
            if (_lockerCollection.ContainsKey(lockerName) == false)
                throw new DeviceLockerNotFoundException(lockerName);

            var locker = _lockerCollection[lockerName];
            locker.CancelLockWaiting();
        }


        /// <summary>
        /// 复位设备锁。
        /// </summary>
        /// <param name="lockerName">锁名称</param>
        /// <param name="reason">复位失败原因</param>
        /// <returns>True：复位成功；False：复位失败</returns>
        public DeviceLocker.Results Reset(string lockerName, out string reason)
        {
            if (_lockerCollection.ContainsKey(lockerName) == false)
                throw new DeviceLockerNotFoundException(lockerName);

            reason = "";
            var locker = _lockerCollection[lockerName];
            locker.Reset();
            return DeviceLocker.Results.Ok;
        }

        /// <summary>
        /// 复位设备锁。
        /// </summary>
        /// <param name="lockerName">锁名称</param>
        /// <param name="reason">复位失败原因</param>
        /// <returns>True：复位成功；False：复位失败</returns>
        public DeviceLocker.Results Reset(LockerNames lockerName, out string reason)
        {
            return Reset(lockerName.ToString(), out reason);
        }
    }
}