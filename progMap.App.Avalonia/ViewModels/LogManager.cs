using Avalonia.Media;
using progMap.ViewModels;
using Rss.TmFramework.Base.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.ViewModels
{
    /// <summary>
    /// Управляет логами 
    /// </summary>
    public class LogManager : ViewModelBase
    {
        private readonly ILogger _loggerVm;

        public LogManager(ILogger logger)
        {
            _loggerVm = logger;
        }

        /// <summary>
        /// Логирование CRC файла
        /// </summary>
        /// <param name="fileCrc">Контрольная сумма файла, полученная из заголовка</param>
        /// <param name="calculatedCrc">Подсчитанная контрольная сумма</param>
        public void LogCrcMismatch(uint fileCrc, uint calculatedCrc)
        {
            _loggerVm.LogInformationWColor(Brushes.DarkViolet,
                $"HEADER_CRC (FROM FILE)    0x{fileCrc:X8}\n\t\t\t\t\t\t   " +
                $"HEADER_CRC                0x{calculatedCrc:X8}\n");
        }

        /// <summary>
        /// Логирование данных заголовка файла
        /// </summary>
        /// <param name="fileSize">Размер файла</param>
        /// <param name="version">Версия прошивки</param>
        /// <param name="fileCrc">Контрольная сумма файла, полученная из заголовка</param>
        /// <param name="headerCrc">Контрольная сумма заголовка, полученная из заголовка</param>
        /// 
        /// <summary>
        /// Отписывает в журнал данные заголовка файла
        /// </summary>
        public void LogFileHeaderInfo(uint size, uint version, uint fileCrc, uint headerCrc)
        {
            _loggerVm.LogInformationWColor(Brushes.DarkViolet, string.Format(
        "{0,-25} {1}" + $"{Environment.NewLine}" +
        "{2, 32}                     0x{3:X8} ({3})" + $"{Environment.NewLine}" +
        "{4, 35}                  0x{5:X8} ({5})" + $"{Environment.NewLine}" +
        "{6, 36}                 0x{7:X8}" + $"{Environment.NewLine}" +
        "{8, 38}               0x{9:X8}",
        "ИМЯ:", "ЗАГОЛОВОК",
        "SIZE:", size,
        "VERSION:", version,
        "FILE_CRC:", fileCrc,
        "HEADER_CRC:", headerCrc
    ));
        }

        /// <summary>
        /// Логирование заголовка
        /// </summary>
        /// <param name="section">Секция, где находится файл</param>
        /// <param name="rcvData">Полученные данные заголовка</param>
        public void LogFileHeader(FileSectionConfigurationVm section, IEnumerable<byte> rcvData)
        {
            _loggerVm.LogInformationWColor(Brushes.Black,
                $"Чтение 16 байт файла из секции \"{section.Name}\" по адресу (0x{section.Address:X8})");

            _loggerVm.LogInformationWColor(Brushes.Black,
                string.Join(" ", rcvData.Select(h => $"0x{h:X2}")));
        }

    }
}
