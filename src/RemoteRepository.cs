using System.IO;
using System;
using System.Net;
using System.IO.Compression;
using Crayon;
using System.Collections.Generic;
using DarkRift.Cli.Utility;

namespace DarkRift.Cli
{
    internal interface IRemoteRepository
    {
        bool DownloadDocumentationTo(string version, string downloadDirectory);
        bool DownloadVersionTo(string version, ServerTier tier, ServerPlatform platform, string downloadDirectory);
        string GetLatestDarkRiftVersion();
    }

    /// <summary>
    /// Represents the DR repository where resources can be pulled from.
    /// </summary>
    internal class RemoteRepository : IRemoteRepository
    {
        /// <summary>
        /// The invoice manager to use.
        /// </summary>
        private readonly IInvoiceManager invoiceManager;

        /// <summary>
        /// The application's context.
        /// </summary>
        private readonly IContext context;

        /// <summary>
        /// The web client utility to use.
        /// </summary>
        private readonly IWebClientUtility webClientUtility;

        /// <summary>
        /// The file utility to use.
        /// </summary>
        private readonly IFileUtility fileUtility;

        /// <summary>
        /// Creates a new repository.
        /// </summary>
        /// <param name="invoiceManager">The invoice manager to use</param>
        public RemoteRepository(IInvoiceManager invoiceManager, IContext context, IWebClientUtility webClientUtility, IFileUtility fileUtility)
        {
            this.invoiceManager = invoiceManager;
            this.context = context;
            this.webClientUtility = webClientUtility;
            this.fileUtility = fileUtility;
        }

        /// <summary>
        /// Downloads and installs a DarkRift version.
        /// </summary>
        /// <param name="version">The version to be installed</param>
        /// <param name="tier">The tier</param>
        /// <param name="platform">The platform</param>
        /// <param name="downloadDirectory">The directory to download the release to</param>
        /// <returns>True if installed successfully otherwise false</returns>
        public bool DownloadVersionTo(string version, ServerTier tier, ServerPlatform platform, string downloadDirectory)
        {
            string stagingPath = fileUtility.GetTempFileName();

            string uri = $"/DarkRift2/Releases/{version}/{tier}/{platform}/";
            if (tier == ServerTier.Pro)
            {
                string invoiceNumber = invoiceManager.GetInvoiceNumber();
                if (invoiceNumber == null)
                {
                    Console.Error.WriteLine(Output.Red($"You must provide an invoice number in order to download Pro DarkRift releases."));
                    return false;
                }

                uri += $"?invoice={invoiceNumber}";
            }

            try
            {
                webClientUtility.DownloadFile(uri, stagingPath);
            }
            catch (WebException e)
            {
                Console.Error.WriteLine(Output.Red($"Could not download DarkRift {version} - {tier} (.NET {platform}):\n\t{e.Message}"));
                return false;
            }

            Console.WriteLine($"Extracting package...");

            try
            {
                fileUtility.ExtractZipTo(stagingPath, downloadDirectory);
            }
            catch (Exception)
            {
                // Make sure we don't leave a partial install
                fileUtility.Delete(downloadDirectory);
            }
            finally
            {
                fileUtility.Delete(stagingPath);
            }

            Console.WriteLine(Output.Green($"Successfully downloaded DarkRift {version} - {tier} (.NET {platform})."));

            return true;
        }

        /// <summary>
        /// Downloads and installs the documentation of a version of DarkRift.
        /// </summary>
        /// <param name="version">The version of DarkRift</param>
        /// <param name="downloadDirectory">The directory to download the documentation to</param>
        /// <returns>True for success otherwise false</returns>
        public bool DownloadDocumentationTo(string version, string downloadDirectory)
        {
            string stagingPath = fileUtility.GetTempFileName();

            string uri = $"/DarkRift2/Releases/{version}/Docs/";
            try
            {
                webClientUtility.DownloadFile(uri, stagingPath);
            }
            catch (WebException e)
            {
                Console.Error.WriteLine(Output.Red($"Could not download documentation for DarkRift {version}:\n\t{e.Message}"));
                return false;
            }

            Console.WriteLine($"Extracting package...");

            fileUtility.ExtractZipTo(stagingPath, downloadDirectory);
            fileUtility.Delete(stagingPath);

            Console.WriteLine(Output.Green($"Successfully downloaded ocumentation for DarkRift {version}."));

            return true;
        }

        /// <summary>
        /// Queries or loads the latest version of DarkRift
        /// </summary>
        /// <returns>The latest version of DarkRift</returns>
        public string GetLatestDarkRiftVersion()
        {
            Console.WriteLine("Querying server for the latest DarkRift version...");

            string uri = $"/DarkRift2/Releases/";
            string latestJson;
            try
            {
                latestJson = webClientUtility.DownloadString(uri);
            }
            catch (WebException e)
            {
                Console.WriteLine(Output.Yellow($"Could not query latest DarkRift version from the server. Will use the last known latest instead.\n\t{e.Message}"));

                return null;
            }

            // Parse out 'latest' field
            VersionMetadata versionMetadata = VersionMetadata.Parse(latestJson);

            Console.WriteLine($"Latest version of DarkRift is {versionMetadata.Latest}.");

            context.Profile.LatestKnownDarkRiftVersion = versionMetadata.Latest;
            context.Save();

            return versionMetadata.Latest;
        }
    }
}
