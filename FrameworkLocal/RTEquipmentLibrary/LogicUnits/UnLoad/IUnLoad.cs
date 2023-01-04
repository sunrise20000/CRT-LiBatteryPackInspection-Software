using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Device.Unit;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UnLoad
{
    public interface IUnLoad
    {
        bool CheckAtm();
        bool CheckVacuum();

        bool SetFastVentValve(bool isOpen, out string reason);
        bool SetSlowVentValve(bool isOpen, out string reason);

        bool SetFastPumpValve(bool isOpen, out string reason);
        bool SetSlowPumpValve(bool isOpen, out string reason);

        bool CheckLidOpen();
        bool CheckLidClose();

        bool SetLift(bool isUp, out string reason);
        bool CheckLiftUp();
        bool CheckLiftDown();

        bool CheckWaferClamped();

        bool CheckWaferUnClamped();

        bool SetWaferClamped(bool clamp, out string reason);

        bool CheckWaferPlaced();
        bool CheckTrayPlaced();
    }
}
