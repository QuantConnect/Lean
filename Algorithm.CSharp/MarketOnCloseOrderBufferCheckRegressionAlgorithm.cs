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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the check for market on close orders time buffer
    /// is done properly regardless of the algorithm and exchange time zones.
    /// </summary>
    public class MarketOnCloseOrderBufferCheckRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly TimeSpan _buffer = TimeSpan.FromMinutes(10);

        private Symbol _symbol;
        private OrderTicket _validOrderTicket;
        private OrderTicket _invalidOrderTicket;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);

            // Let's set the algorithm time zone to one that is ahead of the security's exchange time zone to test the MOC orders buffer check.
            SetTimeZone(TimeZones.London);

            _symbol = AddEquity("SPY").Symbol;

            Orders.MarketOnCloseOrder.SubmissionTimeBuffer = _buffer;
        }

        public override void OnData(Slice slice)
        {
            if (_validOrderTicket != null && _invalidOrderTicket != null)
            {
                return;
            }

            var security = Securities[_symbol];
            var nextUtcMarketCloseTime = security.Exchange.Hours
                .GetNextMarketClose(security.LocalTime, false)
                .ConvertToUtc(security.Exchange.TimeZone);

            var latestSubmissionTime = nextUtcMarketCloseTime - _buffer;
            // Place an order when we are close to the latest allowed submission time
            if (_validOrderTicket == null && UtcTime >= latestSubmissionTime - TimeSpan.FromMinutes(5) && UtcTime <= latestSubmissionTime)
            {
                _validOrderTicket = MarketOnCloseOrder(_symbol, 1);

                if (_validOrderTicket.Status == OrderStatus.Invalid)
                {
                    throw new Exception("MOC order placed at the last minute was expected to be valid.");
                }
            }

            // Place an order when we are past the latest allowed submission time
            if (_invalidOrderTicket == null && UtcTime > latestSubmissionTime)
            {
                _invalidOrderTicket = MarketOnCloseOrder(_symbol, 1);

                if (_invalidOrderTicket.Status != OrderStatus.Invalid ||
                    _invalidOrderTicket.SubmitRequest.Response.ErrorCode != OrderResponseErrorCode.MarketOnCloseOrderTooLate)
                {
                    throw new Exception(
                        "MOC order placed after the latest allowed submission time was not rejected or the reason was not the submission time");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // Set it back to default for other regressions
            Orders.MarketOnCloseOrder.SubmissionTimeBuffer = Orders.MarketOnCloseOrder.DefaultSubmissionTimeBuffer;

            if (_validOrderTicket == null)
            {
                throw new Exception("Valid MOC order was not placed");
            }

            // Verify that our good order filled
            if (_validOrderTicket.Status != OrderStatus.Filled)
            {
                throw new Exception("MOC order failed to fill");
            }

            if (_invalidOrderTicket == null)
            {
                throw new Exception("Invalid MOC order was not placed");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 795;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99999"},
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$67000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.14%"},
            {"OrderListHash", "e44ec9a38c118cc34a487dcfa645a658"}
        };
    }
}
