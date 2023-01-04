// /************************************************************************
// * @file         DeviceLockerNotFoundException.cs
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

using System;

namespace MECF.Framework.RT.EquipmentLibrary.Core.Exceptions
{
    public class DeviceLockerNotFoundException : Exception
    {
        public DeviceLockerNotFoundException(string lockerName) : base(
            $"unable to find the device locker with name {lockerName}")
        {
            LockerName = lockerName;
        }

        public string LockerName { get; }
    }
}