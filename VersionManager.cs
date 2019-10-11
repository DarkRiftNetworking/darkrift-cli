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
    class VersionManager
    {
        /// <summary>
        /// The DarkRift settings directory path.
        /// </summary>
        private static readonly string USER_DR_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".darkrift");

        /// <summary>
        /// Gets the path to a specified installation, downloading it if required.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="netStandard">Whether the .NET Standard build should be used.</param>
        /// <returns>The path to the installation, or null, if it cannot be provided.</returns>
        public static string GetInstallationPath(Version version, bool pro, bool netStandard)
        {
            string fullPath = Path.Combine(USER_DR_DIR, "installed", pro ? "pro" : "free", netStandard ? "standard" : "framework", version.ToString());

            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine($"DarkRift {version} - {(pro ? "Pro" : "Free")} (.NET {(netStandard ? "Standard" : "Framework")}) not installed! Downloading package...");

                string stagingPath = Path.Combine(USER_DR_DIR, "Download.zip");
                
                string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/{(pro ? "Pro" : "Free")}/{(netStandard ? "Standard" : "Framework")}/{version}.zip";
                if (pro)
                {
                    string invoiceNumber = GetInvoiceNumber();
                    if (invoiceNumber == null)
                    {
                        Console.WriteLine(Output.Red($"You must provide an invoice number in order to download Pro DarkRift releases."));
                        return null;
                    }

                    uri += $"?invoice={invoiceNumber}";
                }

                // TODO upload these versions in non-unitypackage format
                using (WebClient myWebClient = new WebClient())
                {
                    myWebClient.DownloadFile(uri, stagingPath);
                }

                Console.WriteLine($"Extracting package...");

                Directory.CreateDirectory(fullPath);

                ZipFile.ExtractToDirectory(stagingPath, fullPath, true);

                Console.WriteLine(Output.Green($"Successfully downloaded package."));
            }

            return fullPath;
        }

        /// <summary>
        /// Returns the user's invoice number, or prompts for it if not set.
        /// </summary>
        /// <returns>The user's invoice number, or null if they do not have one.</returns>
        private static string GetInvoiceNumber()
        {
            Profile profile = Profile.Load();

            if (String.IsNullOrWhiteSpace(profile.InvoiceNumber))
            {
                Console.WriteLine("To download a Pro release you must provide an invoice number to verify your purchase. This will usually be found in your recept from the Unity Asset Store.");
                Console.WriteLine("Please enter it: ");
                string invoiceNumber = Console.ReadLine();

                if (String.IsNullOrWhiteSpace(invoiceNumber))
                {
                    Console.WriteLine(Output.Red("No invoice number passed, no changes made."));
                    return null;
                }

                profile.InvoiceNumber = invoiceNumber;
                profile.Save();
            }

            return profile.InvoiceNumber;
        }
    }
}