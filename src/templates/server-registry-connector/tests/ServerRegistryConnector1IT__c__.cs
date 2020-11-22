using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DarkRift.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace __n__.Tests
{
    /// <summary>
    /// Tests for the ServerRegistryConnector1.
    /// </sumamry>
    [TestClass]
    public class ServerRegistryConnector1IT
    {
        [TestInitialize]
        public void Initialize()
        {

        }

        [TestCleanup]
        public void Cleanup()
        {

        }

        [TestMethod]
        public void TestServerCanJoin()
        {
            // GIVEN a server running in group 1
            DarkRiftServer server1 = StartServer("Group1", true);

            // WHEN a second server starts
            DarkRiftServer server2 = StartServer("Group2", true);

            // THEN the first server discovers the second server
            WaitUntil("Timed out waiting for server 1 to discover server 2.", () => Assert.AreEqual(1, server1.RemoteServerManager.GetGroup("Group2").Count), TimeSpan.FromSeconds(30));

            // AND the second server discovers the first server
            WaitUntil("Timed out waiting for server 2 to discover server 1.", () => Assert.AreEqual(1, server2.RemoteServerManager.GetGroup("Group1").Count), TimeSpan.FromSeconds(30));
        }

        [TestMethod]
        public void TestServerCanLeave()
        {
            // GIVEN a server running in group 1 and another in group 2
            DarkRiftServer server1 = StartServer("Group1", true);
            DarkRiftServer server2 = StartServer("Group2", true);

            // AND the servers know of each other
            WaitUntil("Timed out waiting for server 1 to discover server 2.", () => Assert.AreEqual(1, server1.RemoteServerManager.GetGroup("Group2").Count), TimeSpan.FromSeconds(30));
            WaitUntil("Timed out waiting for server 2 to discover server 1.", () => Assert.AreEqual(1, server2.RemoteServerManager.GetGroup("Group1").Count), TimeSpan.FromSeconds(30));

            // WHEN the first server closes
            server1.Dispose();

            // THEN the second server knows of the closure
            WaitUntil("Timed out waiting for server 2 to see server 1 left.", () => Assert.AreEqual(1, server2.RemoteServerManager.GetGroup("Group1").Count), TimeSpan.FromSeconds(30));
        }

        [TestMethod]
        public void TestServerCanTimeout()
        {
            // GIVEN a server running in group 1 without a health check
            DarkRiftServer server1 = StartServer("Group1", false);

            // AND a server running in group 2
            DarkRiftServer server2 = StartServer("Group2", true);

            // AND the servers know of each other
            WaitUntil("Timed out waiting for server 1 to discover server 2.", () => Assert.AreEqual(1, server1.RemoteServerManager.GetGroup("Group2").Count), TimeSpan.FromSeconds(30));
            WaitUntil("Timed out waiting for server 2 to discover server 1.", () => Assert.AreEqual(1, server2.RemoteServerManager.GetGroup("Group1").Count), TimeSpan.FromSeconds(30));

            // WHEN waiting (a long time)
            // THEN the second server sees server 1 timeout
            WaitUntil("Timed out waiting for server 2 to see server 1 left.", () => Assert.AreEqual(1, server2.RemoteServerManager.GetGroup("Group1").Count), TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// Start a server with our listener and a basic configuration.
        /// </summary>
        /// <param name="group">The server group to create the server in.</param>
        /// <param name="withHealthCheckAvailable">Whether the health cehck plugin should be loaded.</param>
        /// <returns>The created server.</returns>
        private DarkRiftServer StartServer(string group, bool withHealthCheckAvailable = true)
        {
            // Create a two group cluster
            ClusterSpawnData clusterSpawnData = new ClusterSpawnData();
            clusterSpawnData.Groups.Groups.Add(new ClusterSpawnData.GroupsSettings.GroupSettings {
                Name = "Group1",
                Visibility = ServerVisibility.Internal
            });
            clusterSpawnData.Groups.Groups.Add(new ClusterSpawnData.GroupsSettings.GroupSettings {
                Name = "Group2",
                Visibility = ServerVisibility.External,
            });
            clusterSpawnData.Groups.Groups[1].ConnectsTo.Add(new ClusterSpawnData.GroupsSettings.GroupSettings.ConnectsToSettings {
                Name = "Group1"
            });

            // And a server config
            ServerSpawnData serverSpawnData = new ServerSpawnData();
            serverSpawnData.Server.ServerGroup = group;

            // Create listener config
            ushort port = GetFreePort();
            serverSpawnData.Listeners.NetworkListeners.Add(new ServerSpawnData.ListenersSettings.NetworkListenerSettings
            {
                Name = "DefaultListener",
                Type = "BichannelListener",
                Address = IPAddress.Loopback,
                Port = port
            });

            // Add a logger
            serverSpawnData.Logging.LogWriters.Add(new ServerSpawnData.LoggingSettings.LogWriterSettings{
                Name = "ConsoleWriter",
                Type = "ConsoleWriter",
                LogLevels = new LogType[] { LogType.Trace, LogType.Info, LogType.Warning, LogType.Error, LogType.Fatal }
            });
            serverSpawnData.Logging.LogWriters[0].Settings.Add("useFastAnsiColoring", "false");

            // Add the registry connector type
            serverSpawnData.PluginSearch.PluginTypes.Add(typeof(ConsulServerRegistryConnector));

            // And load it in
            serverSpawnData.ServerRegistry = new ServerSpawnData.ServerRegistrySettings {
                AdvertisedHost = "localhost",
                AdvertisedPort = port
            };
            serverSpawnData.ServerRegistry.ServerRegistryConnector.Type = nameof(ServerRegistryConnector1);

            // Create a health check if necessary
            if (withHealthCheckAvailable)
            {
                ushort healthPort = GetFreePort();
                serverSpawnData.Plugins.Plugins.Add(new ServerSpawnData.PluginsSettings.PluginSettings {
                    Type = "HealthCheckPlugin"
                });
                serverSpawnData.Plugins.Plugins[0].Settings.Add("port", healthPort.ToString());

                // Add port to the registry connector
                serverSpawnData.ServerRegistry.ServerRegistryConnector.Settings.Add("healthCheckUrl", $"http://localhost:{healthPort}/health");
            }

            DarkRiftServer server = new DarkRiftServer(serverSpawnData, clusterSpawnData);
            server.StartServer();
            return server;
        }

        /// <summary>
        ///     Returns a port that is unallocated.
        /// </summary>
        /// <returns>The port found.</returns>
        private ushort GetFreePort()
        {
            using Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            return (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
        }

        /// <summary>
        ///     Waits until the given assertion passes, failing if it does not pass within the timeout.
        /// </summary>
        /// <param name="message">The failure message to assert.</param>
        /// <param name="assertion">The assertion function to wait for.</param>
        /// <param name="timeout">The maximum time to wait.</param>
        internal static void WaitUntil(string message, Action assertion, TimeSpan timeout)
        {
            DateTime failAt = DateTime.Now.Add(timeout);
            Exception lastException = null;
            do
            {
                try
                {
                    assertion.Invoke();
                    return;
                }
                catch (AssertFailedException e)
                {
                    lastException = e;
                }

                Thread.Sleep(100);
            }
            while (DateTime.Now < failAt);

            Assert.Fail(message + "\nLast failure was:\n" + lastException);
        }
    }
}
