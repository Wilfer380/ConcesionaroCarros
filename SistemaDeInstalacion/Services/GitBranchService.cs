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
        private const string RefsHeadsPrefix = "refs/heads/";
        private const string OriginPrefix = "origin/";
        private const string ProgramTranslationBranch = "ProgramTranslation";
        private const string HomologationBranch = "homologation";
        private const string ProductionBranch = "production";

        public static string GetBranchLabel(string fallback = "RELEASE")
        {
            if (!string.IsNullOrWhiteSpace(_cached))
                return _cached;

            _cached = GetCurrentBranchLabel(fallback);
            return _cached;
        }

        public static string RefreshBranchLabel(string fallback = "RELEASE")
        {
            _cached = GetCurrentBranchLabel(fallback);
            return _cached;
        }

        public static string GetCurrentBranchLabel(string fallback = "RELEASE")
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // 1) Si estamos corriendo desde un workspace, leemos .git/HEAD.
                var gitDir = FindGitDirectory(baseDir);
                var gitHeadBranch = ReadGitHeadBranch(gitDir);
                if (!string.IsNullOrWhiteSpace(gitHeadBranch))
                    return FormatBranchLabel(gitHeadBranch, fallback);

                // 2) En entornos instalados (sin .git), usamos una "firma" generada al compilar/publicar.
                // Esto asegura que el footer muestre la rama real (ProgramTranslation, etc.)
                // en todos los PCs.
                var stampPath = Path.Combine(baseDir, BranchStampFileName);
                if (File.Exists(stampPath))
                {
                    var stamped = (File.ReadAllText(stampPath) ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(stamped))
                        return FormatBranchLabel(stamped, fallback);
                }
            }
            catch
            {
                // Nunca romper UI por esto.
            }

            return fallback;
        }

        public static string FormatBranchLabel(string value, string fallback = "RELEASE")
        {
            var text = Normalize(value, fallback);
            if (string.IsNullOrWhiteSpace(text) || string.Equals(text, fallback, StringComparison.Ordinal))
                return text;

            var separator = text.IndexOf('/');
            var baseName = separator >= 0 ? text.Substring(0, separator) : text;

            if (IsBranch(baseName, HomologationBranch))
                return separator >= 0 ? HomologationBranch + text.Substring(separator) : HomologationBranch;

            if (IsBranch(baseName, ProductionBranch))
                return separator >= 0 ? ProductionBranch + text.Substring(separator) : ProductionBranch;

            if (IsProgramTranslationWorkBranch(baseName))
                return ProgramTranslationBranch + "/" + text;

            return text;
        }

        public static bool IsSensitiveBranchLabel(string label)
        {
            var text = Normalize(label, string.Empty);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return IsSensitiveSubbranch(text, HomologationBranch) || IsSensitiveSubbranch(text, ProductionBranch);
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

                if (File.Exists(candidate))
                {
                    var gitFile = (File.ReadAllText(candidate) ?? string.Empty).Trim();
                    const string gitDirPrefix = "gitdir:";
                    if (gitFile.StartsWith(gitDirPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var gitDir = gitFile.Substring(gitDirPrefix.Length).Trim();
                        if (!Path.IsPathRooted(gitDir))
                            gitDir = Path.GetFullPath(Path.Combine(current.FullName, gitDir));

                        if (Directory.Exists(gitDir))
                            return gitDir;
                    }
                }

                current = current.Parent;
            }

            return null;
        }

        private static string ReadGitHeadBranch(string gitDir)
        {
            if (string.IsNullOrWhiteSpace(gitDir))
                return null;

            var headPath = Path.Combine(gitDir, "HEAD");
            if (!File.Exists(headPath))
                return null;

            var head = (File.ReadAllText(headPath) ?? string.Empty).Trim();
            if (head.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
            {
                var refValue = head.Substring(4).Trim().Replace('\\', '/');
                if (refValue.StartsWith(RefsHeadsPrefix, StringComparison.OrdinalIgnoreCase))
                    return refValue.Substring(RefsHeadsPrefix.Length);

                return refValue;
            }

            // Detached HEAD: HEAD contiene un hash.
            return head.Length >= 7 ? head.Substring(0, 7) : null;
        }

        private static bool IsSensitiveSubbranch(string label, string baseBranch)
        {
            return label.StartsWith(baseBranch + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBranch(string value, string branchName)
        {
            return string.Equals(value, branchName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProgramTranslationWorkBranch(string value)
        {
            return IsBranch(value, "fix") || IsBranch(value, "feat") || IsBranch(value, "docs");
        }

        private static string Normalize(string value, string fallback)
        {
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            text = text.Replace('\\', '/');

            if (text.StartsWith(RefsHeadsPrefix, StringComparison.OrdinalIgnoreCase))
                text = text.Substring(RefsHeadsPrefix.Length);

            if (text.StartsWith(OriginPrefix, StringComparison.OrdinalIgnoreCase))
                text = text.Substring(OriginPrefix.Length);

            return text;
        }
    }
}
