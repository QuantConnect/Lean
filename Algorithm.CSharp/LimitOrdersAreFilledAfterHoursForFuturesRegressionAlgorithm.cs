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
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Future;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for testing limit orders are filled after hours for futures.
    /// It also asserts that market-on-open orders are not allowed for futures outside of regular market hours
    /// </summary>
    public class LimitOrdersAreFilledAfterHoursForFuturesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Future _futureContract;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 6);
            SetEndDate(2013, 10, 10);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.LastTradingDay,
                contractDepthOffset: 0,
                extendedMarketHours: true
            );
            _futureContract = AddFutureContract(FuturesChain(_continuousContract.Symbol).First(), extendedMarketHours: true);
        }

        public override void OnWarmupFinished()
        {
            // Right after warm up we should be outside regular market hours
            if (_futureContract.Exchange.ExchangeOpen)
            {
                throw new RegressionTestException("We should be outside regular market hours");
            }

            // Market on open order should not be allowed for futures outside of regular market hours
            var futureContractMarketOnOpenOrder = MarketOnOpenOrder(_futureContract.Symbol, 1);
            if (futureContractMarketOnOpenOrder.Status != OrderStatus.Invalid)
            {
                throw new RegressionTestException($"Market on open order should not be allowed for futures outside of regular market hours");
            }
        }

        public override void OnData(Slice slice)
        {
            if (Time.TimeOfDay.Hours > 17 && !Portfolio.Invested)
            {
                // Limit order should be allowed for futures outside of regular market hours.
                // Use a very high limit price so the limit orders get filled immediately
                var futureContractLimitOrder = LimitOrder(_futureContract.Symbol, 1, _futureContract.Price * 2m);
                var continuousContractLimitOrder = LimitOrder(_continuousContract.Mapped, 1, _continuousContract.Price * 2m);
                if (futureContractLimitOrder.Status == OrderStatus.Invalid || continuousContractLimitOrder.Status == OrderStatus.Invalid)
                {
                    throw new RegressionTestException($"Limit order should be allowed for futures outside of regular market hours");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Transactions.GetOrders().Any(order => order.Status != OrderStatus.Filled ))
            {
                throw new RegressionTestException("Not all orders were filled");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // 13:30 and 21:00 UTC are 9:30 and 17 New york, which are the regular market hours litimits for this security
            if (orderEvent.Status == OrderStatus.Filled && !Securities[orderEvent.Symbol].Exchange.DateTimeIsOpen(orderEvent.UtcTime) &&
                (orderEvent.UtcTime.TimeOfDay >= new TimeSpan(13, 30, 0) && orderEvent.UtcTime.TimeOfDay < new TimeSpan(21, 0, 0)))
            {
                throw new RegressionTestException($"Order should have been filled during extended market hours");
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
        public long DataPoints => 52987;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "120.870%"},
            {"Drawdown", "3.700%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101091.4"},
            {"Net Profit", "1.091%"},
            {"Sharpe Ratio", "4.261"},
            {"Sortino Ratio", "29.094"},
            {"Probabilistic Sharpe Ratio", "58.720%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.134"},
            {"Beta", "1.285"},
            {"Annual Standard Deviation", "0.314"},
            {"Annual Variance", "0.098"},
            {"Information Ratio", "15.222"},
            {"Tracking Error", "0.077"},
            {"Treynor Ratio", "1.04"},
            {"Total Fees", "$4.30"},
            {"Estimated Strategy Capacity", "$39000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "33.59%"},
            {"OrderListHash", "8286cb0dd42649527c2c0032ee00e2bd"}
        };
    }
}
