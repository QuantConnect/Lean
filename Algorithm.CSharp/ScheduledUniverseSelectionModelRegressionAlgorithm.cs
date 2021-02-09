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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for testing <see cref="ScheduledUniverseSelectionModel"/> scheduling functions
    /// </summary>
    public class ScheduledUniverseSelectionModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Hour;

            SetStartDate(2017, 01, 01);
            SetEndDate(2017, 02, 01);

            // selection will run on mon/tues/thurs at 00:00/12:00
            SetUniverseSelection(new ScheduledUniverseSelectionModel(
                DateRules.Every(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday),
                TimeRules.Every(TimeSpan.FromHours(12)),
                SelectSymbols
            ));

            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
        }

        private IEnumerable<Symbol> SelectSymbols(DateTime dateTime)
        {
            Log($"SelectSymbols() {Time}");
            if (dateTime.DayOfWeek == DayOfWeek.Monday || dateTime.DayOfWeek == DayOfWeek.Tuesday)
            {
                yield return QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            }
            else if (dateTime.DayOfWeek == DayOfWeek.Wednesday)
            {
                // given the date/time rules specified in Initialize, this symbol will never be selected (not invoked on wednesdays)
                yield return QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            }
            else
            {
                yield return QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            }

            if (dateTime.DayOfWeek == DayOfWeek.Tuesday || dateTime.DayOfWeek == DayOfWeek.Thursday)
            {
                yield return QuantConnect.Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda);
            }
            else if (dateTime.DayOfWeek == DayOfWeek.Friday)
            {
                // given the date/time rules specified in Initialize, this symbol will never be selected (every 6 hours never lands on hour==1)
                yield return QuantConnect.Symbol.Create("EURGBP", SecurityType.Forex, Market.Oanda);
            }
            else
            {
                yield return QuantConnect.Symbol.Create("NZDUSD", SecurityType.Forex, Market.Oanda);
            }
        }

        // some days of the week have different behavior the first time -- less securities to remove
        private readonly HashSet<DayOfWeek> _seenDays = new HashSet<DayOfWeek>();
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Console.WriteLine($"{Time}: {changes}");

            switch (Time.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    ExpectAdditions(changes, "SPY", "NZDUSD");
                    if (_seenDays.Add(DayOfWeek.Monday))
                    {
                        ExpectRemovals(changes, null);
                    }
                    else
                    {
                        ExpectRemovals(changes, "EURUSD", "IBM");
                    }
                    break;

                case DayOfWeek.Tuesday:
                    ExpectAdditions(changes, "EURUSD");
                    if (_seenDays.Add(DayOfWeek.Tuesday))
                    {
                        ExpectRemovals(changes, "NZDUSD");
                    }
                    else
                    {
                        ExpectRemovals(changes, "NZDUSD");
                    }
                    break;

                case DayOfWeek.Wednesday:
                    // selection function not invoked on wednesdays
                    ExpectAdditions(changes, null);
                    ExpectRemovals(changes, null);
                    break;

                case DayOfWeek.Thursday:
                    ExpectAdditions(changes, "IBM");
                    ExpectRemovals(changes, "SPY");
                    break;

                case DayOfWeek.Friday:
                    // selection function not invoked on fridays
                    ExpectAdditions(changes, null);
                    ExpectRemovals(changes, null);
                    break;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Console.WriteLine($"{Time}: {orderEvent}");
        }

        private void ExpectAdditions(SecurityChanges changes, params string[] tickers)
        {
            if (tickers == null && changes.AddedSecurities.Count > 0)
            {
                throw new Exception($"{Time}: Expected no additions: {Time.DayOfWeek}");
            }
            if (tickers == null)
            {
                return;
            }

            foreach (var ticker in tickers)
            {
                if (changes.AddedSecurities.All(s => s.Symbol.Value != ticker))
                {
                    throw new Exception($"{Time}: Expected {ticker} to be added: {Time.DayOfWeek}");
                }
            }
        }

        private void ExpectRemovals(SecurityChanges changes, params string[] tickers)
        {
            if (tickers == null && changes.RemovedSecurities.Count > 0)
            {
                throw new Exception($"{Time}: Expected no removals: {Time.DayOfWeek}");
            }

            if (tickers == null)
            {
                return;
            }

            foreach (var ticker in tickers)
            {
                if (changes.RemovedSecurities.All(s => s.Symbol.Value != ticker))
                {
                    throw new Exception($"{Time}: Expected {ticker} to be removed: {Time.DayOfWeek}");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "86"},
            {"Average Win", "0.16%"},
            {"Average Loss", "-0.10%"},
            {"Compounding Annual Return", "51.162%"},
            {"Drawdown", "1.100%"},
            {"Expectancy", "0.793"},
            {"Net Profit", "3.748%"},
            {"Sharpe Ratio", "7.195"},
            {"Probabilistic Sharpe Ratio", "99.177%"},
            {"Loss Rate", "31%"},
            {"Win Rate", "69%"},
            {"Profit-Loss Ratio", "1.60"},
            {"Alpha", "0.366"},
            {"Beta", "0.161"},
            {"Annual Standard Deviation", "0.055"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "3.061"},
            {"Tracking Error", "0.07"},
            {"Treynor Ratio", "2.443"},
            {"Total Fees", "$33.96"},
            {"Fitness Score", "0.75"},
            {"Kelly Criterion Estimate", "23.91"},
            {"Kelly Criterion Probability Value", "0.076"},
            {"Sortino Ratio", "42.076"},
            {"Return Over Maximum Drawdown", "129.046"},
            {"Portfolio Turnover", "0.751"},
            {"Total Insights Generated", "55"},
            {"Total Insights Closed", "53"},
            {"Total Insights Analysis Completed", "53"},
            {"Long Insight Count", "55"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$814596.0814"},
            {"Total Accumulated Estimated Alpha Value", "$888136.0054"},
            {"Mean Population Estimated Insight Value", "$16757.2831"},
            {"Mean Population Direction", "58.4906%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "55.0223%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "45bc37f5bbb85736d63e89119bc5f09f"}
        };
    }
}
