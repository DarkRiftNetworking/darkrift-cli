using System.IO;
using System;
using System.Net;
using System.IO.Compression;
using Crayon;

namespace DarkRift.Cli
{
    /// <summary>
    /// Manages DarkRift installations.
    /// </summary>
    internal class VersionManager
    {
        /// <summary>
        /// The DarkRift settings directory path.
        /// </summary>
        private static readonly string USER_DR_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".darkrift");

        /// <summary>
        /// The latest version of DarkRift.
        /// </summary>
        private static string latestDarkRiftVersion;

        /// <summary>
        /// Gets the path to a specified installation, downloading it if required.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="netStandard">Whether the .NET Standard build should be used.</param>
        /// <returns>The path to the installation, or null, if it cannot be provided.</returns>
        public static string GetInstallationPath(string version, ServerTier tier, ServerPlatform platform)
        {
            string fullPath = Path.Combine(USER_DR_DIR, "installed", tier.ToString().ToLower(), platform.ToString().ToLower(), version);

            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine($"DarkRift {version} - {tier} (.NET {platform}) not installed! Downloading package...");

                string stagingPath = Path.Combine(USER_DR_DIR, "Download.zip");

                string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/{version}/{tier}/{platform}/";
                if (tier == ServerTier.Pro)
                {
                    string invoiceNumber = GetInvoiceNumber();
                    if (invoiceNumber == null)
                    {
                        Console.Error.WriteLine(Output.Red($"You must provide an invoice number in order to download Pro DarkRift releases."));
                        return null;
                    }

                    uri += $"?invoice={invoiceNumber}";
                }

                try
                {
                    using (WebClient myWebClient = new WebClient())
                    {
                        myWebClient.DownloadFile(uri, stagingPath);
                    }
                }
                catch (WebException e)
                {
                    Console.Error.WriteLine(Output.Red($"Could not download DarkRift {version} - {tier} (.NET {platform}):\n\t{e.Message}"));
                    return null;
                }

                Console.WriteLine($"Extracting package...");

                Directory.CreateDirectory(fullPath);

                ZipFile.ExtractToDirectory(stagingPath, fullPath, true);

                Console.WriteLine(Output.Green($"Successfully downloaded package."));
            }

            return fullPath;
        }

        /// <summary>
        /// Queries or loads the latest version of DarkRift
        /// </summary>
        /// <returns>The latest version of DarkRift</returns>
        public static string GetLatestDarkRiftVersion()
        {
            if (latestDarkRiftVersion != null)
                return latestDarkRiftVersion;

            Console.WriteLine("Querying server for the latest DarkRift version...");

            string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/";
            try
            {
                using (WebClient myWebClient = new WebClient())
                {
                    string latestJson = myWebClient.DownloadString(uri);

                    // Parse out 'latest' field
                    VersionMetadata versionMetadata = VersionMetadata.Parse(latestJson);

                    Console.WriteLine($"Server says the latest version is {versionMetadata.Latest}.");

                    Profile profile = Profile.Load();
                    profile.LatestKnownDarkRiftVersion = versionMetadata.Latest;
                    profile.Save();

                    latestDarkRiftVersion = versionMetadata.Latest;

                    return versionMetadata.Latest;
                }
            }
            catch (WebException e)
            {
                Console.WriteLine(Output.Yellow($"Could not query latest DarkRift version from the server. Will use the last known latest instead.\n\t{e.Message}"));

                latestDarkRiftVersion = Profile.Load().LatestKnownDarkRiftVersion;

                if (latestDarkRiftVersion == null)
                {
                    Console.Error.WriteLine(Output.Red($"No latest DarkRift version stored locally!"));
                    return null;
                }

                Console.WriteLine($"Last known latest version is {latestDarkRiftVersion}.");

                return latestDarkRiftVersion;
            }
        }

        /// <summary>
        /// Returns the user's invoice number, or prompts for it if not set.
        /// </summary>
        /// <returns>The user's invoice number, or null if they do not have one.</returns>
        private static string GetInvoiceNumber()
        {
            Profile profile = Profile.Load();

            if (string.IsNullOrWhiteSpace(profile.InvoiceNumber))
            {
                Console.WriteLine("To download a Pro release you must provide an invoice number to verify your purchase. This will usually be found in your recept from the Unity Asset Store.");
                Console.WriteLine("Please enter it: ");
                string invoiceNumber = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    Console.Error.WriteLine(Output.Red("No invoice number passed, no changes made."));
                    return null;
                }

                profile.InvoiceNumber = invoiceNumber;
                profile.Save();
            }

            return profile.InvoiceNumber;
        }

        /// <summary>
        /// Gets the path to a specified documentation installation, downloading it if required.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <returns>The path to the documentation, or null, if it cannot be provided.</returns>
        internal static string GetDocumentationPath(string version)
        {
            string fullPath = Path.Combine(USER_DR_DIR, "documentation", version);

            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine($"Documentation for DarkRift {version} not installed! Downloading package...");

                string stagingPath = Path.Combine(USER_DR_DIR, "Download.zip");

                string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/{version}/Docs/";
                try
                {
                    using (WebClient myWebClient = new WebClient())
                    {
                        myWebClient.DownloadFile(uri, stagingPath);
                    }
                }
                catch (WebException e)
                {
                    Console.Error.WriteLine(Output.Red($"Could not download documentation for DarkRift {version}:\n\t{e.Message}"));
                    return null;
                }

                Console.WriteLine($"Extracting package...");

                Directory.CreateDirectory(fullPath);

                ZipFile.ExtractToDirectory(stagingPath, fullPath, true);

                Console.WriteLine(Output.Green($"Successfully downloaded package."));
            }

            return fullPath;
        }
    }
}
