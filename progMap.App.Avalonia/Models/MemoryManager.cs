using Avalonia.Media;
using Avalonia.Controls;
using progMap.App.Avalonia.ViewModels;
using progMap.ViewModels;
using Rss.TmFramework.Base.Channels;
using Rss.TmFramework.Base.Helpers;
using Rss.TmFramework.Base.Logging;
using Rss.TmFramework.Base.Protocols.Map;
using Rss.TmFramework.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using progMap.App.Avalonia.Views;
using Avalonia.Threading;
using System.Buffers;

namespace progMap.App.Avalonia.Models
{
    /// <summary>
    /// Описывает основные операции по работе с памятью
    /// </summary>
    public interface IMemoryOperations
    {
        Task<bool> WriteFileSectionAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, string filename);
        Task WriteFileSectionAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, Dispatcher dispatcher);
        Task ReadFileSectionAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, Dispatcher dispatcher);
        Task<bool> EraseSectionAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig);
        Task EraseTkmSectionAsync(SectionConfigurationBaseVm section, DeviceConfigurationVm deviceConfig);
        Task CompareFileAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, Dispatcher dispatcher);
        void ReadMemorySection(MemorySectionConfigurationVm section, DeviceConfigurationVm deviceConfig);
        void WriteMemorySection(MemorySectionConfigurationVm section, DeviceConfigurationVm deviceConfig);
        void ReadFileInfoSection(FileSectionConfigurationVm section, EMapFlagsV5 flags);
        bool UpdateMemoryType(MemoryTypeVm memoryType);
    }

    /// <summary>
    /// Менеджер для работы с операциями с памятью
    /// </summary>
    public class MemoryManager : IMemoryOperations
    {
        private const uint ModuleMemTypeAddr = 0x0000_0400;
        private readonly ILogger _logger;
        private readonly MapEthernetModule _mapModule;
        private readonly MemoryTypeVm _memoryType;
        private readonly ProgressBarManagerViewModel _progressBarViewModel;
        private readonly MemoryAccessHelper _memoryAccessHelper;
        private readonly FileHeaderProcessor _fileHeaderProcessor;

        private readonly MemoryEraser _memoryEraser;
        private readonly FileSectionReader _fileSectionReader;
        private readonly FileSectionWriter _fileSectionWriter;
        private readonly FileComparer _fileComparer;

        public MemoryManager(ILogger logger, MemoryTypeVm memoryTypeVm, MapEthernetModule mapModule, ProgressBarManagerViewModel progressBarViewModel)
        {
            _logger = logger;
            _mapModule = mapModule;
            _memoryType = memoryTypeVm;
            _progressBarViewModel = progressBarViewModel;
            _memoryAccessHelper = new MemoryAccessHelper(logger, mapModule);
            _fileHeaderProcessor = new FileHeaderProcessor(logger);
            _memoryEraser = new MemoryEraser(logger, mapModule, _memoryAccessHelper, progressBarViewModel, memoryTypeVm);
            _fileSectionReader = new FileSectionReader(logger, mapModule, _memoryAccessHelper, progressBarViewModel);
            _fileSectionWriter = new FileSectionWriter(logger, mapModule, _memoryAccessHelper, progressBarViewModel);
            _fileComparer = new FileComparer(logger, mapModule, _memoryAccessHelper, progressBarViewModel);
        }

        /// <summary>
        /// Проверяет установлено ли соединение с прибором
        /// </summary>
        /// <returns>true - установлено соединение с прибором, false не установлено </returns>
        private bool ValidateConnection()
        {
            if (_mapModule?.Channel?.ConnectionState != EConnectionState.Connected)
            {
                _logger.LogInformationWColor(Brushes.Red, "Откройте соединение с прибором!!");
                return false;
            }
            return true;
        }

        #region Memory Section Operations
        public void ReadMemorySection(MemorySectionConfigurationVm section, DeviceConfigurationVm deviceConfig)
        {
            _mapModule.ReadTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.ReadTimeOut);
            if (!ValidateConnection()) return;

            var flags = section.IsVirtual ? EMapFlagsV5.None : EMapFlagsV5.PhysicalAddress;
            var srcAddr = section.Address;

            if (!_memoryAccessHelper.TryReadMemory(srcAddr, (int)section.Size, flags, out var rcvData, null))
            {
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка чтения данных размером {section.Size} байт из секции {section.Name} по адресу 0x{section.Address:X8}!");
                return;
            }

            section.DataString = section.GetFormattedDataString([.. rcvData]) ?? "{ERROR}";
            _logger.LogInformationWColor(Brushes.Green, $"Успешное чтение данных размером {section.Size} байт из секции {section.Name} по адресу 0x{section.Address:X8}.");
        }

        public void WriteMemorySection(MemorySectionConfigurationVm section, DeviceConfigurationVm deviceConfig)
        {
            _mapModule.WriteTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.WriteTimeOut);
            if (!ValidateConnection()) return;

            if (string.IsNullOrWhiteSpace(section.DataString))
            {
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка записи данных в секцию {section.Name} по адресу 0x{section.Address:X8}! Введите данные для записи!");
                return;
            }

            var dataBytes = ParseDataBytes(section.DataString);
            if (dataBytes == null || dataBytes.Length > section.Size) return;

            var flags = EMapFlagsV5.UseReply | (section.IsVirtual ? EMapFlagsV5.None : EMapFlagsV5.PhysicalAddress);
            if (!_memoryAccessHelper.TryWriteMemory(section.Address, dataBytes, flags, null))
            {
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка записи данных в секцию {section.Name} по адресу 0x{section.Address:X8}!");
                return;
            }

            _logger.LogInformationWColor(Brushes.Green, $"Успешная запись данных размером {dataBytes.Length} байт в секцию {section.Name} по адресу 0x{section.Address:X8}.");
        }

        private byte[]? ParseDataBytes(string dataString)
        {
            var dataByteStrings = dataString.Split(new[] { ' ', '\t', ',', '{', '}', '(', ')', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var dataBytes = new byte[dataByteStrings.Length];

            for (var i = 0; i < dataBytes.Length; i++)
            {
                if (!StringHelpers.TryParseByte(dataByteStrings[i], out dataBytes[i]))
                {
                    _logger.LogInformationWColor(Brushes.Red, "Введите данные для записи в соответствии с форматом (один байт 0x00-0xFF (hex) либо 0-255 (dec))!");
                    return null;
                }
            }
            return dataBytes;
        }
        #endregion

        #region File Section Operations
        /// <summary>
        /// Запись данных в файловую секцию для тестирования
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<bool> WriteFileSectionAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, string filename)
        {
            _mapModule.WriteTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.WriteTimeOut);
            if (!ValidateConnection()) return false;
            return await _fileSectionWriter.WriteFileAsync(section, deviceConfig, filename);
        }

        /// <summary>
        /// Запись данных в файловую секцию
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public async Task WriteFileSectionAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, Dispatcher dispatcher)
        {
            _mapModule.WriteTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.WriteTimeOut);
            if (!ValidateConnection()) return;
            await _fileSectionWriter.WriteFileWithDialogAsync(section, deviceConfig, dispatcher);
        }

        /// <summary>
        /// Чтение данных из файловой секции
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public async Task ReadFileSectionAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, Dispatcher dispatcher)
        {
            _mapModule.ReadTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.ReadTimeOut);
            if (!ValidateConnection()) return;
            await _fileSectionReader.ReadFileAsync(section, deviceConfig, dispatcher);
        }

        /// <summary>
        /// Чтение информации о файле (заголовок)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="flags"></param>
        public void ReadFileInfoSection(FileSectionConfigurationVm section, EMapFlagsV5 flags)
        {
            _fileHeaderProcessor.ReadFileHeader(section, flags, _mapModule);
        }

        /// <summary>
        /// Сравнивает выбранный файл с данными из файловой секции
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public async Task CompareFileAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, Dispatcher dispatcher)
        {
            _mapModule.WriteTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.WriteTimeOut);
            _mapModule.ReadTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.ReadTimeOut);
            if (!ValidateConnection()) return;
            await _fileComparer.CompareFilesAsync(section, deviceConfig, dispatcher);
        }
        #endregion

        #region Erase Operations

        /// <summary>
        /// Стирает данные секции
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <returns></returns>
        public async Task<bool> EraseSectionAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig)
        {
            if (!ValidateConnection()) return false;
            if (!UpdateMemoryType(_memoryType)) return false;

            return await _memoryEraser.EraseSectionAsync(section, deviceConfig);
        }

        /// <summary>
        /// Стирает данные секции в формате ТКМ
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <returns></returns>
        public async Task EraseTkmSectionAsync(SectionConfigurationBaseVm section, DeviceConfigurationVm deviceConfig)
        {
            if (!ValidateConnection()) return;
            if (!UpdateMemoryType(_memoryType)) return;

            var eraser = new MemoryEraser(_logger, _mapModule, _memoryAccessHelper, _progressBarViewModel, _memoryType);
            await eraser.EraseTkmSectionAsync(section, deviceConfig);
        }
        #endregion

        /// <summary>
        /// Обновляет данные о текущей используемой памяти
        /// </summary>
        /// <returns></returns>
        public bool UpdateMemoryType(MemoryTypeVm memoryType)
        {
            if (!_memoryAccessHelper.TryReadMemory(ModuleMemTypeAddr, 0x04, EMapFlagsV5.None, out var data, null))
            {
                _logger.LogInformationWColor(Brushes.Red, "Не удалось считать данные об используемой памяти");
                return false;
            }

            memoryType = MemoryTypeVm.All.FirstOrDefault(memType => memType.Code == data[0]);
            if (memoryType == null)
            {
                _logger.LogInformationWColor(Brushes.Red, "Не удалось найти установленный тип памяти");
                return false;
            }

            _logger.LogInformationWColor(Brushes.Black, $"Выбрана память - {memoryType}");
            return true;
        }

        /// <summary>
        /// Устанавливает тип памяти 
        /// </summary>
        public void SetMemoryType(MemoryTypeVm memoryType, MemoryTypeVm selectedMemoryType)
        {
            if (!_memoryAccessHelper.TryWriteMemory(ModuleMemTypeAddr, new byte[] { selectedMemoryType.Code, 0x00, 0x00, 0x00 }, EMapFlagsV5.UseReply, null))
            {
                _logger.LogInformationWColor(Brushes.Red, $"Не удалось установить тип памяти - {selectedMemoryType}");
                return;
            }

            UpdateMemoryType(_memoryType);
        }

    }

    /// <summary>
    /// Класс отвечает за работу с памятью (операции чтения и записи)
    /// </summary>
    public class MemoryAccessHelper
    {
        private readonly MemoryAccess _memoryAccess;

        public MemoryAccessHelper(ILogger logger, MapEthernetModule mapModule)
        {
            _memoryAccess = new MemoryAccess(mapModule, logger);
        }

        public bool TryReadMemory(uint address, int size, EMapFlagsV5 flags, out List<byte> data, Action<int,int>? progressCallback)
        {
            return _memoryAccess.TryReadMemory(address, size, flags, out data, progressCallback);
        }

        public bool TryWriteMemory(uint address, byte[] data, EMapFlagsV5 flags, Action<int,int>? progressCallback)
        {
            return _memoryAccess.TryWriteMemory(address, data, flags, progressCallback);
        }
    }

    /// <summary>
    /// Класс отвечает за чтение и парсинг заголовка файла
    /// </summary>
    public class FileHeaderProcessor
    {
        private readonly ILogger _logger;
        private readonly LogManager _logManager;


        public FileHeaderProcessor(ILogger logger)
        {
            _logger = logger;
            _logManager = new LogManager(logger);
        }

        public void ReadFileHeader(FileSectionConfigurationVm section, EMapFlagsV5 flags, MapEthernetModule module)
        {
            var helper = new MemoryAccessHelper(_logger, module);
            if (!helper.TryReadMemory(section.Address, 16, flags, out var rcvData, null))
            {
                _logger.LogInformationWColor(Brushes.Red, "Ошибка считывания заголовка файла!");
                return;
            }

            var headerBuf = rcvData.Take(12).ToArray();
            var fileSizeBuf = rcvData.Take(4).ToArray();
            var crcHeaderBuf = rcvData.Skip(12).Take(4).ToArray();

            _logManager.LogFileHeader(section, rcvData);

            var fileSectionInfo = section.BigEndianHeader
                ? ProcessBigEndian(section, headerBuf, fileSizeBuf, crcHeaderBuf)
                : ProcessLittleEndian(section, headerBuf, fileSizeBuf, crcHeaderBuf);

            if (fileSectionInfo != null)
            {
                _logManager.LogFileHeaderInfo(fileSectionInfo.FileSize, fileSectionInfo.Version, fileSectionInfo.FileCrc, fileSectionInfo.HeaderCrc);
            }
        }

        private FileSectionHeaderInfo? ProcessBigEndian(FileSectionConfigurationVm section, byte[] headerBuf, byte[] fileSizeBuf, byte[] crcHeaderBuf)
        {
            _logger.LogInformationWColor(Brushes.Black, "Порядок байтов заголовка - Big Endian");
            Array.Reverse(fileSizeBuf);
            Array.Reverse(crcHeaderBuf);
            return ReadFileInfoProcess(section, headerBuf, fileSizeBuf, crcHeaderBuf, true);
        }

        private FileSectionHeaderInfo? ProcessLittleEndian(FileSectionConfigurationVm section, byte[] headerBuf, byte[] fileSizeBuf, byte[] crcHeaderBuf)
        {
            _logger.LogInformationWColor(Brushes.Black, "Порядок байтов заголовка - Little Endian");
            return ReadFileInfoProcess(section, headerBuf, fileSizeBuf, crcHeaderBuf, false);
        }

        private FileSectionHeaderInfo? ReadFileInfoProcess(FileSectionConfigurationVm section, byte[] headerBuf, byte[] fileSizeBuf, byte[] crcHeaderBuf, bool isBigEndian)
        {
            var fileSize = BitConverter.ToUInt32(fileSizeBuf);
            var headerCrc = BitConverter.ToUInt32(crcHeaderBuf);
            var calculatedCrc = HeaderCrc.Crc32(headerBuf, 12);

            if (calculatedCrc != headerCrc)
            {
                _logger.LogInformationWColor(Brushes.Red, "Контрольная сумма заголовка не совпадает!");
                _logManager.LogCrcMismatch(headerCrc, calculatedCrc);
                return null;
            }

            if (fileSize == 0xFFFFFFFF)
            {
                _logger.LogInformationWColor(Brushes.Red,
                    $"Файл из секции \"{section.Name}\" по адресу (0x{section.Address:X8}) не найден!");
                return null;
            }

            var fileSizeWithHeader = fileSize + 16;
            if (fileSizeWithHeader > section.Size)
            {
                _logger.LogInformationWColor(Brushes.Red,
                    $"Размер файла {fileSizeWithHeader} байт превышает размер секции {section.Size} байт!");
                return null;
            }

            _logger.LogInformationWColor(Brushes.DarkViolet, "Контрольная сумма совпадает");

            var versionBuf = headerBuf.Skip(4).Take(4).ToArray();
            var crcFileBuf = headerBuf.Skip(8).Take(4).ToArray();

            if (isBigEndian)
            {
                Array.Reverse(versionBuf);
                Array.Reverse(crcFileBuf);
            }

            return new FileSectionHeaderInfo(
                fileSize,
                BitConverter.ToUInt32(versionBuf),
                BitConverter.ToUInt32(crcFileBuf),
                headerCrc
            );
        }
    }

    /// <summary>
    /// Описывает заголовок файла
    /// </summary>
    public class FileSectionHeaderInfo
    {
        public uint FileSize { get; }
        public uint Version { get; }
        public uint FileCrc { get; }
        public uint HeaderCrc { get; }

        public FileSectionHeaderInfo(uint fileSize, uint version, uint fileCrc, uint headerCrc)
        {
            FileSize = fileSize;
            Version = version;
            FileCrc = fileCrc;
            HeaderCrc = headerCrc;
        }
    }

    /// <summary>
    /// Описывает работу с файлом при записи
    /// </summary>
    public class FileSectionWriter
    {
        private readonly ILogger _logger;
        private readonly LogManager _logManager;
        private readonly MapEthernetModule _mapModule;
        private readonly MemoryAccess _memoryAccess;
        private readonly MemoryAccessHelper _memoryAccessHelper;
        private readonly ProgressBarManagerViewModel _progressBarViewModel;
        private readonly IntelHexManager _intelHexManager;

        public FileSectionWriter(ILogger logger, MapEthernetModule mapModule, MemoryAccessHelper memoryAccessHelper, ProgressBarManagerViewModel progressBarViewModel)
        {
            _logger = logger;
            _mapModule = mapModule;
            _logManager = new LogManager(logger);
            _memoryAccessHelper = memoryAccessHelper;
            _progressBarViewModel = progressBarViewModel;
            _memoryAccess = new MemoryAccess(mapModule, logger);
            _intelHexManager = new IntelHexManager(_memoryAccess, logger);
        }

        /// <summary>
        /// Запись файла для тестирования
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<bool> WriteFileAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, string filename)
        {
            bool errorFlag = false;
            _mapModule.WriteTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.WriteTimeOut);
            var dstAddr = section.Address;
            var ext = Path.GetExtension(filename).ToLower();

            try
            {
                // проверка наличия данных в файле
                var fileData = File.ReadAllBytes(filename);
                if (fileData.Length == 0)
                {
                    _logger.LogInformationWColor(Brushes.Red, $"Данные в файле \"{filename}\" отсутствуют! Выберите другой файл.");
                    return false;
                }

                // обработка файлов .rbin и .rrbf
                if (ext == ".rbin" || ext == ".rrbf")
                {
                    return await ProcessRBinaryFile(section, filename, dstAddr, fileData);
                }
                // обработка остальных файлов
                else
                {
                    return await ProcessGenericFile(section, filename, dstAddr, fileData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка при записи файла: {ex.Message}");
                _progressBarViewModel.IsRunning = false;
                return false;
            }
        }

        /// <summary>
        /// Обработка файлов с расширением rbin, rrbf, relf
        /// </summary>
        /// <param name="section"></param>
        /// <param name="filename"></param>
        /// <param name="dstAddr"></param>
        /// <param name="fileData"></param>
        /// <returns></returns>
        private async Task<bool> ProcessRBinaryFile(FileSectionConfigurationVm section, string filename, uint dstAddr, byte[] fileData)
        {
            // чтение заголовка файла
            byte[] headerBuf = new byte[12];
            Array.Copy(fileData, 0, headerBuf, 0, 12);

            // получение размера файла и контрольной суммы
            uint fileSize = BitConverter.ToUInt32(fileData, 0);
            uint headerCrc = BitConverter.ToUInt32(fileData, 12);

            if (section.BigEndianHeader)
            {
                _logger.LogInformationWColor(Brushes.Black, "Порядок байтов заголовка - Big Endian");
                fileSize = ReverseBytes(fileSize);
                headerCrc = ReverseBytes(headerCrc);
            }
            else
            {
                _logger.LogInformationWColor(Brushes.Black, "Порядок байтов заголовка - Little Endian");
            }

            // проверка контрольной суммы заголовка
            if (HeaderCrc.Crc32(headerBuf, 12) != headerCrc)
            {
                _logger.LogInformationWColor(Brushes.DarkViolet, $"Контрольная сумма заголовка не совпадает. Файл по адресу 0x{section.Address:X8} не был записан.");
                return false;
            }

            // Проверка размера файла
            if (fileData.Length > section.Size)
            {
                _logger.LogInformationWColor(Brushes.DarkViolet, $"Размер файла превышает размер секции. Файл по адресу 0x{section.Address:X8} не был записан.");
                return false;
            }

            // получение данных файла без заголовка
            byte[] payloadData = new byte[fileSize];
            Array.Copy(fileData, 16, payloadData, 0, fileSize);

            // проверка контрольной суммы файла
            uint fileCrc = BitConverter.ToUInt32(fileData, 8);
            if (section.BigEndianHeader)
            {
                fileCrc = ReverseBytes(fileCrc);
            }

            if (HeaderCrc.Crc32(payloadData, fileSize) != fileCrc)
            {
                _logger.LogInformationWColor(Brushes.DarkViolet, $"Контрольная сумма файла не совпадает. Файл по адресу 0x{section.Address:X8} не был записан.");
                return false;
            }

            // логирование информации о файле
            _logger.LogInformationWColor(Brushes.Black, $"Запись в секцию \"{section.Name}\" по адресу 0x{section.Address:X8} файла: \"{filename}\"");
            LogFileInfo(section.BigEndianHeader, fileData);

            // запись и проверка данных
            return await WriteAndVerifyData(dstAddr, fileData, section);
        }

        /// <summary>
        /// Обработка файлов по умолчанию
        /// </summary>
        /// <param name="section"></param>
        /// <param name="filename"></param>
        /// <param name="dstAddr"></param>
        /// <param name="fileData"></param>
        /// <returns></returns>
        private async Task<bool> ProcessGenericFile(FileSectionConfigurationVm section, string filename, uint dstAddr, byte[] fileData)
        {
            _logger.LogInformationWColor(Brushes.Black, $"Запись в секцию \"{section.Name}\" файла: \"{filename}\" размером {fileData.Length} байт");

            // проверка размера файла
            if (fileData.Length > section.Size)
            {
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка записи файла! Размер файла {fileData.Length} байт превышает размер секции {section.Size} байт!");
                return false;
            }

            // запись и проверка данных
            return await WriteAndVerifyData(dstAddr, fileData, section);
        }

        /// <summary>
        /// Запись и верификация данных
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        private async Task<bool> WriteAndVerifyData(uint address, byte[] data, FileSectionConfigurationVm section)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _progressBarViewModel.IsRunning = true;
                    var flags = EMapFlagsV5.UseReply | (section.IsVirtual ? EMapFlagsV5.None : EMapFlagsV5.PhysicalAddress);

                    // запись данных
                    if (!_memoryAccess.TryWriteMemory(address, data, flags, _progressBarViewModel.SetProgress))
                    {
                        _logger.LogInformationWColor(Brushes.Red, "Ошибка записи файла!");
                        return false;
                    }

                    _progressBarViewModel.OperationName = "Считывание файла: ";

                    // чтение данных для проверки
                    if (!_memoryAccess.TryReadMemory(address, data.Length, flags, out List<byte> receivedData, _progressBarViewModel.SetProgress))
                    {
                        _logger.LogInformationWColor(Brushes.Red, "Ошибка считывания файла!");
                        return false;
                    }

                    // проверка совпадения данных
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != receivedData[i])
                        {
                            _logger.LogInformationWColor(Brushes.Red,
                                $"Ошибка записи файла! Файлы не совпадают. Неверно записан байт по адресу 0x{(address + i):X8}\n" +
                                $"Записанный байт: {data[i]:X2}. Считанный байт: {receivedData[i]:X2}.");
                            return false;
                        }
                    }

                    _logger.LogInformationWColor(Brushes.Black, "Записанные и считанные данные совпадают.");
                    _logger.LogInformationWColor(Brushes.Green, "Запись файла завершена успешно.");
                    return true;
                }
                finally
                {
                    _progressBarViewModel.OperationName = "Загрузка файла: ";
                    _progressBarViewModel.IsRunning = false;
                }
            });
        }

        /// <summary>
        /// Переворачивает байты, если выбран BigEndian
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static uint ReverseBytes(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Отписывает лог данных файла
        /// </summary>
        /// <param name="isBigEndian"></param>
        /// <param name="fileData"></param>
        private  void LogFileInfo(bool isBigEndian, byte[] fileData)
        {
            uint fileSize = BitConverter.ToUInt32(fileData, 0);
            uint version = BitConverter.ToUInt32(fileData, 4);
            uint fileCrc = BitConverter.ToUInt32(fileData, 8);
            uint headerCrc = BitConverter.ToUInt32(fileData, 12);

            if (isBigEndian)
            {
                fileSize = ReverseBytes(fileSize);
                version = ReverseBytes(version);
                fileCrc = ReverseBytes(fileCrc);
                headerCrc = ReverseBytes(headerCrc);
            }

            _logManager.LogFileHeaderInfo(fileSize, version, fileCrc, headerCrc);
        }

        /// <summary>
        /// Записывает файл в секцию памяти с диалоговым окном выбора файла
        /// </summary>
        /// <param name="section">Конфигурация секции памяти</param>
        /// <param name="deviceConfig">Конфигурация устройства</param>
        /// <param name="dispatcher">Диспетчер для UI операций</param>
        public async Task WriteFileWithDialogAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, Dispatcher dispatcher)
        {
            try
            {
                _mapModule.WriteTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.WriteTimeOut);
                var dstAddr = section.Address;

                // открытие диалога выбора файла
                var currentWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Windows.Last();
                var openWriteSectionFile = new UserFileDialog
                {
                    Filters = { new FileDialogFilter { Name = section.GetNameDialogFilter(), Extensions = section.ParseExtensions() } },
                    AllowMultiple = false
                };

                var fileNames = await openWriteSectionFile.ShowDialog(currentWindow);
                if (fileNames == null || fileNames.Length == 0) return;

                var filename = fileNames[0];
                var ext = Path.GetExtension(filename).ToLower();

                // обработка HEX файлов
                if (ext == ".hex")
                {
                    _progressBarViewModel.IsRunning = true;
                    //await _intelHexManager.LoadIntelHexFile(filename, _progressBarViewModel.SetProgress, _mapModule, _logger);
                    await _intelHexManager.LoadIntelHexFileAsync(filename, _progressBarViewModel.SetProgress);
                    _progressBarViewModel.IsRunning = false;
                    return;
                }

                // чтение данных файла
                var fileData = File.ReadAllBytes(filename);
                if (fileData.Length == 0)
                {
                    _logger.LogInformationWColor(Brushes.Red, $"Данные в файле \"{filename}\" отсутствуют! Выберите другой файл.");
                    return;
                }

                // обработка бинарных файлов (.rbin, .rrbf, .relf)
                if (ext == ".rbin" || ext == ".rrbf" || ext == ".relf")
                {
                    await ProcessBinaryFileWithDialog(section, filename, dstAddr, fileData, dispatcher);
                }
                // обработка обычных файлов
                else
                {
                    await ProcessGenericFileWithDialog(section, filename, dstAddr, fileData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка при записи файла: {ex.Message}");
                _progressBarViewModel.IsRunning = false;
            }
        }

        /// <summary>
        /// Обработка файлов с расширением rbin, rrbf, relf
        /// </summary>
        /// <param name="section"></param>
        /// <param name="filename"></param>
        /// <param name="dstAddr"></param>
        /// <param name="fileData"></param>
        /// <returns></returns>
        private async Task ProcessBinaryFileWithDialog(FileSectionConfigurationVm section, string filename, uint dstAddr, byte[] fileData, Dispatcher dispatcher)
        {
            // чтение заголовка
            byte[] headerBuf = new byte[12];
            Array.Copy(fileData, 0, headerBuf, 0, 12);

            // получение данных заголовка
            uint fileSize = BitConverter.ToUInt32(fileData, 0);
            uint headerCrc = BitConverter.ToUInt32(fileData, 12);

            if (section.BigEndianHeader)
            {
                _logger.LogInformationWColor(Brushes.Black, "Порядок байтов заголовка - Big Endian");
                fileSize = ReverseBytes(fileSize);
                headerCrc = ReverseBytes(headerCrc);
            }
            else
            {
                _logger.LogInformationWColor(Brushes.Black, "Порядок байтов заголовка - Little Endian");
            }

            // проверка контрольной суммы заголовка
            if (HeaderCrc.Crc32(headerBuf, 12) != headerCrc)
            {
                if (!await ShowConfirmationDialog(dispatcher,
                    "Контрольная сумма заголовка не совпадает! Продолжить запись?"))
                {
                    _logger.LogInformationWColor(Brushes.DarkViolet, $"Контрольная сумма заголовка не совпадает. Файл по адресу 0x{section.Address:X8} не был записан.");
                    return;
                }
                _logger.LogInformationWColor(Brushes.DarkViolet, $"Контрольная сумма заголовка не совпадает. Запись файла..");
            }

            // проверка дополнения файла
            byte[] payloadData = new byte[fileSize];
            Array.Copy(fileData, 16, payloadData, 0, fileSize);
            await CheckFilePadding(section, fileData, fileSize, dispatcher);

            // проверка размера файла
            if (fileData.Length > section.Size)
            {
                if (!await ShowConfirmationDialog(dispatcher,
                    "Размер файла превышает размер секции! Продолжить запись?"))
                {
                    _logger.LogInformationWColor(Brushes.DarkViolet, $"Размер файла превышает размер секции. Файл по адресу 0x{section.Address:X8} не был записан.");
                    return;
                }
                _logger.LogInformationWColor(Brushes.DarkViolet, $"Размер файла превышает размер секции. Запись файла...");
            }

            // проверка контрольной суммы файла
            uint fileCrc = BitConverter.ToUInt32(fileData, 8);
            if (section.BigEndianHeader) fileCrc = ReverseBytes(fileCrc);

            if (HeaderCrc.Crc32(payloadData, fileSize) != fileCrc)
            {
                if (!await ShowConfirmationDialog(dispatcher,
                    "Контрольная сумма файла не совпадает! Продолжить запись?"))
                {
                    _logger.LogInformationWColor(Brushes.DarkViolet, $"Контрольная сумма файла не совпадает. Файл по адресу 0x{section.Address:X8} не был записан.");
                    return;
                }
                _logger.LogInformationWColor(Brushes.DarkViolet, $"Контрольная сумма файла не совпадает. Запись файла...");
            }

            // логирование и запись
            _logger.LogInformationWColor(Brushes.Black, $"Запись в секцию \"{section.Name}\" по адресу 0x{section.Address:X8} файла: \"{filename}\"");
            LogFileInfo(section.BigEndianHeader, fileData);

            await WriteAndVerifyData(dstAddr, fileData, section);
        }

        /// <summary>
        /// Обработка файлов по умолчанию
        /// </summary>
        /// <param name="section"></param>
        /// <param name="filename"></param>
        /// <param name="dstAddr"></param>
        /// <param name="fileData"></param>
        /// <returns></returns>
        private async Task ProcessGenericFileWithDialog(FileSectionConfigurationVm section, string filename, uint dstAddr, byte[] fileData)
        {
            _logger.LogInformationWColor(Brushes.Black, $"Запись в секцию \"{section.Name}\" файла: \"{filename}\" размером {fileData.Length} байт");

            if (fileData.Length > section.Size)
            {
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка записи файла! Размер файла {fileData.Length} байт превышает размер секции {section.Size} байт!");
                return;
            }

            await WriteAndVerifyData(dstAddr, fileData, section);
        }

        /// <summary>
        /// Отображает диалоговое окно с подтверждением
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task<bool> ShowConfirmationDialog(Dispatcher dispatcher, string message)
        {
            await dispatcher.InvokeAsync(async () =>
            {
                await MessageBoxManager.MsgBoxYesNo(message);
            });
            return MessageBoxWindow.FlagResult == true;
        }

        /// <summary>
        /// Проверка файла на дополнение
        /// </summary>
        /// <param name="section"></param>
        /// <param name="fileData"></param>
        /// <param name="fileSize"></param>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        private static async Task CheckFilePadding(FileSectionConfigurationVm section, byte[] fileData, uint fileSize, Dispatcher dispatcher)
        {
            if (fileData.Length - 16 <= fileSize) return;

            byte[] padding = new byte[fileData.Length - fileSize - 16];
            Array.Copy(fileData, 16 + (int)fileSize, padding, 0, padding.Length);

            string message = padding.All(b => b == 255) ?
                "Файл с дополнением (0xFF). Продолжить запись?" :
                padding.All(b => b == 0) ?
                "Файл с дополнением (0x00). Продолжить запись?" :
                "Файл без дополнения. Продолжить запись?";

            if (!await ShowConfirmationDialog(dispatcher, message))
            {
                throw new OperationCanceledException("Пользователь отменил запись файла с дополнением");
            }
        }
    }

    /// <summary>
    /// Описывает работу с файлом при чтении
    /// </summary>
    public class FileSectionReader
    {
        private readonly ILogger _logger;
        private readonly MapEthernetModule _mapModule;
        private readonly MemoryAccessHelper _memoryAccessHelper;
        private readonly ProgressBarManagerViewModel _progressBarViewModel;
        private readonly MemoryAccess _memoryAccess;

        public FileSectionReader(ILogger logger, MapEthernetModule mapModule, MemoryAccessHelper memoryAccessHelper, ProgressBarManagerViewModel progressBarViewModel)
        {
            _logger = logger;
            _mapModule = mapModule;
            _memoryAccessHelper = memoryAccessHelper;
            _progressBarViewModel = progressBarViewModel;
            _memoryAccess = new MemoryAccess(mapModule, logger);
        }

        /// <summary>
        /// Чтение данных из файловой секции
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public async Task ReadFileAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, Dispatcher dispatcher)
        {
            try
            {
                _mapModule.ReadTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.ReadTimeOut);

                // выбор файла для сохранения
                var fileName = await ShowSaveFileDialog(section);
                if (string.IsNullOrWhiteSpace(fileName)) return;

                var srcAddr = section.Address;
                var flags = section.IsVirtual ? EMapFlagsV5.None : EMapFlagsV5.PhysicalAddress;

                // чтение заголовка (16 байт)
                if (!_memoryAccess.TryReadMemory(srcAddr, 16, flags, out var rcvData, null))
                {
                    _logger.LogInformationWColor(Brushes.Red, "Ошибка считывания файла!");
                    return;
                }

                // анализ заголовка
                var (fileSize, headerCrc, isValidHeader) = ParseFileHeader(rcvData, section);

                if (!isValidHeader)
                {
                    if (!await ConfirmContinueReading(dispatcher, "Контрольная сумма заголовка не совпадает!"))
                    {
                        _logger.LogInformationWColor(Brushes.DarkViolet, $"Файл по адресу 0x{section.Address:X8} не был прочитан.");
                        return;
                    }

                    _logger.LogInformationWColor(Brushes.DarkViolet, "Чтение данных как файла без заголовка...");
                    await ReadAndSaveData(section, fileName, srcAddr, (int)section.Size, flags);
                    return;
                }

                // проверка на стертые данные
                if (fileSize == 0xFFFFFFFF)
                {
                    _logger.LogInformationWColor(Brushes.Red, $"Данные в секции \"{section.Name}\" были стёрты!");
                    return;
                }

                // проверка размера
                var fullSize = fileSize + 16;
                if (fullSize > section.Size)
                {
                    _logger.LogInformationWColor(Brushes.Red, "Размер файла превышает размер секции!");
                    return;
                }

                // чтение и сохранение данных с заголовком
                _logger.LogInformationWColor(Brushes.DarkViolet, "Чтение данных как файла с заголовком...");
                await ReadAndSaveData(section, fileName, srcAddr, (int)fullSize, flags);
            }
            catch (Exception ex)
            {
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка при чтении файла: {ex.Message}");
                _progressBarViewModel.OperationName = "Загрузка файла: ";
                _progressBarViewModel.IsRunning = false;
            }
        }

        private async Task<string> ShowSaveFileDialog(FileSectionConfigurationVm section)
        {
            var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();

            var saveDialog= new SaveFileDialog();
            saveDialog.Filters?.Add(new FileDialogFilter()
            {
                Name = section.GetNameDialogFilter(),
                Extensions = section.ParseExtensions()
            });
            return await saveDialog.ShowAsync(currentWindow);

        }

        /// <summary>
        /// Парсинг заголовка после чтения из файловой секции
        /// </summary>
        /// <param name="data"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        private (uint fileSize, uint headerCrc, bool isValid) ParseFileHeader(List<byte> data, FileSectionConfigurationVm section)
        {
            byte[] headerBuf = new byte[12];
            Array.Copy(data.ToArray(), 0, headerBuf, 0, 12);

            uint fileSize = BitConverter.ToUInt32(data.ToArray(), 0);
            uint headerCrc = BitConverter.ToUInt32(data.ToArray(), 12);

            if (section.BigEndianHeader)
            {
                _logger.LogInformationWColor(Brushes.Black, "Порядок байтов заголовка - Big Endian");
                fileSize = ReverseBytes(fileSize);
                headerCrc = ReverseBytes(headerCrc);
            }
            else
            {
                _logger.LogInformationWColor(Brushes.Black, "Порядок байтов заголовка - Little Endian");
            }

            bool isValid = HeaderCrc.Crc32(headerBuf, 12) == headerCrc;
            return (fileSize, headerCrc, isValid);
        }

        /// <summary>
        /// Окно подтверждения чтения файла без заголовка
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task<bool> ConfirmContinueReading(Dispatcher dispatcher, string message)
        {
            await dispatcher.InvokeAsync(async () =>
            {
                await MessageBoxManager.MsgBoxYesNo($"{message}{Environment.NewLine}Прочесть файл без заголовка?");
            });

            return MessageBoxWindow.FlagResult == true;
        }

        /// <summary>
        /// Переворачивает байты, если выбран BigEndian
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static uint ReverseBytes(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Читает данные и сохраняет в выбранный файл
        /// </summary>
        /// <param name="section"></param>
        /// <param name="path"></param>
        /// <param name="srcAddr"></param>
        /// <param name="size"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private async Task ReadAndSaveData(FileSectionConfigurationVm section, string path, uint srcAddr, int size, EMapFlagsV5 flags)
        {
            _progressBarViewModel.OperationName = "Считывание файла: ";

            if (_memoryAccess.TryReadMemory(srcAddr, size, flags, out var data,
                _progressBarViewModel.SetProgress))
            {
                File.WriteAllBytes(path, data.ToArray());
                _logger.LogInformationWColor(Brushes.Black,
                    $"Чтение {size} байт в файл {path} из секции {section.Name}");
            }
            else
            {
                _logger.LogInformationWColor(Brushes.Red, "Ошибка считывания файла!");
            }

            _logger.LogInformationWColor(Brushes.Green, "Чтение данных завершено успешно.");
            _progressBarViewModel.OperationName = "Загрузка файла: ";
        }
    }
    
   
    /// <summary>
    /// Описывает работу с памятью при стирании данных
    /// </summary>
    public class MemoryEraser
    {
        private readonly ILogger _logger;
        private readonly MapEthernetModule _mapModule;
        private readonly MemoryAccessHelper _memoryAccessHelper;
        private readonly ProgressBarManagerViewModel _progressBarViewModel;
        private readonly MemoryAccess _memoryAccess;
        private readonly MemoryTypeVm _memoryTypeVm;

        public MemoryEraser(ILogger logger, MapEthernetModule mapModule, MemoryAccessHelper memoryAccessHelper, ProgressBarManagerViewModel progressBarViewModel, MemoryTypeVm memoryTypeVm)
        {
            _logger = logger;
            _mapModule = mapModule;
            _memoryAccessHelper = memoryAccessHelper;
            _progressBarViewModel = progressBarViewModel;
            _memoryAccess = new MemoryAccess(mapModule, logger);
            _memoryTypeVm = memoryTypeVm;
        }

        /// <summary>
        /// Стирает данные секций  
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <returns></returns>
        public async Task<bool> EraseSectionAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig)
        {
            bool errorFlag = false;
            await Task.Run(() =>
            {
                if (_memoryTypeVm == null) return; // если не найден установленный тип памяти в приборе то выходим

                // если установлен не тот тип памяти
                if (deviceConfig.MemoryType.Code != _memoryTypeVm.Code)
                {
                    _logger.LogInformationWColor(Brushes.Red, $"Стирание невозможно: установлен тип памяти - {_memoryTypeVm.Name}, необходим - {deviceConfig.MemoryType.Name}");
                    errorFlag = true;
                    return;
                }
                _logger.LogInformationWColor(Brushes.Black, $"Стирание сектора \"{section.Name}\" размером {section.Size} байт");

                // 20231021 считаем количество секций, которые надо стереть
                var countSectors = Math.Ceiling((decimal)section.Size / (decimal)deviceConfig.MemoryType.SectorSize);
                _progressBarViewModel.IsRunning = true;


                // стираем нужные секции
                for (int i = 0; i < countSectors; i++)
                {
                    var addr = section.Address + deviceConfig.MemoryType.SectorSize * i;
                    var addrBytes = BitConverter.GetBytes(addr);

                    _logger.LogInformationWColor(Brushes.Black, $"Стирание сектора по адресу: 0x{addr:X8}");

                    _progressBarViewModel.SetProgress(i, (int)countSectors);
                    _progressBarViewModel.OperationName = "Стирание секторов:";

                    if (!StringHelpers.TryParseUint(deviceConfig.EraseVirtualAddr, out uint virtualAddr))
                    {
                        _logger.LogInformationWColor(Brushes.Red, $"Адрес стирания указан в неверном формате. Укажите адрес в формате 0x00000000 - 0xFFFFFFFF (hex).");

                        _progressBarViewModel.SetProgress(0, 1);
                        _progressBarViewModel.OperationName = "Загрузка файла:";
                        _progressBarViewModel.IsRunning = false;

                        errorFlag = true;

                        return;
                    }

                    if (!StringHelpers.TryParseUint(deviceConfig.ErasePhysicalAddr, out uint physicalAddr))
                    {
                        _logger.LogInformationWColor(Brushes.Red, $"Адрес стирания указан в неверном формате. Укажите адрес в формате 0x00000000 - 0xFFFFFFFF (hex).");
                        _progressBarViewModel.IsRunning = false;
                        _progressBarViewModel.SetProgress(0, 1);
                        _progressBarViewModel.OperationName = "Загрузка файла:";
                        errorFlag = true;

                        return;
                    }

                    if (section.IsVirtual == true)
                    {
                        if (!_memoryAccess.TryWriteMemory(virtualAddr, addrBytes, EMapFlagsV5.UseReply, null))
                        {
                            _logger.LogInformationWColor(Brushes.Red, $"Не удалось стереть сектор виртуальной памяти по адресу: 0x{addr:X8}");
                            _progressBarViewModel.SetProgress(0, 1);
                            _progressBarViewModel.IsRunning = false;
                            _progressBarViewModel.OperationName = "Загрузка файла:";
                            errorFlag = true;

                            return;
                        }
                    }

                    else
                    {
                        if (!_memoryAccess.TryWriteMemory(physicalAddr, addrBytes, EMapFlagsV5.UseReply, null))
                        {
                            _logger.LogInformationWColor(Brushes.Red, $"Не удалось стереть сектор физической памяти по адресу: 0x{addr:X8}");
                            _progressBarViewModel.SetProgress(0, 1);
                            _progressBarViewModel.OperationName = "Загрузка файла:";
                            _progressBarViewModel.IsRunning = false;
                            errorFlag = true;

                            return;
                        }
                    }

                    // 20240604 реализован ввод таймаута через редактор для каждого конфига
                    Task.Delay(StringHelpers.ParseInt(deviceConfig.EraseTimeOut)).Wait();
                }

            });

            if (errorFlag)
            {
                _logger.LogInformationWColor(Brushes.Red, $"Не удалось стереть секцию {section.Name}");
                _progressBarViewModel.IsRunning = false;

                return false;
            }

            else
            {
                _logger.LogInformationWColor(Brushes.Black, $"Стирание сектора \"{section.Name}\" завершено");
                _progressBarViewModel.SetProgress(0, 1);
                _progressBarViewModel.OperationName = "Загрузка файла:";
                _progressBarViewModel.IsRunning = false;

                return true;
            }
        }

        /// <summary>
        /// Стирает данные в формате ТКМ
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <returns></returns>
        public async Task EraseTkmSectionAsync(SectionConfigurationBaseVm section, DeviceConfigurationVm deviceConfig)
        {
            await Task.Run(() =>
            {
                _logger.LogInformationWColor(Brushes.Black, $"Стирание сектора \"{section.Name}\" размером {section.Size} байт");

                _progressBarViewModel.IsRunning = true;

                var addrBytes = BitConverter.GetBytes(section.Address);
                var sizeBytes = BitConverter.GetBytes(section.Size);

                var buffer = new List<byte>(addrBytes);
                buffer.AddRange(sizeBytes);

                if (!StringHelpers.TryParseUint(deviceConfig.ErasePhysicalAddr, out uint physicalAddr))
                {
                    _progressBarViewModel.IsRunning = false;
                    _logger.LogInformationWColor(Brushes.Red, $"Адрес стирания указан в неверном формате. Укажите адрес в формате 0x00000000 - 0xFFFFFFFF (hex).");
                    return;
                }

                if (!StringHelpers.TryParseUint(deviceConfig.EraseVirtualAddr, out uint virtualAddr))
                {
                    _logger.LogInformationWColor(Brushes.Red, $"Адрес стирания указан в неверном формате. Укажите адрес в формате 0x00000000 - 0xFFFFFFFF (hex).");
                    _progressBarViewModel.IsRunning = false;
                    return;
                }


                var defaultTimeout = _mapModule.WriteTimeoutMilliseconds;

                _mapModule.WriteTimeoutMilliseconds = int.Parse(deviceConfig.EraseTimeOut);

                if (section.IsVirtual == true)
                {
                    if (!_memoryAccess.TryWriteMemory(virtualAddr, buffer.ToArray(), EMapFlagsV5.UseReply | EMapFlagsV5.None, null))
                        _logger.LogInformationWColor(Brushes.Red, $"Не удалось стереть сектор виртуальной памяти по адресу");
                    else
                        _logger.LogInformationWColor(Brushes.Black, $"Стирание сектора \"{section.Name}\" завершено");

                }

                else
                {

                    if (!_memoryAccess.TryWriteMemory(physicalAddr, buffer.ToArray(), EMapFlagsV5.UseReply | EMapFlagsV5.None, null))
                        _logger.LogInformationWColor(Brushes.Red, $"Не удалось стереть сектор физической памяти по адресу");
                    else
                        _logger.LogInformationWColor(Brushes.Black, $"Стирание сектора \"{section.Name}\" завершено");

                }

                _mapModule.WriteTimeoutMilliseconds = defaultTimeout;

                _progressBarViewModel.IsRunning = false;

                // 20240604 реализован ввод таймаута через редактор для каждого конфига
                Task.Delay(StringHelpers.ParseInt(deviceConfig.EraseTimeOut)).Wait();
            });
        }
    }

    /// <summary>
    /// Описывает работу с данными при сравнении файлов
    /// </summary>
    public class FileComparer
    {
        private readonly ILogger _logger;
        private readonly LogManager _logManager;
        private readonly MapEthernetModule _mapModule;
        private readonly MemoryAccessHelper _memoryAccessHelper;
        private readonly ProgressBarManagerViewModel _progressBarViewModel;
        private readonly MemoryAccess _memoryAccess;

        public FileComparer(ILogger logger, MapEthernetModule mapModule, MemoryAccessHelper memoryAccessHelper, ProgressBarManagerViewModel progressBarViewModel)
        {
            _logger = logger;
            _logManager = new LogManager(logger);
            _mapModule = mapModule;
            _memoryAccessHelper = memoryAccessHelper;
            _progressBarViewModel = progressBarViewModel;
            _memoryAccess = new MemoryAccess(mapModule, logger);
        }


        /// <summary>
        /// Сравнивает файлы
        /// </summary>
        /// <param name="section"></param>
        /// <param name="deviceConfig"></param>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public async Task CompareFilesAsync(FileSectionConfigurationVm section, DeviceConfigurationVm deviceConfig, Dispatcher dispatcher)
        {
            try
            {
                _mapModule.WriteTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.WriteTimeOut);
                _mapModule.ReadTimeoutMilliseconds = StringHelpers.ParseInt(deviceConfig.ReadTimeOut);

                var srcAddr = section.Address;
                var flags = section.IsVirtual ? EMapFlagsV5.None : EMapFlagsV5.PhysicalAddress;

                // выбор файла для сравнения
                var filePath = await ShowFileSelectionDialog(section);
                if (string.IsNullOrEmpty(filePath)) return;

                _logger.LogInformationWColor(Brushes.Black, $"Сравнение по адресу 0x{section.Address:X8} файла: \"{filePath}\"");

                // проверка наличия данных в файле
                var fileData = File.ReadAllBytes(filePath);
                if (fileData.Length == 0)
                {
                    _logger.LogInformationWColor(Brushes.Red, "Данные файла для сравнения отсутствуют!");
                    return;
                }

                // обработка файлов с расширениями .rbin, .rrbf
                if (Path.GetExtension(filePath).Equals(".rbin") || Path.GetExtension(filePath).Equals(".rrbf"))
                {
                    await CompareBinaryFile(section, filePath, srcAddr, fileData, dispatcher);
                }
                else
                {
                    await CompareGenericFile(section, filePath, srcAddr, fileData, flags);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка при сравнении файлов: {ex.Message}");
                return;
            }
        }

        /// <summary>
        /// Открывает диалоговое окно для выбора файла
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        private async Task<string> ShowFileSelectionDialog(FileSectionConfigurationVm section)
        {
            var currentWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Windows.Last();
            var dialog = new UserFileDialog
            {
                Filters = { new FileDialogFilter { Name = section.GetNameDialogFilter(), Extensions = section.ParseExtensions() } },
                AllowMultiple = false
            };

            var fileNames = await dialog.ShowDialog(currentWindow);
            return fileNames?.FirstOrDefault();
        }

        /// <summary>
        /// Описывает алгоритм сравнения файлов
        /// </summary>
        /// <param name="section"></param>
        /// <param name="filePath"></param>
        /// <param name="srcAddr"></param>
        /// <param name="fileData"></param>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        private async Task CompareBinaryFile(FileSectionConfigurationVm section, string filePath, uint srcAddr, byte[] fileData, Dispatcher dispatcher)
        {
            byte[] headerBuf = new byte[12];
            Array.Copy(fileData, 0, headerBuf, 0, 12);

            uint fileSize = BitConverter.ToUInt32(fileData, 0);
            uint headerCrc = BitConverter.ToUInt32(fileData, 12);

            if (section.BigEndianHeader)
            {
                headerCrc = ReverseBytes(headerCrc);
            }

            // проверка контрольной суммы заголовка
            if (HeaderCrc.Crc32(headerBuf, 12) != headerCrc)
            {
                if (!await ConfirmContinueOperation(dispatcher,
                    "Контрольная сумма заголовка не совпадает! Продолжить сравнение?"))
                {
                    _logger.LogInformationWColor(Brushes.DarkViolet, "Файл не был сравнен из-за несовпадения контрольной суммы.");
                    return;
                }
            }

            // логирование информации о файле
            if (section.BigEndianHeader)
            {
                var version = BitConverter.ToUInt32(fileData, 4);
                var fileCrc = ReverseBytes(BitConverter.ToUInt32(fileData, 8));
                fileSize = ReverseBytes(fileSize);

                _logManager.LogFileHeaderInfo(fileSize, version, fileCrc, headerCrc);
            }
            else
            {
                var version = BitConverter.ToUInt32(fileData, 4);
                var fileCrc = BitConverter.ToUInt32(fileData, 8);

                _logManager.LogFileHeaderInfo(fileSize, version, fileCrc, headerCrc);
            }

            // Сравнение данных
            await CompareData(section, srcAddr, fileData,
                EMapFlagsV5.UseReply | (section.IsVirtual ? EMapFlagsV5.None : EMapFlagsV5.PhysicalAddress));
        }

        private async Task CompareGenericFile(FileSectionConfigurationVm section, string filePath, uint srcAddr, byte[] fileData, EMapFlagsV5 flags)
        {
            await Task.Run(() =>
            {
                _progressBarViewModel.OperationName = "Верификация";
                {
                    if (!_memoryAccess.TryReadMemory(srcAddr, fileData.Length, flags, out List<byte> deviceData,
                        _progressBarViewModel.SetProgress))
                    {
                        _logger.LogInformationWColor(Brushes.Red, "Ошибка считывания данных из устройства!");
                        return;
                    }

                    if (deviceData == null || deviceData.Count == 0)
                    {
                        _logger.LogInformationWColor(Brushes.Red, "Данные в устройстве отсутствуют!");
                        return;
                    }

                    CompareDataBytes(fileData, deviceData, srcAddr);
                }
            });
        }

        /// <summary>
        /// Окно подтверждения продолжения сравнения данных
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<bool> ConfirmContinueOperation(Dispatcher dispatcher, string message)
        {
            await dispatcher.InvokeAsync(async () =>
            {
                await MessageBoxManager.MsgBoxYesNo(message);
            });
            return MessageBoxWindow.FlagResult == true;
        }

        /// <summary>
        /// Верификация данных
        /// </summary>
        /// <param name="section"></param>
        /// <param name="srcAddr"></param>
        /// <param name="fileData"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private async Task CompareData(FileSectionConfigurationVm section, uint srcAddr, byte[] fileData, EMapFlagsV5 flags)
        {
            await Task.Run(() =>
            {
                _progressBarViewModel.OperationName = "Верификация";
                if (!_memoryAccess.TryReadMemory(srcAddr, fileData.Length, flags, out List<byte> deviceData,
                        _progressBarViewModel.SetProgress))
                    {
                        _logger.LogInformationWColor(Brushes.Red, "Ошибка считывания данных для сравнения!");
                        return;
                    }

                    CompareDataBytes(fileData, deviceData, srcAddr);
            });
        }

        /// <summary>
        /// Сравнивает файлы побайтно
        /// </summary>
        /// <param name="fileData"></param>
        /// <param name="deviceData"></param>
        /// <param name="baseAddress"></param>
        private void CompareDataBytes(byte[] fileData, List<byte> deviceData, uint baseAddress)
        {
            for (int i = 0; i < fileData.Length; i++)
            {
                if (fileData[i] != deviceData[i])
                {
                    _logger.LogInformationWColor(Brushes.Red,
                        $"Файлы не совпадают. Адрес: 0x{(baseAddress + i):X8}\n" +
                        $"Ожидаемый: {fileData[i]:X2}, Фактический: {deviceData[i]:X2}");
                    return;
                }
            }

            _logger.LogInformationWColor(Brushes.Green, "Файлы совпадают.");
        }

        /// <summary>
        /// Переворачивает байты если выбран bigendian
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private uint ReverseBytes(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
    }
}