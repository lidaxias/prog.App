using progMap.Core;
using ReactiveUI;
using Rss.TmFramework.Base.Helpers;
using System.Globalization;
using System.Text;

namespace progMap.ViewModels
{
    /// <summary>
    /// ViewModel для конфигурации секции памяти
    /// </summary>
    public class MemorySectionConfigurationVm : SectionConfigurationBaseVm
    {
        /// <summary>
        /// Модель секции памяти
        /// </summary>
        public MemorySectionConfiguration MemorySection
        {
            get => Model as MemorySectionConfiguration;
            set => Model = value;
        }

        // Поле для хранения строкового представления данных
        private string _datastring;

        /// <summary>
        /// Строковое представление данных секции
        /// </summary>
        public string DataString
        {
            get => _datastring;
            set => this.RaiseAndSetIfChanged(ref _datastring, value);
        }

        public MemorySectionConfigurationVm(MemorySectionConfiguration model) : base(model)
        {
        }

        /// <summary>
        /// Создает ViewModel из модели
        /// </summary>
        public static MemorySectionConfigurationVm FromModel(MemorySectionConfiguration memorySectionConfiguration) =>
            new(memorySectionConfiguration);

        /// <summary>
        /// Форматирует массив байт в строку шестнадцатеричных значений
        /// </summary>
        private static string FormatBytes(IEnumerable<byte> data) =>
            string.Concat(data.Select(b => $"{b:X2} ")).TrimEnd();

        /// <summary>
        /// Парсит строку шестнадцатеричных значений в массив байт
        /// </summary>
        private static byte[] ParseBytes(string text) =>
            text.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .Select(s => byte.Parse(s, NumberStyles.HexNumber))
                .ToArray();

        /// <summary>
        /// Возвращает форматированное строковое представление данных в зависимости от типа
        /// </summary>
        public string? GetFormattedDataString(byte[]? data)
        {
            if (data == null)
                return "{NULL}";

            switch (MemorySection.DataType)
            {
                case EDataType.ByteArray:
                    return data.GetArrString("X2"); 
                case EDataType.U8:
                    return data.Length != 1 ? null : $"0x{data.Single():X2}"; 
                case EDataType.I8:
                    return data.Length != 1 ? null : $"0x{data.Single():X2}"; 
                case EDataType.U16:
                    return data.Length != 2 ? null : $"0x{BitConverter.ToUInt16(data):X4}"; 
                case EDataType.I16:
                    return data.Length != 2 ? null : $"0x{BitConverter.ToInt16(data):X4}"; 
                case EDataType.U32:
                    return data.Length != 4 ? null : $"0x{BitConverter.ToUInt32(data):X8}"; 
                case EDataType.I32:
                    return data.Length != 4 ? null : $"0x{BitConverter.ToInt32(data):X8}"; 

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Тестовый экземпляр ViewModel с предустановленными значениями
        /// </summary>
        public static MemorySectionConfigurationVm TestInstance => new(new MemorySectionConfiguration
        {
            Name = "Test",
            Address = 0x12345678,
            Size = 0x10,
            DataType = EDataType.ByteArray,
        });
    }

    /// <summary>
    /// Класс для работы с массивами
    /// </summary>
    public static class ArrayHelpers
    {
        public static string GetArrString<T>(this T[] array, string format)
        {
            if (array == null || array.Length == 0)
                return "{ }";

            var fmt = string.Format("0x{{0:{0}}} ", format);

            var sb = new StringBuilder(array.Length * 10);

            foreach (var item in array)
                sb.AppendFormat(fmt, item);

            return sb.ToString();
        }
    }
}