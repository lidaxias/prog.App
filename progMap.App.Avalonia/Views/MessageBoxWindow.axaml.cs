using Avalonia.Layout;
using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Views;

public partial class MessageBoxWindow : Window
{
    private static bool _flagResult;

    /// <summary>
    /// Флаг выставляется в 1, когда MessageBoxResult.Yes
    /// </summary>
    public static bool FlagResult
    {
        get => _flagResult;
        private set => _flagResult = value;
    }

    [Flags]
    public enum MessageBoxButtons
    {
        Ok,
        YesNo,
        YesNoCancel,
        No
    }

    public enum MessageBoxResult
    {
        Ok,
        Yes,
        No
    }
    public MessageBoxWindow()
    {
        InitializeComponent();
    }

    private static TaskCompletionSource<bool> _windowClosedTsc = new TaskCompletionSource<bool>();

    private static void TargetWindowClosed(object sender, EventArgs e)
    {
        _windowClosedTsc.SetResult(true);
    }

    /// <summary>
    /// Создает и показывает всплывающее окно
    /// </summary>
    public async static Task<MessageBoxResult> Show(Window parent, string text, string title, MessageBoxButtons buttons)
    {
        FlagResult = false;

        var msgbox = new MessageBoxWindow()
        {
            Title = title
        };

        msgbox.FindControl<TextBox>("Text").Text = text;
        msgbox.FindControl<TextBox>("Text").Width = 430;

        var buttonPanel = msgbox.FindControl<StackPanel>("Buttons");

        var res = MessageBoxResult.Ok;

        void AddButton(string caption, MessageBoxResult r, bool def = false)
        {
            var btn = new Button { Content = caption, Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };

            if ((r == MessageBoxResult.Ok) || (r == MessageBoxResult.No))
            {
                btn.Click += (_, __) => {
                    res = r;
                    msgbox.Close();
                };
            }
            else
            {
                btn.Click += (_, __) => {
                    res = r;
                    FlagResult = true;
                    msgbox.Close();
                };
            }


            buttonPanel.Children.Add(btn);
            if (def)
                res = r;
        }

        if (buttons == MessageBoxButtons.Ok)
            AddButton("Ок", MessageBoxResult.Ok, true);

        if (buttons == MessageBoxButtons.YesNo)
        {
            AddButton("Да", MessageBoxResult.Yes);
            AddButton("Нет", MessageBoxResult.No, true);
        }

        var tcs = new TaskCompletionSource<MessageBoxResult>();
        msgbox.Closed += delegate { tcs.TrySetResult(res); };


        if (parent != null)
        {
            await msgbox.ShowDialog(parent).ConfigureAwait(false);
        }
        else msgbox.Show();

        return tcs.Task.Result;
    }
}
