using Avalonia.Data.Converters;
using Avalonia.Media;
using progMap.App.Avalonia.Models.Tests;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Converters
{
    /// <summary>
    /// Конвертирует цвет состояния теста в зависимости от результата теста
    /// </summary>
    public class TestCaseStateConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not ETestCommandState)
                throw new NotImplementedException();

            switch ((ETestCommandState)value)
            {
                case ETestCommandState.Empty:
                    return Brushes.White;
                case ETestCommandState.Pending:
                    return Brushes.Orange;
                case ETestCommandState.Completed:
                    return Brushes.Lime;
                case ETestCommandState.Error:
                    return Brushes.Red;
                case ETestCommandState.Ignored:
                    return null;
                default:
                    throw new NotImplementedException();
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
