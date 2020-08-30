using System.IO;
using System;
using System.Net;
using System.IO.Compression;
using Crayon;
using System.Collections.Generic;

namespace DarkRift.Cli
{
    /// <summary>
    /// Represents the DR repository where resources can be pulled from.
    /// </summary>
    internal class RemoteRepository
    {
        /// <summary>
        /// The invoice manager to use.
        /// </summary>
        private readonly InvoiceManager invoiceManager;

        /// <summary>
        /// The application's context.
        /// </summary>
        private readonly Context context;

        /// <summary>
        /// Creates a new repository.
        /// </summary>
        /// <param name="invoiceManager">The invoice manager to use</param>
        public RemoteRepository(InvoiceManager invoiceManager, Context context)
        {
            this.invoiceManager = invoiceManager;
            this.context = context;
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
            string stagingPath = Path.GetTempFileName();

            string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/{version}/{tier}/{platform}/";
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
                using WebClient myWebClient = new WebClient();
                myWebClient.DownloadFile(uri, stagingPath);
            }
            catch (WebException e)
            {
                Console.Error.WriteLine(Output.Red($"Could not download DarkRift {version} - {tier} (.NET {platform}):\n\t{e.Message}"));
                return false;
            }

            Console.WriteLine($"Extracting package...");

            Directory.CreateDirectory(downloadDirectory);

            ZipFile.ExtractToDirectory(stagingPath, downloadDirectory, true);

            Console.WriteLine(Output.Green($"Successfully downloaded DarkRift {version} - {tier} (.NET {platform})."));

            File.Delete(stagingPath);

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
            string stagingPath = Path.GetTempFileName();

            string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/{version}/Docs/";
            try
            {
                using WebClient myWebClient = new WebClient();
                myWebClient.DownloadFile(uri, stagingPath);
            }
            catch (WebException e)
            {
                Console.Error.WriteLine(Output.Red($"Could not download documentation for DarkRift {version}:\n\t{e.Message}"));
                return false;
            }

            Console.WriteLine($"Extracting package...");

            Directory.CreateDirectory(downloadDirectory);

            ZipFile.ExtractToDirectory(stagingPath, downloadDirectory, true);

            Console.WriteLine(Output.Green($"Successfully downloaded ocumentation for DarkRift {version}."));

            File.Delete(stagingPath);

            return true;
        }

        /// <summary>
        /// Queries or loads the latest version of DarkRift
        /// </summary>
        /// <returns>The latest version of DarkRift</returns>
        public string GetLatestDarkRiftVersion()
        {
            Console.WriteLine("Querying server for the latest DarkRift version...");

            string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/";
            try
            {
                using WebClient myWebClient = new WebClient();
                string latestJson = myWebClient.DownloadString(uri);

                // Parse out 'latest' field
                VersionMetadata versionMetadata = VersionMetadata.Parse(latestJson);

                Console.WriteLine($"Latest version of DarkRift is {versionMetadata.Latest}.");

                context.Profile.LatestKnownDarkRiftVersion = versionMetadata.Latest;
                context.Save();

                return versionMetadata.Latest;
            }
            catch (WebException e)
            {
                Console.WriteLine(Output.Yellow($"Could not query latest DarkRift version from the server. Will use the last known latest instead.\n\t{e.Message}"));

                return null;
            }
        }
    }
}
