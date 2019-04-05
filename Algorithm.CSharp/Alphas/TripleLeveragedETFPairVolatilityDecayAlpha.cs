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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
    /// <summary>
    /// Leveraged ETFs (LETF) promise a fixed leverage ratio with respect to an underlying asset or an index.
    /// A Triple-Leveraged ETF allows speculators to amplify their exposure to the daily returns of an underlying index by a factor of 3.
    ///
    /// Increased volatility generally decreases the value of a LETF over an extended period of time as daily compounding is amplified.
    ///
    /// This alpha emits short-biased insight to capitalize on volatility decay for each listed pair of TL-ETFs, by rebalancing the
    /// ETFs with equal weights each day.
    ///
    /// This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha.
    /// </summary>
    public class TripleLeveragedETFPairVolatilityDecayAlpha : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);

            SetCash(100000);

            // Set zero transaction fees
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            // 3X ETF pair tickers
            var ultraLong = QuantConnect.Symbol.Create("UGLD", SecurityType.Equity, Market.USA);
            var ultraShort = QuantConnect.Symbol.Create("DGLD", SecurityType.Equity, Market.USA);

            // Manually curated universe
            UniverseSettings.Resolution = Resolution.Daily;
            SetUniverseSelection(new ManualUniverseSelectionModel(new[] { ultraLong, ultraShort }));

            // Select the demonstration alpha model
            SetAlpha(new RebalancingTripleLeveragedETFAlphaModel(ultraLong, ultraShort));

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Set Immediate Execution Model
            SetExecution(new ImmediateExecutionModel());

            // Set Null Risk Management Model
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// Rebalance a pair of 3x leveraged ETFs and predict that the value of both ETFs in each pair will decrease.
        /// </summary>
        private class RebalancingTripleLeveragedETFAlphaModel : AlphaModel
        {
            private const double _magnitude = 0.001;
            private readonly Symbol _ultraLong;
            private readonly Symbol _ultraShort;
            private readonly TimeSpan _period;

            public RebalancingTripleLeveragedETFAlphaModel(Symbol ultraLong, Symbol ultraShort)
            {
                // Giving an insight period 1 days.
                _period = QuantConnect.Time.OneDay;

                _ultraLong = ultraLong;
                _ultraShort = ultraShort;

                Name = "RebalancingTripleLeveragedETFAlphaModel";
            }

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                return Insight.Group(new[]
                {
                    Insight.Price(_ultraLong, _period, InsightDirection.Down, _magnitude),
                    Insight.Price(_ultraShort, _period, InsightDirection.Down, _magnitude)
                });
            }
        }
    }
}