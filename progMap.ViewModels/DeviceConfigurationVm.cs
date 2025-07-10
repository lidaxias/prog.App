using progMap.Core;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;

namespace progMap.ViewModels
{
    /// <summary>
    /// ViewModel для конфигурации устройства
    /// </summary>
    public class DeviceConfigurationVm : ViewModelBase
    {
        private readonly DeviceConfiguration? _deviceConfiguration;
        private ObservableCollection<SectionConfigurationBaseVm> _allSections;
        private MemoryTypeVm[] _memoryTypes;
        private List<EncodingInfo> _encodes;
        private EncodingInfo _selectedEncode;
        private ChannelTypeVm[] _channelTypes;

        // Список всех доступных кодировок системы
        public List<EncodingInfo> allEncodings = Encoding.GetEncodings().ToList();

        /// <summary>
        /// Название канала 
        /// </summary>
        public string ChannelName
        {
            get => _deviceConfiguration.ChannelType;
            set => _deviceConfiguration.ChannelType = value;
        }

        /// <summary>
        /// Имя устройства
        /// </summary>
        public string Name
        {
            get => _deviceConfiguration.Name;
            set => _deviceConfiguration.Name = value;
        }

        /// <summary>
        /// Строка подключения к устройству
        /// </summary>
        public string ConnectionString
        {
            get => _deviceConfiguration.ConnectionString;
            set => _deviceConfiguration.ConnectionString = value;
        }

        /// <summary>
        /// Доступные типы памяти устройства
        /// </summary>
        public MemoryTypeVm[] MemoryTypes
        {
            get => _memoryTypes;
            set => this.RaiseAndSetIfChanged(ref _memoryTypes, value);
        }

        /// <summary>
        /// Выбранный тип памяти устройства
        /// </summary>
        public MemoryTypeVm MemoryType
        {
            get => MemoryTypes.FirstOrDefault(memType => memType.Name == _deviceConfiguration.MemoryType);
            set => _deviceConfiguration.MemoryType = value.Name;
        }

        /// <summary>
        /// Доступные типы каналов 
        /// </summary>
        public static ChannelTypeVm[] ChannelTypes { get; set; }

        /// <summary>
        /// Выбранный тип канала 
        /// </summary>
        public ChannelTypeVm ChannelType
        {
            get => ChannelTypes.FirstOrDefault(channel => channel.Name == _deviceConfiguration.ChannelType);
            set => _deviceConfiguration.ChannelType = value.Name;
        }

        /// <summary>
        /// Таймаут стирания памяти (мс)
        /// </summary>
        public string EraseTimeOut
        {
            get => _deviceConfiguration.EraseTimeOut;
            set => _deviceConfiguration.EraseTimeOut = value;
        }

        /// <summary>
        /// Таймаут чтения (мс)
        /// </summary>
        public string ReadTimeOut
        {
            get => _deviceConfiguration.ReadTimeOut;
            set => _deviceConfiguration.ReadTimeOut = value;
        }

        /// <summary>
        /// Таймаут записи (мс)
        /// </summary>
        public string WriteTimeOut
        {
            get => _deviceConfiguration.WriteTimeOut;
            set => _deviceConfiguration.WriteTimeOut = value;
        }

        /// <summary>
        /// Виртуальный адрес для стирания
        /// </summary>
        public string EraseVirtualAddr
        {
            get => _deviceConfiguration.EraseVirtualAddr;
            set => _deviceConfiguration.EraseVirtualAddr = value;
        }

        /// <summary>
        /// Физический адрес для стирания
        /// </summary>
        public string ErasePhysicalAddr
        {
            get => _deviceConfiguration.ErasePhysicalAddr;
            set => _deviceConfiguration.ErasePhysicalAddr = value;
        }

        /// <summary>
        /// Кодировка для работы с устройством
        /// </summary>
        public string Encode
        {
            get => _deviceConfiguration.Encode;
            set => _deviceConfiguration.Encode = value;
        }

