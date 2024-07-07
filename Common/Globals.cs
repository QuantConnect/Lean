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
 *
*/

using System.IO;
using System.Reflection;
using QuantConnect.Configuration;

namespace QuantConnect
{
    /// <summary>
    /// Provides application level constant values
    /// </summary>
    public static class Globals
    {
        static Globals()
        {
            Reset();
        }

        /// <summary>
        /// The user Id
        /// </summary>
        public static int UserId { get; set; }

        /// <summary>
        /// The project id
        /// </summary>
        public static int ProjectId { get; set; }

        /// <summary>
        /// The user token
        /// </summary>
        public static string UserToken { get; set; }

        /// <summary>
        /// The organization id
        /// </summary>
        public static string OrganizationID { get; set; }

        /// <summary>
        /// The results destination folder
        /// </summary>
        public static string ResultsDestinationFolder { get; set; }

        /// <summary>
        /// The root directory of the data folder for this application
        /// </summary>
        public static string DataFolder { get; private set; }

        /// <summary>
        /// True if running in live mode
        /// </summary>
        public static bool LiveMode { get; private set; }

        /// <summary>
        /// Resets global values with the Config data.
        /// </summary>
        public static void Reset()
        {
            CacheDataFolder = DataFolder = Config.Get("data-folder", @"../../../Data/");

            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var versionid = Config.Get("version-id");
            if (!string.IsNullOrWhiteSpace(versionid))
            {
                Version += "." + versionid;
            }

            var cacheLocation = Config.Get("cache-location");
            if (!string.IsNullOrEmpty(cacheLocation) && !cacheLocation.IsDirectoryEmpty())
            {
                CacheDataFolder = cacheLocation;
            }

            LiveMode = Config.GetBool("live-mode");

            UserId = Config.GetInt("job-user-id");
            ProjectId = Config.GetInt("project-id");
            UserToken = Config.Get("api-access-token");
            OrganizationID = Config.Get("job-organization-id");
            ResultsDestinationFolder = Config.Get(
                "results-destination-folder",
                Directory.GetCurrentDirectory()
            );
        }

        /// <summary>
        /// The directory used for storing downloaded remote files
        /// </summary>
        public const string Cache = "./cache/data";

        /// <summary>
        /// The version of lean
        /// </summary>
        public static string Version { get; private set; }

        /// <summary>
        /// Data path to cache folder location
        /// </summary>
        public static string CacheDataFolder { get; private set; }

        /// <summary>
        /// Helper method that will build a data folder path checking if it exists on the cache folder else will return data folder
        /// </summary>
        public static string GetDataFolderPath(string relativePath)
        {
            var result = Path.Combine(CacheDataFolder, relativePath);
            if (result.IsDirectoryEmpty())
            {
                result = Path.Combine(DataFolder, relativePath);
            }

            return result;
        }
    }
}
