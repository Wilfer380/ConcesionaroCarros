using ConcesionaroCarros.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SistemaDeInstalacion.Tests
{
    [TestClass]
    public class GitBranchServiceTests
    {
        [DataTestMethod]
        [DataRow("homologation", "homologation")]
        [DataRow("Homologation", "homologation")]
        [DataRow("production", "production")]
        [DataRow("refs/heads/production", "production")]
        public void FormatBranchLabel_DisplaysSensitiveBaseBranchesInLowercase(string branchName, string expected)
        {
            Assert.AreEqual(expected, GitBranchService.FormatBranchLabel(branchName, "LOCAL"));
            Assert.IsFalse(GitBranchService.IsSensitiveBranchLabel(expected));
        }

        [DataTestMethod]
        [DataRow("homologation/fix-login", "homologation/fix-login")]
        [DataRow("origin/homologation/fix-login", "homologation/fix-login")]
        [DataRow("refs/heads/production/release-check", "production/release-check")]
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
        [DataRow("homologation", "homologation", "")]
        [DataRow("production", "production", "")]
        [DataRow("ProgramTranslation", "ProgramTranslation", "")]
        [DataRow("homologation/feature-x", "homologation", "/feature-x")]
        [DataRow("production/fix-x", "production", "/fix-x")]
        [DataRow("ProgramTranslation/feat/login", "ProgramTranslation", "/feat/login")]
        public void SplitBranchLabel_SeparatesBaseBranchFromSuffix(string label, string expectedBase, string expectedSuffix)
        {
            Assert.AreEqual(expectedBase, GitBranchService.GetBranchLabelBase(label));
            Assert.AreEqual(expectedSuffix, GitBranchService.GetBranchLabelSuffix(label));
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
    }
}
