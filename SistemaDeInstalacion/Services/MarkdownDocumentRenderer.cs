using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using System.Globalization;

namespace ConcesionaroCarros.Services
{
    public static class MarkdownDocumentRenderer
    {
        public static FlowDocument Crear(
            string markdown,
            Func<string, bool> linkHandler = null,
            string documentDirectory = null,
            MarkdownRenderTheme theme = null)
        {
            theme = theme ?? MarkdownRenderTheme.CreateForCurrentSystemTheme();

            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                PagePadding = new Thickness(26),
                TextAlignment = TextAlignment.Left,
                LineHeight = 24,
                Background = theme.DocumentBackground,
                Foreground = theme.DocumentForeground
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
                        AgregarBloqueCodigo(doc, codeBuilder.ToString().TrimEnd(), theme);
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
                        AgregarBloqueCodigo(doc, tableBuilder.ToString().TrimEnd(), theme);
                        tableBuilder.Clear();
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (EsComentarioHtml(trimmed))
                    continue;

                if (trimmed == "---")
                {
                    doc.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Border
                    {
                        Height = 1,
                        Background = theme.SeparatorBrush,
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

                        AgregarTitulo(doc, text, size, margin, theme);
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
                    AgregarLista(doc, bullets, false, linkHandler, theme);
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
                    AgregarLista(doc, numerados, true, linkHandler, theme);
                    continue;
                }

                if (EsMarcadorPantallazo(trimmed))
                {
                    AgregarMarcadorPantallazo(doc, trimmed, theme);
                    continue;
                }

                if (EsLineaImagen(trimmed))
                {
                    AgregarImagen(doc, trimmed, documentDirectory, theme);
                    continue;
                }

                AgregarParrafo(doc, trimmed, linkHandler, theme);
            }

            if (codeBuilder.Length > 0)
                AgregarBloqueCodigo(doc, codeBuilder.ToString().TrimEnd(), theme);

            if (doc.Blocks.Count == 0)
                doc.Blocks.Add(new Paragraph(new Run(
                    LocalizedText.Get("Help_NoDocumentContentMessage", "No hay documentación disponible para mostrar."))));

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

        private static bool EsLineaImagen(string trimmed)
        {
            return Regex.IsMatch(trimmed ?? string.Empty, @"^!\[[^\]]*\]\([^\)]+\)$") ||
                   Regex.IsMatch(trimmed ?? string.Empty, "^<img\\s+[^>]*src\\s*=\\s*[\"'][^\"']+[\"'][^>]*>\\s*$", RegexOptions.IgnoreCase);
        }

        private static bool EsComentarioHtml(string trimmed)
        {
            return Regex.IsMatch(trimmed ?? string.Empty, @"^<!--.*-->$");
        }

        private static string LimpiarNumerado(string trimmed)
        {
            return Regex.Replace(trimmed, @"^\d+\.\s+", string.Empty).Trim();
        }

        private static void AgregarTitulo(FlowDocument doc, string texto, double size, Thickness margin, MarkdownRenderTheme theme)
        {
            doc.Blocks.Add(new Paragraph(new Run(texto))
            {
                Tag = HelpAnchorNormalizer.Normalizar(texto),
                FontSize = size,
                FontWeight = FontWeights.Bold,
                Foreground = theme.HeadingBrush,
                Margin = margin
            });
        }

        private static void AgregarParrafo(FlowDocument doc, string texto, Func<string, bool> linkHandler, MarkdownRenderTheme theme)
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = theme.DocumentForeground
            };

            foreach (var inline in CrearInlines(texto, linkHandler, theme))
                paragraph.Inlines.Add(inline);

            doc.Blocks.Add(paragraph);
        }

        private static void AgregarMarcadorPantallazo(FlowDocument doc, string texto, MarkdownRenderTheme theme)
        {
            doc.Blocks.Add(new Paragraph(new Run(texto))
            {
                Margin = new Thickness(0, 6, 0, 14),
                Padding = new Thickness(10, 6, 10, 6),
                FontWeight = FontWeights.Bold,
                Foreground = theme.CalloutForeground,
                Background = theme.CalloutBackground,
                BorderBrush = theme.CalloutBorder,
                BorderThickness = new Thickness(1)
            });
        }

