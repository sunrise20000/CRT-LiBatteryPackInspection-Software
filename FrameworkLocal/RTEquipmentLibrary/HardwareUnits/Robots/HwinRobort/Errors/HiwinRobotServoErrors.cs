using MECF.Framework.RT.EquipmentLibrary.Core.Attributes;
using MECF.Framework.RT.EquipmentLibrary.Core.Interfaces;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Errors
{
    public class HiwinRobotServoErrors : IBitTypeClass

    {
    /// <summary>
    /// ERROR(301):马达尚未励磁或励磁失败。
    /// </summary>
    [BitTypeClassProperty(0, "ERROR(301):马达尚未励磁或励磁失败", true)]
    public bool Error301 { get; private set; }

    /// <summary>
    /// ERROR(302):马达未归原位或归原位失败。
    /// </summary>
    [BitTypeClassProperty(1, "ERROR(302):马达未归原位或归原位失败", true)]
    public bool Error302 { get; private set; }

    /// <summary>
    /// ERROR(303):马达正在移动。
    /// </summary>
    [BitTypeClassProperty(2, "ERROR(303):马达正在移动", true)]
    public bool Error303 { get; private set; }

    /// <summary>
    /// ERROR(304):跟随误差超过设定的最大位置误差。
    /// </summary>
    [BitTypeClassProperty(3, "ERROR(304):跟随误差超过设定的最大位置误差", true)]
    public bool Error304 { get; private set; }

    /// <summary>
    /// ERROR(305):伺服马达编码器错误。
    /// </summary>
    [BitTypeClassProperty(4, "ERROR(305):伺服马达编码器错误", true)]
    public bool Error305 { get; private set; }

    /// <summary>
    /// ERROR(306):伺服马达编码器故障。
    /// </summary>
    [BitTypeClassProperty(5, "ERROR(306):伺服马达编码器故障", true)]
    public bool Error306 { get; private set; }

    /// <summary>
    /// ERROR(307):私服马达温度过高。
    /// </summary>
    [BitTypeClassProperty(6, "ERROR(307):私服马达温度过高", true)]
    public bool Error307 { get; private set; }

    /// <summary>
    /// ERROR(308):马达移动至正软限位位置。
    /// </summary>
    [BitTypeClassProperty(7, "ERROR(308):马达移动至正软限位位置", true)]
    public bool Error308 { get; private set; }

    /// <summary>
    /// ERROR(309):马达移动至负软限位位置。
    /// </summary>
    [BitTypeClassProperty(8, "ERROR(309):马达移动至负软限位位置", true)]
    public bool Error309 { get; private set; }

    /// <summary>
    /// ERROR(310):马达某轴速度或加速度参数异常。
    /// </summary>
    [BitTypeClassProperty(9, "ERROR(310):马达某轴速度或加速度参数异常", true)]
    public bool Error310 { get; private set; }

    /// <summary>
    /// ERROR(311):T/Z/H轴归原点时，R/W轴尚未归原点。
    /// </summary>
    [BitTypeClassProperty(10, "ERROR(311):T/Z/H轴归原点时，R/W轴尚未归原点", true)]
    public bool Error311 { get; private set; }

    /// <summary>
    /// ERROR(312):T/Z/H轴归原点时，R/W轴尚未缩回到安全位置。
    /// </summary>
    [BitTypeClassProperty(11, "ERROR(312):T/Z/H轴归原点时，R/W轴尚未缩回到安全位置", true)]
    public bool Error312 { get; private set; }

    /// <summary>
    /// ERROR(313):控制器温度过高。
    /// </summary>
    [BitTypeClassProperty(12, "ERROR(313):控制器温度过高", true)]
    public bool Error313 { get; private set; }
    }
}
