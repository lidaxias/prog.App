using Avalonia.Media;
using progMap.App.Avalonia.Models.Tests;
using ReactiveUI;
using Rss.TmFramework.Base.Logging;

namespace progMap.App.Avalonia.ViewModels.TestViewModels
{
    /// <summary>
    /// ViewModel задачи
    /// </summary>
    public class TestCaseViewModel : ViewModelBase
    {
        private TestCase _testCase;
        private readonly ILogger _logger;

        /// <summary>
        /// Задача
        /// </summary>
        public TestCase TestCase
        {
            get => _testCase;
            set
            {
                this.RaiseAndSetIfChanged(ref _testCase, value);

                this.RaisePropertyChanged(nameof(Header));
                this.RaisePropertyChanged(nameof(State));
            }
        }
        public static TestCaseViewModel FromModel(TestCase testcase, ILogger logger) => new(testcase, logger);

        /// <summary>
        /// Название задачи
        /// </summary>
        public string Header => TestCase != null ? TestCase.ToString() : string.Empty;

        /// <summary>
        /// Статус выполнения теста
        /// </summary>
        public ETestCommandState State => _testCase.State;

        public TestCaseViewModel() { }
        public TestCaseViewModel(TestCase testCase, ILogger logger)
        {
            _logger = logger;
            TestCase = testCase;
            TestCase.OnStateChange += () =>
            {
                this.RaisePropertyChanged(nameof(State));

                var testCaseDescription = string.IsNullOrEmpty(TestCase.Description) ? "Задача" : TestCase.Description;

                switch (TestCase.State)
                {
                    case ETestCommandState.Empty:
                        break;
                    case ETestCommandState.Pending:
                        logger.LogInformationWColor(Brushes.DarkViolet, TestCase.ToString());
                        break;
                    case ETestCommandState.Completed:
                        logger.LogInformationWColor(Brushes.Green, $"{testCaseDescription} - выполнена успешно");
                        break;
                    case ETestCommandState.Error:
                        logger.LogInformationWColor(Brushes.Red, $"{testCaseDescription} - выполнена с ошибкой");
                        break;
                    case ETestCommandState.Ignored:
                        break;
                    default:
                        break;
                }
            };
            this.RaisePropertyChanged(nameof(Header));
        }
    }
}
