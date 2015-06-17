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
using System.Linq;
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

        /// Initialize the settings array and its defaults:
        private static readonly IReadOnlyDictionary<string, string> DefaultSettings = new Dictionary<string, string>
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

        private static readonly Lazy<Dictionary<string, string>> Settings = new Lazy<Dictionary<string, string>>(() =>
        {
            // initialize settings inside a lazy for free thread-safe, one-time initialization
            if (!File.Exists(ConfigurationFileName))
            {
                return DefaultSettings.ToDictionary(setting => setting.Key, setting => setting.Value);
            }

            return ParseConfigurationFile(File.ReadAllText(ConfigurationFileName));
        });
        
        /// <summary>
        /// Get the matching config setting from the file searching for this key.
        /// </summary>
        /// <param name="key">String key value we're seaching for in the config file.</param>
        /// <param name="defaultValue"></param>
        /// <returns>String value of the configuration setting or empty string if nothing found.</returns>
        public static string Get(string key, string defaultValue = "")
        {
            string value;
            if (!Settings.Value.TryGetValue(key, out value))
            {
                Log.Trace("Config.Get(): Configuration key not found. Key: " + key + " - Using default value: " + defaultValue);
                return defaultValue;
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
            Settings.Value[key] = value;
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
            string value;
            if (!Settings.Value.TryGetValue(key, out value))
            {
                Log.Trace("Config.GetValue(): " + key + " - Using default value: " + defaultValue);
                return defaultValue;
            }

            var type = typeof (T);
            if (type.IsEnum)
            {
                return (T) Enum.Parse(type, value);
            }
            return (T) Convert.ChangeType(value, type);
        }

        /// <summary>
        /// Parses the specified configuration file contents into a key value pair. This include logic
        /// to detect the 'environment' and load those values on top of any default configuration
        /// </summary>
        /// <param name="configurationFileContents">The JSON configuration file's contents</param>
        /// <returns>A dictionary of configuration keys to values</returns>
        public static Dictionary<string, string> ParseConfigurationFile(string configurationFileContents)
        {
            var environment = string.Empty;
            // begin with the defaults and lay on top of them
            var settings = DefaultSettings.ToDictionary(x => x.Key, x => x.Value); 
            var environmentSettings = new Dictionary<string, string>();
            var configuration = JToken.Parse(configurationFileContents);
            foreach (var rootItem in configuration)
            {
                if (rootItem.Type == JTokenType.Property && rootItem.HasValues)
                {
                    var children = rootItem.Children();
                    foreach (var child in children)
                    {
                        // this is an environment
                        if (child.Type == JTokenType.Object && rootItem.Path == environment)
                        {
                            if (child.HasValues)
                            {
                                var children2 = child.Children();
                                foreach (var child2 in children2)
                                {
                                    if (child2.Type == JTokenType.Property)
                                    {
                                        if (child2.HasValues)
                                        {
                                            var children3 = child2.Children();
                                            foreach (var child3 in children3)
                                            {
                                                if (child3.Type == JTokenType.String)
                                                {
                                                    var value = child3.Value<string>();
                                                    var envKey = child3.Path.Substring(child3.Path.LastIndexOf('.') + 1);
                                                    environmentSettings[envKey] = value;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        // this is a top level configuration item
                        else if (child.Type == JTokenType.String)
                        {
                            var value = child.Value<string>();
                            if (child.Path == "environment")
                            {
                                environment = value;
                            }
                            settings[child.Path] = value;
                        }
                    }
                }
            }

            // lay environmet settings on top of global settings
            foreach (var environmentSetting in environmentSettings)
            {
                settings[environmentSetting.Key] = environmentSetting.Value;
            }
            return settings;
        }
    }
}
