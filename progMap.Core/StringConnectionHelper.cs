using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.Core
{
    public static partial class StringConnectionHelper
    {
        /// <summary>
        /// Проверяет правильность ввода строки соединения, 
        /// для Линукс систем позволено вводить simlink для устройств
        /// </summary>
        /// <param name="connectionString">строка соединения</param>
        /// <returns>строка соединения</returns>
        public static string GetComConnectionString(string connectionString)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.WinCE:
                    return connectionString;
                case PlatformID.Unix:
                    // разбор строки соединения
                    var lines = connectionString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    // определение, первый элемент строки соединения - путь к файлу устройства
                    FileInfo fileInfo = new FileInfo(lines[0]);
                    // определение, является ли указанный путь -  путь ссылки на файл устройства
                    if (fileInfo.LinkTarget == null)
                        return connectionString;
                    // указанный путь - ссылка на файл устройства, изменение пути к файлу на то, куда указывает эта ссылка
                    lines[0] = Path.Combine(fileInfo.DirectoryName, fileInfo.LinkTarget);
                    // формирование строки соединения с измененным путем файла устройства
                    return string.Concat(lines.Select((line) => line + ","));
                case PlatformID.Xbox:
                case PlatformID.MacOSX:
                case PlatformID.Other:
                default:
                    return connectionString;
            }
        }
    }
}
