using Avalonia.Media;
using Avalonia.Threading;
using progMap.Core;
using progMap.Interfaces;
using progMap.ViewModels;
using ReactiveUI;
using Rss.TmFramework.Base.Channels;
using Rss.TmFramework.Base.Helpers;
using Rss.TmFramework.Base.Logging;
using Rss.TmFramework.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Services
{
    /// <summary>
    /// Сервис, описывающий основные методы для подключения
    /// </summary>
    public interface IConnectionService
    {
        void ConnectDisconnect(ChannelTypeVm channelTypeVm, DeviceConfigurationVm selectedDeviceConfiguration, ObservableCollection<IProgPlugin> plugins, bool isWritePackets);
        void Connect(ChannelTypeVm channelType, DeviceConfigurationVm selectedDeviceConfiguration, ObservableCollection<IProgPlugin> plugins, bool isWritePackets);
        void Disconnect();
    }

    /// <summary>
    /// Сервис, отвечающий за подключение модулей
    /// </summary>
    public class ConnectionService : ViewModelBase, IConnectionService
    {
        private readonly MapEthernetModule _mapModule;
        private readonly ILogger _logger;
        private readonly Dispatcher _dispatcher;
        private bool _isBusy;

        public bool IsConnected => (_mapModule?.Channel?.ConnectionState ?? EConnectionState.Disconnected) == EConnectionState.Connected;

        /// <summary>
        /// Флаг "DTR установлен"
        /// </summary>
        public bool IsDTR
        {
            get
            {
                if (_mapModule?.Channel is ISerialSignals channel)
                    return channel.DtrEnable;

                return false;
            }
            set
            {
                if (_mapModule?.Channel is ISerialSignals channel)
                    channel.DtrEnable = value;
            }
        }

        /// <summary>
        /// Модуль занят
        /// </summary>
        public IObservable<bool> ModuleIsBusy => this.WhenAnyValue(vm => vm.IsBusy);

        /// <summary>
        /// Флаг "модуль занят"
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        /// <summary>
        /// Модуль подключен
        /// </summary>
        public IObservable<bool> ModuleIsConnected => this.WhenAnyValue(vm => vm.IsConnected);

        public ConnectionService(MapEthernetModule mapModule, ILogger logger, Dispatcher dispatcher)
        {
            _mapModule = mapModule;
            _logger = logger;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Устанавливает/разрывает соединение с прибором
        /// </summary>
        public void ConnectDisconnect(ChannelTypeVm channelTypeVm, DeviceConfigurationVm selectedDeviceConfiguration, ObservableCollection<IProgPlugin> plugins, bool isWritePackets)
        {
            if (IsConnected)
                Disconnect();
            else
                Connect(channelTypeVm, selectedDeviceConfiguration, plugins, isWritePackets);

        }

        /// <summary>
        /// Устанавливает соединение с прибором
        /// </summary>
        public void Connect(ChannelTypeVm channelType, DeviceConfigurationVm selectedDeviceConfiguration, ObservableCollection<IProgPlugin> plugins, bool isWritePackets)
        {
            try
            {
                if (channelType == null)
                {
                    _logger.LogInformationWColor(Brushes.Red, "Не получилось открыть соединение! Типа канала не существует!");
                    return;
                }

                if (Activator.CreateInstance(channelType.Type) is not IChannel channel)
                {
                    _logger.LogInformationWColor(Brushes.Red, "Не получилось открыть соединение!");
                    return;
                }

                channel.ConnectionStateChanged += (channel, state) =>
                {
                    _logger.LogInformationWColor(Brushes.Black, $"Channel.ConnectionStateChanged: {state}");


                    _dispatcher.InvokeAsync(() =>
                    {
                        this.RaisePropertyChanged(nameof(IsConnected));
                        this.RaisePropertyChanged(nameof(ModuleIsBusy));
                        this.RaisePropertyChanged(nameof(IsDTR));
                    });

                    foreach (var plugin in plugins)
                    {
                        plugin.MapModule = _mapModule;
                    }
                };

                channel.DataReceived += (channel, data) =>
                {
                    if (isWritePackets)
                        _logger.LogInformationWColor(Brushes.DarkViolet, $"data = {data.GetArrayString("X2")}, channel = {channel}");
                };

                _mapModule.Channel = channel;
                _mapModule.OpenChannel(StringConnectionHelper.GetComConnectionString(selectedDeviceConfiguration.ConnectionString));

            }
            catch (Exception e)
            {
                _mapModule.Channel = null;

                _dispatcher.InvokeAsync(() =>
                {
                    this.RaisePropertyChanged(nameof(IsConnected));
                    this.RaisePropertyChanged(nameof(ModuleIsBusy));
                    this.RaisePropertyChanged(nameof(IsDTR));
                });
            }
        }

        /// <summary>
        /// Разрывает соединение с прибором
        /// </summary>
        public void Disconnect()
        {
            _mapModule.CloseChannel();
            _mapModule.Channel = null;

            _dispatcher.InvokeAsync(() =>
            {
                this.RaisePropertyChanged(nameof(IsConnected));
                this.RaisePropertyChanged(nameof(ModuleIsBusy));
            });
        }
    }

}
