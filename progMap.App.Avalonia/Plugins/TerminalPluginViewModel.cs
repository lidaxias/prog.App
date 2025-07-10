using ReactiveUI;
using Rss.TmFramework.Modules;
using Rss.TmFramework.Mvvm;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Reactive;
using progMap.Interfaces;

namespace progMap.App.Avalonia.Plugins
{
    /// <summary>
    /// ViewModel для плагина терминала, реализующая интерфейс IProgPlugin.
    /// Обеспечивает сбор, отображение и сохранение сообщений терминала.
    /// </summary>
    public class TerminalPluginViewModel : ViewModelBase, IProgPlugin
    {
        public string Title => "Терминал";

        public MapEthernetModule MapModule { get; set; }

        private string _debugMessages;
        private List<string> _terminalMessages = new List<string>();
        private SaveFileDialog _saveFileTxt;

        /// <summary>
        /// Команда на очистку терминала
        /// </summary>
        public ReactiveCommand<Unit, string> ClearTerminalCommand { get; }

        /// <summary>
        /// Команда на сохранение данных терминала в файл
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveTerminalInFileCommand { get; }

        /// <summary>
        /// Отладочное сообщение
        /// </summary>
        public string DebugMessages
        {
            get => _debugMessages;
            set => this.RaiseAndSetIfChanged(ref _debugMessages, value);
        }

        /// <summary>
        /// Отладочные сообщения
        /// </summary>
        public List<string> TerminalMessages
        {
            get => _terminalMessages;
            set => this.RaiseAndSetIfChanged(ref _terminalMessages, value);
        }

        /// <summary>
        ///  Фильтры диалоговых окон
        /// </summary>
        private void InitFileDialogs()
        {
            _saveFileTxt = new SaveFileDialog();
            _saveFileTxt.Filters?.Add(new FileDialogFilter() { Name = "Текстовые файлы (*.txt)", Extensions = new List<string>(new string[] { "txt" }) });
            _saveFileTxt.Filters?.Add(new FileDialogFilter() { Name = "Все файлы (*.*)", Extensions = new List<string>(new string[] { "*" }) });
        }

        public TerminalPluginViewModel()
        {
            // инициализация диалогов
            InitFileDialogs();

            ClearTerminalCommand = ReactiveCommand.Create(() => DebugMessages = string.Empty);
        }
    }
}
