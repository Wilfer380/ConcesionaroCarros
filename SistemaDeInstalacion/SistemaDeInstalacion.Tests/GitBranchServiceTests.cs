using ConcesionaroCarros.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace SistemaDeInstalacion.Tests
{
    [TestClass]
    public class GitBranchServiceTests
    {
        [DataTestMethod]
        [DataRow("homologation", "Homologation")]
        [DataRow("Homologation", "Homologation")]
        [DataRow("production", "Production")]
        [DataRow("refs/heads/production", "Production")]
        public void FormatBranchLabel_DisplaysSensitiveBaseBranchesAsEnvironmentLabels(string branchName, string expected)
        {
            Assert.AreEqual(expected, GitBranchService.FormatBranchLabel(branchName, "LOCAL"));
            Assert.IsFalse(GitBranchService.IsSensitiveBranchLabel(expected));
        }

        [DataTestMethod]
        [DataRow("homologation/fix-login", "Homologation/fix-login")]
        [DataRow("origin/homologation/fix-login", "Homologation/fix-login")]
        [DataRow("refs/heads/production/release-check", "Production/release-check")]
        public void FormatBranchLabel_DisplaysSensitiveSubbranchesWithSensitiveStyle(string branchName, string expected)
        {
            Assert.AreEqual(expected, GitBranchService.FormatBranchLabel(branchName, "LOCAL"));
            Assert.IsTrue(GitBranchService.IsSensitiveBranchLabel(expected));
        }

        [DataTestMethod]
        [DataRow("ProgramTranslation", "ProgramTranslation")]
        [DataRow("ProgramTranslation/fix-login", "ProgramTranslation/fix-login")]
        [DataRow("refs/heads/ProgramTranslation/fix-login", "ProgramTranslation/fix-login")]
        [DataRow("fix/login", "ProgramTranslation/fix/login")]
        [DataRow("feat/branch-display-policy", "ProgramTranslation/feat/branch-display-policy")]
        [DataRow("docs/foo", "ProgramTranslation/docs/foo")]
        public void FormatBranchLabel_PreservesCommonBranchesWithoutSensitiveStyle(string branchName, string expected)
        {
            Assert.AreEqual(expected, GitBranchService.FormatBranchLabel(branchName, "LOCAL"));
            Assert.IsFalse(GitBranchService.IsSensitiveBranchLabel(expected));
        }

        [DataTestMethod]
        [DataRow("Homologation", "Homologation", "")]
        [DataRow("Production", "Production", "")]
        [DataRow("ProgramTranslation", "ProgramTranslation", "")]
        [DataRow("Homologation/feature-x", "Homologation", "/feature-x")]
        [DataRow("Production/fix-x", "Production", "/fix-x")]
        [DataRow("ProgramTranslation/feat/login", "ProgramTranslation", "/feat/login")]
        [DataRow("Homologation/RamaTrabajo/fix-numero1", "Homologation", "/RamaTrabajo/fix-numero1")]
        public void SplitBranchLabel_SeparatesBaseBranchFromSuffix(string label, string expectedBase, string expectedSuffix)
        {
            Assert.AreEqual(expectedBase, GitBranchService.GetBranchLabelBase(label));
            Assert.AreEqual(expectedSuffix, GitBranchService.GetBranchLabelSuffix(label));
        }

        [DataTestMethod]
        [DataRow("ProgramTranslation", "", "")]
        [DataRow("ProgramTranslation/RamaTrabajo", "/RamaTrabajo", "")]
        [DataRow("ProgramTranslation/RamaTrabajo/fix-numero1", "/RamaTrabajo", "/fix-numero1")]
        [DataRow("Homologation/RamaTrabajo/fix-numero1", "/RamaTrabajo", "/fix-numero1")]
        [DataRow("Production/RamaTrabajo/fix-numero1", "/RamaTrabajo", "/fix-numero1")]
        public void SplitBranchLabel_SeparatesWorkAndFeatureSegments(string label, string expectedWorkSegment, string expectedFeatureSegment)
        {
            Assert.AreEqual(expectedWorkSegment, GitBranchService.GetBranchLabelWorkSegment(label));
            Assert.AreEqual(expectedFeatureSegment, GitBranchService.GetBranchLabelFeatureSegment(label));
        }

        [DataTestMethod]
        [DataRow("feat/login", "ProgramTranslation", "/feat/login")]
        [DataRow("fix/login", "ProgramTranslation", "/fix/login")]
        [DataRow("docs/setup", "ProgramTranslation", "/docs/setup")]
        public void SplitBranchLabel_UsesFormattedOfficialBranches(string branchName, string expectedBase, string expectedSuffix)
        {
            var label = GitBranchService.FormatBranchLabel(branchName, "LOCAL");

            Assert.AreEqual(expectedBase, GitBranchService.GetBranchLabelBase(label));
            Assert.AreEqual(expectedSuffix, GitBranchService.GetBranchLabelSuffix(label));
        }

        [TestMethod]
        public void GetCurrentBranchLabelFromDirectory_DisplaysDirectHomologationChildLineage()
        {
            var workspace = CreateGitWorkspace("RamaTrabajo");
            try
            {
                WriteCreatedFrom(workspace.GitDir, "RamaTrabajo", "homologation");

                Assert.AreEqual(
                    "Homologation/RamaTrabajo",
                    GitBranchService.GetCurrentBranchLabelFromDirectory(workspace.Root, "LOCAL"));
            }
            finally
            {
                Directory.Delete(workspace.Root, true);
            }
        }

        [DataTestMethod]
        [DataRow("origin/homologation")]
        [DataRow("refs/heads/homologation")]
        public void GetCurrentBranchLabelFromDirectory_DisplaysDirectHomologationChildLineageWithPrefixedParent(string parentBranch)
        {
            var workspace = CreateGitWorkspace("fix-numero1");
            try
            {
                WriteCreatedFrom(workspace.GitDir, "fix-numero1", parentBranch);

                Assert.AreEqual(
                    "Homologation/fix-numero1",
                    GitBranchService.GetCurrentBranchLabelFromDirectory(workspace.Root, "LOCAL"));
            }
            finally
            {
                Directory.Delete(workspace.Root, true);
            }
        }

        [TestMethod]
        public void GetCurrentBranchLabelFromDirectory_DisplaysNestedHomologationChildLineage()
        {
            var workspace = CreateGitWorkspace("fix-numero1");
            try
            {
                WriteCreatedFrom(workspace.GitDir, "RamaTrabajo", "homologation");
                WriteCreatedFrom(workspace.GitDir, "fix-numero1", "RamaTrabajo");

                Assert.AreEqual(
                    "Homologation/RamaTrabajo/fix-numero1",
                    GitBranchService.GetCurrentBranchLabelFromDirectory(workspace.Root, "LOCAL"));
            }
            finally
            {
                Directory.Delete(workspace.Root, true);
            }
        }

        [TestMethod]
        public void GetCurrentBranchLabelFromDirectory_DisplaysNestedHomologationChildLineageWithPrefixedParent()
        {
            var workspace = CreateGitWorkspace("fix-numero1");
            try
            {
                WriteCreatedFrom(workspace.GitDir, "RamaTrabajo", "homologation");
                WriteCreatedFrom(workspace.GitDir, "fix-numero1", "refs/heads/RamaTrabajo");

                Assert.AreEqual(
                    "Homologation/RamaTrabajo/fix-numero1",
                    GitBranchService.GetCurrentBranchLabelFromDirectory(workspace.Root, "LOCAL"));
            }
            finally
            {
                Directory.Delete(workspace.Root, true);
            }
        }

        [DataTestMethod]
        [DataRow("ProgramTranslation", "ProgramTranslation/RamaTrabajo/fix-numero1")]
        [DataRow("homologation", "Homologation/RamaTrabajo/fix-numero1")]
        [DataRow("production", "Production/RamaTrabajo/fix-numero1")]
        public void GetCurrentBranchLabelFromDirectory_DisplaysNestedEnvironmentChildLineage(string environmentBranch, string expectedLabel)
        {
            var workspace = CreateGitWorkspace("fix-numero1");
            try
            {
                WriteCreatedFrom(workspace.GitDir, "RamaTrabajo", environmentBranch);
                WriteCreatedFrom(workspace.GitDir, "fix-numero1", "RamaTrabajo");

                Assert.AreEqual(
                    expectedLabel,
                    GitBranchService.GetCurrentBranchLabelFromDirectory(workspace.Root, "LOCAL"));
            }
            finally
            {
                Directory.Delete(workspace.Root, true);
            }
        }

        [TestMethod]
        public void GetCurrentBranchLabelFromDirectory_DisplaysSlashBranchHomologationChildLineage()
        {
            var workspace = CreateGitWorkspace("fix/numero1");
            try
            {
                WriteCreatedFrom(workspace.GitDir, "RamaTrabajo", "homologation");
                WriteCreatedFrom(workspace.GitDir, "fix/numero1", "RamaTrabajo");

                Assert.AreEqual(
                    "Homologation/RamaTrabajo/fix/numero1",
                    GitBranchService.GetCurrentBranchLabelFromDirectory(workspace.Root, "LOCAL"));
            }
            finally
            {
                Directory.Delete(workspace.Root, true);
            }
        }

        [TestMethod]
        public void GetCurrentBranchLabelFromDirectory_FallsBackWhenLineageCannotBeResolved()
        {
            var workspace = CreateGitWorkspace("RamaTrabajo");
            try
            {
                Assert.AreEqual(
                    "RamaTrabajo",
                    GitBranchService.GetCurrentBranchLabelFromDirectory(workspace.Root, "LOCAL"));
            }
            finally
            {
                Directory.Delete(workspace.Root, true);
            }
        }

        [TestMethod]
        public void GetCurrentBranchLabelFromDirectory_FallsBackWhenReflogIsCorrupt()
        {
            var workspace = CreateGitWorkspace("RamaTrabajo");
            try
            {
                WriteBranchLog(workspace.GitDir, "RamaTrabajo", "not a created-from reflog entry");

                Assert.AreEqual(
                    "RamaTrabajo",
                    GitBranchService.GetCurrentBranchLabelFromDirectory(workspace.Root, "LOCAL"));
            }
            finally
            {
                Directory.Delete(workspace.Root, true);
            }
        }

        [TestMethod]
        public void BranchLabelSplitHelpers_ReturnBaseAndSuffix()
        {
            const string label = "Homologation/RamaTrabajo/fix-numero1";

            Assert.AreEqual("Homologation", GitBranchService.GetBranchLabelBase(label));
            Assert.AreEqual("/RamaTrabajo/fix-numero1", GitBranchService.GetBranchLabelSuffix(label));
            Assert.AreEqual("/RamaTrabajo", GitBranchService.GetBranchLabelWorkSegment(label));
            Assert.AreEqual("/fix-numero1", GitBranchService.GetBranchLabelFeatureSegment(label));
        }

        private static GitWorkspace CreateGitWorkspace(string currentBranch)
        {
            var root = Path.Combine(Path.GetTempPath(), "GitBranchServiceTests_" + Guid.NewGuid().ToString("N"));
            var gitDir = Path.Combine(root, ".git");
            Directory.CreateDirectory(gitDir);
            File.WriteAllText(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/" + currentBranch);

            return new GitWorkspace(root, gitDir);
        }

        private static void WriteCreatedFrom(string gitDir, string branch, string parent)
        {
            WriteBranchLog(
                gitDir,
                branch,
                "0000000000000000000000000000000000000000 1111111111111111111111111111111111111111 Test <test@example.com> 0 +0000\tbranch: Created from " + parent);
        }

        private static void WriteBranchLog(string gitDir, string branch, string content)
        {
            var logPath = Path.Combine(
                gitDir,
                "logs",
                "refs",
                "heads",
                branch.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.WriteAllText(logPath, content);
        }

        private sealed class GitWorkspace
        {
            public GitWorkspace(string root, string gitDir)
            {
                Root = root;
                GitDir = gitDir;
            }

            public string Root { get; }
            public string GitDir { get; }
        }
    }
}
