using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using progMap.App.Avalonia.Models;
using progMap.App.Avalonia.Plugins;
using progMap.App.Avalonia.ViewModels.TestViewModels;
using progMap.App.Avalonia.Views.TestViews;
using progMap.App.Avalonia.Views;
using progMap.Core;
using progMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using System.IO;
using Rss.TmFramework.Base.Logging;
using ReactiveUI;

namespace progMap.App.Avalonia.Services
{
    /// <summary>
    /// Сервис, описывающий основные методы по работе с диалоговыми окнами
    /// </summary>
    public interface IWindowService
    {
        Task OpenFileConfiguration(DeviceConfigurationVm selectedDeviceVm, List<DeviceConfigurationVm> deviceConfigurationsVm,
            List<DeviceConfiguration> deviceConfigurations);
        Task OpenTestWindow(TestViewModel testVm);
    }

    /// <summary>
    /// Сервис по работе с диалоговыми окнами
    /// </summary>
    public class WindowService : ViewModelBase, IWindowService
    {
        private readonly ILogger _logger;
        private bool _isTerminalOpened = false;
        private bool _isTestOpened = false;
        private TerminalWindow _terminalWindow;
        private TestWindow _testWindow;

        /// <summary>
        /// Флаг "терминал открыт"
        /// </summary>
        public bool IsTerminalOpened
        {
            get => _isTerminalOpened;
            set => this.RaiseAndSetIfChanged(ref _isTerminalOpened, value);
        }

        /// <summary>
        /// Флаг "окно с тестами открыто"
        /// </summary>
        public bool IsTestOpened
        {
            get => _isTestOpened;
            set => this.RaiseAndSetIfChanged(ref _isTestOpened, value);
        }


        // имя файла по умолчанию для сериализации/десериализации
        private readonly string DeviceConfigFileName = "program.xml";
        private UserFileDialog _openXmlFile;


        private void InitXmlFileDialogs()
        {
            _openXmlFile = new UserFileDialog();
            _openXmlFile.Filters?.Add(new FileDialogFilter() { Name = "Файлы конфигураций (*.xml)", Extensions = new List<string>(new string[] { "xml" }) });
            _openXmlFile.Filters?.Add(new FileDialogFilter() { Name = "Все файлы (*.*)", Extensions = new List<string>(new string[] { "*" }) });
            _openXmlFile.AllowMultiple = false;
        }

        public WindowService(ILogger logger)
        {
            _logger = logger;
            InitXmlFileDialogs();
        }
        /// <summary>
        /// Открывает окно для формирования образа файла
        /// </summary>
        public async Task OpenFileFormationWindow(FileFormatorVm fileFormatorVm, DeviceConfigurationVm selectedDeviceConfig)
        {
            var curDir = AppDomain.CurrentDomain.BaseDirectory; // получаем текущую директорию

            var fullPathToDeviceConfigFile = Path.Combine(curDir, DeviceConfigFileName); // полный путь до файла с конфигурациями

            // если существует файл с конфигурациями
            if (File.Exists(fullPathToDeviceConfigFile))
            {
                fileFormatorVm = new FileFormatorVm(_logger)
                {
                    AllFileSectionConfigurationsVm = selectedDeviceConfig.FileSections.ToList(),
                };

                var fileFormatorWindow = new FileFormatorWindow() { DataContext = fileFormatorVm };
                var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();

                await fileFormatorWindow.ShowDialog(currentWindow);
            }
        }

        /// <summary>
        /// Открывает и десериализует файл с конфигурацией
        /// </summary>
        public async Task OpenFileConfiguration(DeviceConfigurationVm selectedDeviceVm, List<DeviceConfigurationVm> deviceConfigurationsVm,
            List<DeviceConfiguration> deviceConfigurations)
        {
            var curDir = AppDomain.CurrentDomain.BaseDirectory; // получаем текущую директорию

            var fullPathToDeviceConfigFile = Path.Combine(curDir, DeviceConfigFileName); // полный путь до файла с конфигурациями
            var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();

            var fileNames = await _openXmlFile.ShowDialog(currentWindow);

            if (fileNames == null || fileNames.Length == 0)
                return;

            var models = Serializer<List<DeviceConfiguration>>.Deserialize(fileNames[0]);

            fullPathToDeviceConfigFile = fileNames[0];

            deviceConfigurationsVm = models.Select(DeviceConfigurationVm.FromModel).ToList();
            deviceConfigurations = deviceConfigurationsVm.Select(dc => dc.GetModel() as DeviceConfiguration).ToList();
            selectedDeviceVm = deviceConfigurationsVm?.FirstOrDefault();
        }

        /// <summary>
        /// Открывает отдельное окно с циклограммой команд
        /// </summary>
        public async Task OpenTestWindow(TestViewModel testVm)
        {
            if (!IsTestOpened)
            {
                _testWindow = new Views.TestViews.TestWindow() { DataContext = testVm };

                IsTestOpened = true;
            }

            var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();
            _testWindow.Show(currentWindow);
            _testWindow.Closed += (sender, e) => { IsTestOpened = false; };
        }

        /// <summary>
        /// Открывает отдельное окно с терминалом
        /// </summary>
        public async Task OpenTerminalWindow(TerminalPluginViewModel terminalVm)
        {
            if (!IsTerminalOpened)
            {
                _terminalWindow = new TerminalWindow() { DataContext = terminalVm };

                IsTerminalOpened = true;
            }

            var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();
            _terminalWindow.Show(currentWindow);
            _terminalWindow.Closed += (sender, e) => { IsTerminalOpened = false; };
        }

        /// <summary>
        /// Открывает диалоговое окно для сохранения
        /// </summary>
        public async Task<string> ShowSaveFileDialog(Window parentWindow)
        {
            var sfd = new SaveFileDialog();
            return await sfd.ShowAsync(parentWindow);
        }

    }
}
