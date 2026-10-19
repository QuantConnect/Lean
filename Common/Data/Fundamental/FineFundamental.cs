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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Definition of the FineFundamental class
    /// </summary>
    public partial class FineFundamental
    {
        /// <summary>
        /// The end time of this data.
        /// </summary>
        [JsonIgnore]
        public override DateTime EndTime
        {
            get { return Time + QuantConnect.Time.OneDay; }
            set { Time = value - QuantConnect.Time.OneDay; }
        }

        /// <summary>
        /// Price * Total SharesOutstanding.
        /// The most current market cap for example, would be the most recent closing price x the most recent reported shares outstanding.
        /// For ADR share classes, market cap is price * (ordinary shares outstanding / adr ratio).
        /// </summary>
        [JsonIgnore]
        public long MarketCap => CompanyProfile.MarketCap;

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called. The returned object is assumed to be time stamped in the config.ExchangeTimeZone.
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Clones this fine data instance
        /// </summary>
        /// <returns></returns>
        public override BaseData Clone()
        {
            return new FineFundamental(Time, Symbol, _fundamentalInstanceProvider);
        }

        /// <summary>
        /// This is a daily data set
        /// </summary>
        public override List<Resolution> SupportedResolutions()
        {
            return DailyResolution;
        }

        /// <summary>
        /// This is a daily data set
        /// </summary>
        public override Resolution DefaultResolution()
        {
            return Resolution.Daily;
        }
    }
}
