using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace progMap.Core
{
    /// <summary>
    /// Базовый класс конфигурации
    /// </summary>
    public abstract class SectionConfigurationBase : ICloneable
    {
        /// <summary>
        ///Имя секции
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Начальный адрес секции.
        /// </summary>
        [XmlIgnore]
        public uint Address { get; set; }

        /// <summary>
        /// Начальный адрес секции.
        /// </summary>
        [XmlAttribute("address")]
        public string AddressString
        {
            get => $"0x{Address:X8}";
            set => Address = value.StartsWith("0x") || value.StartsWith("0X")
                ? uint.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber)
                : uint.Parse(value);
        }

        /// <summary>
        /// Размер секции в байтах.
        /// </summary>
        [XmlIgnore]
        public uint Size { get; set; }

        /// <summary>
        /// Размер секции в байтах.
        /// </summary>
        [XmlAttribute("size")]
        public string SizeString
        {
            get => $"0x{Size:X8}";
            set => Size = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? uint.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber)
                : uint.Parse(value);
        }

        /// <summary>
        /// Физический адрес или виртуальный (признак устанавливается в пакеты при обменах)
        /// </summary>
        [XmlAttribute("virtual")]
        public bool IsVirtual { get; set; }

        public SectionConfigurationBase() { }

        public SectionConfigurationBase(string name, uint startaddress, uint size, bool isVirtual)
        {
            Name = name;
            Address = startaddress;
            Size = size;
            IsVirtual = isVirtual;
        }

        public override string ToString() => Name;

        public abstract object Clone();

        public static List<SectionConfigurationBase> TestDataFiles => new List<SectionConfigurationBase>
    {
        new FileSectionConfiguration("ALL FLASH", 0x08000000, 0x00100000, true, false),
        new FileSectionConfiguration("BOOT", 0x08000000, 0x00008000, false, false),
        new FileSectionConfiguration("MCU", 0x08010000, 0x00090000, true, false),
        new FileSectionConfiguration("ASIC", 0x080A0000, 0x00090000, true, false)
    };

        public static List<SectionConfigurationBase> TestRam => new List<SectionConfigurationBase>
    {
        new MemorySectionConfiguration("CCMRAM", 0x10000000, 0x00010000, true),
        new MemorySectionConfiguration("CCMRAM_0_3", 0x10000000, 0x00000004, true),
        new MemorySectionConfiguration("CCMRAM_4_7", 0x10000004, 0x00000004, true),
        new MemorySectionConfiguration("RAM", 0x20000000, 0x00020000, true),
    };

        public static List<SectionConfigurationBase> TestDataRegisters => new List<SectionConfigurationBase>
    {
        new MemorySectionConfiguration("SETTINGS", 0x00000100, 89, true),
        new MemorySectionConfiguration("Reg 1", 0x80001000, 0x00000004, true),
        new MemorySectionConfiguration("Reg 2", 0x80001004, 0x00000004, false),
        new MemorySectionConfiguration("Reg 3", 0x80001000, 0x00080000, true),
        new MemorySectionConfiguration("Reg 4", 0x80001000, 0x00080000, true)
    };

        public static List<SectionConfigurationBase> TestAllFiles => new()
        {
            new MemorySectionConfiguration("Reg 1", 0x80001000, 0x00000004, true),
            new MemorySectionConfiguration("Reg 2", 0x80001004, 0x00000004, false),
            new FileSectionConfiguration("ALL FLASH", 0x08000000, 0x00100000, true, false),
            new MemorySectionConfiguration("Reg 3", 0x80001000, 0x00080000, true),
            new MemorySectionConfiguration("Reg 4", 0x80001000, 0x00080000, true),
            new FileSectionConfiguration("BOOT", 0x08000000, 0x00008000, false, false),
            new FileSectionConfiguration("SETTINGS", 0x08008000, 0x00004000, true, false),
            new FileSectionConfiguration("MCU", 0x08010000, 0x00090000, true, false),
            new FileSectionConfiguration("ASIC", 0x080A0000, 0x00090000, true, false),
        };
    }
}
