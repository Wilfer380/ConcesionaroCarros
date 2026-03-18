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
                    "Sistema.md");

                AgregarSeccion(
                    secciones,
                    Path.Combine(docsRoot, "users"),
                    "User",
                    @"Docs\users",
                    "Guía operativa para usuarios finales del sistema.",
                    "User.md");

                AgregarSeccion(
                    secciones,
                    Path.Combine(docsRoot, "Developers"),
                    "Developer",
                    @"Docs\Developers",
                    "Guía técnica para continuidad de desarrollo, empaquetado y soporte.",
                    "Developer.md",
                    "BaseDeDatos.md");

                AgregarSeccion(
                    secciones,
                    Path.Combine(docsRoot, "Administradores"),
                    "Administradores",
                    @"Docs\Administradores",
                    "Guía operativa y técnica para la administración funcional del sistema.",
                    "Administradores.md");

                return secciones;
            }

            AgregarSeccion(
                secciones,
                Path.Combine(docsRoot, "users"),
                "User",
                @"Docs\users",
                "Guía operativa disponible para usuarios finales.",
                "User.md");

            return secciones;
        }

        private static void AgregarSeccion(
            ICollection<DocumentationSectionDefinition> secciones,
            string folderPath,
            string title,
            string displayPath,
            string descripcion,
            params string[] orderedFiles)
        {
            if (!Directory.Exists(folderPath))
                return;

            var documentos = new List<DocumentationDocumentDefinition>();

            foreach (var fileName in orderedFiles ?? Array.Empty<string>())
            {
                var fullPath = Path.Combine(folderPath, fileName);
                if (!File.Exists(fullPath))
                    continue;

                documentos.Add(new DocumentationDocumentDefinition
                {
                    Title = ObtenerTitulo(fileName),
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
        public string Title { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string RelativePath { get; set; }
    }
}
