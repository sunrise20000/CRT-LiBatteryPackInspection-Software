using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.SubstrateTrackings;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Buffers
{
    public class Buffer : BaseDevice, IDevice, IBuffer
    {
        public virtual bool IsMapped { get; set; }

        public Buffer(string module) : base(module, module, module, module)
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
