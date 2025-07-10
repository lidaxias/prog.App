using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.IO;
using AvaloniaEdit;
using System.Linq;

namespace progMap.App.Avalonia.Plugins;

public partial class TerminalPluginView : UserControl
{
    private SaveFileDialog _saveFileTxt;
    public TerminalPluginView()
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
    // позволяет сохранить данные терминала в отдельный файл
    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var currentWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.Last();

        SaveFileDialog sfd = new() { Filters = _saveFileTxt.Filters };
        var fileName = await sfd.ShowAsync(currentWindow);

        if (fileName == null || fileName.Length == 0)
            return;

        using StreamWriter _writer = new(new FileStream(fileName, FileMode.Create));

        var te = terminalEditor.FindControl<TextEditor>("terminalEditor");
        _writer.Write(te.Text);

        _writer.Close();
    }

}