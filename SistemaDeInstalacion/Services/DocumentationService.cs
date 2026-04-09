using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConcesionaroCarros.Services
{
    public sealed class DocumentationService
    {
        public IReadOnlyList<DocumentationSectionDefinition> CargarSecciones(bool esAdministrador)
        {
            var docsRoot = ResolverDocsRoot();
            var secciones = new List<DocumentationSectionDefinition>();

            if (string.IsNullOrWhiteSpace(docsRoot) || !Directory.Exists(docsRoot))
                return secciones;

            if (esAdministrador)
            {
                AgregarSeccion(
                    secciones,
                    docsRoot,
                    "Sistema",
                    "Docs",
                    "Vista general, alcance funcional y mapa documental del sistema.",
                    new DocumentationDocumentRegistration("sistema", "Sistema.md"));

                AgregarSeccion(
                    secciones,
                    Path.Combine(docsRoot, "users"),
                    "User",
                    @"Docs\users",
                    "Guía operativa para usuarios finales del sistema.",
                    new DocumentationDocumentRegistration("users/user", "User.md"));

                AgregarSeccion(
                    secciones,
                    Path.Combine(docsRoot, "Developers"),
                    "Developer",
                    @"Docs\Developers",
                    "Guía técnica para continuidad de desarrollo, empaquetado y soporte.",
                    new DocumentationDocumentRegistration("developers/developer", "Developer.md"),
                    new DocumentationDocumentRegistration("developers/base-de-datos", "BaseDeDatos.md", "Base de datos"));

                AgregarSeccion(
                    secciones,
                    Path.Combine(docsRoot, "Administradores"),
                    "Administradores",
                    @"Docs\Administradores",
                    "Guía operativa y técnica para la administración funcional del sistema.",
                    new DocumentationDocumentRegistration("administradores/administradores", "Administradores.md"));

                return secciones;
            }

            AgregarSeccion(
                secciones,
                Path.Combine(docsRoot, "users"),
                "User",
                @"Docs\users",
                "Guía operativa disponible para usuarios finales.",
                new DocumentationDocumentRegistration("users/user", "User.md"));

            return secciones;
        }

        private static void AgregarSeccion(
            ICollection<DocumentationSectionDefinition> secciones,
            string folderPath,
            string title,
            string displayPath,
            string descripcion,
            params DocumentationDocumentRegistration[] orderedDocuments)
        {
            if (!Directory.Exists(folderPath))
                return;

            var documentos = new List<DocumentationDocumentDefinition>();

            foreach (var document in orderedDocuments ?? Array.Empty<DocumentationDocumentRegistration>())
            {
                if (document == null || string.IsNullOrWhiteSpace(document.FileName) || string.IsNullOrWhiteSpace(document.DocId))
                    continue;

                var fileName = document.FileName;
                var fullPath = Path.Combine(folderPath, fileName);
                if (!File.Exists(fullPath))
                    continue;

                documentos.Add(new DocumentationDocumentDefinition
                {
                    DocId = document.DocId,
                    Title = string.IsNullOrWhiteSpace(document.Title)
                        ? ObtenerTitulo(fileName)
                        : document.Title,
                    FileName = fileName,
                    FullPath = fullPath,
                    RelativePath = Path.Combine(displayPath, fileName)
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

            if (string.Equals(name, "BaseDeDatos", StringComparison.OrdinalIgnoreCase))
                return "Base de datos";

            return name.Replace('_', ' ').Trim();
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
