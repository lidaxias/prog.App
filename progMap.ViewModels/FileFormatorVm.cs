using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Controls;
using ReactiveUI;
using Rss.TmFramework.Base.Helpers;
using System.Reactive;
using Avalonia;
using Rss.TmFramework.Base.Logging;

namespace progMap.ViewModels
{
    /// <summary>
    /// ViewModel для формирования образа файла
    /// </summary>
    public class FileFormatorVm : ViewModelBase
    {
        private readonly ILogger Logger;
        private List<FileSectionConfigurationVm> _allFileSectionConfigurationsVm;
        private List<FileSectionConfigurationVm> _includedFilesSectionConfigurationsVm;
        private SaveFileDialog _saveFileDialog;
        private string _startAddressString;
        private string _sizeString;

        #region prop
        /// <summary>
        /// Список всех файловых секций для выбранной конфигурации
        /// </summary>
        public List<FileSectionConfigurationVm> AllFileSectionConfigurationsVm
        {
            get => _allFileSectionConfigurationsVm;
            set => _allFileSectionConfigurationsVm = value;
        }

        /// <summary>
        /// Список файловых секций, включенных в область памяти
        /// </summary>
        public List<FileSectionConfigurationVm> IncludedFilesSectionConfigurationsVm
        {
            get => _includedFilesSectionConfigurationsVm;
            set => this.RaiseAndSetIfChanged(ref _includedFilesSectionConfigurationsVm, value);
        }

        /// <summary>
        /// Стартовый адрес заданной карты памяти
        /// </summary>
        public string StartAddressString
        {
            get => _startAddressString;
            set
            {
                this.RaiseAndSetIfChanged(ref _startAddressString, value);
                IncludedFilesSectionConfigurationsVm = GetSections(StartAddressString, SizeString);
            }
        }

        /// <summary>
        /// Размер заданной карты памяти в виде строки
        /// </summary>
        public string SizeString
        {
            get => _sizeString;
            set
            {
                this.RaiseAndSetIfChanged(ref _sizeString, value);
                IncludedFilesSectionConfigurationsVm = GetSections(StartAddressString, SizeString);
            }
        }

        #endregion

        /// <summary>
        /// Команда для генерации образа файла
        /// </summary>
        public ReactiveCommand<Unit, Unit> GenerateFileCommand { get; }

        /// <summary>
        /// Инициализирует диалоги
        /// </summary>
        private void InitFileDialogs()
        {
            _saveFileDialog = new SaveFileDialog();
            _saveFileDialog.Filters?.Add(new FileDialogFilter() { Name = "Файлы прошивок (*.bin; *.rbf; *.elf; *.rbin; .*rrbf; *.hex) ", Extensions = new List<string>(new string[] { "bin", "rbf", "elf", "rbin", "rrbf", "hex" }) });
            _saveFileDialog.Filters?.Add(new FileDialogFilter() { Name = "Все файлы (*.*)", Extensions = new List<string>(new string[] { "*" }) });
        }

        /// <summary>
        /// Генерирует образ файла
        /// </summary>
        public async void GenerateFile()
        {
            var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();

            if ((StartAddressString == null) || (SizeString == null))
            {
                Logger.LogInformationWColor(Brushes.Red, $"Введите область карты памяти для формирования образа файла!");
                return;
            }

            var fileName = await _saveFileDialog.ShowAsync(currentWindow);

            if (fileName == null || fileName.Length == 0)
                return;

            // парсинг введенных пользователем данных
            if (!StringHelpers.TryParseUint(StartAddressString, out var startAddress))
            {
                return;
            }

            if (!StringHelpers.TryParseUint(SizeString, out var size))
            {
                return;
            }

            byte[] mem = new byte[size];
            for (var i = 0; i < mem.Length; i++)
                mem[i] = 0xFF;

            foreach (var section in IncludedFilesSectionConfigurationsVm)
            {
                var offset = section.Address - startAddress;

                if (section.FileNameString == null) continue;

                if (!File.Exists(section.FileNameString))
                {
                    Logger.LogInformationWColor(Brushes.Red, $"Образ файла не был сформирован. Не найден файл по пути: {section.FileNameString}");
                    return;
                }

                byte[] fileBytes = File.ReadAllBytes(section.FileNameString);

                if (fileBytes.Length > section.Size)
                {
                    Logger.LogInformationWColor(Brushes.Red, $"Образ файла не был сформирован. Размер файла {fileBytes.Length} байт превышает размер секции {section.Name} ({section.Size} байт)");
                    return;
                }

                if (fileBytes.Length <= mem.Length)

                    try
                    {
                        Array.Copy(fileBytes, 0, mem, offset, fileBytes.Length);
                    }

                    catch (Exception ex)
                    {
                        Logger.LogInformationWColor(Brushes.Red, $"Образ файла не был сформирован в связи с {ex}");
                        return;
                    }
            }

            File.WriteAllBytes(fileName, mem);
            Logger.LogInformationWColor(Brushes.Black, $"Образ файла сформирован.");

        }

        /// <summary>
        /// Возвращает список файловых секций, входящих в область памяти, введенную пользователем
        /// </summary>
        public List<FileSectionConfigurationVm> GetSections(string startAddressString, string sizeString)
        {
            if ((startAddressString == null) || (sizeString == null)) return null;


            // парсинг введенных пользователем данных
            if (!StringHelpers.TryParseUint(startAddressString, out var startAddress))
            {
                return null;
            }

            if (!StringHelpers.TryParseUint(sizeString, out var size))
            {
                return null;
            }

            // адрес конца карты памяти
            var endAddress = startAddress + size;

            List<FileSectionConfigurationVm> sections = new();

            foreach (var section in AllFileSectionConfigurationsVm)
            {
                // если секция попадает в карту памяти, то добавляем её в список
                if ((section.Address >= startAddress) && ((section.Address + section.Size) <= endAddress))
                    sections.Add(section);

                // если нет, то продолжаем проходить по циклу дальше
                else continue;
            }

            return sections;
        }

        public FileFormatorVm() { }
        public FileFormatorVm(ILogger logger)
        {
            Logger = logger;
            AllFileSectionConfigurationsVm = new List<FileSectionConfigurationVm>();
            GenerateFileCommand = ReactiveCommand.Create(GenerateFile);

            InitFileDialogs();
        }
    }
}

