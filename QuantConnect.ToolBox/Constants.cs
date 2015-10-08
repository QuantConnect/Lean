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
using Newtonsoft.Json.Linq;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Provides application constants from configuration
    /// </summary>
    /// <remarks>
    /// This class was added to allow copy/paste of code from the LEAN solution
    /// </remarks>
    public static class Constants
    {
        /// <summary>
        /// Gets the LEAN data folder /Data
        /// </summary>
        public static string DataFolder { get; private set; }

        /// <summary>
        /// Initialize from configuration
        /// </summary>
        /// <remarks>
        /// This method is added to allow applications to opt into this behavior.
        /// Exceptions will be thrown if required keys do not exist.
        /// </remarks>
        public static void Initialize()
        {
            JToken jtoken;
            var config = JObject.Parse(File.ReadAllText("config.json"));
            if (!config.TryGetValue("data-directory", out jtoken))
            {
                throw new Exception("Specify 'data-directory' in config.json");
            }

            DataFolder = jtoken.Value<string>();
        }
    }
}
