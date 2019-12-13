using System.ComponentModel;
using System.Diagnostics;

namespace DarkRift.Cli
{
    /// <summary>
    /// Utility for starting the browser on a specific URL.
    /// </summary>
    internal class BrowserUtil
    {
        /// <summary>
        /// Opens the browser to a specified URL.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        internal static void OpenTo(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Win32Exception)
            {
                // Windows 8/10 can have issues when the browser is not installed correctly, to work around this use explorer.exe to open the URL instead
                // https://stackoverflow.com/a/12248929/2755790
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{url}\""));
            }
        }
    }
}