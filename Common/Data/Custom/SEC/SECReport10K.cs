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
using Newtonsoft.Json;
using QuantConnect.Data.UniverseSelection;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Custom.SEC
{
    /// <summary>
    /// SEC 10-K report (annual earnings) <see cref="BaseData"/> implementation.
    /// Using this class, you can retrieve SEC report data for a security if it exists.
    /// If the ticker you want no longer trades, you can also use the CIK of the company
    /// you want data for as well except for currently traded stocks. This may change in the future.
    /// </summary>
    public class SECReport10K : BaseData, ISECReport
    {
        /// <summary>
        /// Contents of the actual SEC report
        /// </summary>
        public SECReportSubmission Report { get; }

        /// <summary>
        /// Empty constructor required for <see cref="Slice.Get{T}()"/>
        /// </summary>
        public SECReport10K()
        {
        }

        /// <summary>
        /// Constructor used to initialize instance with the given report
        /// </summary>
        /// <param name="report">SEC report submission</param>
        public SECReport10K(SECReportSubmission report)
        {
            Report = report;
            Time = report.FilingDate;
        }

        /// <summary>
        /// Returns a subscription data source pointing towards SEC 10-K report data
        /// </summary>
        /// <param name="config">User configuration</param>
        /// <param name="date">Date data has been requested for</param>
        /// <param name="isLiveMode">Is livetrading</param>
        /// <returns></returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            // Although our data is stored as a JSON file, we can trick the
            // SubscriptionDataReader to load our file all at once so long as we store
            // the file in a single line. Then, we can deserialize the whole file in Reader.
            // FineFundamental uses the same technique to read a JSON file.
            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "sec",
                    config.Symbol.Value.ToLowerInvariant(),
                    Invariant($"{date:yyyyMMdd}_10K.zip#10K.json")
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Collection
            );
        }

        /// <summary>
        /// Parses the data into <see cref="BaseData"/>
        /// </summary>
        /// <param name="config">User subscription config</param>
        /// <param name="line">Line of source file to parse</param>
        /// <param name="date">Date data was requested for</param>
        /// <param name="isLiveMode">Is livetrading mode</param>
        /// <returns></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var reportSubmissions = JsonConvert.DeserializeObject<List<SECReportSubmission>>(line, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var reports = reportSubmissions.Select(report => new SECReport10K(report)
            {
                Symbol = config.Symbol
            });

            return new BaseDataCollection(date, config.Symbol, reports);
        }

        /// <summary>
        /// Indicates if there is support for mapping
        /// </summary>
        /// <returns>True indicates mapping should be used</returns>
        public override bool RequiresMapping()
        {
            return true;
        }

        /// <summary>
        /// Clones the current object into a new object
        /// </summary>
        /// <returns>BaseData clone of the current object</returns>
        public override BaseData Clone()
        {
            return new SECReport10K(Report)
            {
                Symbol = Symbol
            };
        }
    }
}
