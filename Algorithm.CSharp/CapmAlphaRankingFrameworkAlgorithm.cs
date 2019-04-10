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

using MathNet.Numerics;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// CapmAlphaRankingFrameworkAlgorithm: example of custom scheduled universe selection model
    /// Universe Selection inspired by https://www.quantconnect.com/tutorials/strategy-library/capm-alpha-ranking-strategy-on-dow-30-companies
    /// </summary>
    public class CapmAlphaRankingFrameworkAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2016, 1, 1);   //Set Start Date
            SetEndDate(2017, 1, 1);     //Set End Date
            SetCash(100000);            //Set Strategy Cash

            // set algorithm framework models
            SetUniverseSelection(new CapmAlphaRankingUniverseSelectionModel());
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.01m));
        }

        /// <summary>
        /// This universe selection model picks stocks with the highest alpha: interception of the linear regression against a benchmark.
        /// </summary>
        private class CapmAlphaRankingUniverseSelectionModel : UniverseSelectionModel
        {
            private const int period = 21;
            private const string _benchmark = "SPY";

            // Symbols of Dow 30 companies.
            private readonly IEnumerable<Symbol> _symbols = new[]
            {
                "AAPL", "AXP", "BA", "CAT", "CSCO", "CVX", "DD", "DIS", "GE", "GS",
                "HD", "IBM", "INTC", "JPM", "KO", "MCD", "MMM", "MRK", "MSFT",
                "NKE","PFE", "PG", "TRV", "UNH", "UTX", "V", "VZ", "WMT", "XOM"
            }.Select(x => QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA));

            public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
            {
                // Adds the benchmark to the user defined universe
                var benchmark = algorithm.AddEquity(_benchmark, Resolution.Daily);

                // Defines a schedule universe that fires after market open when the month starts
                yield return new ScheduledUniverse(
                    benchmark.Exchange.TimeZone,
                    algorithm.DateRules.MonthStart(benchmark.Symbol),
                    algorithm.TimeRules.AfterMarketOpen(benchmark.Symbol),
                    datetime => SelectPair(algorithm, datetime),
                    algorithm.UniverseSettings,
                    algorithm.SecurityInitializer);
            }

            /// <summary>
            /// Selects the pair (two stocks) with the highest alpha
            /// </summary>
            private IEnumerable<Symbol> SelectPair(QCAlgorithm algorithm, DateTime dateTime)
            {
                var dictionary = new Dictionary<Symbol, double>();

                var benchmark = GetReturns(algorithm, _benchmark);

                foreach (var symbol in _symbols)
                {
                    var prices = GetReturns(algorithm, symbol);

                    // Calculate the Least-Square fitting to the returns of a given symbol and the benchmark
                    var ols = Fit.Line(prices, benchmark);

                    dictionary.Add(symbol, ols.Item1);
                }

                // Returns the top 2 highest alphas
                var orderedDictionary = dictionary.OrderByDescending(key => key.Value);
                return orderedDictionary.Take(2).Select(x => x.Key);
            }

            private double[] GetReturns(QCAlgorithm algorithm, Symbol symbol)
            {
                var window = new RollingWindow<double>(period);
                var rateOfChange = new RateOfChange(1);
                rateOfChange.Updated += (s, item) => window.Add((double)item.Value);

                foreach (var bar in algorithm.History(symbol, period, Resolution.Daily))
                {
                    rateOfChange.Update(bar.EndTime, bar.Close);
                }
                return window.ToArray();
            }
        }
    }
}