using System.Xml.Serialization;
using System.Xml;

namespace progMap.Core
{
    // Класс для сериализации и десериализации объектов в/из XML
    // где T : class, new() - ограничение типа T: должен быть классом и иметь конструктор по умолчанию
    public class Serializer<T> where T : class, new()
    {
        public static void Serialize(T obj, string filename)
        {
            XmlWriterSettings settings = new() { Indent = true };

            XmlSerializer xmlSerializer = new(typeof(T));

            using (var writer = XmlWriter.Create(filename, settings))
            {
                xmlSerializer.Serialize(writer, obj);
            }
        }

        public static T? Deserialize(string filename)
        {
            XmlReaderSettings settings = new();
            XmlSerializer xmlSerializer = new(typeof(T));

            using (var reader = XmlReader.Create(filename, settings))
            {
                return xmlSerializer.Deserialize(reader) as T;
            }
        }
    }
}
