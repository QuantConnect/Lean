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
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting consolidator registration ergonomics on a quote-only feed (forex):
    /// trade bar consolidators and indicators are fed collapsed quote bars instead of being rejected,
    /// <see cref="QCAlgorithm.RegisterIndicator{T}(Symbol,IndicatorBase{T},Func{DateTime,CalendarInfo},Func{IBaseData,T})"/>
    /// accepts calendar periods, and consolidator periods smaller than the subscription period are
    /// rejected at registration time instead of when the first data point arrives.
    /// </summary>
    public class ConsolidatorAutoAdaptationRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private TradeBarConsolidator _consolidator;
        private OnBalanceVolume _obv;
        private RelativeStrengthIndex _weeklyRsi;
        private int _adaptedTradeBars;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 5, 5);
            SetEndDate(2014, 5, 12);

            var eurusd = AddForex("EURUSD", Resolution.Minute, Market.Oanda).Symbol;

            // a trade bar consolidator on a quote-only feed: the engine feeds it quote bars collapsed
            // into mid-point trade bars with zero volume
            _consolidator = new TradeBarConsolidator(TimeSpan.FromHours(1));
            _consolidator.DataConsolidated += (_, bar) =>
            {
                _adaptedTradeBars++;
                if (bar.Volume != 0)
                {
                    throw new RegressionTestException("Expected trade bars collapsed from quote bars to have zero volume");
                }
            };
            SubscriptionManager.AddConsolidator(eurusd, _consolidator);

            // a trade bar indicator on a quote-only feed is fed collapsed trade bars as well
            _obv = OBV(eurusd, Resolution.Hour);

            // RegisterIndicator accepts calendar periods, like Consolidate does
            _weeklyRsi = new RelativeStrengthIndex(2);
            RegisterIndicator(eurusd, _weeklyRsi, Calendar.Weekly);

            // a consolidator period smaller than the subscription period is rejected at registration time,
            // instead of when the first data point arrives
            try
            {
                SubscriptionManager.AddConsolidator(eurusd, new TradeBarConsolidator(TimeSpan.FromSeconds(10)));
                throw new RegressionTestException($"Expected {nameof(ArgumentException)} for a consolidator period smaller than the subscription period");
            }
            catch (ArgumentException)
            {
                // expected, all the required information is available at registration time
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_adaptedTradeBars == 0)
            {
                throw new RegressionTestException("Expected the adapted trade bar consolidator to receive data");
            }
            if (_obv.Samples == 0)
            {
                throw new RegressionTestException("Expected the OnBalanceVolume indicator to be updated with collapsed trade bars");
            }
            if (_weeklyRsi.Samples == 0)
            {
                throw new RegressionTestException("Expected the weekly RSI to be updated when the week boundary was crossed");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 8661;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 3;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000.00"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-3.328"},
            {"Tracking Error", "0.091"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
