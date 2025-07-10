using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.Core
{
    /// <summary>
    /// Типы для отображения
    /// </summary>
    public enum EDataType
    {
        /// <summary>
        /// Отображение массива
        /// </summary>
        ByteArray,
        /// <summary>
        /// Отображение 1 байта со знаком
        /// </summary>
        I8,
        /// <summary>
        /// Отображение 1 байта беззнаковый тип
        /// </summary>
        U8,
        /// <summary>
        /// Отображение 2 байта со знаком
        /// </summary>
        I16,
        /// <summary>
        /// Отображение 2 байта беззнаковый тип
        /// </summary>
        U16,
        /// <summary>
        /// Отображение 4 байт со знаком
        /// </summary>
        I32,
        /// <summary>
        /// Отображение 4 байт беззнаковый тип
        /// </summary>
        U32
    }
}
