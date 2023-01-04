using MECF.Framework.RT.EquipmentLibrary.Core.Attributes;
using MECF.Framework.RT.EquipmentLibrary.Core.Interfaces;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Errors
{
    public class HiwinRobotInputErrors : IBitTypeClass
    {
        /// <summary>
        /// ERROR(101):巨集指令输入错误。
        /// </summary>
        [BitTypeClassProperty(0, "ERROR(101):巨集指令输入错误", true)]
        public bool Error101 { get; private set; }

        /// <summary>
        /// ERROR(102):站名输入错误。
        /// </summary>
        [BitTypeClassProperty(1, "ERROR(102):站名输入错误", true)]
        public bool Error102 { get; private set; }

        /// <summary>
        /// ERROR(103):轴名输入错误。
        /// </summary>
        [BitTypeClassProperty(2, "ERROR(103):轴名输入错误", true)]
        public bool Error103 { get; private set; }

        /// <summary>
        /// ERROR(104):群组名称输入错误。
        /// </summary>
        [BitTypeClassProperty(3, "ERROR(104):群组名称输入错误", true)]
        public bool Error104 { get; private set; }

        /// <summary>
        /// ERROR(105):引数输入错误。
        /// </summary>
        [BitTypeClassProperty(4, "ERROR(105):引数输入错误", true)]
        public bool Error105 { get; private set; }

        /// <summary>
        /// ERROR(106):输入距离超过软体极限。
        /// </summary>
        [BitTypeClassProperty(5, "ERROR(106):输入距离超过软体极限", true)]
        public bool Error106 { get; private set; }
    }
}
