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
using System.Reflection;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests Out of The Money (OTM) future option expiry for calls.
    /// We expect 2 orders from the algorithm, which are:
    ///
    ///   * Initial entry, buy ES Call Option (expiring OTM)
    ///     - contract expires worthless, not exercised, so never opened a position in the underlying
    ///
    ///   * Liquidation of worthless ES call option (expiring OTM). The option exercise order fill price must be zero.
    /// </summary>
    /// <remarks>
    /// Total Trades in regression algorithm should be 1, but expiration is counted as a trade.
    /// See related issue: https://github.com/QuantConnect/Lean/issues/4854
    /// </remarks>
    public class OptionOTMExpiryOrderHasZeroPriceRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _es19m20;
        private Symbol _esOption;
        private Symbol _expectedContract;

        private decimal _cashAfterMarketOrder;
        private string _firstOptionExerciseOrderEventMessage;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 5);
            SetEndDate(2020, 6, 30);

            _es19m20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2020, 6, 19)),
                Resolution.Minute).Symbol;

            // Select a future option call expiring OTM, and adds it to the algorithm.
            _esOption = AddFutureOptionContract(OptionChain(_es19m20)
                .Where(x => x.ID.StrikePrice >= 3300m && x.ID.OptionRight == OptionRight.Call)
                .OrderBy(x => x.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution.Minute).Symbol;

            _expectedContract = QuantConnect.Symbol.CreateOption(_es19m20, Market.CME, OptionStyle.American, OptionRight.Call, 3300m, new DateTime(2020, 6, 19));
            if (_esOption != _expectedContract)
            {
                throw new RegressionTestException($"Contract {_expectedContract} was not found in the chain");
            }
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                MarketOrder(_esOption, 1);
                _cashAfterMarketOrder = Portfolio.Cash;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                // There's lots of noise with OnOrderEvent, but we're only interested in fills.
                return;
            }

            if (!Securities.ContainsKey(orderEvent.Symbol))
            {
                throw new RegressionTestException($"Order event Symbol not found in Securities collection: {orderEvent.Symbol}");
            }

            var security = Securities[orderEvent.Symbol];
            if (security.Symbol == _es19m20)
            {
                throw new RegressionTestException("Invalid state: did not expect a position for the underlying to be opened, since this contract expires OTM");
            }

            if (_cashAfterMarketOrder > 0)
            {
                // This is the exercise order fill event
                if (orderEvent.IsInTheMoney || orderEvent.FillPrice != 0)
                {
                    throw new RegressionTestException($"Expected exercise order event fill price to be zero and to be marked as OTM, " +
                        $"but was the fill price was {orderEvent.FillPrice} and IsInTheMoney = {orderEvent.IsInTheMoney}");
                }
            }

            if (Transactions.GetOrderById(orderEvent.OrderId).Type == OrderType.OptionExercise && _firstOptionExerciseOrderEventMessage == default)
            {
                _firstOptionExerciseOrderEventMessage = orderEvent.Message;
            }
        }

        /// <summary>
        /// Ran at the end of the algorithm to ensure the algorithm has no holdings
        /// </summary>
        /// <exception cref="RegressionTestException">The algorithm has holdings</exception>
        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new RegressionTestException($"Expected no holdings at end of algorithm, but are invested in: {string.Join(", ", Portfolio.Keys)}");
            }

            // No change in cash is expected, only the market order fill price
            if (Portfolio.Cash != _cashAfterMarketOrder)
            {
                throw new RegressionTestException($"Expected no change in cash after the market order. Cash in portfolio: {Portfolio.Cash}. Cash in portfolio after the market order: {_cashAfterMarketOrder}");
            }

            var orders = Transactions.GetOrders().ToList();
            if (orders.Count != 2)
            {
                throw new RegressionTestException($"Expected 2 orders (market order and OTM option exercise), but found: {orders.Count}");
            }

            var exerciseOrder = orders.Find(x => x.Type == OrderType.OptionExercise);
            if (!_firstOptionExerciseOrderEventMessage.Contains("OTM", StringComparison.InvariantCulture) || exerciseOrder.Price != 0)
            {
                throw new RegressionTestException($"Expected the OTM exercise order to have price = 0, but was: {exerciseOrder.Price}");
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
        public long DataPoints => 212196;

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
            {"Average Loss", "-3.85%"},
            {"Compounding Annual Return", "-7.754%"},
            {"Drawdown", "4.300%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "96148.58"},
            {"Net Profit", "-3.851%"},
            {"Sharpe Ratio", "-1.221"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0.131%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.063"},
            {"Beta", "0.003"},
            {"Annual Standard Deviation", "0.052"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-0.198"},
            {"Tracking Error", "0.377"},
            {"Treynor Ratio", "-23.065"},
            {"Total Fees", "$1.42"},
            {"Estimated Strategy Capacity", "$180000000.00"},
            {"Lowest Capacity Asset", "ES XFH59UPHGV9G|ES XFH59UK0MYO1"},
            {"Portfolio Turnover", "0.02%"},
            {"OrderListHash", "1d3c36cec32b24e8911d87d7b9730192"}
        };
    }
}

