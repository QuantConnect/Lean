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
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Configuration class loads the required external setup variables to launch the Lean engine.
    /// </summary>
    public static class Config
    {
        //Location of the configuration file.
        private const string ConfigurationFileName = "config.json";

        private static readonly Lazy<JObject> Settings = new Lazy<JObject>(() =>
        {
            // initialize settings inside a lazy for free thread-safe, one-time initialization
            if (!File.Exists(ConfigurationFileName))
            {
                return new JObject
                {
                    {"algorithm-type-name", "BasicTemplateAlgorithm"},
                    {"live-mode", false},
                    {"data-folder", "../../../Data/"},
                    {"messaging-handler", "QuantConnect.Messaging.Messaging"},
                    {"queue-handler", "QuantConnect.Queues.Queues"},
                    {"api-handler", "QuantConnect.Api.Api"},
                    {"setup-handler", "QuantConnect.Lean.Engine.Setup.ConsoleSetupHandler"},
                    {"result-handler", "QuantConnect.Lean.Engine.Results.ConsoleResultHandler"},
                    {"data-feed-handler", "QuantConnect.Lean.Engine.DataFeeds.FileSystemDataFeed"},
                    {"real-time-handler", "QuantConnect.Lean.Engine.RealTime.BacktestingRealTimeHandler"},
                    {"transaction-handler", "QuantConnect.Lean.Engine.TransactionHandlers.BacktestingTransactionHandler"}
                };
            }

            return JObject.Parse(File.ReadAllText(ConfigurationFileName));
        });

        /// <summary>
        /// Gets the currently selected environment. If sub-environments are defined,
        /// they'll be returned as {env1}.{env2}
        /// </summary>
        /// <returns>The fully qualified currently selected environment</returns>
        public static string GetEnvironment()
        {
            var environments = new List<string>();
            JToken currentEnvironment = Settings.Value;
            var env = currentEnvironment["environment"];
            while (currentEnvironment != null && env != null)
            {
                var currentEnv = env.Value<string>();
                environments.Add(currentEnv);
                var moreEnvironments = currentEnvironment["environments"];
                if (moreEnvironments == null)
                {
                    break;
                }

                currentEnvironment = moreEnvironments[currentEnv];
                env = currentEnvironment["environment"];
            }
            return string.Join(".", environments);
        }
        
        /// <summary>
        /// Get the matching config setting from the file searching for this key.
        /// </summary>
        /// <param name="key">String key value we're seaching for in the config file.</param>
        /// <param name="defaultValue"></param>
        /// <returns>String value of the configuration setting or empty string if nothing found.</returns>
        public static string Get(string key, string defaultValue = "")
        {
            // special case environment requests
            if (key == "environment") return GetEnvironment();

            var token = GetToken(Settings.Value, key);
            if (token == null)
            {
                Log.Trace(string.Format("Config.Get(): Configuration key not found. Key: {0} - Using default value: {1}", key, defaultValue));
                return defaultValue;
            }
            return token.Value<string>();
        }

        /// <summary>
        /// Sets a configuration value. This is really only used to help testing. The key heye can be
        /// specified as {environment}.key to set a value on a specific environment
        /// </summary>
        /// <param name="key">The key to be set</param>
        /// <param name="value">The new value</param>
        public static void Set(string key, string value)
        {
            JToken environment = Settings.Value;
            while (key.Contains("."))
            {
                var envName = key.Substring(0, key.IndexOf("."));
                key = key.Substring(key.IndexOf(".") + 1);
                environment = environment["environments"][envName];
            }
            environment[key] = value;
        }

        /// <summary>
        /// Get a boolean value configuration setting by a configuration key.
        /// </summary>
        /// <param name="key">String value of the configuration key.</param>
        /// <param name="defaultValue">The default value to use if not found in configuration</param>
        /// <returns>Boolean value of the config setting.</returns>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            return GetValue(key, defaultValue);
        }

        /// <summary>
        /// Get the int value of a config string.
        /// </summary>
        /// <param name="key">Search key from the config file</param>
        /// <param name="defaultValue">The default value to use if not found in configuration</param>
        /// <returns>Int value of the config setting.</returns>
        public static int GetInt(string key, int defaultValue = 0)
        {
            return GetValue(key, defaultValue);
        }

        /// <summary>
        /// Get the double value of a config string.
        /// </summary>
        /// <param name="key">Search key from the config file</param>
        /// <param name="defaultValue">The default value to use if not found in configuration</param>
        /// <returns>Double value of the config setting.</returns>
        public static double GetDouble(string key, double defaultValue = 0.0)
        {
            return GetValue(key, defaultValue);
        }

        /// <summary>
        /// Gets a value from configuration and converts it to the requested type, assigning a default if
        /// the configuration is null or empty
        /// </summary>
        /// <typeparam name="T">The requested type</typeparam>
        /// <param name="key">Search key from the config file</param>
        /// <param name="defaultValue">The default value to use if not found in configuration</param>
        /// <returns>Converted value of the config setting.</returns>
        public static T GetValue<T>(string key, T defaultValue = default(T))
            where T : IConvertible
        {
            // special case environment requests
            if (key == "environment" && typeof (T) == typeof (string)) return (T) (object) GetEnvironment();

            var token = GetToken(Settings.Value, key);
            if (token == null)
            {
                Log.Trace(string.Format("Config.GetValue(): {0} - Using default value: {1}", key, defaultValue));
                return defaultValue;
            }

            var type = typeof(T);
            var value = token.Value<string>();
            if (type.IsEnum)
            {
                return (T) Enum.Parse(type, value);
            }
            return (T) Convert.ChangeType(value, type);
        }

        private static JToken GetToken(JToken settings, string key)
        {
            return GetToken(settings, key, settings.SelectToken(key));
        }

        private static JToken GetToken(JToken settings, string key, JToken current)
        {
            var environmentSetting = settings.SelectToken("environment");
            if (environmentSetting != null)
            {
                var environment = settings.SelectToken("environments." + environmentSetting.Value<string>());
                var setting = environment.SelectToken(key);
                if (setting != null)
                {
                    current = setting;
                }
                // allows nesting of environments, live.tradier, live.interactive, ect...
                return GetToken(environment, key, current);
            }
            if (current == null)
            {
                return settings.SelectToken(key);
            }
            return current;
        }
    }
}
