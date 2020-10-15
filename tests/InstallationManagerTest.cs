using System.Collections.Generic;
using System.IO;
using DarkRift.Cli.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DarkRift.Cli
{
    [TestClass]
    public class InstallationManagerTest
    {
        private readonly Mock<IFileUtility> mockIFileUtility = new Mock<IFileUtility>();

        private readonly Mock<IRemoteRepository> mockIRemoteRepository = new Mock<IRemoteRepository>();

        private readonly Mock<IContext> mockIContext = new Mock<IContext>();

        [TestMethod]
        public void TestGetInstallationWhenExists()
        {
            // GIVEN the installation is present
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "pro", "core", "a-version"))).Returns(true);

            // WHEN I get the installation
            InstallationManager classUnderTest = new InstallationManager(null, mockIFileUtility.Object, "a-dir", null);
            DarkRiftInstallation result = classUnderTest.GetInstallation("a-version", ServerTier.Pro, ServerPlatform.Core);

            // THEN the result is a valid DarkRiftInstallation object
            Assert.AreEqual("a-version", result.Version);
            Assert.AreEqual(ServerTier.Pro, result.Tier);
            Assert.AreEqual(ServerPlatform.Core, result.Platform);
            Assert.AreEqual(Path.Combine("a-dir", "pro", "core", "a-version"), result.InstallationPath);
        }

        [TestMethod]
        public void TestGetInstallationWhenNotExists()
        {
            // GIVEN the installation is not present
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "pro", "core", "a-version"))).Returns(false);

            // WHEN I get the installation
            InstallationManager classUnderTest = new InstallationManager(null, mockIFileUtility.Object, "a-dir", null);
            DarkRiftInstallation result = classUnderTest.GetInstallation("a-version", ServerTier.Pro, ServerPlatform.Core);

            // THEN the result is null
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetVersions()
        {
            // GIVEN at least version is available
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "pro", "core"))).Returns(true);
            mockIFileUtility.Setup(f => f.GetDirectories(Path.Combine("a-dir", "pro", "core"))).Returns(new string[] { "dir1", "dir2" });

            // WHEN I get the installed versions
            InstallationManager classUnderTest = new InstallationManager(null, mockIFileUtility.Object, "a-dir", null);
            List<DarkRiftInstallation> result = classUnderTest.GetVersions(ServerTier.Pro, ServerPlatform.Core);

            // THEN the result is the correct DarkRiftInstallation objects
            Assert.AreEqual(2, result.Count);

            Assert.AreEqual("dir1", result[0].Version);
            Assert.AreEqual(ServerTier.Pro, result[0].Tier);
            Assert.AreEqual(ServerPlatform.Core, result[0].Platform);
            Assert.AreEqual(Path.Combine("a-dir", "pro", "core", "dir1"), result[0].InstallationPath);

            Assert.AreEqual("dir2", result[1].Version);
            Assert.AreEqual(ServerTier.Pro, result[1].Tier);
            Assert.AreEqual(ServerPlatform.Core, result[1].Platform);
            Assert.AreEqual(Path.Combine("a-dir", "pro", "core", "dir2"), result[1].InstallationPath);
        }

        [TestMethod]
        public void TestGetVersionsWhenNonePresent()
        {
            // GIVEN the search directory does not exist
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "pro", "core"))).Returns(false);

            // WHEN I get the installed versions
            InstallationManager classUnderTest = new InstallationManager(null, mockIFileUtility.Object, "a-dir", null);
            List<DarkRiftInstallation> result = classUnderTest.GetVersions(ServerTier.Pro, ServerPlatform.Core);

            // THEN the result is an empty array
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestInstall()
        {
            // GIVEN the installation does not exist
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "pro", "core", "a-version"))).Returns(false);

            // AND the installation is available on the remote
            mockIRemoteRepository.Setup(r => r.DownloadVersionTo("a-version", ServerTier.Pro, ServerPlatform.Core, Path.Combine("a-dir", "pro", "core", "a-version")))
                                 .Returns(true);

            // WHEN I install the version
            InstallationManager classUnderTest = new InstallationManager(mockIRemoteRepository.Object, mockIFileUtility.Object, "a-dir", null);
            DarkRiftInstallation result = classUnderTest.Install("a-version", ServerTier.Pro, ServerPlatform.Core, false);

            // THEN the download was run
            mockIRemoteRepository.Verify(r => r.DownloadVersionTo("a-version", ServerTier.Pro, ServerPlatform.Core, Path.Combine("a-dir", "pro", "core", "a-version")));

            // AND the installation returned is correct
            Assert.AreEqual("a-version", result.Version);
            Assert.AreEqual(ServerTier.Pro, result.Tier);
            Assert.AreEqual(ServerPlatform.Core, result.Platform);
            Assert.AreEqual(Path.Combine("a-dir", "pro", "core", "a-version"), result.InstallationPath);
        }

        [TestMethod]
        public void TestInstallWhenForced()
        {
            // GIVEN the installation already exists
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "pro", "core", "a-version"))).Returns(false);

            // AND the installation is available on the remote
            mockIRemoteRepository.Setup(r => r.DownloadVersionTo("a-version", ServerTier.Pro, ServerPlatform.Core, Path.Combine("a-dir", "pro", "core", "a-version")))
                                 .Returns(true);

            // WHEN I force install the version
            InstallationManager classUnderTest = new InstallationManager(mockIRemoteRepository.Object, mockIFileUtility.Object, "a-dir", null);
            DarkRiftInstallation result = classUnderTest.Install("a-version", ServerTier.Pro, ServerPlatform.Core, true);

            // THEN the download was run
            mockIRemoteRepository.Verify(r => r.DownloadVersionTo("a-version", ServerTier.Pro, ServerPlatform.Core, Path.Combine("a-dir", "pro", "core", "a-version")));

            // AND the installation returned is correct
            Assert.AreEqual("a-version", result.Version);
            Assert.AreEqual(ServerTier.Pro, result.Tier);
            Assert.AreEqual(ServerPlatform.Core, result.Platform);
            Assert.AreEqual(Path.Combine("a-dir", "pro", "core", "a-version"), result.InstallationPath);
        }

        [TestMethod]
        public void TestInstallWhenDownloadFails()
        {
            // GIVEN the installation does not exist
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "pro", "core", "a-version"))).Returns(false);

            // AND the installation is not available on the remote
            mockIRemoteRepository.Setup(r => r.DownloadVersionTo("a-version", ServerTier.Pro, ServerPlatform.Core, Path.Combine("a-dir", "pro", "core", "a-version")))
                                 .Returns(false);

            // WHEN I install the version
            InstallationManager classUnderTest = new InstallationManager(mockIRemoteRepository.Object, mockIFileUtility.Object, "a-dir", null);
            DarkRiftInstallation result = classUnderTest.Install("a-version", ServerTier.Pro, ServerPlatform.Core, false);

            // THEN null is returned
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetLatestDarkRiftVersionWhenRemoteAvailable()
        {
            // GIVEN the remote can fetch the latest DarkRift version
            mockIRemoteRepository.Setup(r => r.GetLatestDarkRiftVersion()).Returns("a-version");

            // WHEN I get the latest DarkRift version
            InstallationManager classUnderTest = new InstallationManager(mockIRemoteRepository.Object, null, null, null);
            string result = classUnderTest.GetLatestDarkRiftVersion();

            // THEN the returned version is correct
            Assert.AreEqual("a-version", result);
        }

        [TestMethod]
        public void TestGetLatestDarkRiftVersionWhenMemoised()
        {
            // GIVEN the remote can fetch the latest DarkRift version
            mockIRemoteRepository.Setup(r => r.GetLatestDarkRiftVersion()).Returns("a-version");

            // WHEN I get the latest DarkRift version
            InstallationManager classUnderTest = new InstallationManager(mockIRemoteRepository.Object, null, null, null);
            string result1 = classUnderTest.GetLatestDarkRiftVersion();

            // THEN the returned version is correct
            Assert.AreEqual("a-version", result1);

            // WHEN I get the latest DarkRift version a second time
            string result2 = classUnderTest.GetLatestDarkRiftVersion();

            // THEN the returned version is correct
            Assert.AreEqual("a-version", result2);

            // AND the remote was not contacted again
            mockIRemoteRepository.Verify(r => r.GetLatestDarkRiftVersion(), Times.Once);
        }

        [TestMethod]
        public void TestGetLatestDarkRiftVersionWhenRemoteNotAvailableButProfileKnows()
        {
            // GIVEN the remote cannot fetch the latest DarkRift version
            mockIRemoteRepository.Setup(r => r.GetLatestDarkRiftVersion()).Returns((string)null);

            // AND the profile contains a known version
            Profile profile = new Profile();
            mockIContext.Setup(c => c.Profile).Returns(profile);
            profile.LatestKnownDarkRiftVersion = "a-version";

            // WHEN I get the latest DarkRift version
            InstallationManager classUnderTest = new InstallationManager(mockIRemoteRepository.Object, null, null, mockIContext.Object);
            string result = classUnderTest.GetLatestDarkRiftVersion();

            // THEN the returned version is correct
            Assert.AreEqual("a-version", result);
        }

        [TestMethod]
        public void TestGetLatestDarkRiftVersionWhenRemoteNotAvailableNorInProfile()
        {
            // GIVEN the remote cannot fetch the latest DarkRift version
            mockIRemoteRepository.Setup(r => r.GetLatestDarkRiftVersion()).Returns((string)null);

            // AND the profile does not contain a known version
            Profile profile = new Profile();
            mockIContext.Setup(c => c.Profile).Returns(profile);
            profile.LatestKnownDarkRiftVersion = null;

            // WHEN I get the latest DarkRift version
            InstallationManager classUnderTest = new InstallationManager(mockIRemoteRepository.Object, null, null, mockIContext.Object);
            string result = classUnderTest.GetLatestDarkRiftVersion();

            // THEN the returned version is null
            Assert.IsNull(result);
        }
    }
}
