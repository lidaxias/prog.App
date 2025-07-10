using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using progMap.App.Avalonia.Models;
using progMap.ViewModels;
using Rss.TmFramework.Base.Helpers;
using Rss.TmFramework.Base.Logging;
using Rss.TmFramework.Base.Protocols.Map;
using Rss.TmFramework.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Services
{
    /// <summary>
    /// Сервис, описывающий основные методы для работы с памятью
    /// </summary>
    public interface IMemoryOperationService
    {
        void ReadSection(SectionConfigurationBaseVm section, DeviceConfigurationVm deviceConfig);
        Task ReadFileSectionInfoAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig);
        void WriteSection(SectionConfigurationBaseVm section, DeviceConfigurationVm deviceConfig);
        void EraseSection(SectionConfigurationBaseVm section, DeviceConfigurationVm deviceConfig);
    }

    /// <summary>
    /// Сервис, отвечающий за операции с памятью
    /// </summary>
    public class MemoryOperationService : IMemoryOperationService
    {
        private readonly MapEthernetModule _mapModule;
        private readonly Dispatcher _dispatcher;
        private readonly MemoryManager _memoryManager;

        public MemoryOperationService(MapEthernetModule mapModule, ILogger logger, Dispatcher dispatcher, MemoryManager memoryManager)
        {
            _mapModule = mapModule;
            _dispatcher = dispatcher;
            _memoryManager = memoryManager;
        }

        /// <summary>
        /// Чтение данных из секции
        /// </summary>
        public async void ReadSection(SectionConfigurationBaseVm section, DeviceConfigurationVm _selectedDeviceConfigurationVm)
        {
            switch (section)
            {
                case MemorySectionConfigurationVm memCfg:
                    await Task.Run(() => _memoryManager.ReadMemorySection(memCfg, _selectedDeviceConfigurationVm));
                    break;

                case FileSectionConfigurationVm fileCfg:
                    await Task.Run(() => _memoryManager.ReadFileSectionAsync(fileCfg, _selectedDeviceConfigurationVm, _dispatcher));
                    break;

                default: throw new ArgumentException();
            }
        }

        /// <summary>
        /// Запись данных в секцию
        /// </summary>
        public async void WriteSection(SectionConfigurationBaseVm section, DeviceConfigurationVm _selectedDeviceConfigurationVm)
        {
            switch (section)
            {
                case MemorySectionConfigurationVm memCfg:
                    await Task.Run(() => _memoryManager.WriteMemorySection(memCfg, _selectedDeviceConfigurationVm));
                    break;

                case FileSectionConfigurationVm fileCfg:
                    await Task.Run(() => _memoryManager.WriteFileSectionAsync(fileCfg, _selectedDeviceConfigurationVm, _dispatcher));
                    break;

                default: throw new ArgumentException();
            }
        }

        /// <summary>
        /// Стирает данные секции
        /// </summary>
        public async void EraseSection(SectionConfigurationBaseVm section, DeviceConfigurationVm _selectedDeviceConfigurationVm)
        {
            switch (section)
            {
                case MemorySectionConfigurationVm memCfg:
                    await Task.Run(() => _memoryManager.EraseTkmSectionAsync(memCfg, _selectedDeviceConfigurationVm));
                    break;

                case FileSectionConfigurationVm fileCfg:
                    await Task.Run(() => _memoryManager.EraseTkmSectionAsync(fileCfg, _selectedDeviceConfigurationVm));
                    break;

                default: throw new ArgumentException();
            }
        }

        /// <summary>
        /// Чтение информации о файле, который лежит в текущей файловой секции
        /// </summary>
        public async Task ReadFileSectionInfoAsync(FileSectionConfigurationVm section, DeviceConfigurationVm _selectedDeviceConfigurationVm)
        {
            _mapModule.ReadTimeoutMilliseconds = StringHelpers.ParseInt(_selectedDeviceConfigurationVm.ReadTimeOut);
            var flags = section.IsVirtual ? EMapFlagsV5.None : EMapFlagsV5.PhysicalAddress;

            await Task.Run(() => _memoryManager.ReadFileInfoSection(section, flags));
        }
    }


    public class FileOperationService
    {
        public async Task LoadDataFromFileAsync(MemorySectionConfigurationVm section, ILogger logger)
        {
            var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();
            OpenFileDialog ofd = new();

            var fileNames = await ofd.ShowAsync(currentWindow);

            if (fileNames == null || fileNames.Length == 0)
                return;

            var memoryFile = File.ReadAllText(fileNames[0]);
            var dataByteStrings = memoryFile.Split(new[] { ' ', '\t', ',', '{', '}', '(', ')', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var dataBytes = new byte[dataByteStrings.Length]; // создаем буфер для хранения данных в байтах

            if (dataBytes.Length > section.Size)
            {
                logger.LogInformationWColor(Brushes.Red, $"Размер данных в файле {dataBytes.Length} байт больше размера секции {section.Size} байт!");
                return;
            }

            // парсинг загруженных данных из строки в байты
            for (var i = 0; i < dataBytes.Length; i++)
            {
                if (!StringHelpers.TryParseByte(dataByteStrings[i], out dataBytes[i]))
                {
                    logger.LogInformationWColor(Brushes.Red, $"Неверный формат загруженных данных! Загрузите данные для записи в соответствии с форматом (один байт 0x00-0xFF (hex)  либо 0-255 (dec))!");
                    return;
                }
            }

            section.DataString = null;
            section.DataString = String.Join(" ", dataByteStrings);
            logger.LogInformationWColor(Brushes.Green, $"Успешная загрузка данных из файла {fileNames[0]}");
        }
    }



}
