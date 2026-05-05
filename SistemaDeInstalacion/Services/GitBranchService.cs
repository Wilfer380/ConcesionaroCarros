using System;
using System.IO;

namespace ConcesionaroCarros.Services
{
    /// <summary>
    /// Obtiene el nombre de la rama Git en tiempo de ejecución cuando la app corre desde un workspace con .git.
    /// En ambientes instalados (sin .git), retorna un fallback estable.
    /// </summary>
    public static class GitBranchService
    {
        private static string _cached;
        private const string BranchStampFileName = "build.branch";

        public static string GetBranchLabel(string fallback = "RELEASE")
        {
            if (!string.IsNullOrWhiteSpace(_cached))
                return _cached;

            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // 1) En entornos instalados (sin .git), usamos una "firma" generada al compilar/publicar.
                // Esto asegura que el footer muestre la rama real (ProgramTranslation, etc.)
                // en todos los PCs.
                var stampPath = Path.Combine(baseDir, BranchStampFileName);
                if (File.Exists(stampPath))
                {
                    var stamped = (File.ReadAllText(stampPath) ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(stamped))
                    {
                        _cached = Normalize(stamped, fallback);
                        return _cached;
                    }
                }

                // 2) Si estamos corriendo desde un workspace, leemos .git/HEAD.
                var gitDir = FindGitDirectory(baseDir);
                if (string.IsNullOrWhiteSpace(gitDir))
                {
                    _cached = fallback;
                    return _cached;
                }

                var headPath = Path.Combine(gitDir, "HEAD");
                if (!File.Exists(headPath))
                {
                    _cached = fallback;
                    return _cached;
                }

                var head = (File.ReadAllText(headPath) ?? string.Empty).Trim();
                if (head.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
                {
                    // ref: refs/heads/ProgramTranslation
                    var refValue = head.Substring(4).Trim();
                    var idx = refValue.LastIndexOf('/');
                    var branch = idx >= 0 ? refValue.Substring(idx + 1) : refValue;
                    _cached = Normalize(branch, fallback);
                    return _cached;
                }

                // Detached HEAD: HEAD contiene un hash.
                if (head.Length >= 7)
                {
                    _cached = Normalize(head.Substring(0, 7), fallback);
                    return _cached;
                }
            }
            catch
            {
                // Nunca romper UI por esto.
            }

            _cached = fallback;
            return _cached;
        }

        private static string FindGitDirectory(string startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return null;

            var current = new DirectoryInfo(startPath);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, ".git");
                if (Directory.Exists(candidate))
                    return candidate;

                current = current.Parent;
            }

            return null;
        }

        private static string Normalize(string value, string fallback)
        {
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            // Queremos etiqueta consistente (como se ve hoy: mayúsculas).
            return text.ToUpperInvariant();
        }
    }
}
