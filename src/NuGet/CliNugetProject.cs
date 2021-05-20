using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace DarkRift.Cli.NuGet
{
    /// <summary>
    /// Nuget project using a single directory.
    /// </summary>
    /// <remarks>
    /// Mostly taken from https://github.com/NuGet/NuGet.Client/blob/09872950e37e3a27efd3dfcc31bf020342db079a/src/NuGet.Clients/NuGet.CommandLine/Common/InstallCommandProject.cs
    /// </remarks>
    internal class CliNugetProject : NuGetProject
    {
        /// <summary>
        /// Gets the folder project's root path.
        /// </summary>
        public string Root { get; set; }

        /// <summary>
        /// The framework version we're targeting.
        /// </summary>
        private readonly NuGetFramework targetFramework;

        /// <summary>
        /// The package path resolver.
        /// </summary>
        private readonly PackagePathResolver packagePathResolver;

        public CliNugetProject(string root, PackagePathResolver packagePathResolver, NuGetFramework targetFramework)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            this.packagePathResolver = packagePathResolver ?? throw new ArgumentNullException(nameof(packagePathResolver));
            this.targetFramework = targetFramework ?? throw new ArgumentNullException(nameof(targetFramework));

            InternalMetadata.Add(NuGetProjectMetadataKeys.Name, root);
            InternalMetadata.Add(NuGetProjectMetadataKeys.TargetFramework, targetFramework);
        }

        /// <summary>
        /// Gets installed packages.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{LocalPackageInfo}" />.</returns>
        private IEnumerable<LocalPackageInfo> GetLocalPackages()
        {
            var packages = Enumerable.Empty<LocalPackageInfo>();

            if (Directory.Exists(Root))
            {
                if (packagePathResolver.UseSideBySidePaths)
                {
                    // Id.Version
                    packages = LocalFolderUtility.GetPackagesConfigFolderPackages(Root, NullLogger.Instance);
                }
                else
                {
                    // Id
                    // Ignore packages that are in SxS or a different format.
                    packages = LocalFolderUtility.GetPackagesV2(Root, NullLogger.Instance)
                                                 .Where(PackageIsValidForPathResolver);
                }
            }

            return LocalFolderUtility.GetDistinctPackages(packages);
        }

        /// <summary>
        /// Gets installed packages.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{PackageReference}" />.</returns>
        public IEnumerable<PackageReference> GetFolderPackages()
        {
            return GetLocalPackages().Select(e => new PackageReference(e.Identity, targetFramework));
        }

        /// <summary>
        /// Gets frameworks for an installed package.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{FrameworkSpecificGroup}" />.</returns>
        public IEnumerable<FrameworkSpecificGroup> GetFrameworkItemsForPackage(PackageIdentity packageIdentity)
        {
            return GetLocalPackages().Single(p => p.Identity == packageIdentity)
                                      .GetReader()
                                      .GetFrameworkItems();
        }

        /// <summary>
        /// Asynchronously gets installed packages.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.
        /// The task result (<see cref="Task{TResult}.Result" />) returns an
        /// <see cref="IEnumerable{PackageReference}" />.</returns>
        public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync(CancellationToken _)
        {
            if (!packagePathResolver.UseSideBySidePaths)
            {
                // Without versions packages must be uninstalled to update them.
                var folderPackages = GetFolderPackages();
                return Task.FromResult(folderPackages);
            }

            // For SxS scenarios PackageManagement should not read these references, this would cause uninstalls.
            return Task.FromResult(Enumerable.Empty<PackageReference>());
        }

        /// <summary>
        /// Asynchronously installs a package.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <param name="downloadResourceResult">A download resource result.</param>
        /// <param name="nuGetProjectContext">A NuGet project context.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.
        /// The task result (<see cref="Task{TResult}.Result" />) returns a <see cref="bool" />
        /// indication successfulness of the operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="downloadResourceResult" />
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="nuGetProjectContext" />
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if the package stream for
        /// <paramref name="downloadResourceResult" /> is not seekable.</exception>
        public override Task<bool> InstallPackageAsync(
            PackageIdentity packageIdentity,
            DownloadResourceResult downloadResourceResult,
            INuGetProjectContext nuGetProjectContext,
            CancellationToken token)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            if (downloadResourceResult == null)
            {
                throw new ArgumentNullException(nameof(downloadResourceResult));
            }

            if (nuGetProjectContext == null)
            {
                throw new ArgumentNullException(nameof(nuGetProjectContext));
            }

            if (downloadResourceResult.Status == DownloadResourceResultStatus.Available && !downloadResourceResult.PackageStream.CanSeek)
            {
                throw new ArgumentException("Package stream should be seekable.", nameof(downloadResourceResult));
            }

            var packageDirectory = packagePathResolver.GetInstallPath(packageIdentity);

            return ConcurrencyUtilities.ExecuteWithFileLockedAsync(
                packageDirectory,
                action: async cancellationToken =>
                {
                    var packageExtractionContext = nuGetProjectContext.PackageExtractionContext;

                    // 1. Check if the Package already exists at root, if so, return false
                    if (PackageExists(packageIdentity, packageExtractionContext.PackageSaveMode))
                    {
                        nuGetProjectContext.Log(MessageLevel.Info, "Package already exists in folder.", packageIdentity, Root);
                        return false;
                    }

                    nuGetProjectContext.Log(MessageLevel.Info, "Adding package to folder", packageIdentity, Path.GetFullPath(Root));

                    // 2. Call PackageExtractor to extract the package into the root directory of this FileSystemNuGetProject
                    if (downloadResourceResult.Status == DownloadResourceResultStatus.Available)
                    {
                        downloadResourceResult.PackageStream.Seek(0, SeekOrigin.Begin);
                    }

                    var addedPackageFilesList = new List<string>();
                    if (downloadResourceResult.PackageReader != null)
                    {
                        if (downloadResourceResult.Status == DownloadResourceResultStatus.AvailableWithoutStream)
                        {
                            addedPackageFilesList.AddRange(
                                await PackageExtractor.ExtractPackageAsync(
                                    downloadResourceResult.PackageSource,
                                    downloadResourceResult.PackageReader,
                                    packagePathResolver,
                                    packageExtractionContext,
                                    cancellationToken,
                                    nuGetProjectContext.OperationId));
                        }
                        else
                        {
                            addedPackageFilesList.AddRange(
                                await PackageExtractor.ExtractPackageAsync(
                                    downloadResourceResult.PackageSource,
                                    downloadResourceResult.PackageReader,
                                    downloadResourceResult.PackageStream,
                                    packagePathResolver,
                                    packageExtractionContext,
                                    cancellationToken,
                                    nuGetProjectContext.OperationId));
                        }
                    }
                    else
                    {
                        addedPackageFilesList.AddRange(
                            await PackageExtractor.ExtractPackageAsync(
                                downloadResourceResult.PackageSource,
                                downloadResourceResult.PackageStream,
                                packagePathResolver,
                                packageExtractionContext,
                                cancellationToken,
                                nuGetProjectContext.OperationId));
                    }

                    var packageSaveMode = GetPackageSaveMode(nuGetProjectContext);
                    if (packageSaveMode.HasFlag(PackageSaveMode.Nupkg))
                    {
                        var packageFilePath = GetInstalledPackageFilePath(packageIdentity);
                        if (File.Exists(packageFilePath))
                        {
                            addedPackageFilesList.Add(packageFilePath);
                        }
                    }

                    // Pend all the package files including the nupkg file
                    FileSystemUtility.PendAddFiles(addedPackageFilesList, Root, nuGetProjectContext);

                    nuGetProjectContext.Log(MessageLevel.Info, "Added package to folder.", packageIdentity, Path.GetFullPath(Root));

                    // Extra logging with source for verbosity detailed
                    // Used by external tool CoreXT to track package provenance
                    if (!string.IsNullOrEmpty(downloadResourceResult.PackageSource))
                    {
                        nuGetProjectContext.Log(MessageLevel.Debug, "Added package to folder from source.", packageIdentity, Path.GetFullPath(Root), downloadResourceResult.PackageSource);
                    }

                    return true;
                },
                token: token);
        }

        /// <summary>
        /// Asynchronously uninstalls a package.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <param name="nuGetProjectContext">A NuGet project context.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.
        /// The task result (<see cref="Task{TResult}.Result" />) returns a <see cref="bool" />
        /// indication successfulness of the operation.</returns>
        public override async Task<bool> UninstallPackageAsync(
            PackageIdentity packageIdentity,
            INuGetProjectContext nuGetProjectContext,
            CancellationToken token)
        {
            // Delete the package for nuget.exe install/update scenarios
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            if (nuGetProjectContext == null)
            {
                throw new ArgumentNullException(nameof(nuGetProjectContext));
            }

            var installedPackagesList = await GetInstalledPackagesAsync(token);
            var packageReference = installedPackagesList.Where(p => p.PackageIdentity.Equals(packageIdentity)).FirstOrDefault();
            if (packageReference == null)
            {
                // Package does not exist
                return false;
            }

            var packageFilePath = GetInstalledPackageFilePath(packageIdentity);
            if (File.Exists(packageFilePath))
            {
                var packageDirectoryPath = Path.GetDirectoryName(packageFilePath);
                using (var packageReader = new PackageArchiveReader(packageFilePath))
                {
                    var (runtimePackageDirectory, installedSatelliteFiles) = await PackageHelper.GetInstalledSatelliteFilesAsync(
                        packageReader,
                        packagePathResolver,
                        GetPackageSaveMode(nuGetProjectContext),
                        token
                    );

                    if (!string.IsNullOrEmpty(runtimePackageDirectory))
                    {
                        try
                        {
                            // Delete all the package files now
                            FileSystemUtility.DeleteFiles(installedSatelliteFiles, runtimePackageDirectory, nuGetProjectContext);
                        }
                        catch (Exception ex)
                        {
                            nuGetProjectContext.Log(MessageLevel.Warning, ex.Message);
                            // Catch all exception with delete so that the package file is always deleted
                        }
                    }

                    // Get all the package files before deleting the package file
                    var installedPackageFiles = await PackageHelper.GetInstalledPackageFilesAsync(
                        packageReader,
                        packageIdentity,
                        packagePathResolver,
                        GetPackageSaveMode(nuGetProjectContext),
                        token);

                    try
                    {
                        // Delete all the package files now
                        FileSystemUtility.DeleteFiles(installedPackageFiles, packageDirectoryPath, nuGetProjectContext);
                    }
                    catch (Exception ex)
                    {
                        nuGetProjectContext.Log(MessageLevel.Warning, ex.Message);
                        // Catch all exception with delete so that the package file is always deleted
                    }
                }

                // Delete the package file
                FileSystemUtility.DeleteFile(packageFilePath, nuGetProjectContext);

                // Delete the package directory if any
                FileSystemUtility.DeleteDirectorySafe(packageDirectoryPath, recursive: true, nuGetProjectContext: nuGetProjectContext);

                // If this is the last package delete the package directory
                // If this is the last package delete the package directory
                if (!FileSystemUtility.GetFiles(Root, string.Empty, "*.*").Any()
                    && !FileSystemUtility.GetDirectories(Root, string.Empty).Any())
                {
                    FileSystemUtility.DeleteDirectorySafe(Root, recursive: false, nuGetProjectContext: nuGetProjectContext);
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if a package is installed based on the presence of a .nupkg file.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <returns>A flag indicating whether or not the package is installed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        public bool PackageExists(PackageIdentity packageIdentity)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            return PackageExists(packageIdentity, PackageSaveMode.Nupkg);
        }

        /// <summary>
        /// Determines if a package is installed based on the provided package save mode.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <param name="packageSaveMode">A package save mode.</param>
        /// <returns>A flag indicating whether or not the package is installed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        public bool PackageExists(PackageIdentity packageIdentity, PackageSaveMode packageSaveMode)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            var nupkgPath = GetInstalledPackageFilePath(packageIdentity);
            var nuspecPath = GetInstalledManifestFilePath(packageIdentity);

            var packageExists = !string.IsNullOrEmpty(nupkgPath);
            var manifestExists = !string.IsNullOrEmpty(nuspecPath);

            // When using -ExcludeVersion check that the actual package version matches.
            if (!packagePathResolver.UseSideBySidePaths)
            {
                if (packageExists)
                {
                    using var reader = new PackageArchiveReader(nupkgPath);
                    packageExists = packageIdentity.Equals(reader.NuspecReader.GetIdentity());
                }

                if (manifestExists)
                {
                    var reader = new NuspecReader(nuspecPath);
                    packageExists = packageIdentity.Equals(reader.GetIdentity());
                }
            }

            if (!packageExists)
            {
                packageExists |= !string.IsNullOrEmpty(GetPackageDownloadMarkerFilePath(packageIdentity));
            }

            // A package must have either a nupkg or a nuspec to be valid
            var result = packageExists || manifestExists;

            // Verify nupkg present if specified
            if ((packageSaveMode & PackageSaveMode.Nupkg) == PackageSaveMode.Nupkg)
            {
                result &= packageExists;
            }

            // Verify nuspec present if specified
            if ((packageSaveMode & PackageSaveMode.Nuspec) == PackageSaveMode.Nuspec)
            {
                result &= manifestExists;
            }

            return result;
        }

        /// <summary>
        /// Determines if a manifest is installed.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <returns>A flag indicating whether or not the package is installed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        public bool ManifestExists(PackageIdentity packageIdentity)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            var path = GetInstalledManifestFilePath(packageIdentity);

            var exists = !string.IsNullOrEmpty(path);

            if (exists && !packagePathResolver.UseSideBySidePaths)
            {
                var reader = new NuspecReader(path);
                exists = packageIdentity.Equals(reader.GetIdentity());
            }

            return exists;
        }

        /// <summary>
        /// Determines if a manifest is installed.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <returns>A flag indicating whether or not the package is installed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        public bool PackageAndManifestExists(PackageIdentity packageIdentity)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            return !string.IsNullOrEmpty(GetInstalledPackageFilePath(packageIdentity)) && !string.IsNullOrEmpty(GetInstalledManifestFilePath(packageIdentity));
        }

        /// <summary>
        /// Asynchronously copies satellite files.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <param name="nuGetProjectContext">A NuGet project context.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.
        /// The task result (<see cref="Task{TResult}.Result" />) returns a <see cref="bool" />
        /// indication successfulness of the operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="nuGetProjectContext" />
        /// is <c>null</c>.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="token" />
        /// is cancelled.</exception>
        public async Task<bool> CopySatelliteFilesAsync(
            PackageIdentity packageIdentity,
            INuGetProjectContext nuGetProjectContext,
            CancellationToken token)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            if (nuGetProjectContext == null)
            {
                throw new ArgumentNullException(nameof(nuGetProjectContext));
            }

            token.ThrowIfCancellationRequested();

            var copiedSatelliteFiles = await PackageExtractor.CopySatelliteFilesAsync(
                packageIdentity,
                packagePathResolver,
                GetPackageSaveMode(nuGetProjectContext),
                nuGetProjectContext.PackageExtractionContext,
                token);

            FileSystemUtility.PendAddFiles(copiedSatelliteFiles, Root, nuGetProjectContext);

            return copiedSatelliteFiles.Any();
        }

        /// <summary>
        /// Gets the package .nupkg file path if it exists; otherwise, <c>null</c>.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <returns>The package .nupkg file path if it exists; otherwise, <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        public string GetInstalledPackageFilePath(PackageIdentity packageIdentity)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            // Check the expected location before searching all directories
            var packageDirectory = packagePathResolver.GetInstallPath(packageIdentity);
            var packageName = packagePathResolver.GetPackageFileName(packageIdentity);

            var installPath = Path.GetFullPath(Path.Combine(packageDirectory, packageName));

            // Keep the previous optimization of just going by the existance of the file if we find it.
            if (File.Exists(installPath))
            {
                return installPath;
            }

            // If the file was not found check for non-normalized paths and verify the id/version
            LocalPackageInfo package = null;

            if (packagePathResolver.UseSideBySidePaths)
            {
                // Search for a folder with the id and version
                package = LocalFolderUtility.GetPackagesConfigFolderPackage(
                    Root,
                    packageIdentity,
                    NullLogger.Instance);
            }
            else
            {
                // Search for just the id
                package = LocalFolderUtility.GetPackageV2(
                    Root,
                    packageIdentity,
                    NullLogger.Instance);
            }

            if (package != null && packageIdentity.Equals(package.Identity))
            {
                return package.Path;
            }

            // Default to empty
            return string.Empty;
        }

        /// <summary>
        /// Gets the package .nuspec file path if it exists; otherwise, <c>null</c>.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <returns>The package .nuspec file path if it exists; otherwise, <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        public string GetInstalledManifestFilePath(PackageIdentity packageIdentity)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            // Check the expected location before searching all directories
            var packageDirectory = packagePathResolver.GetInstallPath(packageIdentity);
            var manifestName = packagePathResolver.GetManifestFileName(packageIdentity);

            var installPath = Path.GetFullPath(Path.Combine(packageDirectory, manifestName));

            // Keep the previous optimization of just going by the existance of the file if we find it.
            if (File.Exists(installPath))
            {
                return installPath;
            }

            // Don't look in non-normalized paths for nuspec
            return string.Empty;
        }

        /// <summary>
        /// Gets the package download marker file path if it exists; otherwise, <c>null</c>.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <returns>The package download marker file path if it exists; otherwise, <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        public string GetPackageDownloadMarkerFilePath(PackageIdentity packageIdentity)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            var packageDirectory = packagePathResolver.GetInstallPath(packageIdentity);
            var fileName = packagePathResolver.GetPackageDownloadMarkerFileName(packageIdentity);

            var filePath = Path.GetFullPath(Path.Combine(packageDirectory, fileName));

            // Keep the previous optimization of just going by the existance of the file if we find it.
            if (File.Exists(filePath))
            {
                return filePath;
            }

            return null;
        }

        /// <summary>
        /// Gets the package directory path if the package exists; otherwise, <c>null</c>.
        /// </summary>
        /// <param name="packageIdentity">A package identity.</param>
        /// <returns>The package directory path if the package exists; otherwise, <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packageIdentity" />
        /// is <c>null</c>.</exception>
        public string GetInstalledPath(PackageIdentity packageIdentity)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            var installFilePath = GetInstalledPackageFilePath(packageIdentity);

            if (!string.IsNullOrEmpty(installFilePath))
            {
                return Path.GetDirectoryName(installFilePath);
            }

            // Default to empty
            return string.Empty;
        }

        private PackageSaveMode GetPackageSaveMode(INuGetProjectContext nuGetProjectContext)
        {
            return nuGetProjectContext.PackageExtractionContext?.PackageSaveMode ?? PackageSaveMode.Defaultv2;
        }

        /// <summary>
        /// Verify the package directory name is the same name that
        /// the path resolver creates.
        /// </summary>
        private bool PackageIsValidForPathResolver(LocalPackageInfo package)
        {
            DirectoryInfo packageDirectory = null;

            if (File.Exists(package.Path))
            {
                // Get the parent directory
                packageDirectory = new DirectoryInfo(Path.GetDirectoryName(package.Path));
            }
            else
            {
                // Use the directory directly
                packageDirectory = new DirectoryInfo(package.Path);
            }

            // Verify that the package directory matches the expected name
            var expectedName = packagePathResolver.GetPackageDirectoryName(package.Identity);
            return StringComparer.OrdinalIgnoreCase.Equals(
                packageDirectory.Name,
                expectedName);
        }
    }
}
