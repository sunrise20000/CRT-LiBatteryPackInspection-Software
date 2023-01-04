using Aitex.Core.RT.Device;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Cassette
{
    public class Cassette : BaseDevice, IDevice, ICassette
    {
        public virtual bool IsMapped { get; set; }

        public Cassette(string module) : base(module, module, module, module)
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


        public virtual void OnSlotMapRead(string slotMap)
        {
        }
    }
}
