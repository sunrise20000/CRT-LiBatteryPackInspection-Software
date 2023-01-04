using Aitex.Core.RT.Device;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.EFEM
{
    public class EFEM : BaseDevice, IDevice, IEFEM
    {

        public virtual double ChamberPressure { get; }
        public virtual bool IsMapped { get; set; }

        public EFEM(string module) : base(module, module, module, module)
        {
        }


        public virtual bool Initialize()
        {
            return true;
        }

        public void Monitor()
        {


        }

        public void Terminate()
        {

        }


        public virtual void Reset()
        {

        }

        public virtual bool IsEnableTransferWafer(out string reason)
        {

            reason = "";
            return true;
        }

        public virtual bool IsEnableMapWafer(out string reason)
        {

            reason = "";
            return true;
        }

        public virtual void ConfirmWaferPresent()
        {

        }

        public virtual bool CheckSlitValveOpen(ModuleName module, ModuleName robot)
        {
            return true;
        }

        public virtual bool CheckSlitValveClose(ModuleName module, ModuleName robot)
        {
            return true;
        }


        public virtual bool SetSlitValve(ModuleName module, ModuleName robot, bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }
    }
}
