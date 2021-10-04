using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Common
{
    public static class TypeChanger
    {
        public static object ChangeType(this object value, Type conversionType)
        {
            if (conversionType == null)
                throw new ArgumentNullException(nameof(conversionType));

            if (value == null || value == DBNull.Value)
                return null;

            if ((conversionType == typeof(DateTime?) || conversionType == typeof(decimal?)) && string.IsNullOrWhiteSpace(value.ToString()))
                return null;

            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nullableConverter = new NullableConverter(conversionType);
                conversionType = nullableConverter.UnderlyingType;
            }

            if (conversionType == typeof(Guid))
                return Guid.Parse(value.ToString());

            if (!conversionType.IsEnum)
                return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);

            if (value is string)
                return Enum.Parse(conversionType, (string)value);

            var valueEut = Convert.ChangeType(value, Enum.GetUnderlyingType(conversionType));
            var enumValue = Enum.ToObject(conversionType, valueEut);
            var enumFirstSymbol = enumValue.ToString()[0];
            if (!char.IsDigit(enumFirstSymbol) && enumFirstSymbol != '-')
                return enumValue;

            throw new ArgumentOutOfRangeException($"{value} is invalid value for {conversionType}");
        }

        public static T ChangeType<T>(this object value)
        {
            if (value == null || value == DBNull.Value)
                return default(T);

            var conversionType = typeof(T);

            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nullableConverter = new NullableConverter(conversionType);
                conversionType = nullableConverter.UnderlyingType;
            }

            if (conversionType == typeof(Guid))
                return (T)ChangeType(value, typeof(Guid));

            if (!conversionType.IsEnum)
                return (T)Convert.ChangeType(value, conversionType);

            if (value is string)
                return (T)Enum.Parse(conversionType, (string)value);

            var valueEut = Convert.ChangeType(value, Enum.GetUnderlyingType(conversionType));
            var enumValue = (T)Enum.ToObject(conversionType, valueEut);
            var enumFirstSymbol = enumValue.ToString()[0];
            if (!char.IsDigit(enumFirstSymbol) && enumFirstSymbol != '-')
                return enumValue;

            throw new ArgumentOutOfRangeException($"{value} is invalid value for {conversionType}");
        }
    }
}
