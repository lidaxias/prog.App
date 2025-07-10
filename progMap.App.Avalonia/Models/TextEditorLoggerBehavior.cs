using Avalonia.Media;
using Avalonia;
using Avalonia.Data;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit;
using Rss.TmFramework.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.Collections.Specialized;
using Avalonia.Media.Immutable;
using System.ComponentModel;
using progMap.ViewModels;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Avalonia.Controls;

namespace progMap.App.Avalonia.Models
{
    /// <summary>
    /// Поведение для TextEditor, обеспечивающее отображение цветного лога сообщений
    /// с возможностью прокрутки и ограничением по количеству строк
    /// </summary>
    public class TextEditorLoggerBehavior
    {
        private static TextEditor _textEditor;

        /// <summary>
        /// Присоединенное свойство для привязки коллекции лог-сообщений к TextEditor
        /// </summary>
        public static readonly AvaloniaProperty<ObservableCollection<LogMessage>> ColorLogProperty =
            AvaloniaProperty.RegisterAttached<TextEditorLoggerBehavior, TextEditor, ObservableCollection<LogMessage>>("ColorLog");

        /// <summary>
        /// Хранит состояние для каждого TextEditor (коллекция сообщений, счетчик и трансформер)
        /// </summary>
        private static readonly ConditionalWeakTable<TextEditor, EditorState> _editorStates = new();

        /// <summary>
        /// Внутренний класс для хранения состояния редактора
        /// </summary>
        private class EditorState
        {
            /// <summary>
            /// Текущая коллекция лог-сообщений
            /// </summary>
            public ObservableCollection<LogMessage>? CurrentCollection;

            /// <summary>
            /// Количество уже обработанных сообщений
            /// </summary>
            public int LastProcessedCount;

            /// <summary>
            /// Трансформер для раскраски текста
            /// </summary>
            public SharedColorizingTransformer? Transformer;
        }

        static TextEditorLoggerBehavior()
        {
            ColorLogProperty.Changed.Subscribe(e =>
            {
                if (e.Sender is TextEditor textEditor)
                {
                    _textEditor = textEditor;

                    // Получаем или создаем состояние
                    var state = _editorStates.GetValue(textEditor, _ =>
                    new EditorState { LastProcessedCount = 0 });

                    // Подписываемся на изменение свойства IsPointerOver для автоматической прокрутки
                    textEditor.PropertyChanged += (sender, e) =>
                    {
                        if (textEditor == null) return;

                        if (e.Property.Name == "IsPointerOver") return;
                        textEditor.ScrollToLine(textEditor.LineCount);
                    };

                    // Отписываемся от старой коллекции
                    if (state.CurrentCollection != null)
                    {
                        state.CurrentCollection.CollectionChanged -= LogMessages_CollectionChanged;
                    }

                    if (e.NewValue.Value is ObservableCollection<LogMessage> newLogs)
                    {
                        // Обновляем состояние
                        state.CurrentCollection = newLogs;
                        state.LastProcessedCount = newLogs.Count;

                        // Подписываемся на новую коллекцию
                        newLogs.CollectionChanged += LogMessages_CollectionChanged;

                        // Первоначальное заполнение
                        UpdateTextEditor(textEditor, newLogs, 0);
                    }
                    else
                    {
                        // Если коллекция null, очищаем состояние
                        state.CurrentCollection = null;
                    }
                }
            });
        }

        /// <summary>
        /// Обработчик изменения коллекции лог-сообщений
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы изменения коллекции</param>
        private static void LogMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is not ObservableCollection<LogMessage> logMessages) return;

