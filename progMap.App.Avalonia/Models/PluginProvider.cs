using Avalonia.Threading;
using DynamicData;
using progMap.App.Avalonia.Plugins;
using progMap.Core;
using progMap.Interfaces;
using progMap.ViewModels;
using Rss.TmFramework.Base.Channels;
using Rss.TmFramework.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Models
{
    /// <summary>
    /// Отвечает за загрузку плагинов
    /// </summary>
    public class PluginProvider
    {
        /// <summary>
        /// Возвращает список типов каналов
        /// </summary>
        public static List<ChannelTypeVm>? LoadChannels(string path, Dispatcher dispatcher)
        {
            List<ChannelTypeVm> channels = new(); // список типов каналов
            bool error = false; // флаг ошибки
            List<string> errorNames = new(); // список имён типов каналов, которые не смогли загрузиться

            try
            {
                List<PluginInfo> plugins = Serializer<List<PluginInfo>>.Deserialize(path); // список плагинов
                string[] assemblyNames = plugins.Select(pl => pl.AssemblyName).ToArray(); // список имён сборок, где лежат типы
                string[] typeNames = plugins.Select(pl => pl.TypeName).ToArray(); // список полных имён типов каналов
                string[] displayNames = plugins.Select(pl => pl.DisplayName).ToArray(); // список отображаемых имён каналов

                for (int i = 0; i < plugins.Count; i++)
                {
                    var rcvType = CreateChannel(assemblyNames[i], typeNames[i], dispatcher); // создаем экземпляр типа с указанным именем

                    // если не получилось создать экземпляр
                    if (rcvType == null)
                    {
                        error = true;
                        errorNames.Add(typeNames[i]);
                        continue;
                    }

                    channels.Add(new ChannelTypeVm(displayNames[i], rcvType.GetType())); // добавляем новый тип в список
                }

                string str = $"Не удалось загрузить из конфигурации:{Environment.NewLine}";
                string str1 = string.Join(Environment.NewLine, errorNames); // для отображения списка имён типов каналов, которые не смогли загрузиться

                if (error)

                    dispatcher.InvokeAsync(async () =>
                    {
                        await MessageBoxManager.MsgBoxOk(string.Concat(str, str1)); // если была ошибка то создаем и отображаем msgbox
                    });

                return channels;
            }

            catch (System.IO.FileNotFoundException)
            {
                dispatcher.InvokeAsync(async () =>
                {
                    await MessageBoxManager.MsgBoxOk($"Файл по пути {path} не найден!");
                });

                return null;
            }

            catch (Exception ex)
            {
                dispatcher.InvokeAsync(async () =>
                {
                    await MessageBoxManager.MsgBoxOk(ex.Message.ToString());
                });

                return null;
            }
        }

        /// <summary>
        /// Создает экземпляр типа с указанным именем
        /// </summary>
        public static IChannel? CreateChannel(string fileName, string typeName, Dispatcher dispatcher)
        {
            try
            {
                var curDir = System.IO.Directory.GetCurrentDirectory(); // текущая директория
                var fullName = Path.Combine(curDir, fileName); // полный путь к файлу
                var asm = Assembly.LoadFile(fullName).GetType(typeName); // полученные сборки

                if (asm == null) return null;

                if (!asm.IsInterface)
                    return Activator.CreateInstance(asm) as IChannel; // создаем экземпляр типа с указанным именем

                else
                {
                    return null;
                }
            }

            catch (Exception ex)
            {
                dispatcher.InvokeAsync(async () =>
                {
                    await MessageBoxManager.MsgBoxOk("Ошибка загрузки типов каналов: " + ex.Message);
                });

                return null;
            }
        }


        /// <summary>
        /// Загружает плагины 
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="_module"></param>
        /// <param name="_loggerVm"></param>
        /// <param name="_terminalVm"></param>
        /// <returns></returns>

        public static ObservableCollection<IProgPlugin>? LoadItems(Dispatcher dispatcher, MapEthernetModule _module, out LoggerPluginViewModel _loggerVm, out TerminalPluginViewModel _terminalVm)
        {
            _loggerVm = null;
            _terminalVm = null;
            var plugins = new ObservableCollection<IProgPlugin>();

            // инициализация стандартных плагинов
            var plugTerminal = new TerminalPluginViewModel();
            plugins.Add(plugTerminal);
            _terminalVm = plugTerminal;

            var plugLog = new LoggerPluginViewModel();
            plugins.Add(plugLog);
            _loggerVm = plugLog;

            string curDir = Directory.GetCurrentDirectory();

            try
            {
                foreach (string dllFile in Directory.EnumerateFiles(curDir, "*.dll"))
                {
                    try
                    {
                        string fileName = Path.GetFileName(dllFile);
                        var assembly = Assembly.LoadFrom(dllFile);

                        foreach (Type type in assembly.GetExportedTypes())
                        {
                            if (typeof(IProgPlugin).IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               type != typeof(LoggerPluginViewModel) &&
                               type != typeof(TerminalPluginViewModel))
                            {
                                plugins.Add((IProgPlugin)Activator.CreateInstance(type));
                            }
                        }
                    }

                    catch (BadImageFormatException)
                    {
                        // пропускаем нативные DLL и бинарники неверного формата
                        continue;
                    }

                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка загрузки {dllFile}: {ex.Message}");
                    }
                }
            }

            catch (Exception ex)
            {
                dispatcher.InvokeAsync(async () =>
                {
                    await MessageBoxManager.MsgBoxOk($"Ошибка загрузки плагинов: {ex.Message}");
                });

                return null;
            }

            return plugins;
        }
    }
}
