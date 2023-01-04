using MECF.Framework.RT.EquipmentLibrary.Core.Attributes;
using MECF.Framework.RT.EquipmentLibrary.Core.Interfaces;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Errors
{
    public class HiwinRobotSensorErrors : IBitTypeClass
    {
        /// <summary>
        /// ERROR(201):机械臂取放晶圆前，真空状态已被启动。
        /// </summary>
        [BitTypeClassProperty(0, "ERROR(201):机械臂取放晶圆前，真空状态已被启动", true)]
        public bool Error201 { get; private set; }

        /// <summary>
        /// ERROR(202):机械臂取放晶圆后，真空状态无法被产生或接触。
        /// </summary>
        [BitTypeClassProperty(1, "ERROR(202):机械臂取放晶圆后，真空状态无法被产生或接触", true)]
        public bool Error202 { get; private set; }

        /// <summary>
        /// ERROR(203):机械臂Z轴硬件上极限开关被触发。
        /// </summary>
        [BitTypeClassProperty(2, "ERROR(203):机械臂Z轴硬件上极限开关被触发", true)]
        public bool Error203 { get; private set; }

        /// <summary>
        /// ERROR(204):机械臂Z轴硬件下极限开关被触发。
        /// </summary>
        [BitTypeClassProperty(3, "ERROR(204):机械臂Z轴硬件下极限开关被触发", true)]
        public bool Error204 { get; private set; }

        /// <summary>
        /// ERROR(205):机械臂取晶圆前或放晶圆后，光传感器状态已被启动。
        /// </summary>
        [BitTypeClassProperty(4, "ERROR(205):机械臂取晶圆前或放晶圆后，光传感器状态已被启动", true)]
        public bool Error205 { get; private set; }

        /// <summary>
        /// ERROR(206):机械臂取晶圆后或放晶圆前，光传感器状态已被解除。
        /// </summary>
        [BitTypeClassProperty(5, "ERROR(206):机械臂取晶圆后或放晶圆前，光传感器状态已被解除", true)]
        public bool Error206 { get; private set; }

        /// <summary>
        /// ERROR(207):机械臂磁簧开关为伸出状态，无法被缩回。
        /// </summary>
        [BitTypeClassProperty(6, "ERROR(207):机械臂磁簧开关为伸出状态，无法被缩回", true)]
        public bool Error207 { get; private set; }

        /// <summary>
        /// ERROR(208):机械臂磁簧开关为缩回状态，无法被伸出。
        /// </summary>
        [BitTypeClassProperty(7, "ERROR(208):机械臂磁簧开关为缩回状态，无法被伸出", true)]
        public bool Error208 { get; private set; }

        /// <summary>
        /// ERROR(209):机械臂H轴正硬限位被触发。
        /// </summary>
        [BitTypeClassProperty(8, "ERROR(209):机械臂H轴正硬限位被触发", true)]
        public bool Error209 { get; private set; }

        /// <summary>
        /// ERROR(210):机械臂H轴负硬限位被触发。
        /// </summary>
        [BitTypeClassProperty(9, "ERROR(210):机械臂H轴负硬限位被触发", true)]
        public bool Error210 { get; private set; }

        /// <summary>
        /// ERROR(211):机械臂翻转缸未翻至正面。
        /// </summary>
        [BitTypeClassProperty(10, "ERROR(211):机械臂翻转缸未翻至正面", true)]
        public bool Error211 { get; private set; }

        /// <summary>
        /// ERROR(212):机械臂翻转缸未翻至反面。
        /// </summary>
        [BitTypeClassProperty(11, "ERROR(212):机械臂翻转缸未翻至反面", true)]
        public bool Error212 { get; private set; }
    }
}
