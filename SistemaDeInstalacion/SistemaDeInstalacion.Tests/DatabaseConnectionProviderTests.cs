using ConcesionaroCarros.Db;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SistemaDeInstalacion.Tests
{
    [TestClass]
    public class DatabaseConnectionProviderTests
    {
        [DataTestMethod]
        [DataRow("homologation")]
        [DataRow("Homologation")]
        [DataRow("ProgramTranslation")]
        [DataRow("feature/new-login")]
        [DataRow("feat/environment-workflow")]
        [DataRow("refs/heads/feature/new-login")]
        public void ResolveEnvironment_MapsHomologationAndFeaturesToTest(string branchName)
        {
            Assert.AreEqual(DatabaseEnvironment.Test, DatabaseConnectionProvider.ResolveEnvironment(branchName));
        }

        [DataTestMethod]
        [DataRow("main")]
        [DataRow("production")]
        [DataRow("Produccion")]
        [DataRow("master")]
        [DataRow("production-test/release-check")]
        [DataRow("origin/production-test/release-check")]
        public void ResolveEnvironment_MapsReleaseBranchesToProduction(string branchName)
        {
            Assert.AreEqual(DatabaseEnvironment.Production, DatabaseConnectionProvider.ResolveEnvironment(branchName));
        }
    }
}
