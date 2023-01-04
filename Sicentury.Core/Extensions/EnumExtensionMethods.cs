using System;
using System.Linq;

namespace Sicentury.Core.Extensions
{
    public static class EnumExtensionMethods
    {
        public static string GetDescription(this Enum genericEnum)
        {
            var genericEnumType = genericEnum.GetType();
            var memberInfo = genericEnumType.GetMember(genericEnum.ToString());
            if ((memberInfo.Length <= 0)) 
                return genericEnum.ToString();
            
            var attributes = memberInfo[0]
                .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
            if ((attributes.Any()))
            {
                return ((System.ComponentModel.DescriptionAttribute)attributes.ElementAt(0)).Description;
            }

            return genericEnum.ToString();
        }
    }

}
