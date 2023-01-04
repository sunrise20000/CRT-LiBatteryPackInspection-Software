using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MECF.Framework.RT.EquipmentLibrary.Core.Attributes;

namespace MECF.Framework.RT.EquipmentLibrary.Core.Extensions
{
    public static class BitTypeClassExtension
    {
        /// <summary>
        /// 返回类型中以<see cref="BitTypeClassPropertyAttribute"/>标记的公共属性。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static List<PropertyInfo> GetProperties(IReflect type)
        {
            var piList = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.PropertyType == typeof(bool) &&
                            x.GetCustomAttributes(true)
                                .OfType<BitTypeClassPropertyAttribute>()
                                .Any());

            return piList.ToList();
        }

        /// <summary>
        /// 将整数转换为包含Bit定义的类。
        /// <para>注意：</para>
        /// <para>目标类中和输入整数bit对应的属性需要用<see cref="BitTypeClassPropertyAttribute"/>标记。</para>
        /// </summary>
        /// <typeparam name="T">包含Bit属性定义的类的类型。</typeparam>
        /// <param name="bitValueSource">待转换的整数。</param>
        /// <param name="bitLen">有效的位长度。</param>
        /// <returns></returns>
        public static T ToBitTypeClass<T>(this int bitValueSource, int bitLen)
            where T : new()
        {
            var obj = new T();

            for (var i = 0; i < bitLen; i++)
            {
                var pi = typeof(T)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(x => x.PropertyType == typeof(bool) &&
                                         x.GetCustomAttributes(true)
                                             .OfType<BitTypeClassPropertyAttribute>()
                                             .Any(a => a.BitIndex == i));
                if (pi != null)
                {
                    pi.SetValue(obj, (bitValueSource & (1 << i)) != 0 ? true : false);
                }

            }

            return (T)obj;
        }

        /// <summary>
        /// 将BitType对象中以<see cref="BidArgumentTypeAttribute"/>标记的属性合并整理为int。
        /// </summary>
        /// <typeparam name="T">包含Bit属性定义的类的类型。</typeparam>
        /// <param name="obj">对象实例。</param>
        /// <returns></returns>
        public static int FromBitTypeClass<T>(this T obj)
        {
            var piList = GetProperties(typeof(T));
            if (!piList.Any())
                return -1;

            var integValue = 0;

            foreach (var pi in piList)
            {
                // 如果该属性没有用BitTypeClassPropertyAttribute标记，则忽略
                if (!(pi.GetCustomAttribute(typeof(BitTypeClassPropertyAttribute)) is BitTypeClassPropertyAttribute
                        attr))
                    continue;
                
                // 如果该属性的返回值不是bool型，则忽略
                if (!(pi.GetValue(obj) is bool v)) 
                    continue;
                   
                // 如果该属性为True，则将返回整数的指定Bit设置为1.
                if(v)
                    integValue |= (1 << attr.BitIndex);
            }

            return integValue;
        }

        /// <summary>
        /// 从BitType类聚合错误信息。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static List<string> AggregateErrorMessages<T>(this T obj)
        {
            var piList = GetProperties(typeof(T));
            var piForError = piList.Where(pi =>
                pi.GetCustomAttributes()
                    .OfType<BitTypeClassPropertyAttribute>()
                    .Any(a => a.IsErrorBit))
                .ToList();


            var errs = new List<string>();
            foreach (var pi in piForError)
            {
                if (!(pi.GetValue(obj) is bool vv)) 
                    continue;

                if (!vv) 
                    continue;

                var attr = pi.GetCustomAttribute(
                    typeof(BitTypeClassPropertyAttribute)) as BitTypeClassPropertyAttribute;
                
                errs.Add(attr?.Message??$"Cannot get message from {nameof(BitTypeClassPropertyAttribute)} of property {pi.Name} of object {obj}");
            }

            return errs;
        }
    }
}
