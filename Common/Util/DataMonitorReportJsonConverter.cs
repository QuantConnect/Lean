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
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides json conversion for the <see cref="DataMonitorReport"/> class
    /// </summary>
    public class DataMonitorReportJsonConverter : TypeChangeJsonConverter<DataMonitorReport, DataMonitorReportJsonConverter.DataMonitorReportJson>
    {
        /// <summary>
        /// Convert the input value to a value to be serialzied
        /// </summary>
        /// <param name="value">The input value to be converted before serialziation</param>
        /// <returns>A new instance of TResult that is to be serialzied</returns>
        protected override DataMonitorReportJson Convert(DataMonitorReport value)
        {
            return new DataMonitorReportJson(value);
        }

        /// <summary>
        /// Converts the input value to be deserialized
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to T</param>
        /// <returns>The converted value</returns>
        protected override DataMonitorReport Convert(DataMonitorReportJson value)
        {;
            return value.Convert();
        }

        /// <summary>
        /// Creates an instance of the un-projected type to be deserialized
        /// </summary>
        /// <param name="type">The input object type, this is the data held in the token</param>
        /// <param name="token">The input data to be converted into a T</param>
        /// <returns>A new instance of T that is to be serialized using default rules</returns>
        protected override DataMonitorReport Create(Type type, JToken token)
        {
            var jobject = (JObject) token;
            var instance = jobject.ToObject<DataMonitorReportJson>();
            return Convert(instance);
        }

        /// <summary>
        /// Defines the json structure of the data monitor report
        /// </summary>
        [JsonObject(MemberSerialization.OptIn)]
        public class DataMonitorReportJson
        {
            /// <summary>
            /// Paths of the files that were requested and successfully fetched
            /// </summary>
            [JsonProperty("fetched-data")]
            public ISet<string> FetchedData { get; }

            /// <summary>
            /// Paths of the files that were requested but could not be fetched
            /// </summary>
            [JsonProperty("missing-data")]
            public ISet<string> MissingData { get; }

            /// <summary>
            /// Gets the number of data files that were fetched
            /// </summary>
            [JsonProperty("missing-data-count")]
            public int MissingDataCount { get; }

            /// <summary>
            /// Fets the percentage of data requests that could not be satisfied
            /// </summary>
            [JsonProperty("missing-data-percentage")]
            public double MissingDataPercentage { get; }

            /// <summary>
            /// Rates at which data requests were made per second
            /// </summary>
            [JsonProperty("data-request-rates")]
            public IEnumerable<double> DataRequestRates { get; }

            /// <summary>
            /// Universe data path that were requested and successfully fetched
            /// </summary>
            [JsonProperty("fetched-universe-data")]
            public IEnumerable<string> FetchedUniverseData { get; }

            /// <summary>
            /// Universe data paths that were requested but could not be fetched
            /// </summary>
            [JsonProperty("missing-universe-data")]
            public IEnumerable<string> MissingUniverseData { get; }

            /// <summary>
            /// Universe data paths that were requested and successfully fetched, grouped by security type
            /// </summary>
            [JsonProperty("fetched-universe-data-by-security-type")]
            public IEnumerable<IGrouping<string, string>> FetchedUniverseDataBySecurityType { get; }

            /// <summary>
            /// Universe data paths that were requested and successfully fetched, grouped by market
            /// </summary>
            [JsonProperty("fetched-universe-data-by-market")]
            public IEnumerable<IGrouping<string, string>> FetchedUniverseDataByMarket { get; }

            /// <summary>
            /// Universe data paths that were requested but could not be fetched, grouped by security type
            /// </summary>
            [JsonProperty("missing-universe-data-by-security-type")]
            public IEnumerable<IGrouping<string, string>> MissingUniverseDataBySecurityType { get; }

            /// <summary>
            /// Universe data paths that were requested but could not be fetched, grouped by market
            /// </summary>
            [JsonProperty("missing-universe-data-by-market")]
            public IEnumerable<IGrouping<string, string>> MissingUniverseDataByMarket { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="DataMonitorReportJson"/> class
            /// </summary>
            /// <param name="report">The report instance to copy</param>
            public DataMonitorReportJson(DataMonitorReport report)
            {
                FetchedData = report.FetchedData;
                MissingData = report.MissingData;
                MissingDataCount = report.MissingDataCount;
                MissingDataPercentage = report.MissingDataPercentage;
                DataRequestRates = report.DataRequestRates;
                FetchedUniverseData = report.FetchedUniverseData;
                MissingUniverseData = report.MissingUniverseData;
                FetchedUniverseDataBySecurityType = report.FetchedUniverseDataBySecurityType;
                FetchedUniverseDataByMarket = report.FetchedUniverseDataByMarket;
                MissingUniverseDataBySecurityType = report.MissingUniverseDataBySecurityType;
                MissingUniverseDataByMarket = report.MissingUniverseDataByMarket;
            }

            /// <summary>
            /// Converts this json representation to the <see cref="DataMonitorReport"/> type
            /// </summary>
            /// <returns>A new instance of the <see cref="DataMonitorReport"/> class</returns>
            public DataMonitorReport Convert()
            {
                return new DataMonitorReport(FetchedData, MissingData, DataRequestRates);
            }
        }
    }
}
