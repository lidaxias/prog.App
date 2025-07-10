using Avalonia.Data.Converters;
using Avalonia.Media;
using Rss.TmFramework.Base.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Converters
{
    public class LogMessageStatusConverter : IValueConverter
    {
        public static LogMessageStatusConverter Default => new LogMessageStatusConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ELogMessageType)
            {
                var messageStatus = (ELogMessageType)value;

                switch (messageStatus)
                {
                    case ELogMessageType.Debug:
                        return Brushes.DarkGray;
                    case ELogMessageType.Information:
                        return Brushes.Black;
                    case ELogMessageType.Warning:
                        return Brushes.DarkGoldenrod;
                    case ELogMessageType.Error:
                        return Brushes.Red;
                    case ELogMessageType.Result:
                        return Brushes.DarkViolet;
                    default:
                        break;
                }
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Brush)
            {
                var messageColor = (Brush)value;

                if (messageColor == Brushes.DarkGray)
                    return ELogMessageType.Debug;
                else if (messageColor == Brushes.Black)
                    return ELogMessageType.Information;
                else if (messageColor == Brushes.DarkGoldenrod)
                    return ELogMessageType.Warning;
                else if (messageColor == Brushes.Red)
                    return ELogMessageType.Error;
                else if (messageColor == Brushes.DarkViolet)
                    return ELogMessageType.Result;
            }

            return null;
        }

    }
}
