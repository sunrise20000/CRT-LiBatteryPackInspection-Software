using MECF.Framework.RT.EquipmentLibrary.Core.Attributes;
using MECF.Framework.RT.EquipmentLibrary.Core.Interfaces;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Errors
{
    public class HiwinRobotControllerErrors : IBitTypeClass
    {
        /// <summary>
        /// ERROR(401):紧急停止开关被触发。
        /// </summary>
        [BitTypeClassProperty(0, "ERROR(401):紧急停止开关被触发", true)]
        public bool Error401 { get; private set; }

        /// <summary>
        /// ERROR(402):控制器电源故障。
        /// </summary>
        [BitTypeClassProperty(1, "ERROR(402):控制器电源故障", true)]
        public bool Error402 { get; private set; }

        /// <summary>
        /// ERROR(403):控制器电压过低。
        /// </summary>
        [BitTypeClassProperty(2, "ERROR(403):控制器电压过低", true)]
        public bool Error403 { get; private set; }

        /// <summary>
        /// ERROR(404):控制器电压过高。
        /// </summary>
        [BitTypeClassProperty(3, "ERROR(404):控制器电压过高", true)]
        public bool Error404 { get; private set; }

        /// <summary>
        /// ERROR(405):控制器判定驱动器故障。
        /// </summary>
        [BitTypeClassProperty(4, "ERROR(405):控制器判定驱动器故障", true)]
        public bool Error405 { get; private set; }

        /// <summary>
        /// ERROR(406):控制器电压异常。
        /// </summary>
        [BitTypeClassProperty(5, "ERROR(406):控制器电压异常", true)]
        public bool Error406 { get; private set; }

        /// <summary>
        /// ERROR(407):控制器无法辨识驱动器部。
        /// </summary>
        [BitTypeClassProperty(6, "ERROR(407):控制器无法辨识驱动器部", true)]
        public bool Error407 { get; private set; }

        /// <summary>
        /// ERROR(408):驱动器UPS故障。
        /// </summary>
        [BitTypeClassProperty(7, "ERROR(408):驱动器UPS故障", true)]
        public bool Error408 { get; private set; }
    }
}
