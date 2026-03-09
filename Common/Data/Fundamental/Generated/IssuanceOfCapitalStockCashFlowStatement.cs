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
    /// The cash inflow from offering common stock, which is the additional capital contribution to the entity during the PeriodAsByte.
    /// </summary>
    public class IssuanceOfCapitalStockCashFlowStatement : MultiPeriodField
    {
        /// <summary>
        /// The default period
        /// </summary>
        protected override string DefaultPeriod => "TwelveMonths";

        /// <summary>
        /// Gets/sets the OneMonth period value for the field
        /// </summary>
        [JsonProperty("1M")]
        public double OneMonth => FundamentalService.Get<double>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_CashFlowStatement_IssuanceOfCapitalStock_OneMonth);

        /// <summary>
        /// Gets/sets the TwoMonths period value for the field
        /// </summary>
        [JsonProperty("2M")]
        public double TwoMonths => FundamentalService.Get<double>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_CashFlowStatement_IssuanceOfCapitalStock_TwoMonths);

        /// <summary>
        /// Gets/sets the ThreeMonths period value for the field
        /// </summary>
        [JsonProperty("3M")]
        public double ThreeMonths => FundamentalService.Get<double>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_CashFlowStatement_IssuanceOfCapitalStock_ThreeMonths);

        /// <summary>
        /// Gets/sets the SixMonths period value for the field
        /// </summary>
        [JsonProperty("6M")]
        public double SixMonths => FundamentalService.Get<double>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_CashFlowStatement_IssuanceOfCapitalStock_SixMonths);

        /// <summary>
        /// Gets/sets the NineMonths period value for the field
        /// </summary>
        [JsonProperty("9M")]
        public double NineMonths => FundamentalService.Get<double>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_CashFlowStatement_IssuanceOfCapitalStock_NineMonths);

        /// <summary>
        /// Gets/sets the TwelveMonths period value for the field
        /// </summary>
        [JsonProperty("12M")]
        public double TwelveMonths => FundamentalService.Get<double>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_CashFlowStatement_IssuanceOfCapitalStock_TwelveMonths);

        /// <summary>
        /// Returns true if the field contains a value for the default period
        /// </summary>
        public override bool HasValue => !BaseFundamentalDataProvider.IsNone(typeof(double), FundamentalService.Get<double>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_CashFlowStatement_IssuanceOfCapitalStock_TwelveMonths));

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        public override double Value
        {
            get
            {
                var defaultValue = FundamentalService.Get<double>(TimeProvider.GetUtcNow(), SecurityIdentifier, FundamentalProperty.FinancialStatements_CashFlowStatement_IssuanceOfCapitalStock_TwelveMonths);
                if (!BaseFundamentalDataProvider.IsNone(typeof(double), defaultValue))
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
        public override IReadOnlyDictionary<string, double> GetPeriodValues()
        {
            var result = new Dictionary<string, double>();
            foreach (var kvp in new[] { new Tuple<string, double>("1M",OneMonth), new Tuple<string, double>("2M",TwoMonths), new Tuple<string, double>("3M",ThreeMonths), new Tuple<string, double>("6M",SixMonths), new Tuple<string, double>("9M",NineMonths), new Tuple<string, double>("12M",TwelveMonths) })
            {
                if(!BaseFundamentalDataProvider.IsNone(typeof(double), kvp.Item2))
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
        public override double GetPeriodValue(string period) => FundamentalService.Get<double>(TimeProvider.GetUtcNow(), SecurityIdentifier, Enum.Parse<FundamentalProperty>($"FinancialStatements_CashFlowStatement_IssuanceOfCapitalStock_{ConvertPeriod(period)}"));

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public IssuanceOfCapitalStockCashFlowStatement()
        {
        }

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public IssuanceOfCapitalStockCashFlowStatement(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier) : base(timeProvider, securityIdentifier)
        {
        }
    }
}
