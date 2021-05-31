using System;
using System.IO;
using System.IO.Compression;
using Crayon;

namespace DarkRift.Cli.Templating
{
    /// <summary>
    /// Expands templates into folders.
    /// </summary>
    internal class Templater
    {
        /// <summary>
        /// The path to the templates directory.
        /// </summary>
        private readonly string templatesPath;

        /// <summary>
        /// Creates a new templater.
        /// </summary>
        /// <param name="templatesPath">The path to the templates directory.</param>
        public Templater(string templatesPath)
        {
            this.templatesPath = templatesPath;
        }

        /// <summary>
        /// Expands a template.
        /// </summary>
        /// <param name="type">The template to expand</param>
        /// <param name="targetDirectory">The directory to exapand into.</param>
        /// <param name="version">The DarkRift version to template with.</param>
        /// <param name="tier">The DarkRift tier to template with.</param>
        /// <param name="platform">The DarkRift platform to template with.</param>
        /// <param name="force">Whether to force expansion on a non-empty directory.</param>
        public void Template(string type, string targetDirectory, string version, ServerTier tier, string platform, bool force)
        {
            string templatePath = Path.Combine(templatesPath, type + ".zip");

            Directory.CreateDirectory(targetDirectory);

            if (Directory.GetFiles(targetDirectory).Length > 0 && !force)
                throw new DirectoryNotEmptyException();

            if (!File.Exists(templatePath))
                throw new ArgumentException("Cannot create from template, no template with that name exists.", nameof(type));

            Console.WriteLine($"Creating new {type} '{Path.GetFileName(targetDirectory)}' from template...");

            ZipFile.ExtractToDirectory(templatePath, targetDirectory, true);

            Console.WriteLine($"Cleaning up extracted artifacts...");

            foreach (string path in Directory.GetFiles(targetDirectory, "*.*", SearchOption.AllDirectories))
                FileTemplater.TemplateFileAndPath(path, Path.GetFileName(targetDirectory), version, tier, platform);

            Console.WriteLine(Output.Green($"Created '{Path.GetFileName(targetDirectory)}'"));
        }
    }
}
