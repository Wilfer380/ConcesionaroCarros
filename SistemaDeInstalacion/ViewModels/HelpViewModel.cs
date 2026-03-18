using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Services;
using System;
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

        public ObservableCollection<DocumentationSectionItem> Sections { get; } =
            new ObservableCollection<DocumentationSectionItem>();

        public bool EsAdministrador { get; }

        public string TituloAyuda => EsAdministrador
            ? "Centro de ayuda administrativo"
            : "Centro de ayuda para usuarios";

        public string SubtituloAyuda => EsAdministrador
            ? "Consulta toda la documentación del sistema organizada por carpetas y documentos."
            : "Consulta únicamente la documentación disponible para el usuario final.";

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

        public ICommand SelectDocumentCommand { get; }

        public HelpViewModel(bool esAdministrador)
        {
            EsAdministrador = esAdministrador;
            SelectDocumentCommand = new RelayCommand(o => SeleccionarDocumento(o as DocumentationDocumentItem));
            CargarDocumentacion();
        }

        private void CargarDocumentacion()
        {
            Sections.Clear();
            var sections = _documentationService.CargarSecciones(EsAdministrador);

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
                        Title = document.Title,
                        FileName = document.FileName,
                        FullPath = document.FullPath,
                        RelativePath = document.RelativePath
                    });
                }

                Sections.Add(sectionItem);
            }

            var firstDocument = Sections
                .SelectMany(x => x.Documents)
                .FirstOrDefault();

            if (firstDocument != null)
            {
                SeleccionarDocumento(firstDocument);
                return;
            }

            SelectedDocumentTitle = "Documentación no disponible";
            SelectedDocumentPath = "No se encontraron archivos de ayuda.";
            SelectedDocumentFlow = MarkdownDocumentRenderer.Crear(
                "No se encontró documentación para este perfil.\n\nRevisa que la carpeta `Docs` exista en el directorio de ejecución.",
                ManejarEnlaceMarkdown);
        }

        private void SeleccionarDocumento(DocumentationDocumentItem document)
        {
            if (document == null)
                return;

            if (_selectedDocument != null)
                _selectedDocument.IsSelected = false;

            _selectedDocument = document;
            _selectedDocument.IsSelected = true;

            var contenido = LeerDocumento(document.FullPath);
            SelectedDocumentTitle = document.Title;
            SelectedDocumentPath = document.RelativePath;
            SelectedDocumentFlow = MarkdownDocumentRenderer.Crear(
                RemoverTituloInicial(contenido, document.Title),
                ManejarEnlaceMarkdown);
        }

        private bool ManejarEnlaceMarkdown(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;

            var normalizedTarget = target
                .Replace('/', '\\')
                .Trim();

            var interno = Sections
                .SelectMany(x => x.Documents)
                .FirstOrDefault(x =>
                    string.Equals(x.RelativePath, normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.FileName, normalizedTarget, StringComparison.OrdinalIgnoreCase));

            if (interno != null)
            {
                SeleccionarDocumento(interno);
                return true;
            }

            if (normalizedTarget.StartsWith(@"Docs\", StringComparison.OrdinalIgnoreCase) ||
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

        private static string LeerDocumento(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath))
                    return "El archivo seleccionado no existe en el directorio de documentación.";

                return File.ReadAllText(fullPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return "No fue posible cargar el documento.\n\nDetalle técnico:\n`" + ex.Message + "`";
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
}
