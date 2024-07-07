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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Base class for equity option strategy filter universe regression algorithms which holds some basic shared setup logic
    /// </summary>
    public abstract class OptionStrategyFilteringUniverseBaseAlgorithm
        : QCAlgorithm,
            IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// The filter function
        /// </summary>
        protected Func<OptionFilterUniverse, OptionFilterUniverse> FilterFunc { get; set; }

        /// <summary>
        /// The option symbol
        /// </summary>
        protected Symbol OptionSymbol { get; set; }

        /// <summary>
        /// Expected data count
        /// </summary>
        protected int ExpectedCount { get; set; }

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(200000);

            var equity = AddEquity("GOOG", leverage: 4);
            var option = AddOption(equity.Symbol);
            OptionSymbol = option.Symbol;

            // set our strategy filter for this option chain
            option.SetFilter(FilterFunc);
        }

        protected void AssertOptionStrategyIsPresent(string name, int? quantity = null)
        {
            if (
                Portfolio
                    .Positions.Groups.Where(group =>
                        group.BuyingPowerModel is OptionStrategyPositionGroupBuyingPowerModel
                    )
                    .Count(group =>
                        (
                            (OptionStrategyPositionGroupBuyingPowerModel)@group.BuyingPowerModel
                        ).ToString() == name
                        && (!quantity.HasValue || Math.Abs(group.Quantity) == quantity)
                    ) != 1
            )
            {
                throw new RegressionTestException($"Option strategy: '{name}' was not found!");
            }
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                OptionChain chain;
                if (slice.OptionChains.TryGetValue(OptionSymbol, out chain) && chain.Any())
                {
                    TestFiltering(chain);
                }
            }
        }

        protected abstract void TestFiltering(OptionChain chain);

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new List<Language> { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 0;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public abstract Dictionary<string, string> ExpectedStatistics { get; }
    }
}
