using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AvaloniaEdit;
using Rss.TmFramework.Controls.Avalonia;
using Rss.TmFramework.Logging;
using System.Collections.ObjectModel;


namespace progMap.App.Avalonia.Plugins;

public partial class LoggerPluginView : UserControl
{
    private SaveFileDialog _saveFileTxt;

    public LoggerPluginView()
    {
        InitializeComponent();
        InitFileDialogs();
    }

    // инициализация диалоговых окон
    private void InitFileDialogs()
    {
        _saveFileTxt = new SaveFileDialog();
        _saveFileTxt.Filters?.Add(new FileDialogFilter() { Name = "Текстовые файлы (*.txt)", Extensions = new List<string>(new string[] { "txt" }) });
        _saveFileTxt.Filters?.Add(new FileDialogFilter() { Name = "Все файлы (*.*)", Extensions = new List<string>(new string[] { "*" }) });
    }


    // позволяет сохранить данные журнала в отдельный файл
    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();

        SaveFileDialog sfd = new() { Filters = _saveFileTxt.Filters };
        var fileName = await sfd.ShowAsync(currentWindow);

        if (fileName == null || fileName.Length == 0)
            return;

        using StreamWriter _writer = new(new FileStream(fileName, FileMode.Create));

        var te = loggerEditor.FindControl<TextEditor>("texteditor");
        _writer.Write(te.Text);

        _writer.Close();
    }
}