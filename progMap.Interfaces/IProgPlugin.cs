using Rss.TmFramework.Modules;

namespace progMap.Interfaces
{
    public interface IProgPlugin
    {
        /// <summary>
        /// Возвращает название/заголовок плагина
        /// </summary>
        string Title { get; }

        /// <summary>
        ///  Предоставляет доступ к модулю
        /// </summary>
        MapEthernetModule MapModule { get; set; }
    }
}
