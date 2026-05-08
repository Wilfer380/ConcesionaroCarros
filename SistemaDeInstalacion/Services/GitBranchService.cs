using System;
using System.Collections.Generic;
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
            return GetCurrentBranchLabelFromDirectory(AppDomain.CurrentDomain.BaseDirectory, fallback);
        }

        public static string GetCurrentBranchLabelFromDirectory(string baseDir, string fallback = "RELEASE")
        {
            try
            {
                // 1) Si estamos corriendo desde un workspace, leemos .git/HEAD.
                var gitDir = FindGitDirectory(baseDir);
                var gitHeadBranch = ReadGitHeadBranch(gitDir);
                if (!string.IsNullOrWhiteSpace(gitHeadBranch))
                {
                    var lineageLabel = TryResolveEnvironmentLineage(gitDir, gitHeadBranch);
                    if (!string.IsNullOrWhiteSpace(lineageLabel))
                        return lineageLabel;

                    return FormatBranchLabel(gitHeadBranch, fallback);
                }

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
                return separator >= 0 ? "Homologation" + text.Substring(separator) : "Homologation";

            if (IsBranch(baseName, ProductionBranch))
                return separator >= 0 ? "Production" + text.Substring(separator) : "Production";

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

        public static string GetBranchLabelBase(string label)
        {
            var text = Normalize(label, string.Empty);
            var separator = text.IndexOf('/');
            return separator >= 0 ? text.Substring(0, separator) : text;
        }

        public static string GetBranchLabelSuffix(string label)
        {
            var text = Normalize(label, string.Empty);
            var separator = text.IndexOf('/');
            return separator >= 0 ? text.Substring(separator) : string.Empty;
        }

        public static string GetBranchLabelWorkSegment(string label)
        {
            var suffix = GetBranchLabelSuffix(label);
            if (string.IsNullOrWhiteSpace(suffix))
                return string.Empty;

            var separator = suffix.IndexOf('/', 1);
            return separator >= 0 ? suffix.Substring(0, separator) : suffix;
        }

        public static string GetBranchLabelFeatureSegment(string label)
        {
            var suffix = GetBranchLabelSuffix(label);
            if (string.IsNullOrWhiteSpace(suffix))
                return string.Empty;

            var separator = suffix.IndexOf('/', 1);
            return separator >= 0 ? suffix.Substring(separator) : string.Empty;
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

        private static string TryResolveEnvironmentLineage(string gitDir, string currentBranch)
        {
            if (string.IsNullOrWhiteSpace(gitDir))
                return null;

            var branch = Normalize(currentBranch, string.Empty);
            if (string.IsNullOrWhiteSpace(branch))
                return null;

            string environmentLabel;
            if (TryGetEnvironmentLabel(branch, out environmentLabel))
                return environmentLabel;

            var chain = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (!string.IsNullOrWhiteSpace(branch))
            {
                if (!visited.Add(branch))
                    return null;

                if (TryGetEnvironmentLabel(branch, out environmentLabel))
                {
                    chain.Reverse();
                    return chain.Count == 0 ? environmentLabel : environmentLabel + "/" + string.Join("/", chain);
                }

                chain.Add(branch);
                branch = ReadCreatedFromBranch(gitDir, branch);
            }

            return null;
        }

        private static bool TryGetEnvironmentLabel(string branch, out string label)
        {
            if (IsBranch(branch, ProgramTranslationBranch))
            {
                label = ProgramTranslationBranch;
                return true;
            }

            if (IsBranch(branch, HomologationBranch))
            {
                label = "Homologation";
                return true;
            }

            if (IsBranch(branch, ProductionBranch))
            {
                label = "Production";
                return true;
            }

            label = null;
            return false;
        }

        private static string ReadCreatedFromBranch(string gitDir, string branch)
        {
            try
            {
                var logPath = Path.Combine(
                    gitDir,
                    "logs",
                    "refs",
                    "heads",
                    branch.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(logPath))
                    return null;

                const string createdFromMarker = "branch: Created from ";
                foreach (var line in File.ReadAllLines(logPath))
                {
                    var markerIndex = line.IndexOf(createdFromMarker, StringComparison.OrdinalIgnoreCase);
                    if (markerIndex < 0)
                        continue;

                    var parent = line.Substring(markerIndex + createdFromMarker.Length).Trim();
                    parent = Normalize(parent, string.Empty);
                    return string.IsNullOrWhiteSpace(parent) ? null : parent;
                }
            }
            catch
            {
                return null;
            }

            return null;
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
