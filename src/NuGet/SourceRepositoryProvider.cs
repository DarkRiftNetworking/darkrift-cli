using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace DarkRift.Cli.NuGet
{
    /// <summary>
    /// Simple source repository for NuGet.
    /// </summary>
    /// <remarks>
    /// Mostly taken from https://github.com/NuGet/NuGet.Client/blob/09872950e37e3a27efd3dfcc31bf020342db079a/src/NuGet.Clients/NuGet.CommandLine/CommandLineSourceRepositoryProvider.cs.
    /// </remarks>
    internal class SourceRepositoryProvider : ISourceRepositoryProvider
    {
        /// <summary>
        /// The package source provider being used.
        /// </summary>
        public IPackageSourceProvider PackageSourceProvider { get; }

        /// <summary>
        /// Cache of resource providers.
        /// </summary>
        private readonly List<Lazy<INuGetResourceProvider>> resourceProviders;

        /// <summary>
        /// The loaded repositories.
        /// </summary>
        private readonly List<SourceRepository> repositories;

        /// <summary>
        /// Creates a new source repository provider.
        /// </summary>
        /// <param name="packageSourceProvider">The package source provider to use.</param>
        public SourceRepositoryProvider(IPackageSourceProvider packageSourceProvider)
        {
            PackageSourceProvider = packageSourceProvider;

            resourceProviders = new List<Lazy<INuGetResourceProvider>>(FactoryExtensionsV3.GetCoreV3(Repository.Provider));

            // Create repositories
            repositories = PackageSourceProvider.LoadPackageSources()
                .Where(s => s.IsEnabled)
                .Select(CreateRepository)
                .ToList();
        }

        /// <summary>
        /// Retrieve repositories that have been loaded.
        /// </summary>
        public IEnumerable<SourceRepository> GetRepositories()
        {
            return repositories;
        }

        /// <summary>
        /// Create a repository.
        /// </summary>
        public SourceRepository CreateRepository(PackageSource source)
        {
            return CreateRepository(source, FeedType.Undefined);
        }

        /// <summary>
        /// Create a repository.
        /// </summary>
        public SourceRepository CreateRepository(PackageSource source, FeedType type)
        {
            return new SourceRepository(source, resourceProviders, type);
        }
    }
}
