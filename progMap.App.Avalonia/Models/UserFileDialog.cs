using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace progMap.App.Avalonia.Models
{
    /// <summary>
    /// Кастомное пользовательское диалоговое окно
    /// </summary>
    public class UserFileDialog : OpenFileDialog
    {
        public string? _initialDirectory;

        /// <summary>
        /// Открывает диалоговое окно и сохраняет путь к директории, в которой был открыт последний файл
        /// </summary>
        private static string? _lastDirectory;

        public new async Task<string[]?> ShowDialog(Window owner)
        {
            if (!string.IsNullOrEmpty(_lastDirectory))
            {
                Directory = _lastDirectory;
            }

            var result = await base.ShowAsync(owner);

            if (result != null && result.Length > 0)
            {
                _lastDirectory = Path.GetDirectoryName(result[0]);
            }

            return result;
        }
    }
}


