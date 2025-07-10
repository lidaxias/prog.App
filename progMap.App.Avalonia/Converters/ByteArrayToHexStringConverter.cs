using Avalonia.Data.Converters;
using Rss.TmFramework.Base.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Converters
{
    /// <summary>
    /// Конвертирует массив byte в HEX-строку
    /// </summary>
    public class ByteArrayToHexStringConverter : IValueConverter
    {
        const int U64 = 8;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is byte[] dataBytes))
                return null;

            try
            {
                return string.Concat(dataBytes.Select(b => $"0x{b:X2} ")).TrimEnd();
            }
            catch (Exception ex)
            {

                //MessageBoxWindow.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is string text))
                return null;
            try
            {
                var arrText = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(StringHelpers.ParseByte).ToArray();
                return arrText;
            }
            catch (Exception ex)
            {
                //MessageBoxWindow.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

        }
    }
}