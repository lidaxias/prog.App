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
    /// Конвертирует массив byte в uint
    /// </summary>
    public class ByteArrayToUIntConverter : IValueConverter
    {
        #region Const
        // соответствие типам количеству байт
        const int U16 = 2;
        const int U32 = 4;
        const int U64 = 8;
        #endregion

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is byte[] dataBytes))
                return null;

            if ((byte[])value == Array.Empty<byte>())
                return null;

            try
            {
                return dataBytes.Length switch
                {
                    U16 => $"0x{BitConverter.ToUInt16(dataBytes):X4}",
                    U32 => $"0x{BitConverter.ToUInt32(dataBytes):X8}",
                    U64 => $"0x{BitConverter.ToUInt64(dataBytes):X16}",

                };
            }

            catch (Exception ex)
            {
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is string number))
                return null;

            if ((string)value == "")
                return null;

            try
            {
                var uintNumber = StringHelpers.ParseUint(number);
                var bytesNumber = BitConverter.GetBytes(uintNumber);
                return bytesNumber;
            }

            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