        private static void AgregarImagen(FlowDocument doc, string markdownImage, string documentDirectory, MarkdownRenderTheme theme)
        {
            var match = Regex.Match(markdownImage ?? string.Empty, @"^!\[([^\]]*)\]\(([^\)]+)\)$");
            var isHtmlImage = false;
            if (!match.Success)
            {
                match = Regex.Match(markdownImage ?? string.Empty, "^<img\\s+[^>]*src\\s*=\\s*[\"']([^\"']+)[\"'][^>]*(?:alt\\s*=\\s*[\"']([^\"']*)[\"'])?[^>]*>\\s*$", RegexOptions.IgnoreCase);
                isHtmlImage = match.Success;
            }

            if (!match.Success)
            {
                AgregarParrafo(doc, markdownImage, null, theme);
                return;
            }

            var altText = isHtmlImage
                ? match.Groups[2].Value.Trim()
                : match.Groups[1].Value.Trim();
            var imagePath = isHtmlImage
                ? match.Groups[1].Value.Trim()
                : match.Groups[2].Value.Trim();
            var resolvedPath = ResolveImagePath(imagePath, documentDirectory);

            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return;
            }

            try
            {
                const double maxDisplayWidth = 760d;
                const double maxDisplayHeight = 420d;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(resolvedPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                var preferredWidth = maxDisplayWidth;
                if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
                {
                    var aspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                    preferredWidth = Math.Min(maxDisplayWidth, maxDisplayHeight * aspectRatio);
                }

                var image = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform,
                    Width = preferredWidth,
                    MaxWidth = maxDisplayWidth,
                    MaxHeight = maxDisplayHeight,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 14)
                };

                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

                doc.Blocks.Add(new BlockUIContainer(image));

