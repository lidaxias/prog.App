using Avalonia.Media;
using Avalonia.Threading;
using progMap.Interfaces;
using ReactiveUI;
using Rss.TmFramework.Base.Logging;
using Rss.TmFramework.Controls.Avalonia;
using Rss.TmFramework.Logging;
using Rss.TmFramework.Modules;
using Rss.TmFramework.Mvvm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;

namespace progMap.App.Avalonia.Plugins
{
    /// <summary>
    /// ViewModel для плагина логгера, реализующая интерфейс IProgPlugin.
    /// Обеспечивает сбор, отображение и сохранение лог-сообщений.
    /// </summary>
    public class LoggerPluginViewModel : ViewModelBase, IProgPlugin, IDisposable
    {
        private bool _disposed;
        private readonly Dispatcher _dispatcher; 
        private const int LOG_INTERVAL = 100; 
        private const int MAX_LOG_MESSAGES = 1000; 

        // очередь для хранения входящих лог-сообщений
        private readonly ConcurrentQueue<LogMessage> _logQueue = new();

        // таймер для периодической обработки очереди сообщений
        private readonly System.Timers.Timer _logTimer;

        // коллекция для хранения лог-сообщений с ограничением по размеру
        private readonly FixedSizeLogCollection _logMessages;

        // писатель для записи логов в файл
        private StreamWriter _sw;
        private string _logFilePath; // Путь к файлу логов

        // текущие дата и время для формирования имени файла
        private static readonly string DateTimeNowString = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // шаблон имени файла логов
        private readonly string FileNameLog = DateTimeNowString + " Журнал" + ".txt";

        /// <summary>
        /// Заголовок плагина для отображения в UI
        /// </summary>
        public string Title => "Журнал";

        /// <summary>
        /// Модуль для работы с MAP-протоколом
        /// </summary>
        public MapEthernetModule MapModule { get; set; }

        /// <summary>
        /// Экземпляр логгера для записи сообщений
        /// </summary>
        public ILogger Logger { get; set; } = new Logger();

        /// <summary>
        /// Коллекция лог-сообщений для привязки к UI
        /// </summary>
        public ObservableCollection<LogMessage> LogMessages => _logMessages.Items;

        /// <summary>
        /// Команда для очистки логов
        /// </summary>
        public ReactiveCommand<Unit, Unit> ClearLogCommand { get; }

        /// <summary>
        /// Конструктор инициализирует компоненты логгера
        /// </summary>
        public LoggerPluginViewModel()
        {
            _dispatcher = Dispatcher.UIThread;
            _logMessages = new FixedSizeLogCollection(MAX_LOG_MESSAGES);

            InitializeLogFile(); 

            // Подписка на событие записи логов
            Logger.LogMessageWColor += EnqueueLogMessage;

            // Настройка таймера для обработки очереди сообщений
            _logTimer = new System.Timers.Timer(LOG_INTERVAL);
            _logTimer.Elapsed += ProcessLogQueue;
            _logTimer.Start();

            // Инициализация команды очистки логов
            ClearLogCommand = ReactiveCommand.Create(ClearLogs);
        }

        /// <summary>
        /// Добавляет сообщение в очередь логов и записывает его в файл
        /// </summary>
        /// <param name="time">Время события</param>
        /// <param name="color">Цвет сообщения</param>
        /// <param name="text">Текст сообщения</param>
        private void EnqueueLogMessage(DateTime time, IBrush color, string text)
        {
            var message = LogMessage.Create(time, color, text);
            _logQueue.Enqueue(message);

            _sw.WriteLine($"{time:yyyy.MM.dd HH:mm:ss.ffffff} | {text}");
            _sw.Flush();
        }

        /// <summary>
        /// Обрабатывает очередь сообщений и добавляет их в коллекцию для UI
        /// </summary>
        private void ProcessLogQueue(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_disposed) return;

            var batch = new List<LogMessage>();
            // Извлекаем пачку сообщений (максимум 100 за раз)
            while (_logQueue.TryDequeue(out var message) && batch.Count < 100)
            {
                batch.Add(message);
            }

            if (batch.Count > 0)
            {
                // Добавляем сообщения в UI потоке
                _dispatcher.InvokeAsync(() => _logMessages.AddRange(batch));
            }
        }

        /// <summary>
        /// Инициализирует систему записи логов в файл
        /// </summary>
        private void InitializeLogFile()
        {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var filesDir = Path.Combine(currentDir, "Files");

            if (!Directory.Exists(filesDir))
            {
                Directory.CreateDirectory(filesDir);
            }

            _logFilePath = Path.Combine(filesDir, FileNameLog);

            _sw = new StreamWriter(_logFilePath, append: false, Encoding.UTF8)
            {
                AutoFlush = true 
            };
        }

        /// <summary>
        /// Очищает все логи (коллекцию сообщений и текстовый редактор)
        /// </summary>
        private void ClearLogs()
        {
            _logMessages.Clear();
            LogMessage.ClearBrushCache(); 
            LoggerTextEditorBehavior.ClearTextEditor(); 
        }

        /// <summary>
        /// Освобождает ресурсы логгера
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Отписка от событий и остановка таймера
            _logTimer.Elapsed -= ProcessLogQueue;
            _logTimer.Stop();
            _logTimer.Dispose();

            Logger.LogMessageWColor -= EnqueueLogMessage;

            ClearLogs();

            _sw?.Dispose();
        }
    }

    /// <summary>
    /// Коллекция лог-сообщений с фиксированным максимальным размером
    /// </summary>
    public class FixedSizeLogCollection(int maxSize)
    {
        private readonly object _lock = new(); 
        private readonly int _maxSize = maxSize; 
        private readonly ObservableCollection<LogMessage> _items = new(); 

        /// <summary>
        /// Доступ к элементам коллекции
        /// </summary>
        public ObservableCollection<LogMessage> Items => _items;

        /// <summary>
        /// Добавляет набор сообщений в коллекцию
        /// </summary>
        /// <param name="messages">Коллекция сообщений для добавления</param>
        public void AddRange(IEnumerable<LogMessage> messages)
        {
            lock (_lock)
            {
                foreach (var message in messages)
                {
                    _items.Add(message);

                    // Удаляем старые сообщения при превышении лимита
                    while (_items.Count > _maxSize)
                    {
                        var old = _items[0];
                        _items.RemoveAt(0);
                        old.Dispose(); 
                    }
                }
            }
        }

        /// <summary>
        /// Полностью очищает коллекцию сообщений
        /// </summary>
        public void Clear()
        {
            List<LogMessage> toDispose;
            lock (_lock)
            {
                toDispose = _items.ToList();
                _items.Clear();
            }

            // Освобождаем ресурсы всех сообщений
            foreach (var item in toDispose)
            {
                item.Dispose();
            }
        }
    }
}