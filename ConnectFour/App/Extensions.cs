using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectFour
{
    public static class EnumsExtensions
    {
        /// <summary>
        /// Get DescriptionAttribute for the enum.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Description<T>(this T value) where T : struct
        {
            Type type = value.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
            }

            System.Reflection.MemberInfo[] memberInfo = type.GetMember(value.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    return ((System.ComponentModel.DescriptionAttribute)attrs[0]).Description;
                }
            }

            // If no description attribute found, just return the ToString of the enum
            return value.ToString();
        }
    }
}
