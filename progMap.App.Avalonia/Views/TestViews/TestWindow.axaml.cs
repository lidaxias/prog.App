using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace progMap.App.Avalonia.Views.TestViews;

public partial class TestWindow : Window
{
    public TestWindow()
    {
        InitializeComponent();
    }

    // отображение testwindow поверх остальных окон при установке флажка в checkbox
    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        Topmost = ((CheckBox)sender).IsChecked == true;
    }
}