            foreach (var entry in _editorStates)
            {
                if (entry.Value.CurrentCollection == logMessages)
                {
                    UpdateTextEditor(_textEditor, logMessages, entry.Value.LastProcessedCount);
                    entry.Value.LastProcessedCount = logMessages.Count;
                    break;
                }
            }
        }

        /// <summary>
        /// Очищает TextEditor и связанные с ним ресурсы
        /// </summary>
        public static void ClearTextEditor()
        {
            if (_textEditor == null) return;

            var document = _textEditor.Document;
            if (document == null) return;

            var state = _editorStates.GetOrCreateValue(_textEditor);

            document.Text = string.Empty;

            state.Transformer?.ClearAll();

            state.LastProcessedCount = 0;
        }

        /// <summary>
        /// Обновляет содержимое TextEditor новыми сообщениями из лога
        /// </summary>
        /// <param name="te">Экземпляр TextEditor</param>
        /// <param name="logMessages">Коллекция лог-сообщений</param>
        /// <param name="startIndex">Индекс, с которого начинать обновление</param>
        public static void UpdateTextEditor(TextEditor te, ObservableCollection<LogMessage> logMessages, int startIndex)
        {
            if (te == null || startIndex >= logMessages.Count)
                return;

            var document = te.Document ??= new TextDocument();
            var state = _editorStates.GetOrCreateValue(te);

            const int MaxLines = 1000;

            // Ограничение количества строк в редакторе
            if (document.LineCount > MaxLines)
            {
                var linesToRemove = document.LineCount - MaxLines;
                var removeUpToLine = document.GetLineByNumber(linesToRemove);

                string removedText = document.GetText(0, removeUpToLine.TotalLength);
                int newlineCount = removedText.Count(c => c == '\n');

                document.Remove(0, removeUpToLine.TotalLength);
                state.Transformer?.ShiftLineNumbers(-newlineCount);
            }

            using (document.RunUpdate())
            {
                document.UndoStack.SizeLimit = 0;
                state.Transformer ??= InitTransformer(te);

                var startOffset = document.TextLength;
                for (int i = startIndex; i < logMessages.Count; i++)
                {
                    var msg = logMessages[i];
                    if (!string.IsNullOrEmpty(msg.Text))
                    {
                        // Разбиваем текст на строки
                        var lines = msg.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        var color = msg.Color;

                        foreach (var line in lines)
                        {
                            if (string.IsNullOrEmpty(line)) continue;

                            // Добавляем каждую строку отдельно
                            document.Insert(document.TextLength, line + Environment.NewLine);

                            // Получаем номер строки для текущего сообщения
                            var currentLine = document.GetLineByOffset(startOffset);
                            state.Transformer.AddColoredLine(currentLine.LineNumber, color);

                            startOffset += line.Length + Environment.NewLine.Length;
                        }
                    }
                }

                te.ScrollToLine(document.LineCount);
            }
        }

        /// <summary>
        /// Инициализирует трансформер для раскраски текста
        /// </summary>
        /// <param name="te">Экземпляр TextEditor</param>
        /// <returns>Инициализированный трансформер</returns>
        private static SharedColorizingTransformer InitTransformer(TextEditor te)
        {
            var transformer = new SharedColorizingTransformer();
            te.TextArea.TextView.LineTransformers.Add(transformer);
            return transformer;
        }

        /// <summary>
        /// Кастомный трансформер для раскраски строк текста
        /// </summary>
        private class SharedColorizingTransformer : DocumentColorizingTransformer
        {
            private readonly Dictionary<int, IBrush> _lineColors = new();
            private readonly Dictionary<Color, IBrush> _brushCache = new();
            private const int MaxLineColors = 1000;

            /// <summary>
            /// Очищает все сохраненные цвета строк и кэш кистей
            /// </summary>
            public void ClearAll()
            {
                _lineColors.Clear();
                _brushCache.Clear();
            }

            /// <summary>
            /// Удаляет информацию о цветах для строк до указанного номера
            /// </summary>
            /// <param name="lineNumber">Номер строки</param>
            public void RemoveLinesBefore(int lineNumber)
            {
                var keysToRemove = _lineColors.Keys.Where(k => k < lineNumber).ToList();
                foreach (var key in keysToRemove)
                {
                    _lineColors.Remove(key);
                }

                var updatedEntries = new Dictionary<int, IBrush>();
                foreach (var entry in _lineColors)
                {
                    updatedEntries[entry.Key - lineNumber + 1] = entry.Value;
                }
                _lineColors.Clear();
                foreach (var entry in updatedEntries)
                {
                    _lineColors[entry.Key] = entry.Value;
                }
            }

            /// <summary>
            /// Добавляет информацию о цвете для указанной строки
            /// </summary>
            /// <param name="lineNumber">Номер строки</param>
            /// <param name="color">Цвет текста</param>
            public void AddColoredLine(int lineNumber, Color color)
            {
                if (lineNumber <= 0) return;

                if (_lineColors.Count >= MaxLineColors)
                {
                    var oldestKey = _lineColors.Keys.Min();
                    _lineColors.Remove(oldestKey);
                }

                if (!_brushCache.TryGetValue(color, out var brush))
                {
                    brush = new ImmutableSolidColorBrush(color);
                    _brushCache[color] = brush;
                }

                _lineColors[lineNumber] = brush;
            }

            /// <summary>
            /// Очищает информацию о цветах строк
            /// </summary>
            public void ClearColoredLines()
            {
                _lineColors.Clear();
                if (_brushCache.Count > 20)
                {
                    _brushCache.Clear();
                }
            }

            /// <summary>
            /// Сдвигает номера строк на указанное количество
            /// </summary>
            /// <param name="shiftAmount">Величина сдвига</param>
            public void ShiftLineNumbers(int shiftAmount)
            {
                var newLineColors = new Dictionary<int, IBrush>();

                foreach (var entry in _lineColors)
                {
                    int newLineNumber = entry.Key + shiftAmount;
                    if (newLineNumber > 0)
                    {
                        newLineColors[newLineNumber] = entry.Value;
                    }
                }

                _lineColors.Clear();
                foreach (var entry in newLineColors)
                {
                    _lineColors[entry.Key] = entry.Value;
                }
            }

            /// <summary>
            /// Применяет цвет к указанной строке
            /// </summary>
            /// <param name="line">Строка документа</param>
            protected override void ColorizeLine(DocumentLine line)
            {
                if (_lineColors.TryGetValue(line.LineNumber, out var color))
                {
                    ChangeLinePart(line.Offset, line.EndOffset, element =>
                    {
                        element.TextRunProperties.SetForegroundBrush(color);
                    });
                }
            }
        }

        /// <summary>
        /// Устанавливает коллекцию лог-сообщений для TextEditor
        /// </summary>
        /// <param name="element">TextEditor</param>
        /// <param name="value">Коллекция лог-сообщений</param>
        public static void SetColorLog(AvaloniaObject element, ObservableCollection<LogMessage> value)
        {
            element.SetValue(ColorLogProperty, value);
        }

        /// <summary>
        /// Получает коллекцию лог-сообщений из TextEditor
        /// </summary>
        /// <param name="element">TextEditor</param>
        /// <returns>Коллекция лог-сообщений</returns>
        public static ObservableCollection<LogMessage> GetColorLog(AvaloniaObject element)
        {
            return (ObservableCollection<LogMessage>)element.GetValue(ColorLogProperty);
        }
    }
}