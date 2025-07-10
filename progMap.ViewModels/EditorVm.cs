using progMap.Core;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace progMap.ViewModels
{
    /// <summary>
    /// ViewModel для редактора конфигураций устройств
    /// </summary>
    public class EditorVm : ViewModelBase
    {
        // Коллекция всех конфигураций устройств
        private ObservableCollection<DeviceConfigurationVm> _allConfigurations;

        // Коллекция типов каналов
        private ObservableCollection<ChannelTypeVm> _channelTypes;

        // Список названий типов каналов (для отображения в UI)
        private ObservableCollection<string> _channelTypesList = new ObservableCollection<string>();

        /// <summary>
        /// Коллекция всех конфигураций устройств
        /// </summary>
        public ObservableCollection<DeviceConfigurationVm> AllConfigurations
        {
            get => _allConfigurations;
            set => _allConfigurations = value;
        }


        /// <summary>
        ///  Список редактируемых каналов
        /// </summary>
        public ObservableCollection<ChannelTypeVm> ChannelTypesEd
        {
            get => _channelTypes;
            set
            {
                _channelTypes = value;
                UpdateChannelTypesList();
                this.RaisePropertyChanged(nameof(ChannelTypesEd));
            }
        }

        /// <summary>
        /// Список названий  каналов
        /// </summary>
        public ObservableCollection<string> ChannelTypesList
        {
            get => _channelTypesList;
            set
            {
                _channelTypesList = value;
                this.RaisePropertyChanged(nameof(ChannelTypesList));
            }
        }

        /// <summary>
        /// Обновляет список названий типов каналов на основе коллекции ChannelTypesEd
        /// </summary>
        private void UpdateChannelTypesList()
        {
            ChannelTypesList = new ObservableCollection<string>(
                ChannelTypesEd?.Select(ct => ct.Name) ?? Enumerable.Empty<string>()
            );
        }

        #region Commands

        /// <summary>
        /// Команда для добавления новой конфигурации устройства
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddConfigurationCommand { get; }

        /// <summary>
        /// Команда для перемещения конфигурации вниз в списке
        /// </summary>
        public ReactiveCommand<DeviceConfigurationVm, Unit> MoveConfigDownCommand { get; }

        /// <summary>
        /// Команда для перемещения конфигурации вверх в списке
        /// </summary>
        public ReactiveCommand<DeviceConfigurationVm, Unit> MoveConfigUpCommand { get; }

        /// <summary>
        /// Команда для удаления конфигурации
        /// </summary>
        public ReactiveCommand<DeviceConfigurationVm, Unit> RemoveConfigurationCommand { get; }
        #endregion

        /// <summary>
        /// Тестовые секции конфигурации, используемые при создании новой конфигурации
        /// </summary>
        public static List<SectionConfigurationBase> TestSectionsConfiguration => new()
        {
            new MemorySectionConfiguration("new section", 0, 0, false),
            new MemorySectionConfiguration("new section", 0, 0, false),
            new FileSectionConfiguration("new fileSection", 0, 0, true, false),
            new FileSectionConfiguration("new fileSection", 0, 0, true, false)
        };


        public EditorVm()
        {
            // команда добавления новой конфигурации с тестовыми параметрами
            AddConfigurationCommand = ReactiveCommand.Create(() =>
                AllConfigurations.Add(new DeviceConfigurationVm(
                    new DeviceConfiguration("new config", "UDP", TestSectionsConfiguration,
                    "32001, 192.168.1.1:32000", "0", "0", "0", "0x00001000", "0x00001000", "1251"))));

            RemoveConfigurationCommand = ReactiveCommand.Create<DeviceConfigurationVm>(d => AllConfigurations.Remove(d));
            MoveConfigDownCommand = ReactiveCommand.Create<DeviceConfigurationVm>(cfg =>
            {
                if (!(cfg is DeviceConfigurationVm dc))
                    throw new ArgumentException();

                if (AllConfigurations.Count < 2)
                    return;
                var index = AllConfigurations.IndexOf(dc);

                // проверка, что элемент не последний
                if (index < 0 || index > AllConfigurations.Count - 2)
                    return;

                // перемещение элемента на одну позицию вниз
                AllConfigurations.Remove(dc);
                AllConfigurations.Insert(index + 1, dc);
                this.RaisePropertyChanged(nameof(AllConfigurations));
            });

            MoveConfigUpCommand = ReactiveCommand.Create<DeviceConfigurationVm>(cfg =>
            {
                if (!(cfg is DeviceConfigurationVm dc))
                    throw new ArgumentException();

                if (AllConfigurations.Count < 2)
                    return;
                var index = AllConfigurations.IndexOf(dc);

                // проверка, что элемент не первый
                if (index < 1)
                    return;

                // перемещение элемента на одну позицию вверх
                AllConfigurations.Remove(dc);
                AllConfigurations.Insert(index - 1, dc);
                this.RaisePropertyChanged(nameof(AllConfigurations));
            });
        }
    }
}