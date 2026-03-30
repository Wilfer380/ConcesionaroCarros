using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;

namespace ConcesionaroCarros.Services
{
    public static class GlobalCopyContextService
    {
        private const string CopyMenuTag = "__global_copy_menu_item";

        public static void Register()
        {
            EventManager.RegisterClassHandler(typeof(TextBox), FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnTextBoxContextMenuOpening));
            EventManager.RegisterClassHandler(typeof(RichTextBox), FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnRichTextBoxContextMenuOpening));
            EventManager.RegisterClassHandler(typeof(TextBlock), FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnTextBlockContextMenuOpening));
            EventManager.RegisterClassHandler(typeof(DataGridCell), FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnDataGridCellContextMenuOpening));
        }

        private static void OnTextBoxContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            EnsureCopyMenuItem(
                textBox,
                "Copiar",
                () =>
                {
                    var textToCopy = string.IsNullOrWhiteSpace(textBox.SelectedText) ? textBox.Text : textBox.SelectedText;
                    CopyText(textToCopy);
                },
                !string.IsNullOrWhiteSpace(textBox.Text));
        }

        private static void OnRichTextBoxContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var richTextBox = sender as RichTextBox;
            if (richTextBox == null)
            {
                return;
            }

            EnsureCopyMenuItem(
                richTextBox,
                "Copiar",
                () =>
                {
                    var selectedText = new TextRange(richTextBox.Selection.Start, richTextBox.Selection.End).Text;
                    if (string.IsNullOrWhiteSpace(selectedText))
                    {
                        selectedText = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
                    }

                    CopyText(selectedText);
                },
                !string.IsNullOrWhiteSpace(new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text));
        }

        private static void OnTextBlockContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock == null)
            {
                return;
            }

            EnsureCopyMenuItem(
                textBlock,
                "Copiar",
                () => CopyText(textBlock.Text),
                !string.IsNullOrWhiteSpace(textBlock.Text));
        }

        private static void OnDataGridCellContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var cell = sender as DataGridCell;
            if (cell == null)
            {
                return;
            }

            var text = ExtractCellText(cell);
            EnsureCopyMenuItem(
                cell,
                "Copiar celda",
                () => CopyText(text),
                !string.IsNullOrWhiteSpace(text));
        }

        private static void EnsureCopyMenuItem(FrameworkElement element, string header, Action copyAction, bool isEnabled)
        {
            var menu = element.ContextMenu ?? new ContextMenu();
            element.ContextMenu = menu;

            MenuItem existingItem = null;
            MenuItem existingCopyItem = null;
            foreach (var item in menu.Items)
            {
                var menuItem = item as MenuItem;
                if (menuItem == null)
                {
                    continue;
                }

                if (Equals(menuItem.Tag, CopyMenuTag))
                {
                    existingItem = menuItem;
                    break;
                }

                var itemHeader = menuItem.Header as string;
                if (string.Equals(itemHeader, "Copiar", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(itemHeader, "Copiar celda", StringComparison.OrdinalIgnoreCase) ||
                    menuItem.Command == ApplicationCommands.Copy)
                {
                    existingCopyItem = menuItem;
                }
            }

            if (existingItem == null && existingCopyItem != null)
            {
                existingCopyItem.IsEnabled = isEnabled;
                return;
            }

            if (existingItem == null)
            {
                existingItem = new MenuItem
                {
                    Header = header,
                    Tag = CopyMenuTag
                };

                existingItem.Click += delegate { copyAction(); };
                menu.Items.Add(existingItem);
            }

            existingItem.Header = header;
            existingItem.IsEnabled = isEnabled;
        }

        private static string ExtractCellText(DataGridCell cell)
        {
            var textBlock = cell.Content as TextBlock;
            if (textBlock != null)
            {
                return textBlock.Text;
            }

            var presenter = cell.Content as ContentPresenter;
            var presenterTextBlock = presenter != null ? presenter.Content as TextBlock : null;
            if (presenterTextBlock != null)
            {
                return presenterTextBlock.Text;
            }

            return cell.Content != null ? cell.Content.ToString() : string.Empty;
        }

        private static void CopyText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Clipboard.SetText(text.Trim());
        }
    }
}
