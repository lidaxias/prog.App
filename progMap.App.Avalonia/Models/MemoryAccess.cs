using Avalonia.Media;
using Rss.TmFramework.Base.Logging;
using Rss.TmFramework.Base.Protocols.Map;
using Rss.TmFramework.Modules;
using System;
using System.Collections.Generic;

namespace progMap.App.Avalonia.Models
{
    /// <summary>
    /// Описывает логику для работы с памятью
    /// </summary>
    public class MemoryAccess
    {
        private const int MaxBufSize = 128;
        private readonly MapEthernetModule _module;
        private readonly ILogger _logger;

        /// <summary>
        /// Флаг "завершение текущей операции с файлом"
        /// </summary>
        public bool StopOperation { get; set; } = false;

        /// <summary>
        /// Флаг "отписывать пакеты в журнал"
        /// </summary>
        public bool IsWritePackets { get; set; } = false;

        public MemoryAccess(MapEthernetModule module, ILogger logger)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Получает максимальный размер пакета из прибора
        /// </summary>
        private int GetMaxRxBufferSize()
        {
            if (!_module.TryReadMemory(0x0000_0000, 4, EMapFlagsV5.None, out var data, out var errMsg, out var errPckt))
            {
                if (errPckt == null)
                {
                    _logger.LogInformationWColor(Brushes.Red, $"Превышено время ожидания ответа! Не удалось считать размер пакета. Установленный размер пакета: 128 байт.");
                    return MaxBufSize;
                }
                else
                {
                    var flagError = (errPckt.Flags & EMapFlagsV5.SizeError) | (errPckt.Flags & EMapFlagsV5.AddressError) | (errPckt.Flags & EMapFlagsV5.OperationError);
                    _logger.LogInformationWColor(Brushes.Red, $"Ошибка чтения данных о размере пакета: {flagError}. Установленный размер пакета: 128 байт.{Environment.NewLine}{errMsg}");
                    return MaxBufSize;
                }
            }

            return BitConverter.ToInt32(data);
        }

        /// <summary>
        /// Читает данные по конкретному адресу
        /// </summary>
        public bool TryReadMemory(uint addr, int size, EMapFlagsV5 flags, out List<byte>? rcvdData, Action<int, int>? progressChanged)
        {
            rcvdData = new List<byte>(size);
            int progress = 0;
            var progressMaxSize = size;
            var maxPacketSize = GetMaxRxBufferSize();

            progressChanged?.Invoke(0, progressMaxSize);

            try
            {
                while (size != 0)
                {
                    if (StopOperation)
                    {
                        HandleOperationInterrupted(progressChanged, "чтения");
                        return false;
                    }

                    var packetSize = Math.Min(size, maxPacketSize);

                    if (!_module.TryReadMemory(addr, packetSize, flags, out var data, out var errorMessage, out var errorPacket))
                    {
                        HandleReadWriteError(addr, packetSize, errorMessage, errorPacket, progressChanged, isRead: true);
                        return false;
                    }

                    rcvdData.AddRange(data);
                    progress += packetSize;
                    progressChanged?.Invoke(progress, progressMaxSize);

                    addr += (uint)packetSize;
                    size -= packetSize;
                }

                progressChanged?.Invoke(0, 1);
                return true;
            }
            catch (Exception e)
            {
                HandleException(e, addr, progressChanged, isRead: true);
                return false;
            }
        }

        /// <summary>
        /// Записывает данные по конкретному адресу
        /// </summary>
        public bool TryWriteMemory(uint addr, byte[] data, EMapFlagsV5 flags, Action<int, int>? progressChanged)
        {
            var maxBufferSize = data.Length;
            var offset = 0;
            int progress = 0;
            var buffer = new byte[maxBufferSize];

            progressChanged?.Invoke(0, data.Length);

            try
            {
                var maxRxBufferSize = GetMaxRxBufferSize();

                while (maxBufferSize != 0)
                {
                    if (StopOperation)
                    {
                        HandleOperationInterrupted(progressChanged, "записи");
                        return false;
                    }

                    var pktsize = Math.Min(maxRxBufferSize, maxBufferSize);

                    if (buffer.Length != pktsize)
                        Array.Resize(ref buffer, pktsize);

                    Array.Copy(data, offset, buffer, 0, pktsize);

                    if (!_module.TryWriteMemory(addr, buffer, flags, out var errorMessage, out var errorPacket))
                    {
                        HandleReadWriteError(addr, buffer.Length, errorMessage, errorPacket, progressChanged, isRead: false);
                        return false;
                    }

                    progress += pktsize;
                    progressChanged?.Invoke(progress, data.Length);

                    addr += (uint)pktsize;
                    maxBufferSize -= pktsize;
                    offset += pktsize;
                }

                progressChanged?.Invoke(0, 1);
                return true;
            }
            catch (Exception e)
            {
                HandleException(e, addr, progressChanged, isRead: false);
                return false;
            }
        }

        /// <summary>
        /// Обрабатывает отмену операции пользователем
        /// </summary>
        /// <param name="progressChanged"></param>
        /// <param name="operationType"></param>
        private void HandleOperationInterrupted(Action<int, int>? progressChanged, string operationType)
        {
            _logger.LogInformationWColor(Brushes.Red, $"Операция {operationType} прервана пользователем");
            StopOperation = false;
            progressChanged?.Invoke(0, 1);
        }

        /// <summary>
        /// Обрабатывает ошибку при чтении/записи данных
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="size"></param>
        /// <param name="errorMessage"></param>
        /// <param name="errorPacket"></param>
        /// <param name="progressChanged"></param>
        /// <param name="isRead"></param>
        private void HandleReadWriteError(uint addr, int size, string errorMessage, MapPacketV5? errorPacket, Action<int, int>? progressChanged, bool isRead)
        {
            if (errorPacket == null)
            {
                _logger.LogInformationWColor(Brushes.Red, $"Превышено время ожидания ответа! Не удалось {(isRead ? "прочесть" : "записать")} по адресу 0x{addr:X8} {size} байт");
            }
            else
            {
                var flagError = (errorPacket.Flags & EMapFlagsV5.SizeError) | (errorPacket.Flags & EMapFlagsV5.AddressError) | (errorPacket.Flags & EMapFlagsV5.OperationError);
                _logger.LogInformationWColor(Brushes.Red, $"Ошибка {(isRead ? "чтения" : "записи")} данных: {flagError}. Не удалось {(isRead ? "прочесть" : "записать")} по адресу 0x{addr:X8} {size} байт!{Environment.NewLine}{errorMessage}");
            }

            progressChanged?.Invoke(0, 1);
        }

        /// <summary>
        /// Обрабатывает ошибку при возникновении исключения в момент процесса чтения/записи
        /// </summary>
        /// <param name="e"></param>
        /// <param name="addr"></param>
        /// <param name="progressChanged"></param>
        /// <param name="isRead"></param>
        private void HandleException(Exception e, uint addr, Action<int, int>? progressChanged, bool isRead)
        {
            _logger.LogInformationWColor(Brushes.Red, $"Ошибка {(isRead ? "чтения" : "записи")} данных: {e} по адресу 0x{addr:X8}");
            progressChanged?.Invoke(0, 1);
        }
    }
}