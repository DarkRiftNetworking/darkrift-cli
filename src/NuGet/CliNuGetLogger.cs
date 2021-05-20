using System;
using System.Threading.Tasks;
using NuGet.Common;

namespace DarkRift.Cli.NuGet
{
    /// <summary>
    /// Simple logger to the console.
    /// </summary>
    public class CliNuGetLogger : ILogger
    {
        public void Log(LogLevel level, string data)
        {
            Console.WriteLine(level + ": " + data);
        }

        public void Log(ILogMessage message)
        {
            Console.WriteLine(message.Level + ": " + message.Message);
        }

        public Task LogAsync(LogLevel level, string data)
        {
            Console.WriteLine(level + ": " + data);
            return Task.CompletedTask;
        }

        public Task LogAsync(ILogMessage message)
        {
            Console.WriteLine(message.Level + ": " + message.Message);
            return Task.CompletedTask;
        }

        public void LogDebug(string data)
        {
            Console.WriteLine(data);
        }

        public void LogError(string data)
        {
            Console.WriteLine(data);
        }

        public void LogInformation(string data)
        {
            Console.WriteLine(data);
        }

        public void LogInformationSummary(string data)
        {
            Console.WriteLine(data);
        }

        public void LogMinimal(string data)
        {
            Console.WriteLine(data);
        }

        public void LogVerbose(string data)
        {
            Console.WriteLine(data);
        }

        public void LogWarning(string data)
        {
            Console.WriteLine(data);
        }
    }
}
