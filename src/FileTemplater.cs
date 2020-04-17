using System;
using System.IO;

namespace DarkRift.Cli
{
    /// <summary>
    ///     Handles templating of generated files.
    /// </summary>
    internal class FileTemplater
    {
        /// <summary>
        ///     Template the given path file's path and content.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="resourceName">The name of the resource being created.</param>
        /// <param name="darkriftVersion">The version of DarkRift being used.</param>
        /// <param name="tier">The tier of DarkRift being used.</param>
        /// <param name="platform">The platform the DarkRift being used was built for.</param>
        public static void TemplateFileAndPath(string filePath, string resourceName, Version darkriftVersion, ServerTier tier, ServerPlatform platform)
        {
            string resolvedPath = TemplateString(filePath, resourceName, darkriftVersion, tier, platform);

            // Template the content of files containing __c__
            if (resolvedPath.Contains("__c__"))
            {
                resolvedPath = resolvedPath.Replace("__c__", "");
                File.WriteAllText(filePath, TemplateString(File.ReadAllText(filePath), resourceName, darkriftVersion, tier, platform));
            }

            if (resolvedPath != filePath)
                File.Move(filePath, resolvedPath);

            // Delete files containing __d__
            if (resolvedPath.Contains("__d__"))
                File.Delete(resolvedPath);
        }

        /// <summary>
        ///     Template the given string.
        /// </summary>
        /// <param name="text">The string to template.</param>
        /// <param name="resourceName">The name of the resource being created.</param>
        /// <param name="darkriftVersion">The version of DarkRift being used.</param>
        /// <param name="tier">The tier of DarkRift being used.</param>
        /// <param name="platform">The platform the DarkRift being used was built for.</param>
        private static string TemplateString(string text, string resourceName, Version darkriftVersion, ServerTier tier, ServerPlatform platform)
        {
            // Keep files containing __k__
            if (text.Contains("__k__"))
                text = text.Replace("__k__", "");

            // Template __n__ to the resource name
            if (text.Contains("__n__"))
                text = text.Replace("__n__", resourceName);

            // Template __v__ to the darkrift version
            if (text.Contains("__v__"))
                text = text.Replace("__v__", darkriftVersion.ToString());

            // Template __t__ to 'Pro' or 'Free'
            if (text.Contains("__t__"))
                text = text.Replace("__t__", tier.ToString());

            // Template __p__ to 'Standard' or 'Framework'
            if (text.Contains("__p__"))
                text = text.Replace("__p__", platform.ToString());

            return text;
        }
    }
}
