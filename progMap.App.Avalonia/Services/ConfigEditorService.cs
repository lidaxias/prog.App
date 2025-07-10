using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using progMap.App.Avalonia.ViewModels;
using progMap.App.Avalonia.Views;
using progMap.Core;
using progMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using System.IO;

namespace progMap.App.Avalonia.Services
{
    /// <summary>
    /// Сервис, описывающий основные методы для редактирования конфигурации
    /// </summary>
    public interface IConfigEditorService
    {
         Task<List<DeviceConfigurationVm>> EditConfigAsync(
         string configFilePath,
         IEnumerable<DeviceConfiguration> currentDevices,
         IEnumerable<ChannelTypeVm> channelTypes,
         List<DeviceConfigurationVm> devicesVmCopy);
    }

    /// <summary>
    /// Сервис, отвечающий за редактирование конфигурации файлов
    /// </summary>
    public class ConfigEditorService : IConfigEditorService
    {
        private readonly IConnectionService _connectionService;
        private readonly WindowService _windowService;

        public ConfigEditorService(IConnectionService connectionService, WindowService windowService)
        {
            _connectionService = connectionService;
            _windowService = windowService;
        }

        /// <summary>
        /// Редактирует конфигурацию
        /// </summary>
        /// <param name="configFilePath"></param>
        /// <param name="currentDevices"></param>
        /// <param name="channelTypes"></param>
        /// <param name="devicesVmCopy"></param>
        /// <returns></returns>
        public async Task<List<DeviceConfigurationVm>> EditConfigAsync(
         string configFilePath,
         IEnumerable<DeviceConfiguration> currentDevices,
         IEnumerable<ChannelTypeVm> channelTypes,
         List<DeviceConfigurationVm> devicesVmCopy)
        {
            var configSaver = new ConfigSaver(_connectionService);
            var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();

            var editorVm = File.Exists(configFilePath)
                ? CreateEditorVmWithExistingConfig(currentDevices, channelTypes)
                : new EditorVm { AllConfigurations = new ObservableCollection<DeviceConfigurationVm>() };

            var editorWindow = new EditorWindow() { DataContext = editorVm };
            await editorWindow.ShowDialog(currentWindow);

            if (editorWindow.SaveAsFlag == true)
            {
                await HandleSaveAs(configSaver, _windowService, currentWindow, configFilePath, editorVm);
                return editorVm.AllConfigurations.ToList();
            }
            else if (editorWindow.SaveFlag == true)
            {
                await HandleSave(configSaver, configFilePath, editorVm);
                return editorVm.AllConfigurations.ToList();
            }
            else
            {
                RestoreOriginalConfig(configFilePath, devicesVmCopy);
                return devicesVmCopy;
            }
        }

        private EditorVm CreateEditorVmWithExistingConfig(
            IEnumerable<DeviceConfiguration> devices,
            IEnumerable<ChannelTypeVm> channelTypes)
        {
            return new EditorVm
            {
                AllConfigurations = new ObservableCollection<DeviceConfigurationVm>(devices.Select(d => new DeviceConfigurationVm(d))),
                ChannelTypesEd = new ObservableCollection<ChannelTypeVm>(channelTypes)
            };
        }

        private async Task HandleSaveAs(
            ConfigSaver configSaver,
            WindowService windowService,
            Window currentWindow,
            string currentPath,
            EditorVm editorVm)
        {
            var fileName = await windowService.ShowSaveFileDialog(currentWindow);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            await configSaver.SaveConfigurations(editorVm.AllConfigurations, fileName);
        }

        private async Task HandleSave(
            ConfigSaver configSaver,
            string filePath,
            EditorVm editorVm)
        {
            await configSaver.SaveConfigurations(editorVm.AllConfigurations, filePath);
        }

        /// <summary>
        /// Восстанавливает старую конфигурацию при отмене изменений
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="devicesVmCopy"></param>
        private void RestoreOriginalConfig(
            string filePath,
            List<DeviceConfigurationVm> devicesVmCopy)
        {
            Serializer<List<DeviceConfiguration>>.Serialize(
                devicesVmCopy.Select(dc => dc.GetModel() as DeviceConfiguration).ToList(),
                filePath);
        }
    }

    /// <summary>
    /// Сохраняет конфигурацию файла при изменениях
    /// </summary>
    public class ConfigSaver
    {
        private readonly IConnectionService _connectionService;

        public ConfigSaver(IConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public async Task SaveConfigurations(IEnumerable<DeviceConfigurationVm> configurations, string filePath, bool disconnect = true)
        {
            var deviceList = configurations.Select(dc => dc.GetModel() as DeviceConfiguration).ToList();
            Serializer<List<DeviceConfiguration>>.Serialize(deviceList, filePath);

            if (disconnect)
            {
                _connectionService.Disconnect();
            }
        }
    }
}
