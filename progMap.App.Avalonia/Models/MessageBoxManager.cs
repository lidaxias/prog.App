using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using progMap.App.Avalonia.Views;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Models
{
    /// <summary>
    /// Отвечает за отображение всплывающих окон
    /// </summary>
    public static class MessageBoxManager
    {
        /// <summary>
        /// Формирует всплывающее окно об ошибке 
        /// </summary>
        public static async Task MsgBoxOk(string nameErrorType)
        {
            var currentWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow;

            await Task.Delay(500);

            await MessageBoxWindow.Show(currentWindow, nameErrorType, "Ошибка!", MessageBoxWindow.MessageBoxButtons.Ok);
        }

        /// <summary>
        /// Формирует всплывающее окно
        /// </summary>
        public static async Task MsgBoxYesNo(string nameErrorType)
        {
            var currentWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow;

            await Task.Delay(500);

            await MessageBoxWindow.Show(currentWindow, nameErrorType, "Предупреждение", MessageBoxWindow.MessageBoxButtons.YesNo);
        }

    }
}
