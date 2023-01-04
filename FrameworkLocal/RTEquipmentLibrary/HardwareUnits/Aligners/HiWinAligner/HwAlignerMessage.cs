
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.HwAligner
{
    public class HwAlignerMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }
}
