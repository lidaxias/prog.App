using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.ViewModels
{
    /// <summary>
    /// ViewModel для конфигурации типа памяти
    /// </summary>
    public class MemoryTypeVm : ViewModelBase
    {
        private const uint K = 1024;

        /// <summary>
        /// Список всех типов памяти
        /// </summary>
        public static List<MemoryTypeVm> All { get; private set; }

        public static MemoryTypeVm Default = new MemoryTypeVm(0x00, 256 * K, "ТЗУ");

        /// <summary>
        /// Название типа памяти
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Индентификатор типа памяти
        /// </summary>
        public byte Code { get; }

        /// <summary>
        /// Размер сектора
        /// </summary>
        public uint SectorSize { get; }

        static MemoryTypeVm()
        {
            All = new List<MemoryTypeVm>();
            All.Add(Default);
            All.Add(new MemoryTypeVm(0x01, 002 * K, "ОПЗУ"));
            All.Add(new MemoryTypeVm(0x02, 128 * K, "ППЗУ"));
        }

        public MemoryTypeVm() { }

        public MemoryTypeVm(byte code, uint sectorSize, string name)
        {
            Name = name;
            Code = code;
            SectorSize = sectorSize;
        }

        public override string ToString()
        {
            return $"\"{Name}\" (code: 0x{Code:X2}; sector size: 0x{SectorSize:X4}, bytes)";
        }
    }
}
