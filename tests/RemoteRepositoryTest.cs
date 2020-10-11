using System.Net;
using DarkRift.Cli.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DarkRift.Cli
{
    [TestClass]
    public class RemoteRepositoryTest
    {
        private readonly Mock<IFileUtility> mockIFileUtility = new Mock<IFileUtility>();

        private readonly Mock<IWebClientUtility> mockIWebClientUtility = new Mock<IWebClientUtility>();

        private readonly Mock<IInvoiceManager> mockIInvoiceManager = new Mock<IInvoiceManager>();

        private readonly Mock<IContext> mockIContext = new Mock<IContext>();

        [TestInitialize]
        public void Initialize()
        {
            // GIVEN a temporary file path
            mockIFileUtility.Setup(f => f.GetTempFileName()).Returns("a-staging-path");

            // GIVEN the context is setup
            mockIContext.Setup(c => c.Profile).Returns(new Profile());
        }

        [TestMethod]
        public void TestDownloadVersionToWhenFree()
        {
            // WHEN I download a free server
            RemoteRepository classUnderTest = new RemoteRepository(null, null, mockIWebClientUtility.Object, mockIFileUtility.Object);
            bool result = classUnderTest.DownloadVersionTo("a-version", ServerTier.Free, ServerPlatform.Core, "a-download-path");

            // THEN the result is success
            Assert.IsTrue(result);

            // AND the download was made
            mockIWebClientUtility.Verify(w => w.DownloadFile("/DarkRift2/Releases/a-version/Free/Core/", "a-staging-path"));

            // AND the download was extracted to the correct location
            mockIFileUtility.Verify(f => f.ExtractZipTo("a-staging-path", "a-download-path"));

            // AND the temporary file was deleted again
            mockIFileUtility.Verify(f => f.Delete("a-staging-path"));
        }

        [TestMethod]
        public void TestDownloadVersionToWhenPro()
        {
            // GIVEN the invoice manager returns an invoice number
            mockIInvoiceManager.Setup(i => i.GetInvoiceNumber()).Returns("an-invoice-number");

            // WHEN I download a pro server
            RemoteRepository classUnderTest = new RemoteRepository(mockIInvoiceManager.Object, null, mockIWebClientUtility.Object, mockIFileUtility.Object);
            bool result = classUnderTest.DownloadVersionTo("a-version", ServerTier.Pro, ServerPlatform.Core, "a-download-path");

            // THEN the result is success
            Assert.IsTrue(result);

            // AND the download was made
            mockIWebClientUtility.Verify(w => w.DownloadFile("/DarkRift2/Releases/a-version/Pro/Core/?invoice=an-invoice-number", "a-staging-path"));

            // AND the download was extracted to the correct location
            mockIFileUtility.Verify(f => f.ExtractZipTo("a-staging-path", "a-download-path"));

            // AND the temporary file was deleted again
            mockIFileUtility.Verify(f => f.Delete("a-staging-path"));
        }

        [TestMethod]
        public void TestDownloadVersionToWhenProButNoInvoice()
        {
            // GIVEN the invoice manager does not return an invoice number
            mockIInvoiceManager.Setup(i => i.GetInvoiceNumber()).Returns((string)null);

            // WHEN I download a pro server
            RemoteRepository classUnderTest = new RemoteRepository(mockIInvoiceManager.Object, null, mockIWebClientUtility.Object, mockIFileUtility.Object);
            bool result = classUnderTest.DownloadVersionTo("a-version", ServerTier.Pro, ServerPlatform.Core, "a-download-path");

            // THEN the result is not success
            Assert.IsFalse(result);

            // AND the download was not attempted
            mockIWebClientUtility.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TestDownloadVersionToWhenDownloadFails()
        {
            // GIVEN the web client cannot download the file
            mockIWebClientUtility.Setup(w => w.DownloadFile("/DarkRift2/Releases/a-version/Free/Core/", "a-staging-path")).Throws(new WebException("mock-exception"));

            // WHEN I download a free server
            RemoteRepository classUnderTest = new RemoteRepository(null, null, mockIWebClientUtility.Object, mockIFileUtility.Object);
            bool result = classUnderTest.DownloadVersionTo("a-version", ServerTier.Free, ServerPlatform.Core, "a-download-path");

            // THEN the result is not success
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestDownloadDocumentationTo()
        {
            // WHEN I download a free server
            RemoteRepository classUnderTest = new RemoteRepository(null, null, mockIWebClientUtility.Object, mockIFileUtility.Object);
            bool result = classUnderTest.DownloadDocumentationTo("a-version", "a-download-path");

            // THEN the result is success
            Assert.IsTrue(result);

            // AND the download was made
            mockIWebClientUtility.Verify(w => w.DownloadFile("/DarkRift2/Releases/a-version/Docs/", "a-staging-path"));

            // AND the download was extracted to the correct location
            mockIFileUtility.Verify(f => f.ExtractZipTo("a-staging-path", "a-download-path"));

            // AND the temporary file was deleted again
            mockIFileUtility.Verify(f => f.Delete("a-staging-path"));
        }

        [TestMethod]
        public void TestDownloadDocumentationToWhenDownloadFails()
        {
            // GIVEN the web client cannot download the file
            mockIWebClientUtility.Setup(w => w.DownloadFile("/DarkRift2/Releases/a-version/Docs/", "a-staging-path")).Throws(new WebException("mock-exception"));

            // WHEN I download a free server
            RemoteRepository classUnderTest = new RemoteRepository(null, null, mockIWebClientUtility.Object, mockIFileUtility.Object);
            bool result = classUnderTest.DownloadDocumentationTo("a-version", "a-download-path");

            // THEN the result is not success
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestGetLatestDarkRiftVersion()
        {
            // GIVEN the web client can get the latest version
            mockIWebClientUtility.Setup(w => w.DownloadString("/DarkRift2/Releases/")).Returns("{\"latest\": \"my-version\"}");

            // WHEN I get the latest version
            RemoteRepository classUnderTest = new RemoteRepository(null, mockIContext.Object, mockIWebClientUtility.Object, null);
            string result = classUnderTest.GetLatestDarkRiftVersion();

            // THEN the result is the correct value
            Assert.AreEqual("my-version", result);

            // AND the context was set
            Assert.AreEqual("my-version", mockIContext.Object.Profile.LatestKnownDarkRiftVersion);
            mockIContext.Verify(c => c.Save());
        }

        [TestMethod]
        public void TestGetLatestDarkRiftVersionWhenDownloadFails()
        {
            // GIVEN the web client cannot get the latest version
            mockIWebClientUtility.Setup(w => w.DownloadString("/DarkRift2/Releases/")).Throws(new WebException("mock-exception"));

            // WHEN I get the latest version
            RemoteRepository classUnderTest = new RemoteRepository(null, mockIContext.Object, mockIWebClientUtility.Object, null);
            string result = classUnderTest.GetLatestDarkRiftVersion();

            // THEN the result is null
            Assert.IsNull(result);

            // AND the context was not
            mockIContext.VerifyNoOtherCalls();
        }
    }
}
