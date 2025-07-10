using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Controls;
using DynamicData;
using progMap.App.Avalonia.Models.Tests;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Timers;
using Avalonia;
using Rss.TmFramework.Base.Logging;

namespace progMap.App.Avalonia.ViewModels.TestViewModels
{
    /// <summary>
    /// ViewModel для окна тестирования
    /// </summary>
    public class TestViewModel : ViewModelBase
    {
        private int _selectedTestCaseIndex;
        private string _testStateText = "Загрузите циклограмму...";
        private Cyclogram _cyclogram;
        public static bool _repeatCyclogram = false;

        private readonly ILogger _logger;

        private OpenFileDialog _openFileTest = new OpenFileDialog();

        private ObservableCollection<TestCaseViewModel> _testCases = new ObservableCollection<TestCaseViewModel>();

        private Timer _timer;
        private TimeSpan _timeEllapsed = new TimeSpan();
        private readonly TimeSpan _timerResolution = new TimeSpan(0, 0, 0, 0, 50);
        private bool _isTestEditorWindowOpened;

        /// <summary>
        /// Флаг - Тест запущен
        /// </summary>
        public bool TestIsRunning => CyclogramLoaded && Cyclogram.IsRunning;

        /// <summary>
        /// Циклограмма загружена
        /// </summary>
        public bool CyclogramLoaded => Cyclogram != null;
        public string TestStateText
        {
            get => _testStateText;
            set => this.RaiseAndSetIfChanged(ref _testStateText, value);
        }

        /// <summary>
        /// Остановить тест по ошибке
        /// </summary>
        public bool StopTestByError
        {
            get => Cyclogram != null ? Cyclogram.StopByError : false;
            set => Cyclogram.StopByError = value;
        }

        /// <summary>
        /// Циклограмма
        /// </summary>
        public Cyclogram Cyclogram
        {
            get => _cyclogram;
            set
            {
                this.RaiseAndSetIfChanged(ref _cyclogram, value);
                this.RaisePropertyChanged(nameof(TestCases));
            }
        }

        /// <summary>
        /// Повторять циклограмму
        /// </summary>
        public bool RepeatCyclogram
        {
            get => _repeatCyclogram;
            set => this.RaiseAndSetIfChanged(ref _repeatCyclogram, value);
        }

        /// <summary>
        /// Список всех задач теста
        /// </summary>
        public ObservableCollection<TestCaseViewModel> TestCases
        {
            get => _testCases;
            set => this.RaiseAndSetIfChanged(ref _testCases, value);
        }

        /// <summary>
        /// Текущий выбранный тест
        /// </summary>
        public int SelectedTestCaseIndex
        {
            get => _selectedTestCaseIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedTestCaseIndex, value);
        }

        public TimeSpan TimeEllapsed
        {
            get => _timeEllapsed;
            set
            {
                if (_timeEllapsed == value)
                    return;

                _timeEllapsed = value;
                this.RaisePropertyChanged(nameof(TimeEllapsedText));
            }
        }
        /// <summary>
        /// Флаг - "окно редактирования циклограммы открыто"
        /// </summary>
        public bool IsTestEditorWindowOpened
        {
            get => _isTestEditorWindowOpened;
            set => this.RaiseAndSetIfChanged(ref _isTestEditorWindowOpened, value);
        }

        public string TimeEllapsedText => TimeEllapsed.ToString("hh\\:mm\\:ss\\.fff");

        /// <summary>
        /// Команда - "Открыть файл"
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenFile_Command { get; }

        /// <summary>
        /// Команда - "Начать тест"
        /// </summary>
        public ReactiveCommand<Unit, Unit> StartTest_Command { get; }

        /// <summary>
        /// Команда - "Запустить тест, начиная с выделенного элемента"
        /// </summary>
        public ReactiveCommand<Unit, Unit> StartTestBySelected_Command { get; }

        /// <summary>
        /// Команда - "Остановить тест"
        /// </summary>
        public ReactiveCommand<Unit, Unit> StopTest_Command { get; }

        /// <summary>
        /// Команда на открытие окна с редактором циклограмм
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenEditorWindow_Command { get; }

