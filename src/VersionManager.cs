using System.IO;
using System;
using System.Net;
using System.IO.Compression;
using Crayon;
using System.Collections.Generic;

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
        private static Version latestDarkRiftVersion;

        /// <summary>
        /// Gets the path to a specified installation, downloading it if required.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="platform">Whether the .NET Standard build should be used.</param>
        /// <returns>The path to the installation, or null, if it cannot be provided.</returns>
        public static string GetInstallationPath(Version version, ServerTier tier, ServerPlatform platform)
        {
            return Path.Combine(USER_DR_DIR, "installed", tier.ToString().ToLower(), platform.ToString().ToLower(), version.ToString());
        }

        /// <summary>
        /// Downloads and installs a DarkRift version
        /// </summary>
        /// <param name="version">The version to be installed</param>
        /// <param name="tier">The tier</param>
        /// <param name="platform">The platform</param>
        /// <returns>True if installed successfully otherwise false</returns>
        public static bool DownloadVersion(Version version, ServerTier tier, ServerPlatform platform)
        {
            string fullPath = Path.Combine(USER_DR_DIR, "installed", tier.ToString().ToLower(), platform.ToString().ToLower(), version.ToString());

            string stagingPath = Path.Combine(USER_DR_DIR, "Download.zip");

            string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/{version}/{tier}/{platform}/";
            if (tier == ServerTier.Pro)
            {
                string invoiceNumber = GetInvoiceNumber();
                if (invoiceNumber == null)
                {
                    Console.Error.WriteLine(Output.Red($"You must provide an invoice number in order to download Pro DarkRift releases."));
                    return false;
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
                return false;
            }

            Console.WriteLine($"Extracting package...");

            Directory.CreateDirectory(fullPath);

            ZipFile.ExtractToDirectory(stagingPath, fullPath, true);

            Console.WriteLine(Output.Green($"Successfully downloaded package."));

            return true;
        }

        /// <summary>
        /// Checks if a version of Dark Rift is installed
        /// </summary>
        /// <param name="version"></param>
        /// <param name="tier"></param>
        /// <param name="platform"></param>
        /// <returns>True if is installed otherwise false</returns>
        public static bool IsVersionInstalled(Version version, ServerTier tier, ServerPlatform platform)
        {
            return GetVersions(tier, platform).Contains(version);
        }


        /// <summary>
        /// Gets a list of versions with specific tier and platform
        /// </summary>
        /// <param name="tier">The tier</param>
        /// <param name="platform">The platform</param>
        /// <returns></returns>
        public static List<Version> GetVersions(ServerTier tier, ServerPlatform platform)
        {
            try
            {
                string[] paths = Directory.GetDirectories(Path.Combine(USER_DR_DIR, "installed", $"{tier.ToString().ToLower()}", $"{platform.ToString().ToLower()}"));

                List<Version> versions = new List<Version>();

                // This removes the path and just leaves the version number
                for (int i = 0; i < paths.Length; i++)
                {
                    versions.Add(new Version(Path.GetFileNameWithoutExtension(paths[i])));
                }

                return versions;
            }
            catch
            {
                return new List<Version>();
            };
        }


        /// <summary>
        /// Lists installed DarkRift versions on the console along with the documentation
        /// </summary>
        public static void ListInstalledVersions()
        {
            // Since the free version only supports .Net Framework I'm not adding support here
            List<Version> freeVersions = GetVersions(ServerTier.Free, ServerPlatform.Framework);

            List<Version> proFramework = GetVersions(ServerTier.Pro, ServerPlatform.Framework);
            List<Version> proCore = GetVersions(ServerTier.Pro, ServerPlatform.Core);

            // Well, you gotta install it, you don't know what you are losing
            if (freeVersions.Count == 0 && proFramework.Count == 0 && proCore.Count == 0)
            {
                Console.Error.WriteLine(Output.Red($"You don't have any versions of DarkRift installed"));
                return;
            }

            foreach (Version version in freeVersions)
                PrintVersion(version, ServerTier.Free, ServerPlatform.Framework);
            foreach (Version version in proFramework)
                PrintVersion(version, ServerTier.Pro, ServerPlatform.Framework);
            foreach (Version version in proCore)
                PrintVersion(version, ServerTier.Pro, ServerPlatform.Core);
        }

        /// <summary>
        /// Prints version information on the console
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="platform">Whether the .NET Standard build should be used.</param>
        private static void PrintVersion(Version version, ServerTier tier, ServerPlatform platform)
        {
            string output = "";

            // There's no free or pro in documentation

            output += $"DarkRift {version} - {tier} ({platform})";

            if (Directory.Exists(Path.Combine(USER_DR_DIR, "documentation", version.ToString())))
                output += " and it's documentation are";
            else output += " is";

            output += " installed";

            Console.WriteLine(output);
        }

        /// <summary>
        /// Queries or loads the latest version of DarkRift
        /// </summary>
        /// <returns>The latest version of DarkRift</returns>
        public static Version GetLatestDarkRiftVersion()
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
        /// Gets the path to a specified documentation installation
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <returns>The path to the documentation, or null, if it cannot be provided.</returns>
        public static string GetDocumentationPath(Version version)
        {
            return Path.Combine(USER_DR_DIR, "documentation", version.ToString());
        }

        /// <summary>
        /// Downloads and installs the documentation of a version of Dark Rift
        /// </summary>
        /// <param name="version">The version of Dark Rift</param>
        /// <returns>True for success otherwise false</returns>
        public static bool DownloadDocumentation(Version version)
        {
            string fullPath = GetDocumentationPath(version);

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
                return false;
            }

            Console.WriteLine($"Extracting package...");

            Directory.CreateDirectory(fullPath);

            ZipFile.ExtractToDirectory(stagingPath, fullPath, true);

            Console.WriteLine(Output.Green($"Successfully downloaded package."));

            return true;
        }

        /// <summary>
        /// Checks if documentation for a specific version exists
        /// </summary>
        /// <param name="version">Version of Dark Rift</param>
        /// <returns>True if documentation found otherwise false</returns>
        public static bool IsDocumentationInstalled(Version version)
        {
            return Directory.Exists(GetDocumentationPath(version));
        }
    }
}
