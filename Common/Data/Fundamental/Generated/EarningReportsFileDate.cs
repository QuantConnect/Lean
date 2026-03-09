/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2023 QuantConnect Corporation.
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

using System;
using System.Linq;
using Python.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Specific date on which a company released its filing to the public.
    /// </summary>
    public class EarningReportsFileDate : MultiPeriodField<DateTime>
    {
        /// <summary>
        /// The default period
        /// </summary>
        protected override string DefaultPeriod => "ThreeMonths";

        /// <summary>
        /// Gets/sets the OneMonth period value for the field
        /// </summary>
        [JsonProperty("1M")]
        public DateTime OneMonth => FundamentalService.Get<DateTime>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_FileDate_OneMonth);

        /// <summary>
        /// Gets/sets the TwoMonths period value for the field
        /// </summary>
        [JsonProperty("2M")]
        public DateTime TwoMonths => FundamentalService.Get<DateTime>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_FileDate_TwoMonths);

        /// <summary>
        /// Gets/sets the ThreeMonths period value for the field
        /// </summary>
        [JsonProperty("3M")]
        public DateTime ThreeMonths => FundamentalService.Get<DateTime>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_FileDate_ThreeMonths);

        /// <summary>
        /// Gets/sets the SixMonths period value for the field
        /// </summary>
        [JsonProperty("6M")]
        public DateTime SixMonths => FundamentalService.Get<DateTime>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_FileDate_SixMonths);

        /// <summary>
        /// Gets/sets the NineMonths period value for the field
        /// </summary>
        [JsonProperty("9M")]
        public DateTime NineMonths => FundamentalService.Get<DateTime>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_FileDate_NineMonths);

        /// <summary>
        /// Gets/sets the TwelveMonths period value for the field
        /// </summary>
        [JsonProperty("12M")]
        public DateTime TwelveMonths => FundamentalService.Get<DateTime>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_FileDate_TwelveMonths);

        /// <summary>
        /// Returns true if the field contains a value for the default period
        /// </summary>
        public override bool HasValue => !BaseFundamentalDataProvider.IsNone(typeof(DateTime), FundamentalService.Get<DateTime>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_FileDate_ThreeMonths));

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        public override DateTime Value
        {
            get
            {
                var defaultValue = FundamentalService.Get<DateTime>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_FileDate_ThreeMonths);
                if (!BaseFundamentalDataProvider.IsNone(typeof(DateTime), defaultValue))
                {
                    return defaultValue;
                }
                return base.Value;
            }
        }

        /// <summary>
        /// Gets a dictionary of period names and values for the field
        /// </summary>
        /// <returns>The dictionary of period names and values</returns>
        public override IReadOnlyDictionary<string, DateTime> GetPeriodValues()
        {
            var result = new Dictionary<string, DateTime>();
            foreach (var kvp in new[] { new Tuple<string, DateTime>("1M", OneMonth), new Tuple<string, DateTime>("2M", TwoMonths), new Tuple<string, DateTime>("3M", ThreeMonths), new Tuple<string, DateTime>("6M", SixMonths), new Tuple<string, DateTime>("9M", NineMonths), new Tuple<string, DateTime>("12M", TwelveMonths) })
            {
                if (!BaseFundamentalDataProvider.IsNone(typeof(DateTime), kvp.Item2))
                {
                    result[kvp.Item1] = kvp.Item2;
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the value of the field for the requested period
        /// </summary>
        /// <param name="period">The requested period</param>
        /// <returns>The value for the period</returns>
        public override DateTime GetPeriodValue(string period) => FundamentalService.Get<DateTime>(TimeProvider.GetUtcNow(), SecurityIdentifier, Enum.Parse<FundamentalProperty>($"EarningReports_FileDate_{ConvertPeriod(period)}"));

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public EarningReportsFileDate()
        {
        }

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public EarningReportsFileDate(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier) : base(timeProvider, securityIdentifier)
        {
        }
    }
}
