using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks
{
    public class LoadLock : BaseDevice, IDevice, ILoadLock
    {
        public virtual double ChamberPressure { get; }
        public virtual double ForelinePressure { get; }
        public virtual bool EnableLift { get; }

        public const int SlotCount = 1;

        public LoadLock(string module): base(module, module, module, module)
        {
            
        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Name}.IsAtm", () => { return CheckAtm(); });
            DATA.Subscribe($"{Name}.IsVacuum", () => { return CheckVacuum(); });
            DATA.Subscribe($"{Name}.ChamberPressure", () => ChamberPressure);
            //DATA.Subscribe($"{Name}.ForelinePressure", () => ForelinePressure);
            //DATA.Subscribe($"{Name}.EnableLift", () => { return EnableLift; });

            return true;
        }

        public virtual bool  CheckRotationState()
        {
            return false;
        }

        public virtual bool CheckAtm()
        {
            return false;
        }

        public virtual bool CheckVacuum()
        {
            return false;
        }

        public virtual bool CheckTransferPressure()
        {
            return false;
        }

        public virtual bool CheckIsPumping()
        {
            return false;
        }

        public virtual void AutoCreatWafer()
        { 
        
        }

        public virtual bool SetFastVentValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetSlowVentValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetFastPumpValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }
 
        public virtual bool SetSlowPumpValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void Monitor()
        {

        }

        public virtual void Terminate()
        {

        }

        public virtual void Reset()
        {

        }

        public virtual bool CheckLidOpen()
        {
            return true;
        }

        public virtual bool CheckLidClose()
        {
            return true;
        }

        public virtual bool SetLift(bool isUp, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckLiftUp()
        {
            return true;
        }

        public virtual bool CheckLiftDown()
        {
            return true;
        }
        public virtual bool CheckTrayClamped()
        {
            return true;
        }

        public virtual bool CheckTrayUnClamped()
        {
            return true;
        }

        public virtual bool SetTrayClamped(bool clamp, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckWaferClamped()
        {
            return true;
        }
        public virtual bool CheckWaferUnClamped()
        {
            return true;
        }
        public virtual bool SetWaferClamped(bool clamp, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckWaferPlaced()
        {
            return true;
        }
        public virtual bool CheckTrayPlaced()
        {
            return true;
        }

        public virtual bool CheckEnableTransfer(EnumTransferType type)
        {
            return false;
        }

    }
}
