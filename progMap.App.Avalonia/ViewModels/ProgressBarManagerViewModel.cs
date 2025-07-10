using progMap.App.Avalonia.Models;
using ReactiveUI;
using System.Reactive;

namespace progMap.App.Avalonia.ViewModels
{
    /// <summary>
    /// Менеджер для управления progressbar
    /// </summary>
    public class ProgressBarManagerViewModel : ViewModelBase
    {
        // значения по умолчанию для progressbar
        private int _progressMinimum = 0;
        private int _progressMaximum = 1;
        private int _progressValue = 0;
        private bool _isRunning = false;
        private MemoryAccess _memoryAccess;
        private string _operationName = "Загрузка файла: ";

        /// <summary>
        /// Команда на остановку пользователем текущей операции с файлом
        /// </summary>
        public ReactiveCommand<Unit, Unit> StopCommand { get; }

        public ProgressBarManagerViewModel(MemoryAccess memoryAccess)
        {
            _memoryAccess = memoryAccess;
        }

        public ProgressBarManagerViewModel()
        {
            StopCommand = ReactiveCommand.Create(Stop);
        }

        /// <summary>
        /// Минимальное значение ProgressBar
        /// </summary>
        public int ProgressMinimum
        {
            get => _progressMinimum;
            set => this.RaiseAndSetIfChanged(ref _progressMinimum, value);
        }

        /// <summary>
        /// Максимальное значение ProgressBar
        /// </summary>
        public int ProgressMaximum
        {
            get => _progressMaximum;
            set => this.RaiseAndSetIfChanged(ref _progressMaximum, value);
        }

        /// <summary>
        /// Текущее значение ProgressBar
        /// </summary>
        public int ProgressValue
        {
            get => _progressValue;
            set => this.RaiseAndSetIfChanged(ref _progressValue, value);
        }

        /// <summary>
        /// Имя текущей операции для progressbar
        /// </summary>
        public string OperationName
        {
            get => _operationName;
            set => this.RaiseAndSetIfChanged(ref _operationName, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }

        /// <summary>
        /// Устанавливает значения progressbar
        /// </summary>
        public void SetProgress(int a, int b)
        {
            ProgressValue = a;
            ProgressMaximum = b;
        }

        /// <summary>
        /// Останавливает выполнение текущей операции с файлом
        /// </summary>
        public void Stop()
        {
            if (IsRunning)
            {
                _memoryAccess.StopOperation = true;
                IsRunning = false;
            }
        }
       
    }
}
