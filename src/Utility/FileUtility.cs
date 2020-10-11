using System.IO;
using System.IO.Compression;

namespace DarkRift.Cli.Utility
{
    internal interface IFileUtility
    {
        void Delete(string path);
        void ExtractZipTo(string sourceZip, string targetDirectory);
        string GetTempFileName();
    }

    /// <summary>
    /// Utility abstracting common file methods to improve the testability of modules.
    /// </summary>
    internal class FileUtility : IFileUtility
    {
        /// <summary>
        /// Get a temporary file path.
        /// </summary>
        /// <returns>The temporary file path.</returns>
        public string GetTempFileName()
        {
            return Path.GetTempFileName();
        }

        /// <summary>
        /// Extract the given zip into the specified directory.
        /// </summary>
        /// <param name="sourceZip">The zip to extract.</param>
        /// <param name="targetDirectory">The directory to extract to.</param>
        public void ExtractZipTo(string sourceZip, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            ZipFile.ExtractToDirectory(sourceZip, sourceZip, true);
        }

        /// <summary>
        /// Delete the specified file.
        /// </summary>
        /// <param name="path"></param>
        public void Delete(string path)
        {
            File.Delete(path);
        }
    }
}
