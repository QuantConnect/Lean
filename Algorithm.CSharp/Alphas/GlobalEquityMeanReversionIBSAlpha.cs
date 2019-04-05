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
    /// Equity indices exhibit mean reversion in daily returns. The Internal Bar Strength indicator (IBS),
    /// which relates the closing price of a security to its daily range can be used to identify overbought
    /// and oversold securities.
    ///
    /// This alpha ranks 33 global equity ETFs on its IBS value the previous day and predicts for the following day
    /// that the ETF with the highest IBS value will decrease in price, and the ETF with the lowest IBS value
    /// will increase in price.
    ///
    /// Source: Kakushadze, Zura, and Juan Andrés Serur. “4. Exchange-Traded Funds (ETFs).” 151 Trading Strategies, Palgrave Macmillan, 2018, pp. 90–91.
    ///
    /// This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha.
    ///</summary>
    public class GlobalEquityMeanReversionIBSAlpha : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetCash(100000);

            // Set zero transaction fees
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            // Global Equity ETF tickers
            var symbols = new[] {
                "ECH", "EEM", "EFA", "EPHE", "EPP", "EWA", "EWC", "EWG",
                "EWH", "EWI", "EWJ", "EWL", "EWM", "EWM", "EWO", "EWP",
                "EWQ", "EWS", "EWT", "EWU", "EWY", "EWZ", "EZA", "FXI",
                "GXG", "IDX", "ILF", "EWM", "QQQ", "RSX", "SPY", "THD"}
                .Select(x => QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA));

            // Manually curated universe
            UniverseSettings.Resolution = Resolution.Daily;
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));

            // Use MeanReversionIBSAlphaModel to establish insights
            SetAlpha(new MeanReversionIBSAlphaModel());

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Set Immediate Execution Model
            SetExecution(new ImmediateExecutionModel());

            // Set Null Risk Management Model
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// Uses ranking of Internal Bar Strength (IBS) to create direction prediction for insights
        /// </summary>
        private class MeanReversionIBSAlphaModel : AlphaModel
        {
            private readonly int _numberOfStocks;
            private readonly TimeSpan _predictionInterval;

            public MeanReversionIBSAlphaModel(
                int lookback = 1,
                int numberOfStocks = 2,
                Resolution resolution = Resolution.Daily)
            {
                _numberOfStocks = numberOfStocks;
                _predictionInterval = resolution.ToTimeSpan().Multiply(lookback);
            }

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                var symbolsIBS = new Dictionary<Symbol, decimal>();
                var returns = new Dictionary<Symbol, decimal>();

                foreach (var kvp in algorithm.ActiveSecurities)
                {
                    var security = kvp.Value;
                    if (security.HasData)
                    {
                        var high = security.High;
                        var low = security.Low;
                        var hilo = high - low;

                        // Do not consider symbol with zero open and avoid division by zero
                        if (security.Open * hilo != 0)
                        {
                            // Internal bar strength (IBS)
                            symbolsIBS.Add(security.Symbol, (security.Close - low) / hilo);
                            returns.Add(security.Symbol, security.Close / security.Open - 1);
                        }
                    }
                }

                var insights = new List<Insight>();

                // Number of stocks cannot be higher than half of symbolsIBS length
                var numberOfStocks = Math.Min((int)(symbolsIBS.Count / 2.0), _numberOfStocks);
                if (numberOfStocks == 0)
                {
                    return insights;
                }

                // Rank securities with the highest IBS value
                var ordered = from entry in symbolsIBS
                              orderby Math.Round(entry.Value, 6) descending, entry.Key
                              select entry;
                var highIBS = ordered.Take(numberOfStocks);            // Get highest IBS
                var lowIBS = ordered.Reverse().Take(numberOfStocks);   // Get lowest IBS

                // Emit "down" insight for the securities with the highest IBS value
                foreach (var kvp in highIBS)
                {
                    insights.Add(Insight.Price(kvp.Key, _predictionInterval, InsightDirection.Down, Math.Abs((double)returns[kvp.Key])));
                }

                // Emit "up" insight for the securities with the highest IBS value
                foreach (var kvp in lowIBS)
                {
                    insights.Add(Insight.Price(kvp.Key, _predictionInterval, InsightDirection.Up, Math.Abs((double)returns[kvp.Key])));
                }

                return insights;
            }
        }
    }
}