        public TestViewModel(ILogger logger)
        {
            OpenFile_Command = ReactiveCommand.Create(OpenFile);
            StartTest_Command = ReactiveCommand.Create(StartTest);
            StartTestBySelected_Command = ReactiveCommand.Create(StartTestBySelected);
            StopTest_Command = ReactiveCommand.Create(StopTest);


            _timer = new Timer(_timerResolution.TotalMilliseconds);
            _timer.Elapsed += Timer_Elapsed;
            _logger = logger;
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            TimeEllapsed += _timerResolution;
        }

        public async void OpenFile()
        {
            var currentWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Windows.Last();

            // синхронный запуск диалога, для блокировки окна ПСС 
            var filenames = await _openFileTest.ShowAsync(currentWindow);

            if (filenames == null || filenames.Length == 0)
                return;
            try
            {
                Cyclogram = Cyclogram.OpenCyclogram(filenames[0]);
            }
            catch (Exception ex)
            {

                return;
            }

            if (Cyclogram == null)
                return;

            Cyclogram.StateChanged += Cyclogram_StateChanged;
            Cyclogram.IsRunningChanged += () =>
            {
                this.RaisePropertyChanged(nameof(TestIsRunning));

                // сброс таймера
                if (TestIsRunning)
                {
                    TimeEllapsed = TimeSpan.Zero;
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }
            };

            TestCases.Clear();
            TestCases.AddRange(Cyclogram.TestCases.Select(testcase => TestCaseViewModel.FromModel(testcase, _logger)));

            TestStateText = $"Циклограмма \"{Cyclogram.Name}\": загружена";
        }

        private void Cyclogram_StateChanged()
        {
            if (Cyclogram == null)
                return;

            switch (Cyclogram.TestState)
            {
                case ECyclogramState.None:
                    TestStateText = "Загрузите циклограмму...";
                    break;
                case ECyclogramState.Started:
                    // подготовка теста прошла успешно
                    TestStateText = $"Циклограмма \"{Cyclogram.Name}\" : запущена";
                    _logger.LogInformationWColor(Brushes.DarkViolet, TestStateText);
                    break;
                case ECyclogramState.Pending:
                    TestStateText = $"Циклограмма \"{Cyclogram.Name}\" : выполняется";
                    break;
                case ECyclogramState.StopByError:
                    TestStateText = $"Циклограмма \"{Cyclogram.Name}\" : остановлена по ошибке";
                    _logger.LogInformationWColor(Brushes.Red, TestStateText);
                    break;
                case ECyclogramState.Finished_Ok:
                    TestStateText = $"Циклограмма \"{Cyclogram.Name}\" : успешно завершена";
                    _logger.LogInformationWColor(Brushes.Green, TestStateText);
                    break;
                case ECyclogramState.Finished_Error:
                    TestStateText = $"Циклограмма \"{Cyclogram.Name}\" : завершена с ошибкой";
                    _logger.LogInformationWColor(Brushes.Red, TestStateText);
                    break;
                case ECyclogramState.Exception:
                    TestStateText = $"Циклограмма \"{Cyclogram.Name}\" : завершена в связи с некорректной циклограммой";
                    _logger.LogInformationWColor(Brushes.Red, TestStateText);
                    break;
                case ECyclogramState.Abort:
                    TestStateText = $"Циклограмма \"{Cyclogram.Name}\" : прервана пользователем";
                    _logger.LogInformationWColor(Brushes.DarkViolet, TestStateText);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Запускает тест
        /// </summary>
        private void StartTest()
        {
            if (Cyclogram == null || TestCases == null)
                return;

            Cyclogram.Start(0);

        }

        /// <summary>
        /// Запускает тест с выбранной задачи
        /// </summary>
        private void StartTestBySelected()
        {
            if (Cyclogram == null || TestCases == null || SelectedTestCaseIndex == -1)
                return;

            Cyclogram.Start(SelectedTestCaseIndex);
        }

        /// <summary>
        /// Остановка тестирования
        /// </summary>
        private void StopTest()
        {
            if (Cyclogram == null || TestCases == null || !TestIsRunning)
                return;

            Cyclogram.Stop();
        }
    }
}
