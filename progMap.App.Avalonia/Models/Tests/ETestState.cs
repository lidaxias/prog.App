using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Models.Tests
{
    /// <summary>
    /// Состояние выполнения циклограммы команд
    /// </summary>
    public enum ETestState
    {
        None,
        /// <summary>
        /// Тест начат
        /// </summary>
        Started,
        /// <summary>
        /// Тест выполняется
        /// </summary>
        Pending,
        /// <summary>
        /// Тест остановлен по ошибке
        /// </summary>
        StopByError,
        /// <summary>
        /// Тест выполнен
        /// </summary>
        Finished_Ok,
        /// <summary>
        /// Тест выполнен
        /// </summary>
        Finished_Error,
        /// <summary>
        /// Ошибка в тесте
        /// </summary>
        Exception,
        /// <summary>
        /// Тест прерван
        /// </summary>
        Abort
    }
}
