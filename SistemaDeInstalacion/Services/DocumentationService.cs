using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ConcesionaroCarros.Services
{
    public sealed class DocumentationService
    {
        public IReadOnlyList<DocumentationSectionDefinition> CargarSecciones(DocumentationProfile profile)
        {
            var docsRoot = ResolverDocsRoot();
            var secciones = new List<DocumentationSectionDefinition>();

            if (string.IsNullOrWhiteSpace(docsRoot) || !Directory.Exists(docsRoot))
                return secciones;

            var culture = LocalizationService.Instance.CurrentCulture;

            if (profile == DocumentationProfile.Admin || profile == DocumentationProfile.Developer)
            {
                AgregarSeccion(
                    secciones,
                    docsRoot,
                    TextoPorIdioma(culture, "Sistema", "System", "Sistema"),
                    @"Docs",
                    TextoPorIdioma(culture,
                        "Vista general, alcance funcional y mapa documental del sistema.",
                        "General overview, functional scope, and document map of the system.",
                        "Vis\u00E3o geral, escopo funcional e mapa documental do sistema."),
                    culture,
                    new DocumentationDocumentRegistration(
                        "sistema",
                        "Sistema.md",
                        TextoPorIdioma(culture, "Sistema", "System", "Sistema")));
            }

            AgregarSeccion(
                secciones,
                Path.Combine(docsRoot, "users"),
                TextoPorIdioma(culture, "Usuario", "User", "Usu\u00E1rio"),
                @"Docs\users",
                profile == DocumentationProfile.User
                    ? TextoPorIdioma(culture,
                        "Gu\u00EDa operativa disponible para usuarios finales.",
                        "Operational guide available for end users.",
                        "Guia operacional dispon\u00EDvel para usu\u00E1rios finais.")
                    : TextoPorIdioma(culture,
                        "Gu\u00EDa operativa para usuarios finales del sistema.",
                        "Operational guide for end users of the system.",
                        "Guia operacional para usu\u00E1rios finais do sistema."),
                culture,
                new DocumentationDocumentRegistration(
                    "users/user",
                    "User.md",
                    TextoPorIdioma(culture, "User", "User", "Usu\u00E1rio")));

            if (profile == DocumentationProfile.Developer)
            {
                AgregarSeccion(
                    secciones,
                    Path.Combine(docsRoot, "Developers"),
                    TextoPorIdioma(culture, "Developer", "Developer", "Developer"),
                    @"Docs\Developers",
                    TextoPorIdioma(culture,
                        "Gu\u00EDa t\u00E9cnica para continuidad de desarrollo, empaquetado y soporte.",
                        "Technical guide for development continuity, packaging, and support.",
                        "Guia t\u00E9cnica para continuidade do desenvolvimento, empacotamento e suporte."),
                    culture,
                    new DocumentationDocumentRegistration(
                        "developers/developer",
                        "Developer.md",
                        TextoPorIdioma(culture, "Developer", "Developer", "Developer")),
                    new DocumentationDocumentRegistration(
                        "developers/base-de-datos",
                        "BaseDeDatos.md",
                        TextoPorIdioma(culture, "Base de datos", "Database", "Banco de dados")));
            }

            if (profile == DocumentationProfile.Admin)
            {
                AgregarSeccion(
                    secciones,
                    Path.Combine(docsRoot, "Administradores"),
                    TextoPorIdioma(culture, "Administradores", "Administrators", "Administradores"),
                    @"Docs\Administradores",
                    TextoPorIdioma(culture,
                        "Gu\u00EDa operativa y t\u00E9cnica para la administraci\u00F3n funcional del sistema.",
                        "Operational and technical guide for the functional administration of the system.",
                        "Guia operacional e t\u00E9cnica para a administra\u00E7\u00E3o funcional do sistema."),
                    culture,
                    new DocumentationDocumentRegistration(
                        "administradores/administradores",
                        "Administradores.md",
                        TextoPorIdioma(culture, "Administradores", "Administrators", "Administradores")));
            }

            return secciones;
        }

        private static void AgregarSeccion(
            ICollection<DocumentationSectionDefinition> secciones,
            string folderPath,
            string title,
            string displayPath,
            string descripcion,
            CultureInfo culture,
            params DocumentationDocumentRegistration[] orderedDocuments)
        {
            if (!Directory.Exists(folderPath))
                return;

            var documentos = new List<DocumentationDocumentDefinition>();

            foreach (var document in orderedDocuments ?? Array.Empty<DocumentationDocumentRegistration>())
            {
                if (document == null || string.IsNullOrWhiteSpace(document.FileName) || string.IsNullOrWhiteSpace(document.DocId))
                    continue;

                if (!TryResolverArchivoLocalizado(folderPath, document.FileName, culture, out var localizedFileName, out var fullPath))
                    continue;

                documentos.Add(new DocumentationDocumentDefinition
                {
                    DocId = document.DocId,
                    Title = string.IsNullOrWhiteSpace(document.Title)
                        ? ObtenerTitulo(document.FileName)
                        : document.Title,
                    FileName = localizedFileName,
                    FullPath = fullPath,
                    RelativePath = Path.Combine(displayPath, localizedFileName)
                });
            }

            if (documentos.Count == 0)
                return;

            secciones.Add(new DocumentationSectionDefinition
            {
                Title = title,
                DisplayPath = displayPath,
                Description = descripcion,
                Documents = documentos
            });
        }

        private static bool TryResolverArchivoLocalizado(
            string folderPath,
            string baseFileName,
            CultureInfo culture,
            out string localizedFileName,
            out string fullPath)
        {
            localizedFileName = null;
            fullPath = null;

            foreach (var candidate in EnumerarCandidatosDeArchivo(baseFileName, culture))
            {
                var candidatePath = Path.Combine(folderPath, candidate);
                if (!File.Exists(candidatePath))
                    continue;

                localizedFileName = candidate;
                fullPath = candidatePath;
                return true;
            }

            return false;
        }

        private static IEnumerable<string> EnumerarCandidatosDeArchivo(string baseFileName, CultureInfo culture)
        {
            var extension = Path.GetExtension(baseFileName) ?? string.Empty;
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName) ?? string.Empty;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (culture != null)
            {
                var cultureCandidate = string.Format("{0}.{1}{2}", nameWithoutExtension, culture.Name, extension);
                if (seen.Add(cultureCandidate))
                    yield return cultureCandidate;

                var neutralCandidate = string.Format("{0}.{1}{2}", nameWithoutExtension, culture.TwoLetterISOLanguageName, extension);
                if (seen.Add(neutralCandidate))
                    yield return neutralCandidate;
            }

            if (seen.Add(baseFileName))
                yield return baseFileName;
        }

        private static string ResolverDocsRoot()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Docs"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "Docs")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "Docs")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Docs"))
            };

            return candidates.FirstOrDefault(Directory.Exists) ?? string.Empty;
        }

        private static string ObtenerTitulo(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName) ?? string.Empty;
            var lastDotIndex = name.LastIndexOf('.');
            if (lastDotIndex > 0)
                name = name.Substring(0, lastDotIndex);

            if (string.Equals(name, "BaseDeDatos", StringComparison.OrdinalIgnoreCase))
                return LocalizedText.Get("Documentation_DatabaseTitle", "Base de datos");

            return name.Replace('_', ' ').Trim();
        }

        private static string TextoPorIdioma(CultureInfo culture, string es, string en, string pt)
        {
            if (culture == null)
                return es;

            if (culture.Name.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
                return pt;

            if (culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return en;

            return es;
        }
    }

    public sealed class DocumentationSectionDefinition
    {
        public string Title { get; set; }
        public string DisplayPath { get; set; }
        public string Description { get; set; }
        public IReadOnlyList<DocumentationDocumentDefinition> Documents { get; set; }
    }

    public sealed class DocumentationDocumentDefinition
    {
        public string DocId { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string RelativePath { get; set; }
    }

    public sealed class DocumentationDocumentRegistration
    {
        public DocumentationDocumentRegistration(string docId, string fileName, string title = null)
        {
            DocId = docId;
            FileName = fileName;
            Title = title;
        }

        public string DocId { get; }
        public string FileName { get; }
        public string Title { get; }
    }
}

