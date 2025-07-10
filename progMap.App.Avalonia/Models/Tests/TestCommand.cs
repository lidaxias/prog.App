using System;
using System.Xml.Serialization;

namespace progMap.App.Avalonia.Models.Tests
{
    /// <summary>
    /// Описывает команду в тесте
    /// </summary>
    [Serializable]
    public class TestCommand
    {
        /// <summary>
        /// Название конфигурации, в которой содержится секция
        /// </summary>
        [XmlAttribute]
        public string DeviceConfigurationName { get; set; }

        /// <summary>
        /// Название секции
        /// </summary>
        [XmlAttribute]
        public string SectionName { get; set; }

        /// <summary>
        /// Имя файла для записи
        /// </summary>
        [XmlAttribute]
        public string FileName { get; set; }

        public TestCommand() { }
    }
}
