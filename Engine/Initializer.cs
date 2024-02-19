/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.IO;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Helper class to initialize a Lean engine
    /// </summary>
    public static class Initializer
    {
        /// <summary>
        /// Basic common Lean initialization
        /// </summary>
        public static void Start()
        {
            try
            {
                var mode = "RELEASE";
                #if DEBUG
                mode = "DEBUG";
                #endif

                Log.DebuggingEnabled = Config.GetBool("debug-mode");
                var destinationDir = Globals.ResultsDestinationFolder;
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                    Log.FilePath = Path.Combine(destinationDir, "log.txt");
                }
                Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

                Log.Trace($"Engine.Main(): LEAN ALGORITHMIC TRADING ENGINE v{Globals.Version} Mode: {mode} ({(Environment.Is64BitProcess ? "64" : "32")}bit) Host: {Environment.MachineName}");
                Log.Trace("Engine.Main(): Started " + DateTime.Now.ToShortTimeString());
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        /// <summary>
        /// Get and initializes System Handler
        /// </summary>
        public static LeanEngineSystemHandlers GetSystemHandlers()
        {
            var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);

            //Setup packeting, queue and controls system: These don't do much locally.
            systemHandlers.Initialize();

            return systemHandlers;
        }

        /// <summary>
        /// Get and initializes Algorithm Handler
        /// </summary>
        public static LeanEngineAlgorithmHandlers GetAlgorithmHandlers(bool researchMode = false)
        {
            return LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance, researchMode);
        }
    }
}
