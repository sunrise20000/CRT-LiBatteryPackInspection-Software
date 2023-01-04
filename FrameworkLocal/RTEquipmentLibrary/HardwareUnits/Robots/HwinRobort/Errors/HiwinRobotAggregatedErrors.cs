using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MECF.Framework.RT.EquipmentLibrary.Core.Extensions;
using MECF.Framework.RT.EquipmentLibrary.Core.Interfaces;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Errors
{
    public class HiwinRobotAggregatedErrors : IBitTypeClass
    {
        #region Variables

        private const int BIT_WIDTH = 16;
        private const string ERR_STRING_PATTERN = "^(\\d{4})\\s(\\d{4})\\s(\\d{4})\\s(\\d{4})\\s(\\d{4})\\s(\\d{4})$";

        #endregion

        #region Constructors

        public HiwinRobotAggregatedErrors()
        {
            Input = new HiwinRobotInputErrors();
            Sensor = new HiwinRobotSensorErrors();
            AxisT = new HiwinRobotServoErrors();
            AxisR = new HiwinRobotServoErrors();
            AxisZ = new HiwinRobotServoErrors();
            Controller = new HiwinRobotControllerErrors();
        }

        public HiwinRobotAggregatedErrors(string response)
        {
            var m = Regex.Match(response, ERR_STRING_PATTERN);
            if (!m.Success)
                throw new InvalidOperationException("the format of response string is incorrect.");

            if (m.Groups.Count != 7)
                throw new InvalidOperationException("the group count in response string is incorrect.");

            Input = int.TryParse(m.Groups[1].Value, NumberStyles.HexNumber, null, out var input)
                ? input.ToBitTypeClass<HiwinRobotInputErrors>(BIT_WIDTH)
                : 0xffff.ToBitTypeClass<HiwinRobotInputErrors>(BIT_WIDTH); // 如果转换错误，全部置为错误，以防呆

            Sensor = int.TryParse(m.Groups[2].Value, NumberStyles.HexNumber, null, out var sensor)
                ? sensor.ToBitTypeClass<HiwinRobotSensorErrors>(BIT_WIDTH)
                : 0xffff.ToBitTypeClass<HiwinRobotSensorErrors>(BIT_WIDTH); // 如果转换错误，全部置为错误，以防呆

            AxisT = int.TryParse(m.Groups[3].Value, NumberStyles.HexNumber, null, out var axisT)
                ? axisT.ToBitTypeClass<HiwinRobotServoErrors>(BIT_WIDTH)
                : 0xffff.ToBitTypeClass<HiwinRobotServoErrors>(BIT_WIDTH); // 如果转换错误，全部置为错误，以防呆

            AxisR = int.TryParse(m.Groups[4].Value, NumberStyles.HexNumber, null, out var axisR)
                ? axisR.ToBitTypeClass<HiwinRobotServoErrors>(BIT_WIDTH)
                : 0xffff.ToBitTypeClass<HiwinRobotServoErrors>(BIT_WIDTH); // 如果转换错误，全部置为错误，以防呆

            AxisZ = int.TryParse(m.Groups[5].Value, NumberStyles.HexNumber, null, out var axisZ)
                ? axisZ.ToBitTypeClass<HiwinRobotServoErrors>(BIT_WIDTH)
                : 0xffff.ToBitTypeClass<HiwinRobotServoErrors>(BIT_WIDTH); // 如果转换错误，全部置为错误，以防呆

            Controller = int.TryParse(m.Groups[6].Value, NumberStyles.HexNumber, null, out var cont)
                ? cont.ToBitTypeClass<HiwinRobotControllerErrors>(BIT_WIDTH)
                : 0xffff.ToBitTypeClass<HiwinRobotControllerErrors>(BIT_WIDTH); // 如果转换错误，全部置为错误，以防呆

        }

        #endregion

        #region Properties

        /// <summary>
        /// 输入错误
        /// </summary>
        public HiwinRobotInputErrors Input { get; }

        /// <summary>
        /// 传感器错误
        /// </summary>
        public HiwinRobotSensorErrors Sensor { get; }

        /// <summary>
        /// T轴错误
        /// </summary>
        public HiwinRobotServoErrors AxisT { get; }

        /// <summary>
        /// R轴错误
        /// </summary>
        public HiwinRobotServoErrors AxisR { get; }

        /// <summary>
        /// Z轴错误
        /// </summary>
        public HiwinRobotServoErrors AxisZ { get; }
        
        /// <summary>
        /// 控制器错误
        /// </summary>
        public HiwinRobotControllerErrors Controller { get; }

        /// <summary>
        /// 返回整合的错误信息列表。
        /// </summary>
        public List<string> AggregateErrorMessages
        {
            get
            {
                var errs = new List<string>();
                errs.AddRange(Input.AggregateErrorMessages());
                errs.AddRange(Sensor.AggregateErrorMessages());

                // 伺服轴的错误输出文本中加入轴名称
                errs.AddRange(AxisT.AggregateErrorMessages()
                    .Select(x=>string.Concat($"[{nameof(AxisT)}]", x)));
                errs.AddRange(AxisR.AggregateErrorMessages()
                    .Select(x => string.Concat($"[{nameof(AxisR)}]", x)));
                errs.AddRange(AxisZ.AggregateErrorMessages()
                    .Select(x => string.Concat($"[{nameof(AxisZ)}]", x)));

                errs.AddRange(Controller.AggregateErrorMessages());

                return errs;
            }
        }

        #endregion

        #region Methods

        public static bool IsErrResponse(string response)
        {
            return Regex.Match(response, ERR_STRING_PATTERN).Success;
        }

        #endregion
    }
}
