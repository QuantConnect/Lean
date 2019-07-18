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
using System.Linq;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
    /// <summary>
    /// In a perfect market, you could buy 100 EUR worth of USD, sell 100 EUR worth of GBP,
    /// and then use the GBP to buy USD and wind up with the same amount in USD as you received when
    /// you bought them with EUR. This relationship is expressed by the Triangle Exchange Rate, which is
    ///
    /// Triangle Exchange Rate = (A/B) * (B/C) * (C/A)
    ///
    /// where (A/B) is the exchange rate of A-to-B. In a perfect market, TER = 1, and so when
    /// there is a mispricing in the market, then TER will not be 1 and there exists an arbitrage opportunity.
    ///
    /// This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha.
    /// </summary>
    public class TriangleExchangeRateArbitrageAlpha : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2019, 2, 1);
            SetCash(100000);

            // Set zero transaction fees
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            // Select trio of currencies to trade where
            // Currency A = USD
            // Currency B = EUR
            // Currency C = GBP
            var symbols = new[] { "EURUSD", "EURGBP", "GBPUSD" }
                .Select(x => QuantConnect.Symbol.Create(x, SecurityType.Forex, Market.Oanda));

            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Minute;
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));

            // Use ForexTriangleArbitrageAlphaModel to establish insights
            SetAlpha(new ForexTriangleArbitrageAlphaModel(symbols, Resolution.Minute));

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Set Immediate Execution Model
            SetExecution(new ImmediateExecutionModel());

            // Set Null Risk Management Model
            SetRiskManagement(new NullRiskManagementModel());
        }

        private class ForexTriangleArbitrageAlphaModel : AlphaModel
        {
            private readonly Symbol[] _symbols;
            private readonly TimeSpan _insightPeriod;

            public ForexTriangleArbitrageAlphaModel(
                IEnumerable<Symbol> symbols,
                Resolution resolution = Resolution.Minute)
            {
                _symbols = symbols.ToArray();
                _insightPeriod = resolution.ToTimeSpan().Multiply(5);
            }

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                // Check to make sure all currency symbols are present
                if (data.QuoteBars.Count < 3)
                {
                    return Enumerable.Empty<Insight>();
                }

                // Extract QuoteBars for all three Forex securities
                var barA = data[_symbols[0]];
                var barB = data[_symbols[1]];
                var barC = data[_symbols[2]];

                // Calculate the triangle exchange rate
                // Bid(Currency A -> Currency B) * Bid(Currency B -> Currency C) * Bid(Currency C -> Currency A)
                // If exchange rates are priced perfectly, then this yield 1.If it is different than 1, then an arbitrage opportunity exists
                var triangleRate = barA.Ask.Close / barB.Bid.Close / barC.Ask.Close;

                // If the triangle rate is significantly different than 1, then emit insights
                if (triangleRate > 1.0005m)
                {
                    return Insight.Group(new[]
                    {
                        Insight.Price(_symbols[0], _insightPeriod, InsightDirection.Up, 0.0001),
                        Insight.Price(_symbols[1], _insightPeriod, InsightDirection.Down, 0.0001),
                        Insight.Price(_symbols[2], _insightPeriod, InsightDirection.Up, 0.0001)
                    });
                }

                return Enumerable.Empty<Insight>();
            }
        }
    }
}