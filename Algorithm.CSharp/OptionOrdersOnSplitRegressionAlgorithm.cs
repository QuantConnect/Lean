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
using System.Linq;
using System.Collections.Generic;

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Util;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that option orders are not allowed on split dates
    /// </summary>
    public class OptionOrdersOnSplitRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;

        private OrderTicket _ticket;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 11);
            SetCash(100000);

            _aapl = AddEquity("AAPL", Resolution.Minute, extendedMarketHours: true, dataNormalizationMode: DataNormalizationMode.Raw).Symbol;

            var option = AddOption(_aapl, Resolution.Minute);
            option.SetFilter(-1, +1, 0, 365);
        }

        public override void OnData(Slice slice)
        {
            if (slice.Splits.TryGetValue(_aapl, out var split))
            {
                Debug($"Split: {Time} - {split}");

                if (split.Type == SplitType.SplitOccurred)
                {
                    var contract = Securities.Values
                        .Where(x => x.Type.IsOption() && !x.Symbol.IsCanonical())
                        .OrderBy(x => x.Symbol.ID.StrikePrice)
                        .First();
                    _ticket = MarketOrder(contract.Symbol, 1);

                    if (_ticket.Status != OrderStatus.Invalid ||
                        _ticket.SubmitRequest.Response.IsSuccess ||
                        _ticket.SubmitRequest.Response.ErrorCode != OrderResponseErrorCode.OptionOrderOnStockSplit ||
                        _ticket.SubmitRequest.Response.ErrorMessage != "Options orders are not allowed when a split occurred for its underlying stock")
                    {
                        throw new Exception(
                            $"Expected invalid order ticket with error code {nameof(OrderResponseErrorCode.OptionOrderOnStockSplit)}, " +
                            $"but received {_ticket.SubmitRequest.Response.ErrorCode} - {_ticket.SubmitRequest.Response.ErrorMessage}");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_ticket == null)
            {
                throw new Exception("Expected invalid order ticket with error code OptionOrderOnStockSplit, but no order was submitted");
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
        public long DataPoints => 6972054;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Start Equity", "100000"},
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
            {"Information Ratio", "-2.491"},
            {"Tracking Error", "0.042"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
