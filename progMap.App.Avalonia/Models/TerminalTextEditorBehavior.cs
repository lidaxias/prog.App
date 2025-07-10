using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit;
using Rss.TmFramework.Controls.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progMap.App.Avalonia.Models
{
    /// <summary>
    /// Поведение для TerminalEditor
    /// </summary>
    public class TerminalTextEditorBehavior
    {
        public static int SavedCaretOffset { get; set; }
        private static TextEditor _textEditor;

        // Параметры RegisterAttached: <Владелец, Хост, Значение>
        public static readonly AvaloniaProperty<string> BindableTextProperty =
            AvaloniaProperty.RegisterAttached<TerminalTextEditorBehavior, TextEditor, string>("BindableText");

        static TerminalTextEditorBehavior()
        {
            // Каждый раз когда значение BindableText изменится, будет выполняться код в обработчике изменений

            BindableTextProperty.Changed.Subscribe(e =>
            {
                // Если отправитель - TerminalTextEditorControl, то отписывается от изменений
                if (e.Sender is TextEditor terminalTextEditor)
                {
                    _textEditor = terminalTextEditor;
                    if (_textEditor == null) return;

                    _textEditor.PropertyChanged += TerminalTextEditorPropertyChanged;

                    if (e.NewValue.Value != null)
                    {
                        // Устанавливается новое значение текста
                        _textEditor.Text = e.NewValue.Value;
                        // Подписываемся обратно
                        _textEditor.TextChanged += TextEditorOnTextChanged;
                    }

                        _textEditor.ScrollToLine(_textEditor.LineCount);
                        _textEditor.CaretOffset = _textEditor.Text.Length;
                }
            });
        }

        private static void TerminalTextEditorPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is TextEditor textEditor)
            {
                if (textEditor == null) return;

                if (e.Property.Name == "IsPointerOver" || e.Property.Name == "IsKeyboardFocusWithin") return;

                    textEditor.ScrollToLine(textEditor.LineCount);
                    textEditor.CaretOffset = textEditor.Text.Length;
            }
        }


        /// <summary>
        /// Вызывается когда текст изменяется
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void TextEditorOnTextChanged(object? sender, EventArgs e)
        {
            if (sender is TextEditor textEditor)
            {
                SetBindableText(textEditor, textEditor.Text);
                _textEditor.CaretOffset = _textEditor.Text.Length;
            }
        }

        /// <summary>
        /// Обновляет значение BindableText (а значит, и значения, связанного через привязку)
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetBindableText(AvaloniaObject element, string value)
        {
            element.SetValue(BindableTextProperty, value);
        }

        /// <summary>
        /// Позволяет получить текущее значение BindableText
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static string GetBindableText(AvaloniaObject element)
        {
            return element.GetValue(BindableTextProperty).ToString();
        }
    }

    public class TextEditorState
    {
        public int Offset { get; set; }  
        public double VerticalScroll { get; set; }  
        public double HorizontalScroll { get; set; }  

    }
}

