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
    /// The nature of the period covered by an individual set of financial results. The output can be: Quarter, Semi-annual or Annual. Assuming a 12-month fiscal year, quarter typically covers a three-month period, semi-annual a six-month period, and annual a twelve-month period. Annual could cover results collected either from preliminary results or an annual report
    /// </summary>
    public class EarningReportsPeriodType : MultiPeriodField<string>
    {
        /// <summary>
        /// The default period
        /// </summary>
        protected override string DefaultPeriod => "ThreeMonths";

        /// <summary>
        /// Gets/sets the OneMonth period value for the field
        /// </summary>
        [JsonProperty("1M")]
        public string OneMonth => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_PeriodType_OneMonth);

        /// <summary>
        /// Gets/sets the TwoMonths period value for the field
        /// </summary>
        [JsonProperty("2M")]
        public string TwoMonths => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_PeriodType_TwoMonths);

        /// <summary>
        /// Gets/sets the ThreeMonths period value for the field
        /// </summary>
        [JsonProperty("3M")]
        public string ThreeMonths => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_PeriodType_ThreeMonths);

        /// <summary>
        /// Gets/sets the SixMonths period value for the field
        /// </summary>
        [JsonProperty("6M")]
        public string SixMonths => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_PeriodType_SixMonths);

        /// <summary>
        /// Gets/sets the NineMonths period value for the field
        /// </summary>
        [JsonProperty("9M")]
        public string NineMonths => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_PeriodType_NineMonths);

        /// <summary>
        /// Gets/sets the TwelveMonths period value for the field
        /// </summary>
        [JsonProperty("12M")]
        public string TwelveMonths => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_PeriodType_TwelveMonths);

        /// <summary>
        /// Returns true if the field contains a value for the default period
        /// </summary>
        public override bool HasValue => !BaseFundamentalDataProvider.IsNone(typeof(string), FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_PeriodType_ThreeMonths));

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        public override string Value
        {
            get
            {
                var defaultValue = FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.EarningReports_PeriodType_ThreeMonths);
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
            foreach (var kvp in new[] { new Tuple<string, string>("1M", OneMonth), new Tuple<string, string>("2M", TwoMonths), new Tuple<string, string>("3M", ThreeMonths), new Tuple<string, string>("6M", SixMonths), new Tuple<string, string>("9M", NineMonths), new Tuple<string, string>("12M", TwelveMonths) })
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
        public override string GetPeriodValue(string period) => FundamentalService.Get<string>(TimeProvider.GetUtcNow(), SecurityIdentifier, Enum.Parse<FundamentalProperty>($"EarningReports_PeriodType_{ConvertPeriod(period)}"));

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public EarningReportsPeriodType()
        {
        }

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public EarningReportsPeriodType(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier) : base(timeProvider, securityIdentifier)
        {
        }
    }
}
