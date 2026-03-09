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
    /// The accession number is a unique number that EDGAR assigns to each submission as the submission is received.
    /// </summary>
    public class EarningReportsAccessionNumber : MultiPeriodField<string>
    {
        /// <summary>
        /// The default period
        /// </summary>
        protected override string DefaultPeriod => "OneMonth";

        /// <summary>
        /// Gets/sets the OneMonth period value for the field
        /// </summary>
        [JsonProperty("1M")]
        public string OneMonth => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_AccessionNumber_OneMonth);

        /// <summary>
        /// Gets/sets the TwoMonths period value for the field
        /// </summary>
        [JsonProperty("2M")]
        public string TwoMonths => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_AccessionNumber_TwoMonths);

        /// <summary>
        /// Gets/sets the ThreeMonths period value for the field
        /// </summary>
        [JsonProperty("3M")]
        public string ThreeMonths => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_AccessionNumber_ThreeMonths);

        /// <summary>
        /// Gets/sets the SixMonths period value for the field
        /// </summary>
        [JsonProperty("6M")]
        public string SixMonths => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_AccessionNumber_SixMonths);

        /// <summary>
        /// Gets/sets the NineMonths period value for the field
        /// </summary>
        [JsonProperty("9M")]
        public string NineMonths => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_AccessionNumber_NineMonths);

        /// <summary>
        /// Returns true if the field contains a value for the default period
        /// </summary>
        public override bool HasValue => !BaseFundamentalDataProvider.IsNone(typeof(string), FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_AccessionNumber_OneMonth));

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        public override string Value
        {
            get
            {
                var defaultValue = FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_AccessionNumber_OneMonth);
                if (!BaseFundamentalDataProvider.IsNone(typeof(string), defaultValue))
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
        public override IReadOnlyDictionary<string, string> GetPeriodValues()
        {
            var result = new Dictionary<string, string>();
            foreach (var kvp in new[] { new Tuple<string, string>("1M", OneMonth), new Tuple<string, string>("2M", TwoMonths), new Tuple<string, string>("3M", ThreeMonths), new Tuple<string, string>("6M", SixMonths), new Tuple<string, string>("9M", NineMonths) })
            {
                if (!BaseFundamentalDataProvider.IsNone(typeof(string), kvp.Item2))
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
        public override string GetPeriodValue(string period) => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, Enum.Parse<FundamentalProperty>($"EarningReports_AccessionNumber_{ConvertPeriod(period)}"));

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public EarningReportsAccessionNumber()
        {
        }

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public EarningReportsAccessionNumber(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier) : base(timeProvider, securityIdentifier)
        {
        }
    }
}
