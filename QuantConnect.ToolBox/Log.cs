using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Provides time stamped writing to the console
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Writes the message in normal text
        /// </summary>
        public static void Trace(string format, params object[] args)
        {
            Console.WriteLine("{0}: {1}", DateTime.UtcNow.ToString("o"), string.Format(format, args));
        }

        /// <summary>
        /// Writes the message in red
        /// </summary>
        public static void Error(string format, params object[] args)
        {
            var foregroundColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("{0}: ERROR:: {1}", DateTime.UtcNow.ToString("o"), string.Format(format, args));
            Console.ForegroundColor = foregroundColor;
        }
    }
}
