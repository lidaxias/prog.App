using System.Xml.Serialization;

namespace progMap.Core
{
    /// <summary>
    /// Конфигурация прибора
    /// </summary>

    [Serializable]
    [XmlInclude(typeof(MemorySectionConfiguration))]
    [XmlInclude(typeof(FileSectionConfiguration))]

    public class DeviceConfiguration : ICloneable
    {
        /// <summary>
        /// Имя прибора
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Тип канала
        /// </summary>
        [XmlAttribute("channel")]
        public string ChannelType { get; set; }

        /// <summary>
        /// Строка подключения
        /// </summary>
        [XmlAttribute("connectionstring")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Таймаут на стирание
        /// </summary>
        [XmlAttribute("erasetimeout")]
        public string EraseTimeOut { get; set; }

        /// <summary>
        /// Таймаут на запись
        /// </summary>
        [XmlAttribute("writetimeout")]
        public string WriteTimeOut { get; set; }

        /// <summary>
        /// Таймаут на чтение
        /// </summary>
        [XmlAttribute("readtimeout")]
        public string ReadTimeOut { get; set; }

        /// <summary>
        /// Адрес стирания виртуальных секций памяти
        /// </summary>
        [XmlAttribute("erasevirtual")]
        public string EraseVirtualAddr { get; set; }

        /// <summary>
        /// Адрес стирания физических секций памяти
        /// </summary>
        [XmlAttribute("erasephysical")]
        public string ErasePhysicalAddr { get; set; }

        /// <summary>
        /// Тип памяти
        /// </summary>
        [XmlAttribute("memorytype")]
        public string? MemoryType { get; set; }

        /// <summary>
        /// Кодировка для вывода сообщений терминала
        /// </summary>
        [XmlAttribute("encode")]
        public string? Encode { get; set; }


        /// <summary>
        /// Список секций памяти
        /// </summary>
        [XmlArrayItemAttribute(typeof(MemorySectionConfiguration), ElementName = "MemorySection")]
        [XmlArrayItemAttribute(typeof(FileSectionConfiguration), ElementName = "FileSection")]

        public List<SectionConfigurationBase> Sections { get; set; }

        public DeviceConfiguration() { }

        public DeviceConfiguration(string name, string channelType, List<SectionConfigurationBase> sections, string connectionString, string eraseTimeOut, string writeTimeOut, string readTimeOut,
            string eraseVirtual, string erasePhysical, string encode)
        {
            Name = name;
            ChannelType = channelType;
            Sections = sections;
            ConnectionString = connectionString;
            EraseTimeOut = eraseTimeOut;
            WriteTimeOut = writeTimeOut;
            ReadTimeOut = readTimeOut;
            EraseVirtualAddr = eraseVirtual;
            ErasePhysicalAddr = erasePhysical;
            Encode = encode;
        }

        public DeviceConfiguration(string name, string channelType, List<SectionConfigurationBase> sections, string connectionString, string memoryType, string eraseTimeOut, string writeTimeOut, string readTimeOut,
             string eraseVirtual, string erasePhysical, string encode) : this(name, channelType, sections, connectionString, eraseTimeOut, writeTimeOut, readTimeOut, eraseVirtual, erasePhysical, encode)
        {
            MemoryType = memoryType;
        }

        public override string ToString() => Name;

        public object Clone()
        {
            return new DeviceConfiguration
            {
                Name = Name.Clone() as string,
                ChannelType = ChannelType.Clone() as string,
                ConnectionString = ConnectionString.Clone() as string,
                MemoryType = MemoryType?.Clone() as string,
                Sections = Sections.Select(s => s.Clone() as SectionConfigurationBase).ToList(),
                EraseTimeOut = EraseTimeOut.Clone() as string,
                WriteTimeOut = WriteTimeOut.Clone() as string,
                ReadTimeOut = ReadTimeOut.Clone() as string,
                EraseVirtualAddr = EraseVirtualAddr.Clone() as string,
                ErasePhysicalAddr = ErasePhysicalAddr.Clone() as string,
                Encode = Encode?.Clone() as string
            };
        }

        public static List<DeviceConfiguration> Default { get; set; } = new List<DeviceConfiguration>();

        public static List<DeviceConfiguration> TestData => new(new[]
        {
            new DeviceConfiguration("config1", "UDP", SectionConfigurationBase.TestAllFiles, "32001, 192.168.2.3:32000", "ТЗУ", "2000", "2000", "2000", "0x00001000", "0x00001000", "1251"),
            new DeviceConfiguration("config2", "Serial+SLIP", SectionConfigurationBase.TestDataRegisters, "COM1, 9600, 8n1", "2000", "2000", "2000", "0x00001000", "0x00001000", "1251"),
            new DeviceConfiguration("ФАЙЛЫ", "UDP", SectionConfigurationBase.TestDataFiles, "32001, 192.168.2.200:32000", "2000", "2000", "2000", "0x00001000", "0x00001000", "1251"),
            new DeviceConfiguration("config4", "UDP", SectionConfigurationBase.TestDataRegisters, "32001, 192.168.2.3:32000", "2000", "2000", "2000", "0x00001000", "0x00001000", "1251"),
            new DeviceConfiguration("RAM", "UDP", SectionConfigurationBase.TestRam, "32001, 192.168.2.200:32000", "2000", "2000", "2000", "0x00001000", "0x00001000", "1251")
        });
    }
}
