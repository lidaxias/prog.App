using Avalonia.Data.Converters;
using Rss.TmFramework.Base.Helpers;
using System;
using System.Globalization;

namespace progMap.App.Avalonia.Converters
{
    /// <summary>
    /// Конвертирует значение типа uint в строку
    /// </summary>
    public class UIntToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is uint uintAddress))
                return null;

            var stringAddress = uintAddress.ToHexString();
            return stringAddress;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is string stringAddress))
                return null;

            StringHelpers.TryParseUint(stringAddress, out uint result);
            return result;
        }
    }
}
