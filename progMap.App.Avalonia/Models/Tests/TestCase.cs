using Avalonia.Media;
using progMap.App.Avalonia.ViewModels;
using progMap.Core;
using progMap.ViewModels;
using Rss.TmFramework.Base.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace progMap.App.Avalonia.Models.Tests
{
    /// <summary>
    /// Описывает один тест в рамках циклограммы
    /// </summary>
    [Serializable]
    public class TestCase
    {
        private readonly IMemoryOperations _memoryManager;
        private readonly ILogger _logger;
        private ETestCommandState _state;

        private bool resultError;

        /// <summary>
        /// Описание задачи
        /// </summary>
        [XmlAttribute]
        public string? Description { get; set; }

        /// <summary>
        /// Список команд на отправку
        /// </summary>
        [XmlArray("TestCommands"), XmlArrayItem("TestCommand")]
        public List<TestCommand> TestCommands { get; set; }


        public TestCase(IMemoryOperations memoryManager, ILogger logger)
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _logger = logger;
            TestCommands = new List<TestCommand>();
        }

        /// <summary>
        /// Статус выполнения тестовой задачи
        /// </summary>
        [XmlIgnore]
        public ETestCommandState State
        {
            get => _state;
            set
            {
                if (_state == value)
                    return;

                _state = value;
                OnStateChange?.Invoke();
            }
        }

        /// <summary>
        /// Событие, статус выпонения тестовой задачи изменен
        /// </summary>
        public event Action OnStateChange;

        /// <summary>
        /// Выполнение тестовой задачи, 0 - не выполнена, 1 - выполнена успешно
        /// </summary>
        public async Task<bool> Execute()
        {
            State = ETestCommandState.Pending;
            resultError = false;

            foreach (var testCommand in TestCommands)
            {
                if (resultError == true)
                {
                    State = ETestCommandState.Error;
                    return resultError;
                }
                try
                {
                    var deviceConfig = DeviceConfiguration.Default.FirstOrDefault(dc => dc.Name.Trim() == testCommand.DeviceConfigurationName.Trim());
                    if (deviceConfig == null)
                    {
                        State = ETestCommandState.Error;
                        resultError = true;

                        _logger.LogInformationWColor(Brushes.Red, $"Не найдена конфигурация {testCommand.DeviceConfigurationName.Trim()} ");
                        return resultError;
                    }

                    var deviceConfigVm = DeviceConfigurationVm.FromModel(deviceConfig);
                    if (deviceConfigVm == null)
                    {
                        State = ETestCommandState.Error;
                        resultError = true;

                        _logger.LogInformationWColor(Brushes.Red, $"Не найдена конфигурация {testCommand.DeviceConfigurationName.Trim()} ");
                        return resultError;
                    }

                    var section = deviceConfig.Sections.SingleOrDefault(s => s.Name.Trim() == testCommand.SectionName.Trim());
                    if (section == null)
                    {
                        State = ETestCommandState.Error;
                        resultError = true;

                        
                        _logger.LogInformationWColor(Brushes.Red, $"Не найдена секция {testCommand.SectionName.Trim()} в конфигурации {testCommand.DeviceConfigurationName.Trim()} ");
                        return resultError;
                    }

                    var sectionVm = new FileSectionConfigurationVm(section as FileSectionConfiguration);
                    if (sectionVm == null)
                    {
                        State = ETestCommandState.Error;
                        resultError = true;

                        _logger.LogInformationWColor(Brushes.Red, $"Не найдена секция {testCommand.SectionName.Trim()} в конфигурации {testCommand.DeviceConfigurationName.Trim()} ");
                        return resultError;
                    }

                    bool resultErase = await _memoryManager.EraseSectionAsync(sectionVm, deviceConfigVm);

                    if (!resultErase)
                    {
                        State = ETestCommandState.Error;
                        resultError = true;

                        _logger.LogInformationWColor(Brushes.Red, $"Не удалось стереть файл в секции {testCommand.SectionName.Trim()} в конфигурации {testCommand.DeviceConfigurationName.Trim()} ");
                        return resultError;
                    }

                    if (!File.Exists(testCommand.FileName))
                    {
                        State = ETestCommandState.Error;
                        resultError = true;

                        _logger.LogInformationWColor(Brushes.Red, $"Не найден файл {testCommand.FileName} для секции {testCommand.SectionName.Trim()} в конфигурации {testCommand.DeviceConfigurationName.Trim()} ");
                        return resultError;
                    }

                    bool resultWrite = await _memoryManager.WriteFileSectionAsync(sectionVm, deviceConfigVm, testCommand.FileName);

                    if (!resultWrite)
                    {
                        State = ETestCommandState.Error;
                        resultError = true;

                        _logger.LogInformationWColor(Brushes.Red, $"Не удалось записать файл {testCommand.FileName} в секцию {testCommand.SectionName.Trim()} в конфигурации {testCommand.DeviceConfigurationName.Trim()} ");
                        return resultError;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformationWColor(Brushes.Red, ex.Message);

                    State = ETestCommandState.Error;
                    resultError = true;

                    _logger.LogInformationWColor(Brushes.Red, $"Не удалось стереть файл в секции {testCommand.SectionName.Trim()} в конфигурации {testCommand.DeviceConfigurationName.Trim()} ");
                    return resultError;
                }
            };

            State = resultError ? ETestCommandState.Error : ETestCommandState.Completed;
            return resultError;
        }
        public override string ToString()
        {
            StringBuilder sb1 = new();

            foreach (var command in TestCommands)

                sb1.AppendLine($"Конфигурация: {command.DeviceConfigurationName} Секция: {command.SectionName} Запись файла: {command.FileName}");

            string description = Description + (!string.IsNullOrEmpty(Description) ? Environment.NewLine : string.Empty);

            return
                $"{description}" +
                $"{sb1}{Environment.NewLine}";
        }
    }
}
