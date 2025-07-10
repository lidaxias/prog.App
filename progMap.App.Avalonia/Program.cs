using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;

namespace progMap.App.Avalonia
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Настройка обработки необработанных исключений
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                File.WriteAllText("crash1.log", e.ExceptionObject.ToString());
                var exception = e.ExceptionObject as Exception;
                LogCrash(exception);
            };

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {

                LogCrash(ex);
                throw;
            }
        }

        private static void LogCrash(Exception? ex)
        {
            string logMessage = $"[{DateTime.Now}] CRASH: {ex?.ToString() ?? "Unknown error"}";
            File.AppendAllText("crash.log", logMessage + Environment.NewLine);
        }

        //Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
                => AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .LogToTrace()
                    .UseReactiveUI();

    }
}
