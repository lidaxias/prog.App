using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace progMap.App.Avalonia.Views;

public partial class TerminalWindow : Window
{
    public TerminalWindow()
    {
        InitializeComponent();
    }

    // ������������ scrollviewer ���� ��� ���������� ������ ������ � textbox 
    private void TextBox_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "Text")
        {
            if (sender is not TextBox tb)
                throw new Exception("Can't scroll scrollviewer!");

            tb.CaretIndex = tb.Text.Length - 1;
        }
    }

    // ����������� terminalwindow ������ ��������� ���� ��� ��������� ������ � checkbox
    private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        Topmost = ((CheckBox)sender).IsChecked == true;
    }
}