                if (!string.IsNullOrWhiteSpace(altText))
                {
                    doc.Blocks.Add(new Paragraph(new Run(altText))
                    {
                        Margin = new Thickness(0, -4, 0, 14),
                        FontStyle = FontStyles.Italic,
                        Foreground = theme.CaptionBrush
                    });
                }
            }
            catch
            {
                return;
            }
        }

        private static string ResolveImagePath(string imagePath, string documentDirectory)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return string.Empty;

            imagePath = imagePath.Trim();

            if (Uri.TryCreate(imagePath, UriKind.Absolute, out var absoluteUri))
                return absoluteUri.IsFile ? absoluteUri.LocalPath : string.Empty;

            try
            {
                if (Path.IsPathRooted(imagePath))
                    return Path.GetFullPath(imagePath);

                if (string.IsNullOrWhiteSpace(documentDirectory))
                    return string.Empty;

                return Path.GetFullPath(Path.Combine(documentDirectory, imagePath.Replace('/', Path.DirectorySeparatorChar)));
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is NotSupportedException ||
                ex is PathTooLongException ||
                ex is UriFormatException)
            {
                return string.Empty;
            }
        }

        private static void AgregarLista(FlowDocument doc, IEnumerable<string> items, bool numerada, Func<string, bool> linkHandler, MarkdownRenderTheme theme)
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
                    Foreground = theme.DocumentForeground
                };

                foreach (var inline in CrearInlines(item, linkHandler, theme))
                    paragraph.Inlines.Add(inline);

                list.ListItems.Add(new ListItem(paragraph));
            }

            doc.Blocks.Add(list);
        }

        private static void AgregarBloqueCodigo(FlowDocument doc, string texto, MarkdownRenderTheme theme)
        {
            var paragraph = new Paragraph(new Run(texto ?? string.Empty))
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Margin = new Thickness(0, 8, 0, 14),
                Padding = new Thickness(14),
                Background = theme.CodeBlockBackground,
                BorderBrush = theme.CodeBlockBorder,
                BorderThickness = new Thickness(1),
                LineHeight = 21,
                Foreground = theme.CodeForeground
            };

            doc.Blocks.Add(paragraph);
        }

        private static IEnumerable<Inline> CrearInlines(string texto, Func<string, bool> linkHandler, MarkdownRenderTheme theme)
        {
            var result = new List<Inline>();
            var pattern = "(`[^`]+`)|(\\*\\*.+?\\*\\*)|(\\[[^\\]]+\\]\\([^\\)]+\\))";
            var matches = Regex.Matches(texto ?? string.Empty, pattern);
            var lastIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                    AgregarTextoConNegrita(result, (texto ?? string.Empty).Substring(lastIndex, match.Index - lastIndex));

                var segment = match.Value;
                if (segment.StartsWith("`") && segment.EndsWith("`"))
                {
                    result.Add(new Run(segment.Substring(1, segment.Length - 2))
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = theme.InlineCodeBackground,
                        Foreground = theme.CodeForeground
                    });
                }
                else if (segment.StartsWith("**") && segment.EndsWith("**") && segment.Length > 4)
                {
                    result.Add(new Run(segment.Substring(2, segment.Length - 4))
                    {
                        FontWeight = FontWeights.Bold
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
                            Foreground = theme.HyperlinkBrush,
                            FontWeight = FontWeights.SemiBold,
                            TextDecorations = TextDecorations.Underline,
                            Cursor = Cursors.Hand
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
                AgregarTextoConNegrita(result, (texto ?? string.Empty).Substring(lastIndex));

            if (result.Count == 0)
                AgregarTextoConNegrita(result, texto ?? string.Empty);

            return result;
        }

        private static void AgregarTextoConNegrita(ICollection<Inline> inlines, string texto)
        {
            var value = texto ?? string.Empty;
            var matches = Regex.Matches(value, @"\*\*(.+?)\*\*");
            var lastIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                    inlines.Add(new Run(value.Substring(lastIndex, match.Index - lastIndex)));

                var boldText = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(boldText))
                    inlines.Add(new Bold(new Run(boldText)));

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < value.Length)
                inlines.Add(new Run(value.Substring(lastIndex)));

            if (matches.Count == 0 && value.Length > 0)
                inlines.Add(new Run(value));
        }
    }

    public sealed class MarkdownRenderTheme
    {
        public static MarkdownRenderTheme Light { get; } = CreateLightTheme();
        public static MarkdownRenderTheme Dark { get; } = CreateDarkTheme();

        public Brush DocumentBackground { get; }
        public Brush DocumentForeground { get; }
        public Brush HeadingBrush { get; }
        public Brush SeparatorBrush { get; }
        public Brush HyperlinkBrush { get; }
        public Brush InlineCodeBackground { get; }
        public Brush CodeBlockBackground { get; }
        public Brush CodeBlockBorder { get; }
        public Brush CodeForeground { get; }
        public Brush CaptionBrush { get; }
        public Brush CalloutForeground { get; }
        public Brush CalloutBackground { get; }
        public Brush CalloutBorder { get; }
        public Brush HelpNavigationPanelBrush { get; }
        public Brush HelpNavigationCardBrush { get; }
        public Brush HelpSurfaceBrush { get; }
        public Brush HelpPanelBrush { get; }
        public Brush HelpBorderBrush { get; }
        public Brush HelpTextBrush { get; }
        public Brush HelpMutedBrush { get; }
        public Brush HelpAccentBrush { get; }
        public Brush HelpAccentSoftBrush { get; }
        public Brush HelpToolbarHoverBrush { get; }
        public Brush HelpPlaceholderSurfaceBrush { get; }
        public Brush HelpPlaceholderBorderBrush { get; }
        public Brush HelpComboBoxBackgroundBrush { get; }
        public Brush HelpComboBoxHoverBrush { get; }
        public Brush HelpComboBoxSelectedBrush { get; }
        public Brush HelpComboBoxPopupBrush { get; }
        public bool IsDark { get; }

        private MarkdownRenderTheme(
            bool isDark,
            Brush documentBackground,
            Brush documentForeground,
            Brush headingBrush,
            Brush separatorBrush,
            Brush hyperlinkBrush,
            Brush inlineCodeBackground,
            Brush codeBlockBackground,
            Brush codeBlockBorder,
            Brush codeForeground,
            Brush captionBrush,
            Brush calloutForeground,
            Brush calloutBackground,
            Brush calloutBorder,
            Brush helpNavigationPanelBrush,
            Brush helpNavigationCardBrush,
            Brush helpSurfaceBrush,
            Brush helpPanelBrush,
            Brush helpBorderBrush,
            Brush helpTextBrush,
            Brush helpMutedBrush,
            Brush helpAccentBrush,
            Brush helpAccentSoftBrush,
            Brush helpToolbarHoverBrush,
            Brush helpPlaceholderSurfaceBrush,
            Brush helpPlaceholderBorderBrush,
            Brush helpComboBoxBackgroundBrush,
            Brush helpComboBoxHoverBrush,
            Brush helpComboBoxSelectedBrush,
            Brush helpComboBoxPopupBrush)
        {
            IsDark = isDark;
            DocumentBackground = documentBackground;
            DocumentForeground = documentForeground;
            HeadingBrush = headingBrush;
            SeparatorBrush = separatorBrush;
            HyperlinkBrush = hyperlinkBrush;
            InlineCodeBackground = inlineCodeBackground;
            CodeBlockBackground = codeBlockBackground;
            CodeBlockBorder = codeBlockBorder;
            CodeForeground = codeForeground;
            CaptionBrush = captionBrush;
            CalloutForeground = calloutForeground;
            CalloutBackground = calloutBackground;
            CalloutBorder = calloutBorder;
            HelpNavigationPanelBrush = helpNavigationPanelBrush;
            HelpNavigationCardBrush = helpNavigationCardBrush;
            HelpSurfaceBrush = helpSurfaceBrush;
            HelpPanelBrush = helpPanelBrush;
            HelpBorderBrush = helpBorderBrush;
            HelpTextBrush = helpTextBrush;
            HelpMutedBrush = helpMutedBrush;
            HelpAccentBrush = helpAccentBrush;
            HelpAccentSoftBrush = helpAccentSoftBrush;
            HelpToolbarHoverBrush = helpToolbarHoverBrush;
            HelpPlaceholderSurfaceBrush = helpPlaceholderSurfaceBrush;
            HelpPlaceholderBorderBrush = helpPlaceholderBorderBrush;
            HelpComboBoxBackgroundBrush = helpComboBoxBackgroundBrush;
            HelpComboBoxHoverBrush = helpComboBoxHoverBrush;
            HelpComboBoxSelectedBrush = helpComboBoxSelectedBrush;
            HelpComboBoxPopupBrush = helpComboBoxPopupBrush;
        }

        public static MarkdownRenderTheme CreateForCurrentSystemTheme()
        {
            try
            {
                var resolvedTheme = ThemeManager.ResolveTheme(ThemeManager.CurrentPreference);
                return string.Equals(resolvedTheme, ThemeManager.DarkMode, StringComparison.Ordinal)
                    ? Dark
                    : Light;
            }
            catch
            {
            }

            return Light;
        }

        public static MarkdownRenderTheme FromBrushes(Brush backgroundBrush, Brush foregroundBrush)
        {
            var background = TryGetColor(backgroundBrush);
            var foreground = TryGetColor(foregroundBrush);

            if (background.HasValue)
                return GetByBackground(background.Value);

            if (foreground.HasValue)
                return GetByForeground(foreground.Value);

            return Light;
        }

        private static MarkdownRenderTheme GetByBackground(Color color)
        {
            return ComputePerceivedLuminance(color) < 0.5 ? Dark : Light;
        }

        private static MarkdownRenderTheme GetByForeground(Color color)
        {
            return ComputePerceivedLuminance(color) > 0.6 ? Dark : Light;
        }

        private static double ComputePerceivedLuminance(Color color)
        {
            return ((0.299d * color.R) + (0.587d * color.G) + (0.114d * color.B)) / 255d;
        }

        private static Color? TryGetColor(Brush brush)
        {
            return brush is SolidColorBrush solidBrush ? solidBrush.Color : (Color?)null;
        }

        private static MarkdownRenderTheme CreateLightTheme()
        {
            return new MarkdownRenderTheme(
                false,
                CreateBrush(Colors.Transparent),
                CreateBrush(Color.FromRgb(47, 63, 82)),
                CreateBrush(Color.FromRgb(35, 79, 155)),
                CreateBrush(Color.FromRgb(214, 223, 235)),
                CreateBrush(Color.FromRgb(24, 58, 110)),
                CreateBrush(Color.FromRgb(233, 239, 246)),
                CreateBrush(Color.FromRgb(244, 247, 251)),
                CreateBrush(Color.FromRgb(213, 223, 236)),
                CreateBrush(Color.FromRgb(36, 57, 87)),
                CreateBrush(Color.FromRgb(107, 126, 149)),
                CreateBrush(Color.FromRgb(201, 77, 77)),
                CreateBrush(Color.FromRgb(255, 244, 244)),
                CreateBrush(Color.FromRgb(237, 196, 196)),
                CreateBrush(Color.FromRgb(247, 250, 252)),
                CreateBrush(Color.FromRgb(252, 253, 254)),
                CreateBrush(Color.FromRgb(255, 255, 255)),
                CreateBrush(Color.FromRgb(245, 248, 251)),
                CreateBrush(Color.FromRgb(214, 223, 235)),
                CreateBrush(Color.FromRgb(47, 63, 82)),
                CreateBrush(Color.FromRgb(107, 126, 149)),
                CreateBrush(Color.FromRgb(35, 79, 155)),
                CreateBrush(Color.FromRgb(232, 240, 252)),
                CreateBrush(Color.FromRgb(235, 242, 248)),
                CreateBrush(Color.FromRgb(248, 251, 253)),
                CreateBrush(Color.FromRgb(230, 238, 245)),
                CreateBrush(Color.FromRgb(243, 247, 251)),
                CreateBrush(Color.FromRgb(231, 239, 246)),
                CreateBrush(Color.FromRgb(220, 232, 241)),
                CreateBrush(Color.FromRgb(238, 244, 248)));
        }

        private static MarkdownRenderTheme CreateDarkTheme()
        {
            return new MarkdownRenderTheme(
                true,
                CreateBrush(Colors.Transparent),
                CreateBrush(Color.FromRgb(226, 233, 242)),
                CreateBrush(Color.FromRgb(150, 187, 255)),
                CreateBrush(Color.FromRgb(67, 81, 104)),
                CreateBrush(Color.FromRgb(138, 182, 255)),
                CreateBrush(Color.FromRgb(49, 59, 76)),
                CreateBrush(Color.FromRgb(35, 43, 56)),
                CreateBrush(Color.FromRgb(70, 85, 107)),
                CreateBrush(Color.FromRgb(224, 232, 243)),
                CreateBrush(Color.FromRgb(156, 171, 194)),
                CreateBrush(Color.FromRgb(255, 170, 170)),
                CreateBrush(Color.FromRgb(74, 39, 45)),
                CreateBrush(Color.FromRgb(121, 72, 82)),
                CreateBrush(Color.FromRgb(23, 30, 39)),
                CreateBrush(Color.FromRgb(29, 37, 48)),
                CreateBrush(Color.FromRgb(21, 28, 36)),
                CreateBrush(Color.FromRgb(27, 35, 45)),
                CreateBrush(Color.FromRgb(52, 65, 82)),
                CreateBrush(Color.FromRgb(225, 233, 243)),
                CreateBrush(Color.FromRgb(159, 176, 194)),
                CreateBrush(Color.FromRgb(143, 186, 255)),
                CreateBrush(Color.FromRgb(35, 48, 65)),
                CreateBrush(Color.FromRgb(39, 49, 62)),
                CreateBrush(Color.FromRgb(24, 32, 41)),
                CreateBrush(Color.FromRgb(61, 76, 93)),
                CreateBrush(Color.FromRgb(27, 35, 45)),
                CreateBrush(Color.FromRgb(39, 54, 70)),
                CreateBrush(Color.FromRgb(52, 65, 82)),
                CreateBrush(Color.FromRgb(26, 34, 44)));
        }

        private static SolidColorBrush CreateBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }

    internal static class HelpAnchorNormalizer
    {
        public static string Normalizar(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = SafeUnescapeDataString(value.Trim());
            value = RepararTextoMalCodificado(value);

            var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            var lastWasDash = false;

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    lastWasDash = false;
                }
                else if (!lastWasDash)
                {
                    builder.Append('-');
                    lastWasDash = true;
                }
            }

            return builder.ToString().Trim('-');
        }

        private static string SafeUnescapeDataString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value ?? string.Empty;

            try
            {
                return Uri.UnescapeDataString(value);
            }
            catch (UriFormatException)
            {
                return value;
            }
        }

        private static string RepararTextoMalCodificado(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value ?? string.Empty;

            if (value.IndexOf("Ãƒ") < 0 && value.IndexOf("Ã‚") < 0)
                return value;

            try
            {
                var bytes = Encoding.GetEncoding(1252).GetBytes(value);
                var repaired = Encoding.UTF8.GetString(bytes);
                return string.IsNullOrWhiteSpace(repaired) ? value : repaired;
            }
            catch
            {
                return value;
            }
        }
    }
}

