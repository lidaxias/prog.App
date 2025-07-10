using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace progMap.Core
{
    /// <summary>
    /// Конфигурация загружаемых плагинов
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// Имя сборки
        /// </summary>
        [XmlAttribute("assembly_name")]
        public string AssemblyName { get; set; }

        /// <summary>
        /// Имя типа
        /// </summary>
        [XmlAttribute("type_name")]
        public string TypeName { get; set; }

        /// <summary>
        /// Отображение имени типа
        /// </summary>
        [XmlAttribute("display_name")]
        public string DisplayName { get; set; }

        public PluginInfo() { }

        public PluginInfo(string assemblyName, string typeName, string displayName)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            DisplayName = displayName;
        }
    }
}
