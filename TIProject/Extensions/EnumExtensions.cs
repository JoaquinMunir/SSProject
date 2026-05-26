using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace TIProject.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum? enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            var type = enumValue.GetType();
            var memberName = enumValue.ToString();
            
            if (string.IsNullOrEmpty(memberName))
                return string.Empty;

            var memberInfo = type.GetMember(memberName).FirstOrDefault();

            if (memberInfo == null)
                return memberName;

            var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.Name ?? memberName;
        }
    }
}