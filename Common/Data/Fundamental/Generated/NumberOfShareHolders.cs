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
    /// The number of shareholders on record
    /// </summary>
    public class NumberOfShareHolders : MultiPeriodField<long>
    {
        /// <summary>
        /// The default period
        /// </summary>
        protected override string DefaultPeriod => "TwelveMonths";

        /// <summary>
        /// Gets/sets the OneMonth period value for the field
        /// </summary>
        [JsonProperty("1M")]
        public long OneMonth => FundamentalService.Get<long>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_NumberOfShareHolders_OneMonth);

        /// <summary>
        /// Gets/sets the TwoMonths period value for the field
        /// </summary>
        [JsonProperty("2M")]
        public long TwoMonths => FundamentalService.Get<long>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_NumberOfShareHolders_TwoMonths);

        /// <summary>
        /// Gets/sets the ThreeMonths period value for the field
        /// </summary>
        [JsonProperty("3M")]
        public long ThreeMonths => FundamentalService.Get<long>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_NumberOfShareHolders_ThreeMonths);

        /// <summary>
        /// Gets/sets the SixMonths period value for the field
        /// </summary>
        [JsonProperty("6M")]
        public long SixMonths => FundamentalService.Get<long>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_NumberOfShareHolders_SixMonths);

        /// <summary>
        /// Gets/sets the NineMonths period value for the field
        /// </summary>
        [JsonProperty("9M")]
        public long NineMonths => FundamentalService.Get<long>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_NumberOfShareHolders_NineMonths);

        /// <summary>
        /// Gets/sets the TwelveMonths period value for the field
        /// </summary>
        [JsonProperty("12M")]
        public long TwelveMonths => FundamentalService.Get<long>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_NumberOfShareHolders_TwelveMonths);

        /// <summary>
        /// Returns true if the field contains a value for the default period
        /// </summary>
        public override bool HasValue => !BaseFundamentalDataProvider.IsNone(typeof(long), FundamentalService.Get<long>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_NumberOfShareHolders_TwelveMonths));

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        public override long Value
        {
            get
            {
                var defaultValue = FundamentalService.Get<long>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_NumberOfShareHolders_TwelveMonths);
                if (!BaseFundamentalDataProvider.IsNone(typeof(long), defaultValue))
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
        public override IReadOnlyDictionary<string, long> GetPeriodValues()
        {
            var result = new Dictionary<string, long>();
            foreach (var kvp in new[] { new Tuple<string, long>("1M", OneMonth), new Tuple<string, long>("2M", TwoMonths), new Tuple<string, long>("3M", ThreeMonths), new Tuple<string, long>("6M", SixMonths), new Tuple<string, long>("9M", NineMonths), new Tuple<string, long>("12M", TwelveMonths) })
            {
                if (!BaseFundamentalDataProvider.IsNone(typeof(long), kvp.Item2))
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
        public override long GetPeriodValue(string period) => FundamentalService.Get<long>(TimeProvider.GetUtcNow(), SecurityIdentifier, Enum.Parse<FundamentalProperty>($"FinancialStatements_NumberOfShareHolders_{ConvertPeriod(period)}"));

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public NumberOfShareHolders()
        {
        }

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public NumberOfShareHolders(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier) : base(timeProvider, securityIdentifier)
        {
        }
    }
}
