using progMap.App.Avalonia.ViewModels.TestViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace progMap.App.Avalonia.Models.Tests
{
    /// <summary>
    /// Описывает циклограмму
    /// </summary>
    [Serializable]
    public class Cyclogram
    {
        private ECyclogramState _testState;
        private bool _isRunning;
        private CancellationTokenSource _cancelTest;

        /// <summary>
        /// Изменилось состояние теста
        /// </summary>
        public event Action StateChanged;

        /// <summary>
        /// Изменилось состояние запуска циклограммы
        /// </summary>
        public event Action IsRunningChanged;

        [XmlIgnore]
        public bool StopByError { get; set; }

        /// <summary>
        /// Счётчик числа циклов
        /// </summary>
        [XmlIgnore]
        public uint CycleCounter { get; set; }

        /// <summary>
        /// Название циклограммы
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// Статус выполнения теста
        /// </summary>
        [XmlIgnore]
        public ECyclogramState TestState
        {
            get => _testState;
            private set
            {
                if (value == _testState)
                    return;

                _testState = value;
                StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Флаг "Идет тест"
        /// </summary>
        [XmlIgnore]
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (value == _isRunning)
                    return;

                _isRunning = value;
                IsRunningChanged?.Invoke();
            }
        }

        /// <summary>
        /// Задачи теста
        /// </summary>        
        [XmlArray("TestCases"), XmlArrayItem("TestCase")]
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();

        public Cyclogram() { }
        public static Cyclogram OpenCyclogram(string filename)
        {
            try
            {
                using (var file = new FileStream(filename, FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof(Cyclogram));
                    var cyclogram = (Cyclogram)serializer.Deserialize(file);

                    return cyclogram;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Запуск циклограммы
        /// </summary>
        /// <param name="startPosition">стартовая позиция циклограммы</param>        
        public void Start(int startPosition)
        {
            // проверяем правильность указанной позиции
            if (startPosition >= TestCases.Count)
                throw new IndexOutOfRangeException("Указанная позиция для запуска теста находится за пределом listBox_Commands");

            for (int i = 0; i < startPosition; i++)
                TestCases[i].State = ETestCommandState.Ignored;

            for (int i = startPosition; i < TestCases.Count; i++)
                TestCases[i].State = ETestCommandState.Empty;

            TestState = ECyclogramState.Started;

            _cancelTest = new CancellationTokenSource();

            Task testTask = new Task(() => Tester(startPosition, _cancelTest.Token), _cancelTest.Token);
            testTask.Start();
        }

        private async void Tester(int startPosition, CancellationToken stopTe)
        {
            IsRunning = true;
            CycleCounter = 0;

            try
            {
                TestState = ECyclogramState.Pending;

                var error = false;

                for (int indexTestCase = startPosition; indexTestCase < TestCases.Count; indexTestCase++)
                {
                    var testCase = TestCases[indexTestCase];

                    // отмена операции
                    if (stopTe.IsCancellationRequested)
                        stopTe.ThrowIfCancellationRequested();

                    // выполнение тестовой задачи

                    var err = await (testCase.Execute());
                    error |= err;

                    if (error && StopByError)
                    {
                        TestState = ECyclogramState.StopByError;
                        break;
                    }
                }

                TestState = error ? ECyclogramState.Finished_Error : ECyclogramState.Finished_Ok;
            }
            catch (OperationCanceledException ex)
            {
                TestState = ECyclogramState.Abort;
            }
            finally
            {
                IsRunning = false;

                if (TestViewModel._repeatCyclogram)
                {
                    if (!(TestState == ECyclogramState.Abort) && !(TestState == ECyclogramState.StopByError))
                    {
                        CycleCounter++;
                        Start(0);
                    }

                }
            }
        }

        /// <summary>
        /// Остановка циклограммы
        /// </summary>
        public void Stop()
        {
            if (IsRunning)
                _cancelTest?.Cancel();
        }

        /// <summary>
        /// Сохранить конфигурацию
        /// </summary>
        public void Save(string filename)
        {
            try
            {
                File.WriteAllText(filename, Environment.NewLine);

                using (var file = new FileStream(filename, FileMode.OpenOrCreate))
                {
                    var serializer = new XmlSerializer(typeof(Cyclogram), new[] { typeof(TestCase), typeof(TestCommand) });
                    serializer.Serialize(file, this);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
