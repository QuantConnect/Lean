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
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing portfolio construction model control over rebalancing,
    /// specifying a custom rebalance function that returns null in some cases, see GH 4075.
    /// </summary>
    public class PortfolioRebalanceOnCustomFuncRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _lastRebalanceTime;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            // Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
            // Commented so regression algorithm is more sensitive
            //Settings.MinimumOrderMarginPortfolioPercentage = 0.005m;

            SetStartDate(2015, 1, 1);
            SetEndDate(2018, 1, 1);

            Settings.RebalancePortfolioOnInsightChanges = false;
            Settings.RebalancePortfolioOnSecurityChanges = false;

            SetUniverseSelection(new CustomUniverseSelectionModel("CustomUniverseSelectionModel",
                time => new List<string> { "AAPL", "IBM", "FB", "SPY", "AIG", "BAC", "BNO" }
            ));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(
                time =>
                {
                    // for performance only run rebalance logic once a week
                    if (time.DayOfWeek != DayOfWeek.Monday)
                    {
                        return null;
                    }

                    if (_lastRebalanceTime == default(DateTime))
                    {
                        // initial rebalance
                        _lastRebalanceTime = time;
                        return time;
                    }

                    var deviation = 0m;
                    var count = Securities.Values.Count(security => security.Invested);
                    if (count > 0)
                    {
                        _lastRebalanceTime = time;
                        var portfolioValuePerSecurity = Portfolio.TotalPortfolioValue / count;
                        foreach (var security in Securities.Values.Where(security => security.Invested))
                        {
                            var reservedBuyingPowerForCurrentPosition = security.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                                                                            new ReservedBuyingPowerForPositionParameters(security)).AbsoluteUsedBuyingPower
                                                                        // see GH issue 4107
                                                                        * security.BuyingPowerModel.GetLeverage(security);
                            // we sum up deviation for each security
                            deviation += (portfolioValuePerSecurity - reservedBuyingPowerForCurrentPosition) / portfolioValuePerSecurity;
                        }

                        // if securities are deviated 1.5% from their theoretical share of TotalPortfolioValue we rebalance
                        if (deviation >= 0.015m)
                        {
                            return time;
                        }
                    }
                    return null;
                }));
            SetExecution(new ImmediateExecutionModel());
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{orderEvent}");
            if (orderEvent.Status == OrderStatus.Submitted)
            {
                if (UtcTime - _lastRebalanceTime > TimeSpan.Zero || UtcTime.DayOfWeek != DayOfWeek.Monday)
                {
                    throw new Exception($"{UtcTime} {orderEvent.Symbol} {UtcTime - _lastRebalanceTime}");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 11386;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "27"},
            {"Average Win", "0.93%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "18.532%"},
            {"Drawdown", "9.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "166459.50"},
            {"Net Profit", "66.459%"},
            {"Sharpe Ratio", "1.447"},
            {"Sortino Ratio", "1.345"},
            {"Probabilistic Sharpe Ratio", "84.195%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.088"},
            {"Beta", "0.401"},
            {"Annual Standard Deviation", "0.081"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "0.49"},
            {"Tracking Error", "0.093"},
            {"Treynor Ratio", "0.292"},
            {"Total Fees", "$41.31"},
            {"Estimated Strategy Capacity", "$320000.00"},
            {"Lowest Capacity Asset", "BNO UN3IMQ2JU1YD"},
            {"Portfolio Turnover", "0.11%"},
            {"OrderListHash", "5e609a00e4c40ccbe762c9aeabf74d98"}
        };
    }
}
