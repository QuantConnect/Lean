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
using Newtonsoft.Json;
using QuantConnect.Logging;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Configuration class loads the required external setup variables to launch the Lean engine.
    /// </summary>
    public static class Config
    {
        //Location of the configuration file.
        private const string _config = "config.json";

        //Has the configuration been loaded from disk:
        private static bool _loaded;

        /// Initialize the settings array and its defaults:
        private static Dictionary<string, string> _settings = new Dictionary<string, string>
        {
            //User configurable: Select which algorithm the engine can run.
            {"algorithm-type-name", "BasicTemplateAlgorithm"},

            //Engine code:
            {"local", "true"},
            {"live-mode", "false"},
            {"data-folder", @"../../../Data/"},
            {"result-handler", "QuantConnect.Lean.Engine.Results.ConsoleResultHandler"},
            {"messaging-handler", "QuantConnect.Messaging.Messaging"},
            {"queue-handler", "QuantConnect.Queues.Queues"},
            {"api-handler", "QuantConnect.Api.Api"}
        };

        /// <summary>
        /// Initialize the configuration file and if it doesnt exist create one with the default values above.
        /// </summary>
        private static void Initialize()
        {
            var file = "";
            
            if (_loaded) return;

            // if we find the configuration, load it, otherwise just stick with the defaults in _settings
            if (File.Exists(_config))
            {
                file = File.ReadAllText(_config);
                _settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(file);
            }

            _loaded = true;
        }
        
        /// <summary>
        /// Get the matching config setting from the file searching for this key.
        /// </summary>
        /// <param name="key">String key value we're seaching for in the config file.</param>
        /// <param name="defaultValue"></param>
        /// <returns>String value of the configuration setting or empty string if nothing found.</returns>
        public static string Get(string key, string defaultValue = "")
        {
            var value = "";
            try
            {
                if (!_loaded) Initialize();

                if (_settings != null && _settings.ContainsKey(key))
                {
                    value = _settings[key];
                }
                else
                {
                    value = defaultValue;
                    Log.Trace("Config.Get(): Configuration key not found. Key: " + key + " - Using default value: "+ defaultValue);
                }
            }
            catch (Exception err)
            {
                Log.Error("Config.Get(): " + err.Message);
            }
            return value;
        }

        /// <summary>
        /// Sets a configuration value. This is really only used to help testing
        /// </summary>
        /// <param name="key">The key to be set</param>
        /// <param name="value">The new value</param>
        public static void Set(string key, string value)
        {
            _settings[key] = value;
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
            var value = Get(key);
            if (string.IsNullOrEmpty(value))
            {
                Log.Trace("Config.GetValue(): " + key + " - Using default value: " + defaultValue);
                return defaultValue;
            }

            var type = typeof (T);
            if (type.IsEnum)
            {
                return (T) Enum.Parse(type, value);
            }
            return (T)Convert.ChangeType(value, type);
        }
    }
}
