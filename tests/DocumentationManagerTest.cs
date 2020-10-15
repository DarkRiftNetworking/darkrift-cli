using System.Collections.Generic;
using System.IO;
using DarkRift.Cli.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DarkRift.Cli
{
    [TestClass]
    public class DocumentationManagerTest
    {
        private readonly Mock<IFileUtility> mockIFileUtility = new Mock<IFileUtility>();

        private readonly Mock<IRemoteRepository> mockIRemoteRepository = new Mock<IRemoteRepository>();

        [TestMethod]
        public void TestGetInstallationWhenExists()
        {
            // GIVEN the installation is present
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "a-version"))).Returns(true);

            // WHEN I get the installation
            DocumentationManager classUnderTest = new DocumentationManager(null, mockIFileUtility.Object, "a-dir");
            DocumentationInstallation result = classUnderTest.GetInstallation("a-version");

            // THEN the result is a valid DocumentationManager object
            Assert.AreEqual("a-version", result.Version);
            Assert.AreEqual(Path.Combine("a-dir", "a-version"), result.InstallationPath);
        }

        [TestMethod]
        public void TestGetInstallationWhenNotExists()
        {
            // GIVEN the installation is not present
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "a-version"))).Returns(false);

            // WHEN I get the installation
            DocumentationManager classUnderTest = new DocumentationManager(null, mockIFileUtility.Object, "a-dir");
            DocumentationInstallation result = classUnderTest.GetInstallation("a-version");

            // THEN the result is null
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetVersions()
        {
            // GIVEN at least version is available
            mockIFileUtility.Setup(f => f.DirectoryExists("a-dir")).Returns(true);
            mockIFileUtility.Setup(f => f.GetDirectories("a-dir")).Returns(new string[] { "dir1", "dir2" });

            // WHEN I get the installed versions
            DocumentationManager classUnderTest = new DocumentationManager(null, mockIFileUtility.Object, "a-dir");
            List<DocumentationInstallation> result = classUnderTest.GetVersions();

            // THEN the result is the correct DocumentationManager objects
            Assert.AreEqual(2, result.Count);

            Assert.AreEqual("dir1", result[0].Version);
            Assert.AreEqual(Path.Combine("a-dir", "dir1"), result[0].InstallationPath);

            Assert.AreEqual("dir2", result[1].Version);
            Assert.AreEqual(Path.Combine("a-dir", "dir2"), result[1].InstallationPath);
        }

        [TestMethod]
        public void TestGetVersionsWhenNonePresent()
        {
            // GIVEN the search directory does not exist
            mockIFileUtility.Setup(f => f.DirectoryExists("a-dir")).Returns(false);

            // WHEN I get the installed versions
            DocumentationManager classUnderTest = new DocumentationManager(null, mockIFileUtility.Object, "a-dir");
            List<DocumentationInstallation> result = classUnderTest.GetVersions();

            // THEN the result is an empty array
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestInstall()
        {
            // GIVEN the installation does not exist
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "a-version"))).Returns(false);

            // AND the installation is available on the remote
            mockIRemoteRepository.Setup(r => r.DownloadDocumentationTo("a-version", Path.Combine("a-dir", "a-version")))
                                 .Returns(true);

            // WHEN I install the version
            DocumentationManager classUnderTest = new DocumentationManager(mockIRemoteRepository.Object, mockIFileUtility.Object, "a-dir");
            DocumentationInstallation result = classUnderTest.Install("a-version", false);

            // THEN the download was run
            mockIRemoteRepository.Verify(r => r.DownloadDocumentationTo("a-version", Path.Combine("a-dir", "a-version")));

            // AND the installation returned is correct
            Assert.AreEqual("a-version", result.Version);
            Assert.AreEqual(Path.Combine("a-dir", "a-version"), result.InstallationPath);
        }

        [TestMethod]
        public void TestInstallWhenForced()
        {
            // GIVEN the installation already exists
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "a-version"))).Returns(false);

            // AND the installation is available on the remote
            mockIRemoteRepository.Setup(r => r.DownloadDocumentationTo("a-version", Path.Combine("a-dir", "a-version")))
                                 .Returns(true);

            // WHEN I force install the version
            DocumentationManager classUnderTest = new DocumentationManager(mockIRemoteRepository.Object, mockIFileUtility.Object, "a-dir");
            DocumentationInstallation result = classUnderTest.Install("a-version", true);

            // THEN the download was run
            mockIRemoteRepository.Verify(r => r.DownloadDocumentationTo("a-version", Path.Combine("a-dir", "a-version")));

            // AND the installation returned is correct
            Assert.AreEqual("a-version", result.Version);
            Assert.AreEqual(Path.Combine("a-dir", "a-version"), result.InstallationPath);
        }

        [TestMethod]
        public void TestInstallWhenDownloadFails()
        {
            // GIVEN the installation does not exist
            mockIFileUtility.Setup(f => f.DirectoryExists(Path.Combine("a-dir", "a-version"))).Returns(false);

            // AND the installation is not available on the remote
            mockIRemoteRepository.Setup(r => r.DownloadDocumentationTo("a-version", Path.Combine("a-dir", "a-version")))
                                 .Returns(false);

            // WHEN I install the version
            DocumentationManager classUnderTest = new DocumentationManager(mockIRemoteRepository.Object, mockIFileUtility.Object, "a-dir");
            DocumentationInstallation result = classUnderTest.Install("a-version", false);

            // THEN null is returned
            Assert.IsNull(result);
        }
    }
}
