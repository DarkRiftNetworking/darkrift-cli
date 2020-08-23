using System;

using DarkRift.Server;

namespace __n__
{
    /// <summary>
    /// A simple log writer that outputs to standard out.
    /// </summary>
    class LogWriter1 : LogWriter
    {
        /// <summary>
        /// The version number of your log writer.
        /// </summary>
        public override Version Version => new Version(1, 0, 0);

        /// <summary>
        /// Constructor. Called as the log writer is loaded.
        /// </summary>
        public LogWriter1(LogWriterLoadData logWriterLoadData) : base(logWriterLoadData)
        {
        }
    
        /// <summary>
        /// Handles a log event.
        /// </summary>
        public override void WriteEvent(WriteEventArgs args)
        {
            Console.WriteLine(args.FormattedMessage);
        }
    }
}
