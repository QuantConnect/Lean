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
        /// Defines the delegate used to perform trace logging, this allows other application
        /// users of the toolbox projects to intercept their logging
        /// </summary>
        public static Action<string> TraceHandler = TraceHandlerImpl;

        /// <summary>
        /// Defines the delegate used to perform error logging, this allows other application
        /// users of the toolbox projects to intercept their logging
        /// </summary>
        public static Action<string> ErrorHandler = ErrorHandlerImpl;

        /// <summary>
        /// Writes the message in normal text
        /// </summary>
        public static void Trace(string format, params object[] args)
        {
            TraceHandler(string.Format(format, args));
        }

        /// <summary>
        /// Writes the message in red
        /// </summary>
        public static void Error(string format, params object[] args)
        {
            ErrorHandler(string.Format(format, args));
        }

        private static void TraceHandlerImpl(string msg)
        {
            Console.WriteLine("{0}: {1}", DateTime.UtcNow.ToString("o"), msg);
        }

        private static void ErrorHandlerImpl(string msg)
        {
            var foregroundColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("{0}: ERROR:: {1}", DateTime.UtcNow.ToString("o"), msg);
            Console.ForegroundColor = foregroundColor;
        }
    }
}
