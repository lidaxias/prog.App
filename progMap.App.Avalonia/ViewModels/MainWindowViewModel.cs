using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using progMap.App.Avalonia.Models;
using progMap.App.Avalonia.Plugins;
using progMap.App.Avalonia.Services;
using progMap.App.Avalonia.ViewModels.TestViewModels;
using progMap.Core;
using progMap.Interfaces;
using progMap.ViewModels;
using ReactiveUI;
using Rss.TmFramework.Modules;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using System.Text;
using System.Threading.Tasks;
using Brushes = Avalonia.Media.Brushes;
using ILogger = Rss.TmFramework.Base.Logging.ILogger;
using Logger = Rss.TmFramework.Logging.Logger;
using Path = System.IO.Path;
using Timer = System.Threading.Timer;

namespace progMap.App.Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Const
        /// <summary>
        /// Карта памяти ПК
        /// </summary>
        private const uint ModuleDebugLine = 0x0000_0000;

        /// <summary>
        /// Адрес на стирание в пульте (приборе)
        /// </summary>
        private const uint ModuleEraseAddr = 0x0000_0100;

        /// <summary>
        /// Адрес на контрольную сумму в пульте (приборе)
        /// </summary>
        private const uint ModuleCrcAddr = 0x0002_0000;

        /// <summary>
        /// Адрес на тип памяти в пульте (приборе)
        /// </summary>
        private const uint ModuleMemTypeAddr = 0x0000_0400;

        /// <summary>
        /// Адрес, на который приходит текущая надпись для progressbar от прибора
        /// </summary>
        private const uint DescriptionProgressBarAddr = 0x0000_0004;

        /// <summary>
        /// Адрес, на который приходит текущее значение для progressbar от прибора
        /// </summary>
        private const uint CurValProgressBarAddr = 0x0000_0008;

        /// <summary>
        /// Адрес, на который приходит максимальное значение для progressbar о прибора
        /// </summary>
        private const uint MaxValProgressBarAddr = 0x0000_000C;

        #endregion

        #region Private fields

        private readonly Dispatcher _dispatcher;
        private readonly Timer _timer;

        private StreamWriter _sw;
        private string _terminalFilePath;

        // текущие дата и время
        private static readonly string DateTimeNowString = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // имя файла для сохранения данных из журнала
        private readonly string FileNameTerminal = DateTimeNowString + " Терминал" + ".txt";

        private LoggerPluginViewModel _loggerPluginVm;

        private ChannelTypeVm[] _channelTypesVm;
        private ChannelTypeVm _selectedChannelTypeVm;

        private static DeviceConfigurationVm[] _deviceConfigurationsVm;
        private static DeviceConfigurationVm? _selectedDeviceConfigurationVm;
        private List<DeviceConfigurationVm> _devicesVm;
        private List<DeviceConfigurationVm> _devicesVmCopy;
        private List<DeviceConfiguration> _devices;

        private readonly MapEthernetModule _mapModule = new();
        private static MemoryTypeVm _memoryTypeInstall = null;

        // флаги
        private static bool _isRunning = false;
        private bool _isWritePackets = false;

        // диалоги
        private SaveFileDialog _saveWriteSectionFile;
        private UserFileDialog _openWriteSectionFile;

        private readonly EditorVm _editorVm;
        private readonly FileFormatorVm _fileFormatorVm;
        private TerminalPluginViewModel _terminalVm;
        private TestViewModel _testVm;
        private readonly MemoryAccess _memoryAccess;
        private readonly MemoryManager _memoryManager;

        // имя файла по умолчанию для сериализации/десериализации
        private readonly string DeviceConfigFileName = "program.xml";

        // сервисы
        private readonly MemoryOperationService _memoryOperationService;
        private ConnectionService _connectionService;
        private readonly WindowService _windowService;
        private readonly ConfigEditorService _configEditorService;
        private readonly FileOperationService _fileOperationService;

        #endregion

        #region Commands

        /// <summary>
        /// Команда на остановку пользователем текущей операции с файлом
        /// </summary>
        public ReactiveCommand<Unit, Unit> StopCommand { get; }

        /// <summary>
        /// Команда на подключение/отключение канала
        /// </summary>
        public ReactiveCommand<ChannelTypeVm, Unit> ConnectDisconnectCommand { get; }

        /// <summary>
        /// Команда на запись данных
        /// </summary>
        public ReactiveCommand<SectionConfigurationBaseVm, Unit> WriteMemorySectionCommand { get; }

        /// <summary>
        /// Команда на чтение данных
        /// </summary>
        public ReactiveCommand<SectionConfigurationBaseVm, Unit> ReadMemorySectionCommand { get; }

        /// <summary>
        /// Команда на чтение информации о файле текущей конкретной секции
        /// </summary>
        public ReactiveCommand<FileSectionConfigurationVm, Unit> ReadFileSectionInfoCommand { get; }

        /// <summary>
        /// Команда на стирание данных секции
        /// </summary>
        public ReactiveCommand<SectionConfigurationBaseVm, Unit> EraseSectionCommand { get; }

        /// <summary>
        /// Команда на сравнение файлов
        /// </summary>
        public ReactiveCommand<FileSectionConfigurationVm, Unit> CompareFileSectionCommand { get; }

        /// <summary>
        /// Команда на загрузку MAP - файла
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenMapFileCommand { get; }

        /// <summary>
        /// Команда на загрузку строки данных из бинарного/текстового файла
        /// </summary>
        public ReactiveCommand<MemorySectionConfigurationVm, Unit> LoadMemorySectionCommand { get; }

        /// <summary>
        /// Команда обновления типа памяти
        /// </summary>
        public ReactiveCommand<Unit, bool> UpdateMemoryTypeCommand { get; }

        /// <summary>
        /// Команда установки типа памяти
        /// </summary>
        public ReactiveCommand<Unit, Unit> SetMemoryTypeCommand { get; }

        /// <summary>
        /// Команда на редактирование конфигурации приборов
        /// </summary>
        public ReactiveCommand<Unit, Task> EditConfigCommand { get; }

        /// <summary>
        /// Команда на загрузку конфигураций прибора из файла .xml
        /// </summary>
        public ReactiveCommand<Unit, Task> OpenFileConfigurationCommand { get; }

        /// <summary>
        /// Команда на открытие отдельного окна с терминалом
        /// </summary>
        public ReactiveCommand<Unit, Task> OpenTerminalWindowCommand { get; }

        /// <summary>
        /// Команда на открытие отдельного окна с тестами
        /// </summary>
        public ReactiveCommand<Unit, Task> OpenTestWindowCommand { get; }

        /// <summary>
        /// Команда на очистку журнала
        /// </summary>
        public ReactiveCommand<Unit, Unit> ClearLogCommand { get; }

        /// <summary>
        /// Команда на открытие окна для формирования образов файла
        /// </summary>
        public ReactiveCommand<Unit, Task> FileFormationCommand { get; }
        #endregion

        #region Properties

        /// <summary>
        /// Логгер
        /// </summary>
        public ILogger Logger { get; set; } = new Logger();
     
        /// <summary>
        /// Отображение progressBar
        /// </summary>
        public ProgressBarManagerViewModel ProgressBarViewModel { get; set; } = new();
        
        /// <summary>
        /// Список погруженных плагинов
        /// </summary>
        public ObservableCollection<IProgPlugin> ProgPlugins { get; set; } = new();

        /// <summary>
        /// Журнал в виде встроенного плагина
        /// </summary>
        public LoggerPluginViewModel LoggerPluginViewModel
        {
            get => _loggerPluginVm;
            set => this.RaiseAndSetIfChanged(ref _loggerPluginVm, value);
        }

        /// <summary>
        /// Сервис, отвечающий за установку соединения с приборами
        /// </summary>
        public ConnectionService ConnectionService
        {
            get => _connectionService;
            set => this.RaiseAndSetIfChanged(ref _connectionService, value);
        }

        /// <summary>
        /// Конфигурации прибора
        /// </summary>
        public DeviceConfigurationVm[] DeviceConfigurationsVm
        {
            get => _deviceConfigurationsVm;
            set => this.RaiseAndSetIfChanged(ref _deviceConfigurationsVm, value);
        }

        /// <summary>
        /// Текущая выбранная конфигурация прибора
        /// </summary>
        public DeviceConfigurationVm? SelectedDeviceConfigurationVm
        {
            get => _selectedDeviceConfigurationVm;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDeviceConfigurationVm, value);
                SelectedChannelType = ChannelTypes?.SingleOrDefault(ct => ct.Name == SelectedDeviceConfigurationVm?.ChannelName);

                this.RaisePropertyChanged(nameof(SelectedMemoryType));
                this.RaisePropertyChanged(nameof(SelectedMemoryTypeName));
            }
        }

        /// <summary>
        /// Типы каналов
        /// </summary>
        public ChannelTypeVm[] ChannelTypes
        {
            get => _channelTypesVm;
            set => this.RaiseAndSetIfChanged(ref _channelTypesVm, value);
        }

        /// <summary>
        /// Выбранный тип канала
        /// </summary>
        public ChannelTypeVm SelectedChannelType
        {
            get => _selectedChannelTypeVm;
            set => this.RaiseAndSetIfChanged(ref _selectedChannelTypeVm, value);
        }

        /// <summary>
        /// "Скопированные" конфигурации прибора для редактора конфигураций в случае отмены сохранения 
        /// </summary>
        /// 
        public List<DeviceConfigurationVm> DevicesVmCopy
        {
            get => _devicesVmCopy;
            set => this.RaiseAndSetIfChanged(ref _devicesVmCopy, value);
        }

        /// <summary>
        /// Конфигурации прибора для редактора конфигураций в случае сохранения 
        /// </summary>
        public List<DeviceConfigurationVm> DevicesVm
        {
            get => _devicesVm;
            set => this.RaiseAndSetIfChanged(ref _devicesVm, value);
        }

        /// <summary>
        /// Модели конфигураций прибора редактора конфигураций для последующей сериализации
        /// </summary>
        public List<DeviceConfiguration> Devices
        {
            get => _devices;
            set => this.RaiseAndSetIfChanged(ref _devices, value);
        }

        /// <summary>
        /// Флаг "выполняется операция с файлом"
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }

        /// <summary>
        /// Флаг "отписывать все пришедшие пакеты в журнал"
        /// </summary>
        public bool IsWritePackets
        {
            get => _isWritePackets;
            set => this.RaiseAndSetIfChanged(ref _isWritePackets, value);
        }

        /// <summary>
        /// Установленный тип памяти
        /// </summary>
        public MemoryTypeVm MemoryTypeInstall
        {
            get => _memoryTypeInstall;
            set => this.RaiseAndSetIfChanged(ref _memoryTypeInstall, value);
        }

        /// <summary>
        /// Выбранный тип памяти
        /// </summary>
        public MemoryTypeVm SelectedMemoryType => SelectedDeviceConfigurationVm != null ? SelectedDeviceConfigurationVm.MemoryType : MemoryTypeVm.Default;

        /// <summary>
        /// Имя выбранного типа памяти
        /// </summary>
        public string SelectedMemoryTypeName => SelectedDeviceConfigurationVm != null ? SelectedDeviceConfigurationVm.MemoryType.Name : MemoryTypeVm.Default.Name;

        /// <summary>
        /// Терминал 
        /// </summary>
        public TerminalPluginViewModel TerminalVm
        {
            get => _terminalVm;
            set => this.RaiseAndSetIfChanged(ref _terminalVm, value);
        }

        /// <summary>
        /// Циклограмма
        /// </summary>
        public TestViewModel TestVm
        {
            get => _testVm;
            set => this.RaiseAndSetIfChanged(ref _testVm, value);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Инициализирует диалоги
        /// </summary>
        private void InitFileDialogs()
        {
            _openWriteSectionFile = new UserFileDialog();
            _openWriteSectionFile.Filters?.Add(new FileDialogFilter() { Name = "Файлы прошивок (*.bin; *.rbf; *.elf; *.rbin; .*rrbf; *.hex) ", Extensions = new List<string>(new string[] { "bin", "rbf", "elf", "rbin", "rrbf", "hex" }) });
            _openWriteSectionFile.Filters?.Add(new FileDialogFilter() { Name = "Файлы прошивок Марафон (*.rbin; *.rrbf)", Extensions = new List<string>(new string[] { "rbin", "rrbf" }) });
            _openWriteSectionFile.Filters?.Add(new FileDialogFilter() { Name = "Файлы IntelHex (*.hex)", Extensions = new List<string>(new string[] { "hex" }) });
            _openWriteSectionFile.Filters?.Add(new FileDialogFilter() { Name = "Все файлы (*.*)", Extensions = new List<string>(new string[] { "*" }) });
            _openWriteSectionFile.AllowMultiple = false;
        }

        /// <summary>
        /// Инициализирует файл с данными терминала
        /// </summary>
        private void InitializeTerminalFile()
        {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var filesDir = Path.Combine(currentDir, "Files");

            if (!Directory.Exists(filesDir))
            {
                Directory.CreateDirectory(filesDir);
            }

            _terminalFilePath = Path.Combine(filesDir, FileNameTerminal);

            _sw = new StreamWriter(_terminalFilePath, append: false, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        /// <summary>
        /// Инициализирует расширения 
        /// </summary>
        /// <param name="section"></param>
        public void InitFileSectionExtensions(FileSectionConfigurationVm section)
        {
            _openWriteSectionFile = new UserFileDialog();
            _openWriteSectionFile.Filters?.Add(new FileDialogFilter() { Name = section.GetNameDialogFilter(), Extensions = section.ParseExtensions() });
            _openWriteSectionFile.AllowMultiple = false;

            _saveWriteSectionFile = new SaveFileDialog();
            _saveWriteSectionFile.Filters?.Add(new FileDialogFilter() { Name = section.GetNameDialogFilter(), Extensions = section.ParseExtensions() });
        }

        /// <summary>
        /// Обновление терминала
        /// </summary>
        public void UpdateTerminal()
        {
            List<string> tmp = new();

            lock (TerminalVm.TerminalMessages)
            {
                if (TerminalVm.TerminalMessages.Count == 0)
                    return;
                try
                {
                    tmp.Add(TerminalVm.TerminalMessages);
                    TerminalVm.TerminalMessages.Clear();
                }

                catch (Exception ex)
                {
                    return;
                }
            }

            TerminalVm.DebugMessages += string.Concat(tmp);
        }

        /// <summary>
        /// Открывает окно редактора конфигураций и поддерживает редактирование
        /// </summary>
        private async Task EditConfigAsync()
        {
            DevicesVmCopy = Devices.Select(dc => dc.Clone() as DeviceConfiguration)
                  .Select(DeviceConfigurationVm.FromModel).ToList();

            var updatedConfigs = await _configEditorService.EditConfigAsync(
                    DeviceConfigFileName,
                    Devices,
                    ChannelTypes,
                    DevicesVmCopy);

            // Обновляем конфигурации в MainWindowViewModel
            DeviceConfigurationsVm = updatedConfigs.ToArray();
            SelectedDeviceConfigurationVm = DeviceConfigurationsVm?.FirstOrDefault();
            Devices = DeviceConfigurationsVm.Select(dc => dc.GetModel() as DeviceConfiguration).ToList();
        }

        /// <summary>
        /// Устанавливает соединение с прибором
        /// </summary>
        /// <param name="channelType"></param>
        /// <returns></returns>
        private async Task ConnectDisconnectAsync(ChannelTypeVm channelType)
        {
            _connectionService.ConnectDisconnect
            (channelType, SelectedDeviceConfigurationVm, ProgPlugins, IsWritePackets);
        }

        /// <summary>
        /// Сериализует строку подключения
        /// </summary>
        public void SerializeConnectionString()
        {
            var curDir = AppDomain.CurrentDomain.BaseDirectory; // получаем текущую директорию

            var fullPathToDeviceConfigFile = Path.Combine(curDir, DeviceConfigFileName); // полный путь до файла с конфигурациями
            if (File.Exists(fullPathToDeviceConfigFile))
            {
                SelectedDeviceConfigurationVm = DeviceConfigurationsVm?.FirstOrDefault();
                Devices = DeviceConfigurationsVm.Select(dc => dc.GetModel() as DeviceConfiguration).ToList();
                Serializer<List<DeviceConfiguration>>.Serialize(Devices.ToList(), fullPathToDeviceConfigFile);
            }
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
        #endregion

        public MainWindowViewModel()
        {
            _dispatcher = Dispatcher.UIThread;
            ProgPlugins = PluginProvider.LoadItems(_dispatcher, _mapModule, out LoggerPluginViewModel _loggerVm, out TerminalPluginViewModel _terminalVm);

            TerminalVm = _terminalVm;
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMilliseconds(300);
            _timer = new Timer((e) =>
            {
                UpdateTerminal();
            }, null, startTimeSpan, periodTimeSpan);

            Logger = _loggerVm.Logger;
            TestVm = new TestViewModel(Logger);

            var currentDir = AppDomain.CurrentDomain.BaseDirectory; // получаем текущую директорию
            var fullPathToPluginsFile = Path.Combine(currentDir, "plugins.xml");
            var fullPathToDeviceConfigFile = Path.Combine(currentDir, DeviceConfigFileName); // полный путь до файла с конфигурациями

            // инициализация диалогов
            InitFileDialogs();

            // инициализация файла терминала
            InitializeTerminalFile();

            // регистрируем кодировки
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _mapModule.Logger = Logger;
            _memoryManager = new MemoryManager(Logger, MemoryTypeInstall, _mapModule, ProgressBarViewModel);
            _memoryAccess = new MemoryAccess(_mapModule, Logger);
            _memoryOperationService = new MemoryOperationService(_mapModule, Logger, _dispatcher, _memoryManager);
            _connectionService = new ConnectionService(_mapModule, Logger, _dispatcher);
            _windowService = new WindowService(Logger);
            _configEditorService = new ConfigEditorService(_connectionService, _windowService);
            _fileOperationService = new();


            _mapModule.WriteReplyReceived += (module, packet) =>
            {
                switch (packet.Address)
                {
                    case ModuleEraseAddr:
                        break;
                }
            };

            _mapModule.WriteRequestReceived += (module, packet) =>
            {
                _dispatcher.InvokeAsync(() =>
                {
                    switch (packet.Address)
                    {
                        case ModuleDebugLine:
                            lock (TerminalVm.TerminalMessages)
                            {
                                TerminalVm.TerminalMessages.Add(Encoding.GetEncoding(SelectedDeviceConfigurationVm.Encode).GetString(packet.Data).Replace("\n", Environment.NewLine));
                            }

                            // отписываем новое сообщение в файл для хранения данных терминала
                            if (_sw != null)
                            {
                                _sw.Write(Encoding.GetEncoding(SelectedDeviceConfigurationVm.Encode).GetString(packet.Data).Replace("\n", Environment.NewLine));
                                _sw.Flush();
                            }

                            break;
                        case DescriptionProgressBarAddr:
                            if (packet.Data != null)
                                ProgressBarViewModel.OperationName = Encoding.GetEncoding(SelectedDeviceConfigurationVm.Encode).GetString(packet.Data);
                            break;
                        case CurValProgressBarAddr:
                            if (packet.Data != null)
                                ProgressBarViewModel.ProgressValue = BitConverter.ToInt32(packet.Data);
                            break;
                        case MaxValProgressBarAddr:
                            if (packet.Data != null)
                                ProgressBarViewModel.ProgressMaximum = BitConverter.ToInt32(packet.Data);
                            break;
                    }
                });
            };

            // подгружаем типы каналов из конфигурации
            try
            {
                ChannelTypes = PluginProvider.LoadChannels(fullPathToPluginsFile, _dispatcher).ToArray();
                DeviceConfigurationVm.ChannelTypes = ChannelTypes;
            }

            catch (System.NullReferenceException)
            {
                _dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(100);

                    await MessageBoxManager.MsgBoxOk("Не удалось загрузить из конфигурации типы каналов!");
                });
            }

            catch (Exception ex)
            {
                _dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(100);

                    await MessageBoxManager.MsgBoxOk(ex.Message.ToString());
                });
            }

            SelectedChannelType = ChannelTypes?.FirstOrDefault();

            // сериализация/десериализация данных
            if (Design.IsDesignMode)
            {
                Debug.WriteLine("DESIGN MODE!!!");
                DeviceConfigurationsVm = DeviceConfiguration.TestData.Select(DeviceConfigurationVm.FromModel).ToArray();
            }
            else
            {
#if DEBUG
                //Serializer<List<DeviceConfiguration>>.Serialize(DeviceConfiguration.TestData.ToList(), DeviceConfigFileName);
#endif
                if (File.Exists(fullPathToDeviceConfigFile))
                {
                    var models = Serializer<List<DeviceConfiguration>>.Deserialize(fullPathToDeviceConfigFile);

                    DeviceConfigurationsVm = models.Select(dc => new DeviceConfigurationVm(dc)).ToArray();
                    Devices = DeviceConfigurationsVm.Select(dc => dc.GetModel() as DeviceConfiguration).ToList();
                    DeviceConfiguration.Default = DeviceConfigurationsVm.Select(dc => dc.GetModel() as DeviceConfiguration).ToList();


                    foreach (var deviceConfig in DeviceConfigurationsVm)
                    {
                        if (deviceConfig.MemoryType == null)
                            deviceConfig.MemoryType = MemoryTypeVm.Default;
                    }
                }

                else
                {
                    _dispatcher.InvokeAsync(async () =>
                    {
                        await MessageBoxManager.MsgBoxOk($"Файл конфигурации не найден{Environment.NewLine}{fullPathToDeviceConfigFile}!");
                    });
                }
            }

            SelectedDeviceConfigurationVm = DeviceConfigurationsVm?.FirstOrDefault();

            // инициализация команд
            ConnectDisconnectCommand = ReactiveCommand.Create<ChannelTypeVm>(type => ConnectDisconnectAsync(type));

            EditConfigCommand = ReactiveCommand.Create(EditConfigAsync);

            EraseSectionCommand = ReactiveCommand.Create<SectionConfigurationBaseVm>(section => _memoryOperationService.EraseSection
            (section, SelectedDeviceConfigurationVm), _connectionService.ModuleIsConnected);

            WriteMemorySectionCommand = ReactiveCommand.Create<SectionConfigurationBaseVm>
                (section => _memoryOperationService.WriteSection(section, SelectedDeviceConfigurationVm), _connectionService.ModuleIsConnected);

            CompareFileSectionCommand = ReactiveCommand.Create<FileSectionConfigurationVm>
                (section => _memoryManager.CompareFileAsync(section, SelectedDeviceConfigurationVm, _dispatcher), _connectionService.ModuleIsConnected);

            ReadMemorySectionCommand = ReactiveCommand.Create<SectionConfigurationBaseVm>
                (section => _memoryOperationService.ReadSection(section, SelectedDeviceConfigurationVm), _connectionService.ModuleIsConnected);

            ReadFileSectionInfoCommand = ReactiveCommand.CreateFromTask<FileSectionConfigurationVm>
                (section => _memoryOperationService.ReadFileSectionInfoAsync(section, SelectedDeviceConfigurationVm), _connectionService.ModuleIsConnected);

            OpenFileConfigurationCommand = ReactiveCommand.Create(() => _windowService.OpenFileConfiguration(SelectedDeviceConfigurationVm, DeviceConfigurationsVm.ToList(), Devices));

            LoadMemorySectionCommand = ReactiveCommand.Create<MemorySectionConfigurationVm>(section => _fileOperationService.LoadDataFromFileAsync(section, Logger));

            UpdateMemoryTypeCommand = ReactiveCommand.Create(() => _memoryManager.UpdateMemoryType(MemoryTypeInstall), _connectionService.ModuleIsConnected);

            SetMemoryTypeCommand = ReactiveCommand.Create(() => _memoryManager.SetMemoryType(MemoryTypeInstall, SelectedMemoryType), _connectionService.ModuleIsConnected);

            OpenTerminalWindowCommand = ReactiveCommand.Create(() => _windowService.OpenTerminalWindow(TerminalVm));

            OpenTestWindowCommand = ReactiveCommand.Create(() => _windowService.OpenTestWindow(TestVm));

            StopCommand = ReactiveCommand.Create(Stop);

            FileFormationCommand = ReactiveCommand.Create(() => _windowService.OpenFileFormationWindow(_fileFormatorVm, SelectedDeviceConfigurationVm));

            Logger.LogInformationWColor(Brushes.Black, "Starting..");

        }
    
    }
}
