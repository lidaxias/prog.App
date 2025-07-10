using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using progMap.Core;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace progMap.ViewModels
{
    /// <summary>
    /// ViewModel для конфигурации файловой секции
    /// </summary>
    public class FileSectionConfigurationVm : SectionConfigurationBaseVm
    {
        private string _fileNameString;
        private string _selectedFormat;

        /// <summary>
        /// Список доступных форматов файлов
        /// </summary>
        public List<string> Formats { get; set; } = new List<string>
        {
            "с заголовком", "без заголовка"
        };

        /// <summary>
        /// Выбранный формат файла из списка Formats
        /// </summary>
        public string SelectedFormat
        {
            get => Formats.FirstOrDefault(e => e == FileFormat);
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedFormat, value);
                FileFormat = value;
            }
        }

        /// <summary>
        /// Модель файловой секции конфигурации
        /// </summary>
        public FileSectionConfiguration FileSection
        {
            get => Model as FileSectionConfiguration;
            set => Model = value;
        }

        /// <summary>
        /// Расширения загружаемых файлов
        /// </summary>
        public string Extensions
        {
            get => FileSection.Extensions;
            set => FileSection.Extensions = value;
        }

        /// <summary>
        /// Порядок байтов в заголовке файла (big-endian/little-endian)
        /// </summary>
        public bool BigEndianHeader
        {
            get => FileSection.BigEndianHeader;
            set => FileSection.BigEndianHeader = value;
        }

        /// <summary>
        /// Строковое представление имени файла
        /// </summary>
        public string FileNameString
        {
            get => _fileNameString;
            set => this.RaiseAndSetIfChanged(ref _fileNameString, value);
        }

        /// <summary>
        /// Формат работы с файлом
        /// </summary>
        public string FileFormat
        {
            get => FileSection.FileFormat;
            set
            {
                FileSection.FileFormat = value;
            }
        }

        /// <summary>
        /// Команда для открытия диалога выбора файла
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }

        /// <summary>
        /// Открывает диалог и выбирает файл
        /// </summary>
        public async void OpenFile()
        {
            var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();

            OpenFileDialog ofd = new();

            ofd.Filters?.Add(new FileDialogFilter()
            {
                Name = "Файлы прошивок (*.bin; *.rbf; *.elf; *.rbin; .*rrbf; *.hex)",
                Extensions = new List<string>(new string[] { "bin", "rbf", "elf", "rbin", "rrbf", "hex" })
            });
            ofd.Filters?.Add(new FileDialogFilter()
            {
                Name = "Все файлы (*.*)",
                Extensions = new List<string>(new string[] { "*" })
            });
            ofd.AllowMultiple = false;

            var fileNames = await ofd.ShowAsync(currentWindow);
            if (fileNames == null || fileNames.Length == 0)
                return;

            FileNameString = fileNames[0];
        }

        public FileSectionConfigurationVm(FileSectionConfiguration sectionFileConfiguration) : base(sectionFileConfiguration)
        {
            OpenFileCommand = ReactiveCommand.Create(OpenFile);
        }

        /// <summary>
        /// Создает ViewModel из модели
        /// </summary>
        public static FileSectionConfigurationVm FromModel(FileSectionConfiguration sectionFileConfiguration) => new(sectionFileConfiguration);

        /// <summary>
        /// Получает имя фильтра для диалога из строки расширений
        /// </summary>
        public string GetNameDialogFilter()
        {
            string result = new(Extensions.TakeWhile(n => n != '|').ToArray());
            return result.Trim();
        }

        /// <summary>
        /// Парсинг строки расширений в список
        /// </summary>
        public List<string> ParseExtensions()
        {
            string ext = new(Extensions.TakeWhile(n => n != '|').ToArray());

            string ext2 = Extensions.Substring(ext.Length, Extensions.Length - ext.Length).Trim('|');

            string[] extensions = ext2.Split(';');
            List<string> resultExtensions = new List<string>();

            foreach (string extension in extensions)
            {
                if (extension.Trim(' ') == "*.*")
                    resultExtensions.Add("*");
                else
                    resultExtensions.Add(extension.Trim(new Char[] { ' ', '*', '.', '|' }));
            }

            return resultExtensions.ToList();
        }
    }
}