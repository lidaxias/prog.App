using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace progMap.Core
{
    /// <summary>
    /// Конфигурация секции памяти (обычной)
    /// </summary>
    [Serializable]
    public class MemorySectionConfiguration : SectionConfigurationBase
    {
        /// <summary>
        /// Тип данных
        /// </summary>
        [XmlAttribute("datatype")]
        public EDataType DataType { get; set; }

        public MemorySectionConfiguration() : base()
        {
        }

        public MemorySectionConfiguration(string name, uint sectionStartaddress, uint size, bool isVirtual) : base(name, sectionStartaddress, size, isVirtual)
        {
        }

        public override object Clone()
        {
            return new MemorySectionConfiguration
            {
                Name = Name.Clone() as string,
                Size = Size,
                Address = Address,
                IsVirtual = IsVirtual
            };
        }
    }
}
