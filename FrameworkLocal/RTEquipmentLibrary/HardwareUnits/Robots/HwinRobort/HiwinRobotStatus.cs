using MECF.Framework.RT.EquipmentLibrary.Core.Attributes;
using MECF.Framework.RT.EquipmentLibrary.Core.Interfaces;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort
{
    public class HiwinRobotStatus : IBitTypeClass
    {

        #region Constructor


        #endregion

        #region Properties

        /// <summary>
        /// 返回是否已接收巨集指令尚未被执行。
        /// </summary>
        [BitTypeClassProperty(0, "已接收巨集指令尚未被执行")]
        public bool IsCommandNotExecuted { get; private set; }

        /// <summary>
        /// 返回是否正压力传感器被触发。
        /// </summary>
        [BitTypeClassProperty(1, "正压力传感器被触发")]
        public bool IsPosPressureSensorTriggered { get; private set; }

        /// <summary>
        /// 返回是否负压力传感器被触发。
        /// </summary>
        [BitTypeClassProperty(2, "负压力传感器被触发")]
        public bool IsNegPressureSensorTriggered { get; private set; }

        /// <summary>
        /// 返回是否电磁阀已被启动。
        /// </summary>
        [BitTypeClassProperty(3, "电磁阀已被启动")]
        public bool IsSolenoidValveTurnedOn { get; private set; }

        /// <summary>
        /// 返回是否单轴或多轴马达发生错误。
        /// </summary>
        [BitTypeClassProperty(4, "单轴或多轴马达发生错误", true)]
        public bool IsMotorError { get; private set; }

        /// <summary>
        /// 返回是否单个或多个极限开关被触发。
        /// </summary>
        [BitTypeClassProperty(5, "单个或多个极限开关被触发", true)]
        public bool IsEndLimitTriggered { get; private set; }

        /// <summary>
        /// 返回是否单个或多个轴伺服马达归零未成功。
        /// </summary>
        [BitTypeClassProperty(6, "单个或多个轴伺服马达归零未成功", true)]
        public bool IsHomingFailed { get; private set; }

        /// <summary>
        /// 保留位1。
        /// </summary>
        [BitTypeClassProperty(7, "保留位1")]
        public bool Reserved1 { get; private set; }

        /// <summary>
        /// 返回是否指令正在执行中。
        /// </summary>
        [BitTypeClassProperty(8, "指令正在执行中")]
        public bool IsCommandExecuting { get; private set; }

        /// <summary>
        /// 返回是否单个或多个轴正在移动中。
        /// </summary>
        [BitTypeClassProperty(9, "单个或多个轴正在移动中")]
        public bool IsMoving { get; private set; }

        /// <summary>
        /// 返回是否单个或多个马达已被解除励磁。
        /// </summary>
        [BitTypeClassProperty(10, "单个或多个马达已被解除励磁中", true)]
        public bool IsServoOff { get; private set; }

        /// <summary>
        /// 返回是否已回原点但RW轴未在原点位置。
        /// </summary>
        [BitTypeClassProperty(11, "已回原点但RW轴未在原点位置", true)]
        public bool IsHomedButRWAxis { get; private set; }

        /// <summary>
        /// 返回是否已回原点并且处于缩回状态。
        /// </summary>
        [BitTypeClassProperty(12, "已回原点并且处于缩回状态")]
        public bool IsHomedAndRetracted { get; private set; }

        /// <summary>
        /// 保留位2。
        /// </summary>
        [BitTypeClassProperty(13, "保留位2")]
        public bool Reserved2 { get; private set; }

        /// <summary>
        /// 返回是否控制器返回错误。
        /// </summary>
        [BitTypeClassProperty(14, "控制器返回错误", true)]
        public bool IsControllerError { get; private set; }

        /// <summary>
        /// 保留位3。
        /// </summary>
        [BitTypeClassProperty(15, "保留位3")]
        public bool Reserved3 { get; private set; }

        #endregion
    }
}
