using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.PackageExtraction;
using NuGet.Packaging.Signing;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

namespace DarkRift.Cli.NuGet
{
    /// <summary>
    /// Helper for interacting with NuGet libraries.
    /// </summary>
    internal class NuGetHelper
    {
        private readonly string packagesFolder;

        private readonly string projectPackagesFolder;

        private readonly Context context;

        /// <summary>
        /// Creates a new NuGet helper.
        /// </summary>
        /// <param name="packageFolder">The folder packages will be stored in once downloaded.</param>
        /// <param name="projectPackageFolder">The project folder packages will be stored in once downloaded.</param>
        /// <param name="context">The CLI context.</param>
        public NuGetHelper(string packageFolder, string projectPackageFolder, Context context)
        {
            this.context = context;
            this.packagesFolder = packageFolder;
            this.projectPackagesFolder = projectPackageFolder;
        }

        /// <summary>
        /// Adds a package to the project from the NuGet stream.
        /// </summary>
        /// <param name="packageId">The package to install.</param>
        /// <param name="version">The version of the package to install.</param>
        /// <param name="useFrameworkVersion">Specifies the framework version of this poackage should be installed</param>
        /// <param name="usePrereleaseVersion">Specifies prerelease versions can be installed</param>
        public void AddPackage(string packageId, string version, bool usePrereleaseVersion)
        {
            // On mono, parallel builds are broken for some reason. See https://gist.github.com/4201936 for the errors
            // That are thrown.
            if (RuntimeEnvironmentHelper.IsMono)
            {
                HttpSourceResourceProvider.Throttle = SemaphoreSlimThrottle.CreateBinarySemaphore();
            }

            InstallPackageAsync(
                packageId,
                version != null ? new NuGetVersion(version) : null,
                usePrereleaseVersion
            ).Wait();
        }

        private async Task InstallPackageAsync(string packageId, NuGetVersion version, bool usePrereleaseVersion)
        {
            // Avoid searching for the highest version in the global packages folder,
            // it needs to come from the feeds instead. Once found it may come from
            // the global packages folder unless NoCache is true.
            bool excludeCacheAsSource = version == null;

            var targetFramework = NuGetFramework.Parse(context.Project.Runtime.Platform == ServerPlatform.Core ? "netstandard2.0" : "net45");

            // Create the project and set the framework if available.
            var project = new CliNugetProject(
                root: packagesFolder,
                packagePathResolver: new PackagePathResolver(packagesFolder),
                targetFramework: targetFramework
            );

            var settings = Settings.LoadDefaultSettings(packagesFolder);               //TODO machine wide settings?
            var sourceProvider = new PackageSourceProvider(settings);
            var sourceRepositoryProvider = new SourceRepositoryProvider(sourceProvider);
            var packageManager = new NuGetPackageManager(sourceRepositoryProvider, settings, packagesFolder);

            var packageSources = GetPackageSources(settings, excludeCacheAsSource, sourceProvider);
            var primaryRepositories = packageSources.Select(sourceRepositoryProvider.CreateRepository);

            var allowPrerelease = usePrereleaseVersion || (version != null && version.IsPrerelease);

            using var sourceCacheContext = new SourceCacheContext();
            var resolutionContext = new ResolutionContext(
                DependencyBehavior.Lowest,
                includePrelease: allowPrerelease,
                includeUnlisted: false,
                versionConstraints: VersionConstraints.None,
                gatherCache: new GatherCache(),
                sourceCacheContext: sourceCacheContext
            );

            if (version == null)
            {
                // Write out a helpful message before the http messages are shown
                Console.WriteLine($"Installing package {packageId} to {packagesFolder}.");

                // Find the latest version using NuGetPackageManager
                var resolvePackage = await NuGetPackageManager.GetLatestVersionAsync(
                    packageId,
                    project,
                    resolutionContext,
                    primaryRepositories,
                    new CliNuGetLogger(),
                    CancellationToken.None);

                if (resolvePackage == null || resolvePackage.LatestVersion == null)
                {
                    throw new Exception("Unable to find package.");
                }

                version = resolvePackage.LatestVersion;
            }

            // Get a list of packages already in the folder.
            var installedPackages = project.GetFolderPackages();

            // Find existing versions of the package
            var alreadyInstalledVersions = new HashSet<NuGetVersion>(installedPackages
                .Where(e => StringComparer.OrdinalIgnoreCase.Equals(packageId, e.PackageIdentity.Id))
                .Select(e => e.PackageIdentity.Version));

            var packageIdentity = new PackageIdentity(packageId, version);

            // Check if the package already exists or a higher version exists already.
            var skipInstall = project.PackageExists(packageIdentity);

            // Skip if a higher version exists.
            skipInstall |= alreadyInstalledVersions.Any(e => e >= version);

            if (skipInstall)
            {
                Console.WriteLine("Package already exists.");
            }
            else
            {
                var clientPolicyContext = ClientPolicyContext.GetClientPolicy(settings, new CliNuGetLogger());

                var projectContext = new ConsoleProjectContext(new CliNuGetLogger())
                {
                    PackageExtractionContext = new PackageExtractionContext(
                        PackageSaveMode.Defaultv2,
                        PackageExtractionBehavior.XmlDocFileSaveMode,
                        clientPolicyContext,
                        new CliNuGetLogger())
                };

                PackageSaveMode effectivePackageSaveMode = PackageSaveMode.Files | PackageSaveMode.Nuspec;

                if (effectivePackageSaveMode != PackageSaveMode.None)
                {
                    projectContext.PackageExtractionContext.PackageSaveMode = effectivePackageSaveMode;
                }

                var downloadContext = new PackageDownloadContext(resolutionContext.SourceCacheContext, packagesFolder, false)
                {
                    ClientPolicyContext = clientPolicyContext
                };

                await packageManager.InstallPackageAsync(
                    project,
                    packageIdentity,
                    resolutionContext,
                    projectContext,
                    downloadContext,
                    primaryRepositories,
                    Enumerable.Empty<SourceRepository>(),
                    CancellationToken.None
                );
            }

            // Copy package to local dir
            // TODO Doesen't handle dependencies...
            IEnumerable<FrameworkSpecificGroup> availablePackages = project.GetFrameworkItemsForPackage(packageIdentity);
            FrameworkSpecificGroup bestPackage = NuGetFrameworkUtility.GetNearest(availablePackages, targetFramework);

            string packageRoot = project.GetInstalledPackageFilePath(packageIdentity);
            string projectPackageRoot = Path.Combine(projectPackagesFolder, packageId);

            foreach (string item in bestPackage.Items)
                File.Copy(Path.Combine(packageRoot, item), Path.Combine(projectPackageRoot, item));
        }

        protected IReadOnlyCollection<PackageSource> GetPackageSources(ISettings settings, bool excludeCacheAsSource, PackageSourceProvider sourceProvider)
        {
            var availableSources = sourceProvider.LoadPackageSources().Where(source => source.IsEnabled);
            var packageSources = new List<PackageSource>(availableSources);

            if (!excludeCacheAsSource)
            {
                // Add the v3 global packages folder
                var globalPackageFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

                if (!string.IsNullOrEmpty(globalPackageFolder) && Directory.Exists(globalPackageFolder))
                {
                    packageSources.Add(new FeedTypePackageSource(globalPackageFolder, FeedType.FileSystemV3));
                }
            }

            return packageSources;
        }
    }
}
