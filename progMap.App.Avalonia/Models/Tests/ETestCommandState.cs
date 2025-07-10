using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Models.Tests
{
    /// <summary>
    /// Состояние команды
    /// </summary>
    public enum ETestCommandState
    {
        /// <summary>
        /// не задано
        /// </summary>
        Empty = 0,
        /// <summary>
        /// команда выполняется
        /// </summary>
        Pending,
        /// <summary>
        /// команда выполнена
        /// </summary>
        Completed,
        /// <summary>
        /// ошибка при выполнении команды
        /// </summary>
        Error,
        /// <summary>
        /// Команда пропущена и не выполнялась
        /// </summary>
        Ignored
    }
}
