using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using DarkRift;
using DarkRift.Client;
using DarkRift.Server;

using __n__.Client;
using __n__.Server;

namespace __n__.Tests
{
    [TestClass]
    public class NetworkListener1IT
    {
        [TestMethod]
        public void TestClientCanConnectToServer()
        {
            // GIVEN a running server
            DarkRiftServer server = StartServer();

            // WHEN I connect a client
            DarkRiftClient client = ConnectClient(server);

            // THEN the client is connected
            Assert.AreEqual(ConnectionState.Connected, client.ConnectionState);

            // AND the server contains one client
            Assert.AreEqual(1, server.ClientManager.Count);
        }

        /// <summary>
        /// Start a server with our listener and a basic configuration.
        /// </summary>
        /// <returns>The created server.</returns>
        private DarkRiftServer StartServer()
        {
            ServerSpawnData serverSpawnData = new ServerSpawnData();

            // Add listener type
            serverSpawnData.PluginSearch.PluginTypes.Add(typeof(NetworkListener1));

            // Create listener config
            serverSpawnData.Listeners.NetworkListeners.Add(new ServerSpawnData.ListenersSettings.NetworkListenerSettings
            {
                Name = "ListenerUnderTest",
                Type = nameof(NetworkListener1),
                Address = IPAddress.Loopback,
                Port = GetFreePort()
            });

            // Add a logger
            serverSpawnData.Logging.LogWriters.Add(new ServerSpawnData.LoggingSettings.LogWriterSettings{
                Name = "ConsoleWriter",
                Type = "ConsoleWriter",
                LogLevels = new LogType[] { LogType.Trace, LogType.Info, LogType.Warning, LogType.Error, LogType.Fatal }
            });
            serverSpawnData.Logging.LogWriters[0].Settings.Add("useFastAnsiColoring", "false");

            DarkRiftServer server = new DarkRiftServer(serverSpawnData);
            server.Start();
            return server;
        }

        /// <summary>
        /// Connect a client with our client connection to the given server.
        /// </summary>
        /// <returns>The created client.</returns>
        private DarkRiftClient ConnectClient(DarkRiftServer server)
        {
            NetworkClientConnection1 connection = new NetworkClientConnection1(IPAddress.Loopback, server.NetworkListenerManager.GetNetworkListenerByName("ListenerUnderTest").Port);

            DarkRiftClient client = new DarkRiftClient();
            client.Connect(connection);

            return client;
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
    }
}
