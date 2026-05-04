using System;
using System.Configuration;
using System.IO;

namespace ConcesionaroCarros.Db
{
    public enum DatabaseEnvironment
    {
        Test,
        Production
    }

    public sealed class DatabaseConnectionProvider
    {
        public const string ProductionDatabasePathKey = "CC_SHARED_DATABASE_PATH";
        public const string TestDatabasePathKey = "CC_TEST_DATABASE_PATH";
        public const string BranchNameKey = "CC_DATABASE_BRANCH";

        private const string DefaultDbFileName = "WegInstaladores.db";

        private static readonly Lazy<DatabaseConnectionProvider> LazyInstance =
            new Lazy<DatabaseConnectionProvider>(() => new DatabaseConnectionProvider());

        private DatabaseConnectionProvider()
        {
            BranchName = ResolveBranchName();
            EnvironmentName = ResolveEnvironment(BranchName);
            var configuredPath = ResolveConfiguredDatabasePath(EnvironmentName);
            IsRootedConfiguredDatabasePath = IsRootedConfiguredPath(configuredPath);
            DatabasePath = ResolvePath(configuredPath, DefaultDbFileName);
            ConnectionString = "Data Source=" + DatabasePath;
        }

        public static DatabaseConnectionProvider Instance => LazyInstance.Value;

        public string BranchName { get; }

        public DatabaseEnvironment EnvironmentName { get; }

        public string DatabasePath { get; }

        public string ConnectionString { get; }

        public bool IsRootedConfiguredDatabasePath { get; }

        public string DatabaseDirectory =>
            Path.GetDirectoryName(DatabasePath) ?? AppDomain.CurrentDomain.BaseDirectory;

        public static DatabaseEnvironment ResolveEnvironment(string branchName)
        {
            var branch = NormalizeBranchName(branchName);

            if (string.IsNullOrWhiteSpace(branch))
                return DatabaseEnvironment.Production;

            if (branch.Equals("homologation", StringComparison.OrdinalIgnoreCase) ||
                branch.Equals("Homologation", StringComparison.OrdinalIgnoreCase) ||
                branch.Equals("ProgramTranslation", StringComparison.OrdinalIgnoreCase) ||
                branch.StartsWith("feature/", StringComparison.OrdinalIgnoreCase) ||
                branch.StartsWith("feat/", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseEnvironment.Test;
            }

            if (branch.Equals("main", StringComparison.OrdinalIgnoreCase) ||
                branch.Equals("production", StringComparison.OrdinalIgnoreCase) ||
                branch.Equals("master", StringComparison.OrdinalIgnoreCase) ||
                branch.Equals("Produccion", StringComparison.OrdinalIgnoreCase) ||
                branch.StartsWith("production-test/", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseEnvironment.Production;
            }

            return DatabaseEnvironment.Production;
        }

        private static string ResolveConfiguredDatabasePath(DatabaseEnvironment environmentName)
        {
            var key = environmentName == DatabaseEnvironment.Test
                ? TestDatabasePathKey
                : ProductionDatabasePathKey;

            var configuredPath = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(configuredPath) && environmentName == DatabaseEnvironment.Test)
                configuredPath = ConfigurationManager.AppSettings[ProductionDatabasePathKey];

            return configuredPath;
        }

        private static string ResolvePath(string configuredPath, string defaultFileName)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, defaultFileName);

            configuredPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());

            if (Path.IsPathRooted(configuredPath))
                return configuredPath;

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuredPath));
        }

        private static bool IsRootedConfiguredPath(string configuredPath)
        {
            return !string.IsNullOrWhiteSpace(configuredPath) &&
                Path.IsPathRooted(Environment.ExpandEnvironmentVariables(configuredPath.Trim()));
        }

        private static string ResolveBranchName()
        {
            var configuredBranch = ConfigurationManager.AppSettings[BranchNameKey];
            if (!string.IsNullOrWhiteSpace(configuredBranch))
                return NormalizeBranchName(configuredBranch);

            foreach (var envKey in new[]
            {
                BranchNameKey,
                "BUILD_SOURCEBRANCHNAME",
                "BUILD_SOURCEBRANCH",
                "GITHUB_REF_NAME",
                "GITHUB_HEAD_REF",
                "BRANCH_NAME",
                "GIT_BRANCH"
            })
            {
                var value = Environment.GetEnvironmentVariable(envKey);
                if (!string.IsNullOrWhiteSpace(value))
                    return NormalizeBranchName(value);
            }

            return NormalizeBranchName(ReadGitHeadBranch());
        }

        private static string ReadGitHeadBranch()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            while (!string.IsNullOrWhiteSpace(directory))
            {
                var gitPath = Path.Combine(directory, ".git");
                var gitHeadPath = Path.Combine(gitPath, "HEAD");
                if (File.Exists(gitPath))
                {
                    var gitFile = File.ReadAllText(gitPath).Trim();
                    const string gitDirPrefix = "gitdir:";
                    if (gitFile.StartsWith(gitDirPrefix, StringComparison.OrdinalIgnoreCase))
                        gitHeadPath = Path.Combine(gitFile.Substring(gitDirPrefix.Length).Trim(), "HEAD");
                }

                if (File.Exists(gitHeadPath))
                {
                    var head = File.ReadAllText(gitHeadPath).Trim();
                    const string prefix = "ref: refs/heads/";
                    return head.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        ? head.Substring(prefix.Length)
                        : head;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }

            return null;
        }

        private static string NormalizeBranchName(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                return string.Empty;

            var branch = branchName.Trim().Replace('\\', '/');

            const string refsHeadsPrefix = "refs/heads/";
            if (branch.StartsWith(refsHeadsPrefix, StringComparison.OrdinalIgnoreCase))
                branch = branch.Substring(refsHeadsPrefix.Length);

            const string originPrefix = "origin/";
            if (branch.StartsWith(originPrefix, StringComparison.OrdinalIgnoreCase))
                branch = branch.Substring(originPrefix.Length);

            return branch;
        }
    }
}
