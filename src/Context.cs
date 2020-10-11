using Microsoft.VisualBasic.CompilerServices;
using System;
using System.IO;

namespace DarkRift.Cli
{
    internal interface IContext
    {
        Profile Profile { get; }
        Project Project { get; }

        void Save();
    }

    /// <summary>
    /// The application's context.
    /// </summary>
    internal class Context : IContext
    {
        /// <summary>
        /// The profile to use.
        /// </summary>
        public Profile Profile { get; }

        /// <summary>
        /// The project to use.
        /// </summary>
        public Project Project { get; }

        /// <summary>
        /// The path to the profile file.
        /// </summary>
        private readonly string profileFile;

        /// <summary>
        /// The path to the project file.
        /// </summary>
        private readonly string projectFile;

        /// <summary>
        /// Creates a new context.
        /// </summary>
        /// <param name="profile">The profile to use.</param>
        /// <param name="project">The project to use.</param>
        /// <param name="profileFile">The path to the profile file.</param>
        /// <param name="projectFile">The path to the project file.</param>
        private Context(Profile profile, Project project, string profileFile, string projectFile)
        {
            Profile = profile;
            Project = project;
            this.profileFile = profileFile;
            this.projectFile = projectFile;
        }

        /// <summary>
        /// Load context from files.
        /// </summary>
        /// <param name="profileFile">The path to the profile file.</param>
        /// <param name="projectFile">The path to the project file.</param>
        /// <returns>The loaded context.</returns>
        public static Context Load(string profileFile, string projectFile)
        {
            Profile profile;
            try
            {
                profile = Profile.Load(profileFile);
            }
            catch (IOException)
            {
                profile = new Profile();
            }

            Project project;
            try
            {
                project = Project.Load(projectFile);
            }
            catch (IOException)
            {
                project = null;
            }

            return new Context(
                profile,
                project,
                profileFile,
                projectFile
            );
        }

        /// <summary>
        /// Update context files with changes made.
        /// </summary>
        public void Save()
        {
            Profile.Save(profileFile);
            Project?.Save(projectFile);
        }
    }
}
