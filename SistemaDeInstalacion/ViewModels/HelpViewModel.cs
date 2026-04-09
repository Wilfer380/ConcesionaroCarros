using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class HelpViewModel : BaseViewModel
    {
        private readonly DocumentationService _documentationService = new DocumentationService();
        private DocumentationDocumentItem _selectedDocument;
        private FlowDocument _selectedDocumentFlow;
        private string _selectedDocumentTitle;
        private string _selectedDocumentPath;
        private string _selectedDocumentContent;
        private string _selectedDocumentAnchor;
        private string _emptyStateTitle;
        private string _emptyStateDescription;
        private string _emptyStateHint;
        private HelpSectionNavigationOption _selectedSectionOption;
        private HelpDocumentNavigationState _pendingNavigationState;
        private readonly Dictionary<string, DocumentationDocumentItem> _documentsByDocId =
            new Dictionary<string, DocumentationDocumentItem>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HelpDocumentNavigationState> _navigationStatesByDocId =
            new Dictionary<string, HelpDocumentNavigationState>(StringComparer.OrdinalIgnoreCase);
        private readonly List<DocumentationSectionItem> _allSections = new List<DocumentationSectionItem>();
        private readonly List<HelpDocumentHistoryEntry> _documentHistory = new List<HelpDocumentHistoryEntry>();
        private int _documentHistoryIndex = -1;
        private bool _isUpdatingQuickAccessSelection;

        public ObservableCollection<HelpSectionNavigationOption> SectionOptions { get; } =
            new ObservableCollection<HelpSectionNavigationOption>();

        public bool EsAdministrador { get; }

        public string TituloAyuda => EsAdministrador
            ? "Centro de ayuda administrativo"
            : "Centro de ayuda para usuarios";

        public string SubtituloAyuda => EsAdministrador
            ? "Consulta toda la documentacion del sistema organizada por carpetas y documentos."
            : "Consulta unicamente la documentacion disponible para el usuario final.";

        public string CorreoSoporte => "wandica@weg.net";

        public HelpSectionNavigationOption SelectedSectionOption
        {
            get => _selectedSectionOption;
            set
            {
                if (ReferenceEquals(_selectedSectionOption, value))
                    return;

                _selectedSectionOption = value;
                OnPropertyChanged();

                if (!_isUpdatingQuickAccessSelection)
                    NavegarDesdeAccesoRapido(value);
            }
        }

        public bool CanNavigateBack => _documentHistoryIndex > 0;

        public bool CanNavigateForward =>
            _documentHistoryIndex >= 0 && _documentHistoryIndex < _documentHistory.Count - 1;

        public bool HasSelectedDocument => _selectedDocument != null;

        public string SelectedDocumentTitle
        {
            get => _selectedDocumentTitle;
            set
            {
                _selectedDocumentTitle = value;
                OnPropertyChanged();
            }
        }

        public string SelectedDocumentPath
        {
            get => _selectedDocumentPath;
            set
            {
                _selectedDocumentPath = value;
                OnPropertyChanged();
            }
        }

        public FlowDocument SelectedDocumentFlow
        {
            get => _selectedDocumentFlow;
            set
            {
                _selectedDocumentFlow = value;
                OnPropertyChanged();
            }
        }

        public string SelectedDocumentAnchor
        {
            get => _selectedDocumentAnchor;
            set
            {
                _selectedDocumentAnchor = value;
                OnPropertyChanged();
            }
        }

        public string EmptyStateTitle
        {
            get => _emptyStateTitle;
            set
            {
                _emptyStateTitle = value;
                OnPropertyChanged();
            }
        }

        public string EmptyStateDescription
        {
            get => _emptyStateDescription;
            set
            {
                _emptyStateDescription = value;
                OnPropertyChanged();
            }
        }

        public string EmptyStateHint
        {
            get => _emptyStateHint;
            set
            {
                _emptyStateHint = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateBackCommand { get; }
        public ICommand NavigateForwardCommand { get; }
        public HelpDocumentNavigationState PendingNavigationState => _pendingNavigationState;

        public HelpViewModel(bool esAdministrador)
        {
            EsAdministrador = esAdministrador;
            NavigateBackCommand = new RelayCommand(_ => NavegarHistorial(-1));
            NavigateForwardCommand = new RelayCommand(_ => NavegarHistorial(1));
            CargarDocumentacion();
        }

        private void CargarDocumentacion()
        {
            SectionOptions.Clear();
            _allSections.Clear();
            _documentsByDocId.Clear();
            _navigationStatesByDocId.Clear();
            _documentHistory.Clear();
            _documentHistoryIndex = -1;
            _pendingNavigationState = null;
            IReadOnlyList<DocumentationSectionDefinition> sections;

            try
            {
                sections = _documentationService.CargarSecciones(EsAdministrador);
            }
            catch (Exception ex)
            {
                LimpiarDocumentoSeleccionado();
                EmptyStateTitle = "No fue posible cargar la documentacion";
                EmptyStateDescription = "Se produjo un error al inspeccionar la carpeta de ayuda configurada para este perfil.";
                EmptyStateHint = "Detalle tecnico: " + ex.Message;
                return;
            }

            foreach (var section in sections)
            {
                var sectionItem = new DocumentationSectionItem
                {
                    Title = section.Title,
                    DisplayPath = section.DisplayPath,
                    Description = section.Description
                };

                foreach (var document in section.Documents ?? Array.Empty<DocumentationDocumentDefinition>())
                {
                    sectionItem.Documents.Add(new DocumentationDocumentItem
                    {
                        DocId = document.DocId,
                        Title = document.Title,
                        FileName = document.FileName,
                        FullPath = document.FullPath,
                        RelativePath = document.RelativePath
                    });
                }

                foreach (var document in sectionItem.Documents)
                {
                    if (!string.IsNullOrWhiteSpace(document.DocId) && !_documentsByDocId.ContainsKey(document.DocId))
                        _documentsByDocId.Add(document.DocId, document);
                }

                _allSections.Add(sectionItem);
            }

            CargarOpcionesDeSeccion();
            LimpiarDocumentoSeleccionado();

            if (_allSections.SelectMany(x => x.Documents).Any())
            {
                EmptyStateTitle = "Selecciona un documento";
                EmptyStateDescription = "Usa el desplegable de navegacion para abrir una guia, manual o procedimiento cuando lo necesites.";
                EmptyStateHint = "No se muestra contenido automaticamente para que elijas exactamente la documentacion que queres consultar.";
                return;
            }

            EmptyStateTitle = "Documentacion no disponible";
            EmptyStateDescription = "No se encontraron archivos de ayuda para este perfil en la carpeta configurada.";
            EmptyStateHint = "Revisa que la carpeta Docs exista en el directorio de ejecucion y contenga documentos habilitados para este usuario.";
        }

        private void SeleccionarDocumento(DocumentationDocumentItem document)
        {
            SeleccionarDocumento(document, null, true);
        }

        private void SeleccionarDocumento(DocumentationDocumentItem document, string anchor)
        {
            SeleccionarDocumento(document, anchor, true);
        }

        private void SeleccionarDocumento(DocumentationDocumentItem document, string anchor, bool registrarEnHistorial)
        {
            if (document == null)
                return;

            var normalizedAnchor = HelpAnchorNormalizer.Normalizar(anchor);
            _pendingNavigationState = CrearEstadoNavegacionPendiente(document, normalizedAnchor);
            OnPropertyChanged(nameof(PendingNavigationState));

            if (_selectedDocument != null)
                _selectedDocument.IsSelected = false;

            _selectedDocument = document;
            _selectedDocument.IsSelected = true;

            var contenido = LeerDocumento(document.FullPath);

            _selectedDocumentContent = contenido;
            SelectedDocumentTitle = document.Title;
            SelectedDocumentPath = ConstruirUriCanonica(document.DocId, _pendingNavigationState?.Anchor);
            SelectedDocumentAnchor = _pendingNavigationState?.Anchor ?? string.Empty;
            EmptyStateTitle = string.Empty;
            EmptyStateDescription = string.Empty;
            EmptyStateHint = string.Empty;
            ActualizarAccesoRapidoSeleccionado(document.DocId);
            RegenerarSelectedDocumentFlow();

            OnPropertyChanged(nameof(HasSelectedDocument));

            if (registrarEnHistorial)
                RegistrarDocumentoEnHistorial(document.DocId, _pendingNavigationState?.Anchor);
        }

        private void CargarOpcionesDeSeccion()
        {
            foreach (var section in _allSections)
            {
                foreach (var document in section.Documents)
                {
                    SectionOptions.Add(new HelpSectionNavigationOption
                    {
                        DocId = document.DocId,
                        DisplayName = ConstruirNombreAccesoRapido(section.Title, document.Title),
                        Description = document.RelativePath
                    });
                }
            }
        }

        private void LimpiarDocumentoSeleccionado()
        {
            if (_selectedDocument != null)
                _selectedDocument.IsSelected = false;

            _selectedDocument = null;
            _selectedDocumentContent = string.Empty;
            SelectedDocumentTitle = string.Empty;
            SelectedDocumentPath = string.Empty;
            SelectedDocumentAnchor = string.Empty;
            SelectedDocumentFlow = null;
            SelectedSectionOption = null;
            OnPropertyChanged(nameof(HasSelectedDocument));
            ActualizarEstadoNavegacion();
        }

        private static string ConstruirNombreAccesoRapido(string sectionTitle, string documentTitle)
        {
            var normalizedSectionTitle = NormalizarTitulo(sectionTitle);
            var normalizedDocumentTitle = NormalizarTitulo(documentTitle);

            if (string.IsNullOrWhiteSpace(normalizedSectionTitle))
                return documentTitle ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedDocumentTitle))
                return sectionTitle ?? string.Empty;

            if (string.Equals(
                normalizedSectionTitle,
                normalizedDocumentTitle,
                StringComparison.OrdinalIgnoreCase))
            {
                return sectionTitle;
            }

            return string.Format("{0} / {1}", sectionTitle, documentTitle);
        }

        private void NavegarDesdeAccesoRapido(HelpSectionNavigationOption selectedOption)
        {
            if (selectedOption == null || string.IsNullOrWhiteSpace(selectedOption.DocId))
                return;

            if (!_documentsByDocId.TryGetValue(selectedOption.DocId, out var document))
                return;

            SeleccionarDocumento(document);
        }

        private void RegistrarDocumentoEnHistorial(string docId, string anchor)
        {
            if (string.IsNullOrWhiteSpace(docId))
                return;

            var normalizedAnchor = HelpAnchorNormalizer.Normalizar(anchor);
            if (_documentHistoryIndex >= 0 &&
                _documentHistoryIndex < _documentHistory.Count)
            {
                var currentEntry = _documentHistory[_documentHistoryIndex];
                if (string.Equals(currentEntry.DocId, docId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(currentEntry.Anchor, normalizedAnchor, StringComparison.OrdinalIgnoreCase))
                {
                    ActualizarEstadoNavegacion();
                    return;
                }
            }

            if (_documentHistoryIndex < _documentHistory.Count - 1)
                _documentHistory.RemoveRange(_documentHistoryIndex + 1, _documentHistory.Count - _documentHistoryIndex - 1);

            _documentHistory.Add(new HelpDocumentHistoryEntry
            {
                DocId = docId,
                Anchor = normalizedAnchor
            });

            _documentHistoryIndex = _documentHistory.Count - 1;
            ActualizarEstadoNavegacion();
        }

        private void NavegarHistorial(int direction)
        {
            if (direction == 0)
                return;

            var nextIndex = _documentHistoryIndex + direction;
            if (nextIndex < 0 || nextIndex >= _documentHistory.Count)
            {
                ActualizarEstadoNavegacion();
                return;
            }

            var entry = _documentHistory[nextIndex];
            if (!_documentsByDocId.TryGetValue(entry.DocId, out var document))
            {
                ActualizarEstadoNavegacion();
                return;
            }

            _documentHistoryIndex = nextIndex;

            SeleccionarDocumento(document, entry.Anchor, false);
            ActualizarEstadoNavegacion();
        }

        private void ActualizarEstadoNavegacion()
        {
            OnPropertyChanged(nameof(CanNavigateBack));
            OnPropertyChanged(nameof(CanNavigateForward));
        }

        public void ActualizarEstadoVisualDocumentoActual(string anchor, double verticalOffset, double scrollableHeight)
        {
            if (_selectedDocument == null || string.IsNullOrWhiteSpace(_selectedDocument.DocId))
                return;

            var normalizedAnchor = HelpAnchorNormalizer.Normalizar(anchor);
            var safeScrollableHeight = scrollableHeight < 0 ? 0 : scrollableHeight;
            var safeVerticalOffset = verticalOffset < 0 ? 0 : verticalOffset;
            var ratio = safeScrollableHeight > 0
                ? Math.Max(0d, Math.Min(1d, safeVerticalOffset / safeScrollableHeight))
                : 0d;

            _navigationStatesByDocId[_selectedDocument.DocId] = new HelpDocumentNavigationState
            {
                DocId = _selectedDocument.DocId,
                Anchor = normalizedAnchor,
                VerticalOffset = safeVerticalOffset,
                ScrollableHeight = safeScrollableHeight,
                VerticalRatio = ratio
            };
        }

        public void RegenerarSelectedDocumentFlow(MarkdownRenderTheme theme = null)
        {
            if (_selectedDocument == null)
                return;

            _pendingNavigationState = CrearEstadoNavegacionPendiente(_selectedDocument, _selectedDocumentAnchor);
            OnPropertyChanged(nameof(PendingNavigationState));

            try
            {
                SelectedDocumentFlow = MarkdownDocumentRenderer.Crear(
                    RemoverTituloInicial(_selectedDocumentContent, _selectedDocument.Title),
                    ManejarEnlaceMarkdown,
                    ObtenerDirectorioDocumento(_selectedDocument.FullPath),
                    theme);
            }
            catch (Exception ex)
            {
                SelectedDocumentFlow = CrearDocumentoDeError(
                    "No fue posible renderizar el documento seleccionado.",
                    ex.Message);
            }
        }

        private static string ObtenerDirectorioDocumento(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return null;

            return Path.GetDirectoryName(fullPath);
        }

        private static FlowDocument CrearDocumentoDeError(string mensaje, string detalle)
        {
            var document = new FlowDocument();

            document.Blocks.Add(new Paragraph(new Run(mensaje ?? string.Empty))
            {
                FontSize = 18,
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(0, 0, 0, 12)
            });

            if (!string.IsNullOrWhiteSpace(detalle))
            {
                document.Blocks.Add(new Paragraph(new Run(detalle))
                {
                    Margin = new System.Windows.Thickness(0, 0, 0, 8)
                });
            }

            return document;
        }

        private HelpDocumentNavigationState CrearEstadoNavegacionPendiente(DocumentationDocumentItem document, string normalizedAnchor)
        {
            if (document == null)
                return null;

            if (!string.IsNullOrWhiteSpace(normalizedAnchor))
            {
                return new HelpDocumentNavigationState
                {
                    DocId = document.DocId,
                    Anchor = normalizedAnchor
                };
            }

            if (!string.IsNullOrWhiteSpace(document.DocId) &&
                _navigationStatesByDocId.TryGetValue(document.DocId, out var savedState))
                return savedState.Clone();

            return new HelpDocumentNavigationState
            {
                DocId = document.DocId,
                Anchor = string.Empty
            };
        }

        private bool ManejarEnlaceMarkdown(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;

            var isInternalHelpTarget = EsTargetDeAyudaInterna(target);
            var normalizedTarget = NormalizarTarget(target, out var anchor);

            if (string.IsNullOrWhiteSpace(normalizedTarget))
            {
                if (_selectedDocument == null || string.IsNullOrWhiteSpace(anchor))
                    return false;

                SeleccionarDocumento(_selectedDocument, anchor);
                return true;
            }

            if (TryResolveCanonicalHelpLink(target, out var canonicalDocument, out anchor))
            {
                SeleccionarDocumento(canonicalDocument, anchor);
                return true;
            }

            var resolvedRelativeTarget = ResolverRutaLogicaDocumento(normalizedTarget);

            var interno = EnumerarTodosLosDocumentos()
                .FirstOrDefault(x =>
                    string.Equals(x.RelativePath, normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.RelativePath, resolvedRelativeTarget, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.FileName, normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.FileName, resolvedRelativeTarget, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.DocId, normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.DocId, resolvedRelativeTarget, StringComparison.OrdinalIgnoreCase));

            if (interno != null)
            {
                SeleccionarDocumento(interno, anchor);
                return true;
            }

            if (isInternalHelpTarget ||
                normalizedTarget.StartsWith(@"Docs\", StringComparison.OrdinalIgnoreCase) ||
                normalizedTarget.StartsWith("Docs/", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string NormalizarTarget(string target, out string anchor)
        {
            anchor = null;
            var targetSinAncla = (target ?? string.Empty).Trim();

            if (targetSinAncla.StartsWith("help://", StringComparison.OrdinalIgnoreCase))
                return targetSinAncla;

            var anchorIndex = targetSinAncla.IndexOf('#');
            if (anchorIndex >= 0)
            {
                anchor = targetSinAncla.Substring(anchorIndex + 1);
                targetSinAncla = targetSinAncla.Substring(0, anchorIndex);
            }

            return targetSinAncla
                .Replace('/', '\\')
                .Trim();
        }

        private string ResolverRutaLogicaDocumento(string target)
        {
            var normalizedTarget = NormalizarRutaLogica(target);
            if (string.IsNullOrWhiteSpace(normalizedTarget))
                return string.Empty;

            if (normalizedTarget.StartsWith("Docs/", StringComparison.OrdinalIgnoreCase))
                return normalizedTarget.Replace('/', '\\');

            var currentRelativePath = NormalizarRutaLogica(_selectedDocument?.RelativePath);
            if (string.IsNullOrWhiteSpace(currentRelativePath))
                return normalizedTarget.Replace('/', '\\');

            var currentDirectory = Path.GetDirectoryName(currentRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(currentDirectory))
                return normalizedTarget.Replace('/', '\\');

            var combined = Path.Combine(currentDirectory, normalizedTarget.Replace('/', Path.DirectorySeparatorChar));
            return NormalizarRutaLogica(combined).Replace('/', '\\');
        }

        private bool TryResolveCanonicalHelpLink(string target, out DocumentationDocumentItem document, out string anchor)
        {
            document = null;
            anchor = null;

            if (string.IsNullOrWhiteSpace(target) || !target.StartsWith("help://", StringComparison.OrdinalIgnoreCase))
                return false;

            var canonicalTarget = target.Trim().Substring("help://".Length);
            var anchorIndex = canonicalTarget.IndexOf('#');
            if (anchorIndex >= 0)
            {
                anchor = canonicalTarget.Substring(anchorIndex + 1);
                canonicalTarget = canonicalTarget.Substring(0, anchorIndex);
            }

            var docId = canonicalTarget
                .Trim()
                .Trim('/')
                .Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(docId))
                return false;

            return _documentsByDocId.TryGetValue(docId, out document);
        }

        private static string NormalizarRutaLogica(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var partes = path
                .Trim()
                .Replace('\\', '/')
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            var stack = new List<string>();
            foreach (var parteCruda in partes)
            {
                var parte = parteCruda.Trim();
                if (parte == ".")
                    continue;

                if (parte == "..")
                {
                    if (stack.Count > 0)
                        stack.RemoveAt(stack.Count - 1);

                    continue;
                }

                stack.Add(parte);
            }

            return string.Join("/", stack);
        }

        private IEnumerable<DocumentationDocumentItem> EnumerarTodosLosDocumentos()
        {
            return _allSections.SelectMany(x => x.Documents);
        }

        private void ActualizarAccesoRapidoSeleccionado(string docId)
        {
            var selectedOption = SectionOptions.FirstOrDefault(x =>
                string.Equals(x.DocId, docId, StringComparison.OrdinalIgnoreCase));

            _isUpdatingQuickAccessSelection = true;
            try
            {
                SelectedSectionOption = selectedOption;
            }
            finally
            {
                _isUpdatingQuickAccessSelection = false;
            }
        }

        private static bool EsTargetDeAyudaInterna(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;

            var trimmedTarget = target.Trim();
            if (trimmedTarget.StartsWith("help://", StringComparison.OrdinalIgnoreCase) ||
                trimmedTarget.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                return true;

            var normalizedTarget = NormalizarRutaLogica(trimmedTarget);
            if (string.IsNullOrWhiteSpace(normalizedTarget))
                return false;

            if (normalizedTarget.StartsWith("Docs/", StringComparison.OrdinalIgnoreCase))
                return true;

            return normalizedTarget.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
        }

        private static string ConstruirUriCanonica(string docId, string anchor)
        {
            if (string.IsNullOrWhiteSpace(docId))
                return string.Empty;

            var canonicalUri = "help://" + docId.Trim().Trim('/').Replace('\\', '/');
            var normalizedAnchor = HelpAnchorNormalizer.Normalizar(anchor);
            if (!string.IsNullOrWhiteSpace(normalizedAnchor))
                canonicalUri += "#" + normalizedAnchor;

            return canonicalUri;
        }

        private static string LeerDocumento(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath))
                    return "El archivo seleccionado no existe en el directorio de documentacion.";

                return File.ReadAllText(fullPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return "No fue posible cargar el documento.\n\nDetalle tecnico:\n`" + ex.Message + "`";
            }
        }

        private static string RemoverTituloInicial(string contenido, string titulo)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return string.Empty;

            var lines = contenido
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .ToList();

            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
                lines.RemoveAt(0);

            if (lines.Count == 0)
                return string.Empty;

            var primeraLinea = (lines[0] ?? string.Empty).Trim();
            var tituloNormalizado = NormalizarTitulo(titulo);

            if (primeraLinea.StartsWith("#"))
            {
                var textoEncabezado = NormalizarTitulo(primeraLinea.TrimStart('#').Trim());
                if (string.Equals(textoEncabezado, tituloNormalizado, StringComparison.OrdinalIgnoreCase))
                {
                    lines.RemoveAt(0);
                    while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
                        lines.RemoveAt(0);
                }
            }

            return string.Join("\n", lines);
        }

        private static string RecortarDesdeAncla(string contenido, string anchor)
        {
            if (string.IsNullOrWhiteSpace(contenido) || string.IsNullOrWhiteSpace(anchor))
                return contenido ?? string.Empty;

            var anchorNormalizada = HelpAnchorNormalizer.Normalizar(anchor);
            if (string.IsNullOrWhiteSpace(anchorNormalizada))
                return contenido;

            var lines = contenido
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .ToList();

            for (var i = 0; i < lines.Count; i++)
            {
                var line = (lines[i] ?? string.Empty).Trim();
                if (!line.StartsWith("#"))
                    continue;

                var titulo = line.TrimStart('#').Trim();
                if (string.Equals(HelpAnchorNormalizer.Normalizar(titulo), anchorNormalizada, StringComparison.OrdinalIgnoreCase))
                    return string.Join("\n", lines.Skip(i));
            }

            return contenido;
        }

        private static string NormalizarAncla(string value)
        {
            return HelpAnchorNormalizer.Normalizar(value);
        }

        private static string RepararTextoMalCodificado(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value ?? string.Empty;

            if (value.IndexOf('Ã') < 0 && value.IndexOf('Â') < 0)
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

        private static string NormalizarTitulo(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var chars = value
                .Where(char.IsLetterOrDigit)
                .ToArray();

            return new string(chars).Trim().ToUpperInvariant();
        }

    }

    public class DocumentationSectionItem : BaseViewModel
    {
        public string Title { get; set; }
        public string DisplayPath { get; set; }
        public string Description { get; set; }

        public ObservableCollection<DocumentationDocumentItem> Documents { get; } =
            new ObservableCollection<DocumentationDocumentItem>();
    }

    public class DocumentationDocumentItem : BaseViewModel
    {
        private bool _isSelected;

        public string DocId { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string RelativePath { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public sealed class HelpSectionNavigationOption
    {
        public string DocId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }

    public sealed class HelpDocumentHistoryEntry
    {
        public string DocId { get; set; }
        public string Anchor { get; set; }
    }

    public sealed class HelpDocumentNavigationState
    {
        public string DocId { get; set; }
        public string Anchor { get; set; }
        public double VerticalOffset { get; set; }
        public double ScrollableHeight { get; set; }
        public double VerticalRatio { get; set; }

        public bool HasVisualPosition => ScrollableHeight > 0 || VerticalOffset > 0;

        public HelpDocumentNavigationState Clone()
        {
            return new HelpDocumentNavigationState
            {
                DocId = DocId,
                Anchor = Anchor,
                VerticalOffset = VerticalOffset,
                ScrollableHeight = ScrollableHeight,
                VerticalRatio = VerticalRatio
            };
        }
    }
}
