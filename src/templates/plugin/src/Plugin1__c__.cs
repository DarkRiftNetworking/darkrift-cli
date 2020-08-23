using System;

using DarkRift;
using DarkRift.Server;

namespace __n__
{
    /// <summary>
    /// A simple command that relays messages to all clients
    /// </summary>
    class Plugin1 : Plugin
    {
        /// <summary>
        /// The version number of your plugin. Increasing this will trigger a plugin upgrade in DarkRift Pro.
        /// </summary>
        public override Version Version => new Version(1, 0, 0);

        /// <summary>
        /// Flag to DarkRift stating if this plugin can handle events from multiple threads. If you're unsure, leave this false!
        /// </summary>
        public override bool ThreadSafe => false;

        /// <summary>
        /// Constructor. Called as the plugin is loaded.
        /// </summary>
        public Plugin1(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
        }

        /// <summary>
        /// Event handler for when a client has connected.
        /// </summary>
        private void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += MessageReceived;
        }

        /// <summary>
        /// Event handler for when a client has disconnected.
        /// </summary>
        private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            e.Client.MessageReceived -= MessageReceived;
        }

        /// <summary>
        /// Event handler for when the server recevies a message from any client.
        /// </summary>
        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                foreach (IClient client in ClientManager.GetAllClients())
                    client.SendMessage(message, e.SendMode);
            }
        }
    }
}
