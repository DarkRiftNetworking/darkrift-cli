using System.IO;
using System.IO.Compression;

namespace DarkRift.Cli.Utility
{
    internal interface IFileUtility
    {
        void Delete(string path);
        bool DirectoryExists(string path);
        void ExtractZipTo(string sourceZip, string targetDirectory);
        string[] GetDirectories(string path);
        string GetTempFileName();
    }

    /// <summary>
    /// Utility abstracting common file methods to improve the testability of modules.
    /// </summary>
    internal class FileUtility : IFileUtility
    {
        /// <summary>
        /// Mockable <see cref="Path.GetTempFileName"/>
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
            ZipFile.ExtractToDirectory(sourceZip, targetDirectory, true);
        }

        /// <summary>
        /// Mockable <see cref="File.Delete(string)"/>
        /// </summary>
        /// <param name="path"></param>
        public void Delete(string path)
        {
            File.Delete(path);
        }

        /// <summary>
        /// Mockable <see cref="Directory.Exists(string)"/>
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>Whether the directory exists.</returns>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Mockable <see cref="Directory.GetDirectories(string)"/>
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <returns></returns>
        public string[] GetDirectories(string path)
        {
            return Directory.GetDirectories(path);
        }
    }
}