        /// <summary>
        /// Выбранная кодировка из списка доступных
        /// </summary>
        public EncodingInfo SelectedEncode
        {
            get => Encodes.FirstOrDefault(e => e.Name == Encode);
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedEncode, value);
                Encode = value.Name;
            }
        }

        /// <summary>
        /// Список доступных кодировок
        /// </summary>
        public List<EncodingInfo> Encodes
        {
            get => _encodes;
            set => this.RaiseAndSetIfChanged(ref _encodes, value);
        }

        /// <summary>
        /// Файловые секции конфигурации
        /// </summary>
        public FileSectionConfigurationVm[]? FileSections => AllSections.OfType<FileSectionConfigurationVm>().ToArray();

        /// <summary>
        /// Секции памяти конфигурации
        /// </summary>
        public SectionConfigurationBaseVm[]? MemorySections => AllSections.OfType<MemorySectionConfigurationVm>().ToArray();

        /// <summary>
        /// Все секции конфигурации
        /// </summary>
        public ObservableCollection<SectionConfigurationBaseVm> AllSections
        {
            get => _allSections;
            set => _allSections = value;
        }

        /// <summary>
        /// Команда добавления новой секции памяти
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddMemorySectionCommand { get; }

        /// <summary>
        /// Команда добавления новой файловой секции
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddFileSectionCommand { get; }

        /// <summary>
        /// Команда перемещения секции вниз в списке
        /// </summary>
        public ReactiveCommand<SectionConfigurationBaseVm, Unit> MoveSectionDownCommand { get; }

        /// <summary>
        /// Команда перемещения секции вверх в списке
        /// </summary>
        public ReactiveCommand<SectionConfigurationBaseVm, Unit> MoveSectionUpCommand { get; }

        /// <summary>
        /// Команда удаления секции памяти
        /// </summary>
        public ReactiveCommand<SectionConfigurationBaseVm, Unit> RemoveMemorySectionCommand { get; }

        public DeviceConfigurationVm(DeviceConfiguration deviceConfiguration)
        {
            // инициализация списков доступных значений
            Encodes = allEncodings;
            MemoryTypes = MemoryTypeVm.All.ToArray();

            // инициализация команд
            AddMemorySectionCommand = ReactiveCommand.Create(() =>
                AllSections.Add(new MemorySectionConfigurationVm(new MemorySectionConfiguration("new section", 0, 0, false))));

            AddFileSectionCommand = ReactiveCommand.Create(() =>
                AllSections.Add(new FileSectionConfigurationVm(new FileSectionConfiguration("new fileSection", 0, 0, true, false))));

            RemoveMemorySectionCommand = ReactiveCommand.Create<SectionConfigurationBaseVm>(d =>
            {
                if (d is FileSectionConfigurationVm fc)
                    AllSections.Remove(fc);

                if (d is MemorySectionConfigurationVm mc)
                    AllSections.Remove(mc);
            });

            MoveSectionDownCommand = ReactiveCommand.Create<SectionConfigurationBaseVm>(sct =>
            {
                if (!(sct is SectionConfigurationBaseVm sctb))
                    throw new ArgumentException();

                if (AllSections.Count < 2)
                    return;
                var index = AllSections.IndexOf(sctb);

                if (index < 0 || index > AllSections.Count - 2)
                    return;

                AllSections.Remove(sctb);
                AllSections.Insert(index + 1, sctb);
                this.RaisePropertyChanged(nameof(AllSections));
            });

            MoveSectionUpCommand = ReactiveCommand.Create<SectionConfigurationBaseVm>(cfg =>
            {
                if (!(cfg is SectionConfigurationBaseVm sctb))
                    throw new ArgumentException();

                if (AllSections.Count < 2)
                    return;
                var index = AllSections.IndexOf(sctb);

                if (index < 1)
                    return;

                AllSections.Remove(sctb);
                AllSections.Insert(index - 1, sctb);
                this.RaisePropertyChanged(nameof(AllSections));
            });

            _deviceConfiguration = deviceConfiguration;

            // инициализация коллекции секций из модели
            AllSections = new ObservableCollection<SectionConfigurationBaseVm>(_deviceConfiguration.Sections.Select(m =>
            {
                if (m is MemorySectionConfiguration mc)
                    return new MemorySectionConfigurationVm(mc) as SectionConfigurationBaseVm;

                if (m is FileSectionConfiguration fc)
                    return new FileSectionConfigurationVm(fc) as SectionConfigurationBaseVm;

                throw new ArgumentException();
            }));
        }

        /// <summary>
        /// Создает ViewModel из модели устройства
        /// </summary>
        /// <param name="deviceConfiguration">Модель конфигурации устройства</param>
        /// <returns>Новый экземпляр ViewModel</returns>
        public static DeviceConfigurationVm FromModel(DeviceConfiguration deviceConfiguration) => new(deviceConfiguration);

        /// <summary>
        /// Получает модель устройства из ViewModel
        /// </summary>
        /// <returns>Модель конфигурации устройства</returns>
        public DeviceConfiguration GetModel()
        {
            _deviceConfiguration.Sections = AllSections.Select(s => s.GetModel()).ToList();
            return _deviceConfiguration;
        }

        public override string ToString() => Name;
    }
}