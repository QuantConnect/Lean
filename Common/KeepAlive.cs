/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
*/
/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using QuantConnect.Logging;

namespace QuantConnect
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Keep Alive Thread: If the Ping isn't called within Timeout, terminate the instance:
    /// </summary>
    public static class KeepAlive
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private static bool _set = false;
        private static bool _started = false;
        private static DateTime _lastRegister = DateTime.Now;
        private static Object _lock = new Object();

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Last registered ping:
        /// </summary>
        public static DateTime LastRegister
        {
            get
            {
                return _lastRegister;
            }
            set
            {
                File.WriteAllText("keepalive.dat", value.ToString("u"));
                _lastRegister = value;
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Touch the common keep alive file:
        /// </summary>
        public static void Ping()
        {
            lock (_lock)
            {
                _set = true;
            }
        }

        /// <summary>
        /// Scan the set flag to write to the common file:
        /// </summary>
        public static void Run()
        {
            if (OS.IsWindows) return;

            while (true)
            {
                lock (_lock)
                {
                    LastRegister = DateTime.Now;
                }

                //Sleep for a few minutes; don't need to write this quickly.
                Thread.Sleep(60000);
            } // End of While

        } // End of Run();

    } // End of Keep Alive

} // End of Namespace.