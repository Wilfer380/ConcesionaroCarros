using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ConcesionaroCarros.Services
{
    public static class MarkdownDocumentRenderer
    {
        public static FlowDocument Crear(string markdown, Func<string, bool> linkHandler = null)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                PagePadding = new Thickness(26),
                TextAlignment = TextAlignment.Left,
                LineHeight = 24
            };

            var lines = (markdown ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');

            var codeBuilder = new StringBuilder();
            var tableBuilder = new StringBuilder();
            var inCodeBlock = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = (lines[i] ?? string.Empty).TrimStart('\uFEFF');
                var trimmed = line.Trim();

                if (trimmed.StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        AgregarBloqueCodigo(doc, codeBuilder.ToString().TrimEnd());
                        codeBuilder.Clear();
                        inCodeBlock = false;
                    }
                    else
                    {
                        inCodeBlock = true;
                    }

                    continue;
                }

                if (inCodeBlock)
                {
                    codeBuilder.AppendLine(line);
                    continue;
                }

                if (EsLineaTabla(trimmed))
                {
                    tableBuilder.AppendLine(line);
                    var isLastTableLine = i == lines.Length - 1 || !EsLineaTabla((lines[i + 1] ?? string.Empty).Trim());
                    if (isLastTableLine)
                    {
                        AgregarBloqueCodigo(doc, tableBuilder.ToString().TrimEnd());
                        tableBuilder.Clear();
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (trimmed == "---")
                {
                    doc.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(Color.FromRgb(214, 223, 235)),
                        Margin = new Thickness(0, 14, 0, 18)
                    }));
                    continue;
                }

                var headingMatch = Regex.Match(trimmed, @"^(#{1,6})\s*(.+?)\s*#*\s*$");
                if (headingMatch.Success)
                {
                    var text = headingMatch.Groups[2].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var level = headingMatch.Groups[1].Value.Length;
                        var size = level == 1 ? 28 :
                            level == 2 ? 21 :
                            level == 3 ? 17 :
                            level == 4 ? 15 :
                            level == 5 ? 14 :
                            13;
                        var margin = level == 1
                            ? new Thickness(0, 20, 0, 12)
                            : new Thickness(0, 12, 0, 8);

                        AgregarTitulo(doc, text, size, margin);
                        continue;
                    }
                }

                if (EsBullet(trimmed))
                {
                    var bullets = new List<string>();
                    while (i < lines.Length && EsBullet((lines[i] ?? string.Empty).Trim()))
                    {
                        bullets.Add(LimpiarBullet((lines[i] ?? string.Empty).Trim()));
                        i++;
                    }

                    i--;
                    AgregarLista(doc, bullets, false, linkHandler);
                    continue;
                }

                if (EsNumero(trimmed))
                {
                    var numerados = new List<string>();
                    while (i < lines.Length && EsNumero((lines[i] ?? string.Empty).Trim()))
                    {
                        numerados.Add(LimpiarNumerado((lines[i] ?? string.Empty).Trim()));
                        i++;
                    }

                    i--;
                    AgregarLista(doc, numerados, true, linkHandler);
                    continue;
                }

                if (EsMarcadorPantallazo(trimmed))
                {
                    AgregarMarcadorPantallazo(doc, trimmed);
                    continue;
                }

                AgregarParrafo(doc, trimmed, linkHandler);
            }

            if (codeBuilder.Length > 0)
                AgregarBloqueCodigo(doc, codeBuilder.ToString().TrimEnd());

            if (doc.Blocks.Count == 0)
                doc.Blocks.Add(new Paragraph(new Run("No hay documentación disponible para mostrar.")));

            return doc;
        }

        private static bool EsLineaTabla(string trimmed)
        {
            return trimmed.StartsWith("|");
        }

        private static bool EsBullet(string trimmed)
        {
            return trimmed.StartsWith("- ") || trimmed.StartsWith("* ");
        }

        private static string LimpiarBullet(string trimmed)
        {
            return trimmed.Length > 2 ? trimmed.Substring(2).Trim() : string.Empty;
        }

        private static bool EsNumero(string trimmed)
        {
            return Regex.IsMatch(trimmed, @"^\d+\.\s+");
        }

        private static bool EsMarcadorPantallazo(string trimmed)
        {
            var value = (trimmed ?? string.Empty);
            return value.StartsWith("Aqui va un pantallazo:", StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("Historia de pantallazo:", StringComparison.OrdinalIgnoreCase);
        }

        private static string LimpiarNumerado(string trimmed)
        {
            return Regex.Replace(trimmed, @"^\d+\.\s+", string.Empty).Trim();
        }

        private static void AgregarTitulo(FlowDocument doc, string texto, double size, Thickness margin)
        {
            doc.Blocks.Add(new Paragraph(new Run(texto))
            {
                FontSize = size,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(35, 79, 155)),
                Margin = margin
            });
        }

        private static void AgregarParrafo(FlowDocument doc, string texto, Func<string, bool> linkHandler)
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(47, 63, 82))
            };

            foreach (var inline in CrearInlines(texto, linkHandler))
                paragraph.Inlines.Add(inline);

            doc.Blocks.Add(paragraph);
        }

        private static void AgregarMarcadorPantallazo(FlowDocument doc, string texto)
        {
            doc.Blocks.Add(new Paragraph(new Run(texto))
            {
                Margin = new Thickness(0, 6, 0, 14),
                Padding = new Thickness(10, 6, 10, 6),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(201, 77, 77)),
                Background = new SolidColorBrush(Color.FromRgb(255, 244, 244)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(237, 196, 196)),
                BorderThickness = new Thickness(1)
            });
        }

        private static void AgregarLista(FlowDocument doc, IEnumerable<string> items, bool numerada, Func<string, bool> linkHandler)
        {
            var list = new List
            {
                MarkerStyle = numerada ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
                Margin = new Thickness(20, 0, 0, 10)
            };

            foreach (var item in items ?? Enumerable.Empty<string>())
            {
                var paragraph = new Paragraph
                {
                    Margin = new Thickness(0, 0, 0, 4),
                    Foreground = new SolidColorBrush(Color.FromRgb(47, 63, 82))
                };

                foreach (var inline in CrearInlines(item, linkHandler))
                    paragraph.Inlines.Add(inline);

                list.ListItems.Add(new ListItem(paragraph));
            }

            doc.Blocks.Add(list);
        }

        private static void AgregarBloqueCodigo(FlowDocument doc, string texto)
        {
            var paragraph = new Paragraph(new Run(texto ?? string.Empty))
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Margin = new Thickness(0, 8, 0, 14),
                Padding = new Thickness(14),
                Background = new SolidColorBrush(Color.FromRgb(244, 247, 251)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(213, 223, 236)),
                BorderThickness = new Thickness(1),
                LineHeight = 21
            };

            doc.Blocks.Add(paragraph);
        }

        private static IEnumerable<Inline> CrearInlines(string texto, Func<string, bool> linkHandler)
        {
            var result = new List<Inline>();
            var pattern = "(`[^`]+`)|(\\[[^\\]]+\\]\\([^\\)]+\\))";
            var matches = Regex.Matches(texto ?? string.Empty, pattern);
            var lastIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                    result.Add(new Run((texto ?? string.Empty).Substring(lastIndex, match.Index - lastIndex)));

                var segment = match.Value;
                if (segment.StartsWith("`") && segment.EndsWith("`"))
                {
                    result.Add(new Run(segment.Substring(1, segment.Length - 2))
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = new SolidColorBrush(Color.FromRgb(233, 239, 246)),
                        Foreground = new SolidColorBrush(Color.FromRgb(36, 57, 87))
                    });
                }
                else
                {
                    var linkMatch = Regex.Match(segment, @"^\[([^\]]+)\]\(([^\)]+)\)$");
                    if (linkMatch.Success)
                    {
                        var text = linkMatch.Groups[1].Value;
                        var target = linkMatch.Groups[2].Value;

                        var hyperlink = new Hyperlink(new Run(text))
                        {
                            Foreground = new SolidColorBrush(Color.FromRgb(35, 79, 155)),
                            TextDecorations = TextDecorations.Underline
                        };

                        hyperlink.Click += (_, __) =>
                        {
                            linkHandler?.Invoke(target);
                        };

                        result.Add(hyperlink);
                    }
                    else
                    {
                        result.Add(new Run(segment));
                    }
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < (texto ?? string.Empty).Length)
                result.Add(new Run((texto ?? string.Empty).Substring(lastIndex)));

            if (result.Count == 0)
                result.Add(new Run(texto ?? string.Empty));

            return result;
        }
    }
}
