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
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that the <see cref="QCAlgorithm.OnMarginCallWarning"/> and <see cref="QCAlgorithm.OnMarginCall"/>
    /// events are fired when trading equities
    /// </summary>
    public class EquityMarginCallAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        private bool _receivedMarginCallWarning;

        private bool _onMarginCallWasCalled;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 30);
            SetCash(100000);

            var equity = AddEquity("GOOG");
            equity.SetLeverage(100);
            _symbol = equity.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_symbol, 86);
            }
        }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            Debug($"OnMarginCall at {Time}");
            _onMarginCallWasCalled = true;
            foreach (var request in requests)
            {
                var security = Portfolio.Securities[request.Symbol];

                // Ensure margin call orders only happen when the exchange is open
                if (!security.Exchange.ExchangeOpen)
                {
                    throw new RegressionTestException("Margin calls should not occur outside regular market hours!");
                }
            }
        }

        public override void OnMarginCallWarning()
        {
            Debug($"OnMarginCallWarning at {Time}");
            _receivedMarginCallWarning = true;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_receivedMarginCallWarning)
            {
                throw new RegressionTestException("OnMarginCallWarning was not invoked");
            }

            if (!_onMarginCallWasCalled)
            {
                throw new RegressionTestException("OnMarginCall was not invoked");
            }

            // margin call orders should have liquidated part of the position and get us within the maintenance margin
            if (Portfolio.MarginRemaining < 0)
            {
                throw new RegressionTestException("MarginRemaining should be positive");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3190;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "7"},
            {"Average Win", "0%"},
            {"Average Loss", "-6.17%"},
            {"Compounding Annual Return", "48311.308%"},
            {"Drawdown", "72.300%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "113866.54"},
            {"Net Profit", "13.867%"},
            {"Sharpe Ratio", "1535787524789540"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "87.476%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "11835996770264000"},
            {"Beta", "-13.194"},
            {"Annual Standard Deviation", "7.707"},
            {"Annual Variance", "59.395"},
            {"Information Ratio", "1533248041772298"},
            {"Tracking Error", "7.72"},
            {"Treynor Ratio", "-897105767214540.6"},
            {"Total Fees", "$91.53"},
            {"Estimated Strategy Capacity", "$18000.00"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "2904.79%"},
            {"Drawdown Recovery", "5"},
            {"OrderListHash", "80d456f6613030d3ff67b6c59dba5707"}
        };
    }
}
