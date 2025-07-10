using System;
using System.Collections.Generic;
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
    /// Конфигурация секции памяти (файловой)
    /// </summary>

    [Serializable]
    public class FileSectionConfiguration : SectionConfigurationBase
    {
        /// <summary>
        /// Расширения загружаемых файлов
        /// </summary>
        [XmlAttribute("extensions")]
        public string Extensions { get; set; }

        /// <summary>
        /// Заголовок для файла в bigendian - 1, littleendian - 0
        /// </summary>
        [XmlAttribute("bigendian")]
        public bool BigEndianHeader { get; set; }

        /// <summary>
        /// Выбор формата работы с файлом
        /// </summary>
        [XmlAttribute("format")]
        public string FileFormat { get; set; }

        public FileSectionConfiguration() { }

        public FileSectionConfiguration(string name, uint sectionStartaddress, uint size, bool isVirtual,
            bool bigEndianHeader, string extensions = "Все файлы (*.*)|  *.*", string fileFormat = "с заголовком") : base(name, sectionStartaddress, size, isVirtual)
        {
            Extensions = extensions;
            BigEndianHeader = bigEndianHeader;
            FileFormat = fileFormat;
        }
        public override object Clone()
        {
            return new FileSectionConfiguration
            {
                Name = Name.Clone() as string,
                Extensions = Extensions.Clone() as string,
                Size = Size,
                Address = Address,
                IsVirtual = IsVirtual,
                BigEndianHeader = BigEndianHeader,
                FileFormat = FileFormat?.Clone() as string

            };
        }
    }
}
