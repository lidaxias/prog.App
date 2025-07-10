using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace progMap.App.Avalonia.Views;

public partial class EditorWindow : Window
{
    // флаги
    private bool _saveFlag;
    private bool _saveAsFlag;

    public EditorWindow()
    {
        InitializeComponent();
    }


    /// <summary>
    /// Флаг "сохранить"
    /// </summary>
    public bool SaveFlag
    {
        get => _saveFlag;
        private set => _saveFlag = value;
    }

    /// <summary>
    /// Флаг "сохранить как"
    /// </summary>
    public bool SaveAsFlag
    {
        get => _saveAsFlag;
        private set => _saveAsFlag = value;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFlag = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFlag = false;
        Close();
    }

    private void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        SaveAsFlag = true;
        Close();

    }
}