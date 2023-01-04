
using System;
using shortid.Configuration;

namespace Sicentury.Core
{
    public class GlobalDefs
    {
        /// <summary>
        /// 比较两个Double型变量是否相等时的最小误差值。
        /// </summary>
        public const double DBL_VAL_COMPARISION_TOLERANCE = 1e-6;

        
        /// <summary>
        /// 比较两个Double型变量是否相等。
        /// <para>注意：未考虑NaN和Infinity情况。</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>

        public static bool IsDoubleValueEquals(double a, double b, double tolerance = DBL_VAL_COMPARISION_TOLERANCE)
        {
            return Math.Abs(a - b) < tolerance;
        }

        /// <summary>
        /// 尝试将制定的字符串转换为Double型。如果转换失败，返回默认值。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="valueIfFault"></param>
        /// <returns></returns>
        public static double TryParseToDouble(string str, double valueIfFault = 0)
        {
            var ret = double.TryParse(str, out var dbv);
            if (ret == false)
                dbv = valueIfFault;
            return dbv;
        }

        /// <summary>
        /// 获取指定长度和格式的唯一识别码。
        /// </summary>
        /// <param name="length"></param>
        /// <param name="isUseNumber"></param>
        /// <returns></returns>
        public static string GetShortUid(int length = 8, bool isUseNumber = true)
        {
            return shortid.ShortId.Generate(new GenerationOptions(isUseNumber, false, length));
        }
    }
}
