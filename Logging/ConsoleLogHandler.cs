using System;
using System.Diagnostics;

namespace QuantConnect.Logging
{
    /// <summary>
    /// ILogHandler implementation that writes log output to console.
    /// </summary>
    public class ConsoleLogHandler : ILogHandler
    {
        private const string DateFormat = "yyyyMMdd HH:mm:ss";

        /// <inheritdoc />
        public void Error(string text)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(DateTime.Now.ToString(DateFormat) + " ERROR:: " + text);
            Console.ForegroundColor = original;

            //Log to system log:
            //Only run logger on Linux, this conditional copied from OS.IsLinux and then inverted
            var platform = (int)Environment.OSVersion.Platform;
            if (platform != 4 && platform != 6 && platform != 128) return;

            try
            {
                var cExecutable = new ProcessStartInfo
                {
                    FileName = "logger",
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    Arguments = "'" + text + "'",
                };
                //Don't wait for exit:
                Process.Start(cExecutable);
            }
            catch (Exception err)
            {
                Console.WriteLine("Log.SystemLog(): Error with system log: " + err.Message);
            }
        }

        /// <inheritdoc />
        public void Debug(string text)
        {
            Console.WriteLine(DateTime.Now.ToString(DateFormat) + " DEBUGGING :: " + text);
        }

        /// <inheritdoc />
        public void Trace(string text)
        {
            Console.WriteLine(DateTime.Now.ToString(DateFormat) + " Trace:: " + text);
        }
    }
}