using Avalonia.Media;
using Rss.TmFramework.Base.Logging;
using Rss.TmFramework.Base.Protocols.Map;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Models
{
    /// <summary>
    /// Менеджер для работы с файлами формата Intel HEX
    /// Обеспечивает загрузку, парсинг и запись данных в память устройства
    /// </summary>
    public class IntelHexManager
    {
        private const int HexRecordStartIndex = 1;     // позиция начала записи в строке (после ':')
        private const int ByteCountLength = 2;         // длина поля с количеством байт (в символах)
        private const int AddressLength = 4;           // длина поля адреса (в символах)
        private const int RecordTypeLength = 2;        // длина поля типа записи (в символах)

        /// <summary>
        /// Сервис для работы с памятью устройства
        /// </summary>
        private readonly MemoryAccess _memoryAccess;

        /// <summary>
        /// Логгер 
        /// </summary>
        private readonly ILogger _logger;

        public IntelHexManager(MemoryAccess memoryAccess, ILogger logger)
        {
            _memoryAccess = memoryAccess ?? throw new ArgumentNullException(nameof(memoryAccess));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Асинхронно загружает и записывает HEX-файл в память устройства
        /// </summary>
 
        public async Task LoadIntelHexFileAsync(string filename, Action<int, int>? progressChanged)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("Filename cannot be empty", nameof(filename));

            try
            {
                _logger.LogInformationWColor(Brushes.Black, $"Начата запись файла IntelHex: \"{filename}\"");

                var lines = await File.ReadAllLinesAsync(filename);

                if (lines.Length == 0)
                {
                    _logger.LogInformationWColor(Brushes.Red, $"Файл \"{filename}\" пуст!");
                    return;
                }

                // Обработка строк HEX-файла
                var result = await ProcessHexLinesAsync(lines, progressChanged);

                LogOperationResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogInformationWColor(Brushes.Red, $"Критическая ошибка при обработке HEX-файла: {ex.Message}");
                throw;
            }
            finally
            {
                progressChanged?.Invoke(0, 1);
            }
        }

        /// <summary>
        /// Обрабатывает строки HEX-файла и выполняет операции с памятью
        /// </summary>
        private async Task<bool> ProcessHexLinesAsync(string[] lines, Action<int, int>? progressChanged)
        {
            return await Task.Run(() =>
            {
                uint extendedAddress = 0;  // Текущий расширенный адрес
                var maxLines = lines.Length - 2;  // Максимальное количество строк для обработки
                progressChanged?.Invoke(0, maxLines);

                for (int i = 0; i < maxLines; i++)
                {
                    if (!TryParseHexLine(lines[i], out var record))
                    {
                        _logger.LogInformationWColor(Brushes.Red,
                            $"Ошибка парсинга строки {i + 1}: некорректный формат");
                        return false;
                    }

                    // обработка записи расширенного адреса
                    if (record.Type == 4) // Extended Linear Address Record
                    {
                        extendedAddress = GetExtendedAddress(record.Data);
                        continue;
                    }

                    if (!ProcessMemoryOperation(extendedAddress + record.Address, record.Data, i + 1))
                        return false;

                    progressChanged?.Invoke(i, maxLines);
                }

                return true;
            });
        }

        /// <summary>
        /// Выполняет запись и верификацию данных в памяти устройства
        /// </summary>
        private bool ProcessMemoryOperation(uint address, byte[] data, int lineNumber)
        {
            const EMapFlagsV5 flags = EMapFlagsV5.UseReply | EMapFlagsV5.PhysicalAddress;

            if (!_memoryAccess.TryWriteMemory(address, data, flags, null))
            {
                LogMemoryError("записи", data.Length, address, lineNumber);
                return false;
            }

            // чтение для верификации
            if (!_memoryAccess.TryReadMemory(address, data.Length, flags, out var readData, null))
            {
                LogMemoryError("чтения", data.Length, address, lineNumber);
                return false;
            }

            // сравнение записанных и прочитанных данных
            if (!data.SequenceEqual(readData))
            {
                _logger.LogInformationWColor(Brushes.Red,
                    $"Несоответствие данных в строке {lineNumber}!\n" +
                    $"Записано: {BitConverter.ToString(data)}\n" +
                    $"Прочитано: {BitConverter.ToString(readData.ToArray())}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Преобразует данные записи в расширенный адрес
        /// </summary>
        /// <param name="data">Данные из записи типа 4</param>
        private uint GetExtendedAddress(byte[] data)
        {
            Array.Reverse(data);
            return (uint)BitConverter.ToUInt16(data) << 16;  // Сдвиг на 2 байта
        }

        /// <summary>
        /// Логирует результат операции с HEX-файлом
        /// </summary>
        private void LogOperationResult(bool success)
        {
            var message = success
                ? "Запись файла завершена успешно."
                : "Запись файла завершена с ошибками!";

            var color = success ? Brushes.Green : Brushes.Red;
            _logger.LogInformationWColor(color, message);
        }

        /// <summary>
        /// Логирует ошибки операций с памятью
        /// </summary>
        private void LogMemoryError(string operation, int byteCount, uint address, int lineNumber)
        {
            _logger.LogInformationWColor(Brushes.Red,
                $"Ошибка {operation} {byteCount} байт по адресу 0x{address:X8} (строка {lineNumber})!");
        }

        /// <summary>
        /// Парсит строку HEX-файла в структуру HexRecord
        /// </summary>
        private static bool TryParseHexLine(string line, out HexRecord record)
        {
            record = default;

            try
            {
                if (string.IsNullOrEmpty(line) || line[0] != ':')
                    return false;

                var byteCount = Convert.ToUInt32(line.Substring(HexRecordStartIndex, ByteCountLength), 16);

                record = new HexRecord
                {
                    ByteCount = byteCount,
                    Address = uint.Parse(line.Substring(HexRecordStartIndex + ByteCountLength, AddressLength),
                                       NumberStyles.HexNumber),
                    Type = Convert.ToUInt32(line.Substring(
                        HexRecordStartIndex + ByteCountLength + AddressLength,
                        RecordTypeLength), 16),
                    Data = HexStringToByteArray(line.Substring(
                        HexRecordStartIndex + ByteCountLength + AddressLength + RecordTypeLength,
                        (int)byteCount * 2))
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Преобразует HEX-строку в массив байт
        /// </summary>
        private static byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0) 
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))  
                .ToArray();
        }

        /// <summary>
        /// Структура для хранения данных одной записи HEX-файла
        /// </summary>
        private struct HexRecord
        {
            /// <summary>Количество байт данных</summary>
            public uint ByteCount { get; set; }

            /// <summary>Адрес в памяти</summary>
            public uint Address { get; set; }

            /// <summary>Тип записи (0-данные, 4-расширенный адрес)</summary>
            public uint Type { get; set; }

            /// <summary>Массив данных</summary>
            public byte[] Data { get; set; }
        }
    }
}