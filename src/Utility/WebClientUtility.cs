using System.Net;

namespace DarkRift.Cli.Utility
{
    internal interface IWebClientUtility
    {
        void DownloadFile(string address, string fileName);
        string DownloadString(string address);
    }

    /// <summary>
    /// Utility abstracting common web client methods to improve the testability of modules.
    /// </summary>
    internal class WebClientUtility : IWebClientUtility
    {
        /// <summary>
        /// The web client being wrapped.
        /// </summary>
        private readonly WebClient webClient;

        /// <summary>
        /// Creates a new WebClientUtility.
        /// </summary>
        /// <param name="webClient">The web client being wrapped.</param>
        public WebClientUtility(WebClient webClient)
        {
            this.webClient = webClient;
        }

        /// <summary>
        /// Downloads a file from the specified address.
        /// </summary>
        /// <param name="address">The address to download from.</param>
        /// <param name="fileName">The file to download to.</param>
        public void DownloadFile(string address, string fileName)
        {
            webClient.DownloadFile(address, fileName);
        }

        /// <summary>
        /// Downloads a string from the specified address.
        /// </summary>
        /// <param name="address">The address to download from.</param>
        /// <returns>The downloaded string.</param>
        public string DownloadString(string address)
        {
            return webClient.DownloadString(address);
        }
    }
}
