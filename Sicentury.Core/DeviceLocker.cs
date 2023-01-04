// /************************************************************************
// * @file     DeviceLocker.cs
// * @author   Su Liang
// * @date     2022/11/16
// *
// * @copyright &copy Sicentury Inc.
// *
// * @brief    设备资源锁定器。
// *
// * @details  当某个设备或其它资源不能被多个线程同时使用时，可使用此锁定器实现资源互斥操作。
// *
// *
// * *****************************************************************************/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Sicentury.Core
{
    public class DeviceLocker
    {
        public enum  Results
        {
            Ok,
            Error,
            IHaveLocker
        }
        
        private readonly object _syncRoot = new object();
        
        /// <summary>
        /// 返回占有该设备的对象名称.
        /// <para>关于可能使用到的占用者名称，请参考枚举类型<see cref="ModuleName"/>。</para>
        /// </summary>
        private readonly List<string> _occupiersCollection;
        private CancellationTokenSource _cts;
        private SemaphoreSlim _semResource;
        private readonly int _maxRes;

        #region Constructor

        public DeviceLocker(string lockerName, int maxResCount)
        {
            LockerName = lockerName;
            _occupiersCollection = new List<string>();
            _maxRes = maxResCount;
            _semResource = new SemaphoreSlim(maxResCount, maxResCount);
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// 设备名称。
        /// </summary>
        public string LockerName { get; }

        #endregion

        #region MyRegion

        /// <summary>
        /// 锁定设备。
        /// </summary>
        /// <param name="occupier"></param>
        /// <param name="ct"></param>
        /// <param name="timeoutInMillisecond"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public Results TryLock(string occupier, out string reason, int timeoutInMillisecond)
        {
            lock (_syncRoot)
            {
                var a = _occupiersCollection.FirstOrDefault(x => x == occupier.ToString());
                if (a != null)
                {
                    reason = $"{LockerName} had been lock by {occupier}";
                    return Results.IHaveLocker;
                }

                if (_cts != null)
                {
                    reason = $"{LockerName} is waiting to be locked";
                    return Results.Error;
                }
                
                _cts = new CancellationTokenSource();
            }

            try
            {
                if (_semResource.Wait(timeoutInMillisecond, _cts.Token))
                {
                    _occupiersCollection.Add(occupier);
                    reason = "";
                    return Results.Ok;
                }

                reason =
                    $"Unable to lock {LockerName}, no enough resources, locked by {string.Join(", ", _occupiersCollection)}";
                return Results.Error;
            }
            catch (OperationCanceledException)
            {
                reason = $"It's canceled to lock {LockerName}";
                return Results.Error;
            }
            catch (Exception ex)
            {
                reason =
                    $"Unable to lock {LockerName}, {ex.Message}";
                return Results.Error;
            }
            finally
            {
                // 无论锁定是否成功，均销毁CancellationTokenSource。
                _cts = null;
            }
        }

        /// <summary>
        /// 释放设备。
        /// </summary>
        /// <param name="occupier"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public Results TryUnlock(string occupier, out string reason)
        {
            lock (_syncRoot)
            {
                var a = _occupiersCollection.FirstOrDefault(x => x == occupier);
                if (a == null)
                {
                    reason = $"{LockerName} is never locked by {occupier}";
                    return Results.Error;
                }

                try
                {
                    _semResource.Release(1);
                    _occupiersCollection.Remove(occupier);
                    reason = "";
                    return Results.Ok;
                }
                catch (Exception ex)
                {
                    reason = $"Unable to lock {LockerName} by {occupier}, {ex.Message}";
                    return Results.Error;
                }
            }
        }

        /// <summary>
        /// 取消等待可用的锁。
        /// </summary>
        public void CancelLockWaiting()
        {
            _cts?.Cancel();
        }
        
        public void Reset()
        {
            lock (_syncRoot)
            {
                _cts?.Cancel();
                Thread.Sleep(100); // 等待 Semaphore Cancel
                _semResource = new SemaphoreSlim(_maxRes, _maxRes);
                _occupiersCollection.Clear();
            }
        }
        
        #endregion
    }
}