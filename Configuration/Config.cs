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
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using static System.FormattableString;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Configuration class loads the required external setup variables to launch the Lean engine.
    /// </summary>
    public static class Config
    {
        //Location of the configuration file.
        private static string ConfigurationFileName = "config.json";

        /// <summary>
        /// Set configuration file on-fly
        /// </summary>
        /// <param name="fileName"></param>
        public static void SetConfigurationFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                Log.Trace(Invariant($"Using {fileName} as configuration file"));
                ConfigurationFileName = fileName;
            }
            else
            {
                Log.Error(Invariant($"Configuration file {fileName} does not exist, using {ConfigurationFileName}"));
            }
        }

        /// <summary>
        /// Merge CLI arguments with configuration file + load custom config file via CLI arg
        /// </summary>
        /// <param name="cliArguments"></param>
        public static void MergeCommandLineArgumentsWithConfiguration(Dictionary<string, object> cliArguments)
        {
            if (cliArguments.ContainsKey("config"))
            {
                SetConfigurationFile(cliArguments["config"] as string);
                Reset();
            }

            var jsonArguments = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(cliArguments));

            Settings.Value.Merge(jsonArguments, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union
            });
        }

        /// <summary>
        /// Resets the config settings to their default values.
        /// Called in regression tests where multiple algorithms are run sequentially,
        /// and we need to guarantee that every test starts with the same configuration.
        /// </summary>
        public static void Reset()
        {
            Settings = new Lazy<JObject>(ConfigFactory);
        }

        private static Lazy<JObject> Settings = new Lazy<JObject>(ConfigFactory);

        private static JObject ConfigFactory()
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
                    {"job-queue-handler", "QuantConnect.Queues.JobQueue"},
                    {"api-handler", "QuantConnect.Api.Api"},
                    {"setup-handler", "QuantConnect.Lean.Engine.Setup.ConsoleSetupHandler"},
                    {"result-handler", "QuantConnect.Lean.Engine.Results.BacktestingResultHandler"},
                    {"data-feed-handler", "QuantConnect.Lean.Engine.DataFeeds.FileSystemDataFeed"},
                    {"real-time-handler", "QuantConnect.Lean.Engine.RealTime.BacktestingRealTimeHandler"},
                    {"transaction-handler", "QuantConnect.Lean.Engine.TransactionHandlers.BacktestingTransactionHandler"}
                };
            }

            return JObject.Parse(File.ReadAllText(ConfigurationFileName));
        }

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
                Log.Trace(Invariant($"Config.Get(): Configuration key not found. Key: {key} - Using default value: {defaultValue}"));
                return defaultValue;
            }
            return token.ToString();
        }

        /// <summary>
        /// Gets the underlying JToken for the specified key
        /// </summary>
        public static JToken GetToken(string key)
        {
            return GetToken(Settings.Value, key);
        }

        /// <summary>
        /// Sets a configuration value. This is really only used to help testing. The key heye can be
        /// specified as {environment}.key to set a value on a specific environment
        /// </summary>
        /// <param name="key">The key to be set</param>
        /// <param name="value">The new value</param>
        public static void Set(string key, dynamic value)
        {
            JToken environment = Settings.Value;
            while (key.Contains("."))
            {
                var envName = key.Substring(0, key.IndexOf(".", StringComparison.InvariantCulture));
                key = key.Substring(key.IndexOf(".", StringComparison.InvariantCulture) + 1);
                var environments = environment["environments"];
                if (environments == null)
                {
                    environment["environments"] = environments = new JObject();
                }
                environment = environments[envName];
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
        {
            // special case environment requests
            if (key == "environment" && typeof (T) == typeof (string)) return (T) (object) GetEnvironment();

            var token = GetToken(Settings.Value, key);
            if (token == null)
            {
                var defaultValueString = defaultValue is IConvertible
                    ? ((IConvertible) defaultValue).ToString(CultureInfo.InvariantCulture)
                    : defaultValue is IFormattable
                        ? ((IFormattable) defaultValue).ToString(null, CultureInfo.InvariantCulture)
                        : Invariant($"{defaultValue}");

                Log.Trace(Invariant($"Config.GetValue(): {key} - Using default value: {defaultValueString}"));
                return defaultValue;
            }

            var type = typeof(T);
            string value;
            try
            {
                value = token.Value<string>();
            }
            catch (Exception)
            {
                value = token.ToString();
            }

            if (type.IsEnum)
            {
                return (T) Enum.Parse(type, value);
            }

            if (typeof(IConvertible).IsAssignableFrom(type))
            {
                return (T) Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }

            // try and find a static parse method
            try
            {
                var parse = type.GetMethod("Parse", new[]{typeof(string)});
                if (parse != null)
                {
                    var result = parse.Invoke(null, new object[] {value});
                    return (T) result;
                }
            }
            catch (Exception err)
            {
                Log.Trace(Invariant($"Config.GetValue<{typeof(T).Name}>({key},{defaultValue}): Failed to parse: {value}. Using default value."));
                Log.Error(err);
                return defaultValue;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception err)
            {
                Log.Trace(Invariant($"Config.GetValue<{typeof(T).Name}>({key},{defaultValue}): Failed to JSON deserialize: {value}. Using default value."));
                Log.Error(err);
                return defaultValue;
            }
        }

        /// <summary>
        /// Tries to find the specified key and parse it as a T, using
        /// default(T) if unable to locate the key or unable to parse it
        /// </summary>
        /// <typeparam name="T">The desired output type</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The output value</param>
        /// <returns>True on successful parse, false when output value is default(T)</returns>
        public static bool TryGetValue<T>(string key, out T value)
        {
            return TryGetValue(key, default(T), out value);
        }

        /// <summary>
        /// Tries to find the specified key and parse it as a T, using
        /// defaultValue if unable to locate the key or unable to parse it
        /// </summary>
        /// <typeparam name="T">The desired output type</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value to use on key not found or unsuccessful parse</param>
        /// <param name="value">The output value</param>
        /// <returns>True on successful parse, false when output value is defaultValue</returns>
        public static bool TryGetValue<T>(string key, T defaultValue, out T value)
        {
            try
            {
                value = GetValue(key, defaultValue);
                return true;
            }
            catch
            {
                value = defaultValue;
                return false;
            }
        }

        /// <summary>
        /// Write the contents of the serialized configuration back to the disk.
        /// </summary>
        public static void Write()
        {
            if (!Settings.IsValueCreated) return;
            var serialized = JsonConvert.SerializeObject(Settings.Value, Formatting.Indented);
            File.WriteAllText(ConfigurationFileName, serialized);
        }

        /// <summary>
        /// Flattens the jobject with respect to the selected environment and then
        /// removes the 'environments' node
        /// </summary>
        /// <param name="overrideEnvironment">The environment to use</param>
        /// <returns>The flattened JObject</returns>
        public static JObject Flatten(string overrideEnvironment)
        {
            return Flatten(Settings.Value, overrideEnvironment);
        }

        /// <summary>
        /// Flattens the jobject with respect to the selected environment and then
        /// removes the 'environments' node
        /// </summary>
        /// <param name="config">The configuration represented as a JObject</param>
        /// <param name="overrideEnvironment">The environment to use</param>
        /// <returns>The flattened JObject</returns>
        public static JObject Flatten(JObject config, string overrideEnvironment)
        {
            var clone = (JObject)config.DeepClone();

            // remove the environment declaration
            var environmentProperty = clone.Property("environment");
            if (environmentProperty != null) environmentProperty.Remove();

            if (!string.IsNullOrEmpty(overrideEnvironment))
            {
                var environmentSections = overrideEnvironment.Split('.');

                for (int i = 0; i < environmentSections.Length; i++)
                {
                    var env = string.Join(".environments.", environmentSections.Where((x, j) => j <= i));

                    var environments = config["environments"];
                    if (!(environments is JObject)) continue;

                    var settings = ((JObject) environments).SelectToken(env);
                    if (settings == null) continue;

                    // copy values for the selected environment to the root
                    foreach (var token in settings)
                    {
                        var path = Path.GetExtension(token.Path);
                        var dot = path.IndexOf(".", StringComparison.InvariantCulture);
                        if (dot != -1) path = path.Substring(dot + 1);

                        // remove if already exists on clone
                        var jProperty = clone.Property(path);
                        if (jProperty != null) jProperty.Remove();

                        var value = (token is JProperty ? ((JProperty) token).Value : token).ToString();
                        clone.Add(path, value);
                    }
                }
            }

            // remove all environments
            var environmentsProperty = clone.Property("environments");
            environmentsProperty?.Remove();

            return clone;
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
                var environmentSettingValue = environmentSetting.Value<string>();
                if (!string.IsNullOrWhiteSpace(environmentSettingValue))
                {
                    var environment = settings.SelectToken("environments." + environmentSettingValue);
                    if (environment != null)
                    {
                        var setting = environment.SelectToken(key);
                        if (setting != null)
                        {
                            current = setting;
                        }
                        // allows nesting of environments, live.tradier, live.interactive, ect...
                        return GetToken(environment, key, current);
                    }
                }
            }
            if (current == null)
            {
                return settings.SelectToken(key);
            }
            return current;
        }
    }
}
