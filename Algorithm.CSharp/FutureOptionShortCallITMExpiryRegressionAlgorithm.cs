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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests In The Money (ITM) future option expiry for short calls.
    /// We expect 3 orders from the algorithm, which are:
    ///
    ///   * Initial entry, sell ES Call Option (expiring ITM)
    ///   * Option assignment, sell 1 contract of the underlying (ES)
    ///   * Future contract expiry, liquidation (buy 1 ES future)
    ///
    /// Additionally, we test delistings for future options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    /// </summary>
    public class FutureOptionShortCallITMExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _es19m20;
        private Symbol _esOption;
        private Symbol _expectedContract;

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

            // Select a future option expiring ITM, and adds it to the algorithm.
            _esOption = AddFutureOptionContract(OptionChain(_es19m20)
                .Where(x => x.ID.StrikePrice <= 3100m && x.ID.OptionRight == OptionRight.Call)
                .OrderByDescending(x => x.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution.Minute).Symbol;

            _expectedContract = QuantConnect.Symbol.CreateOption(_es19m20, Market.CME, OptionStyle.American, OptionRight.Call, 3100m, new DateTime(2020, 6, 19));
            if (_esOption != _expectedContract)
            {
                throw new RegressionTestException($"Contract {_expectedContract} was not found in the chain");
            }

            Schedule.On(DateRules.Tomorrow, TimeRules.AfterMarketOpen(_es19m20, 1), () =>
            {
                MarketOrder(_esOption, -1);
            });
        }

        public override void OnData(Slice slice)
        {
            // Assert delistings, so that we can make sure that we receive the delisting warnings at
            // the expected time. These assertions detect bug #4872
            foreach (var delisting in slice.Delistings.Values)
            {
                if (delisting.Type == DelistingType.Warning)
                {
                    if (delisting.Time != new DateTime(2020, 6, 19))
                    {
                        throw new RegressionTestException($"Delisting warning issued at unexpected date: {delisting.Time}");
                    }
                }
                if (delisting.Type == DelistingType.Delisted)
                {
                    if (delisting.Time != new DateTime(2020, 6, 20))
                    {
                        throw new RegressionTestException($"Delisting happened at unexpected date: {delisting.Time}");
                    }
                }
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
                AssertFutureOptionOrderExercise(orderEvent, security, Securities[_expectedContract]);
            }
            else if (security.Symbol == _expectedContract)
            {
                AssertFutureOptionContractOrder(orderEvent, security);
            }
            else
            {
                throw new RegressionTestException($"Received order event for unknown Symbol: {orderEvent.Symbol}");
            }

            Log($"{orderEvent}");
        }

        private void AssertFutureOptionOrderExercise(OrderEvent orderEvent, Security future, Security optionContract)
        {
            if (orderEvent.Message.Contains("Assignment"))
            {
                if (orderEvent.FillPrice != 3100m)
                {
                    throw new RegressionTestException("Option was not assigned at expected strike price (3100)");
                }
                if (orderEvent.Direction != OrderDirection.Sell || future.Holdings.Quantity != -1)
                {
                    throw new RegressionTestException($"Expected Qty: -1 futures holdings for assigned future {future.Symbol}, found {future.Holdings.Quantity}");
                }

                return;
            }

            if (orderEvent.Direction == OrderDirection.Buy && future.Holdings.Quantity != 0)
            {
                // We buy back the underlying at expiration, so we expect a neutral position then
                throw new RegressionTestException($"Expected no holdings when liquidating future contract {future.Symbol}");
            }
        }

        private void AssertFutureOptionContractOrder(OrderEvent orderEvent, Security option)
        {
            if (orderEvent.Direction == OrderDirection.Sell && option.Holdings.Quantity != -1)
            {
                throw new RegressionTestException($"No holdings were created for option contract {option.Symbol}");
            }
            if (orderEvent.IsAssignment && option.Holdings.Quantity != 0)
            {
                throw new RegressionTestException($"Holdings were found after option contract was assigned: {option.Symbol}");
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
            {"Total Orders", "3"},
            {"Average Win", "10.24%"},
            {"Average Loss", "-6.46%"},
            {"Compounding Annual Return", "6.498%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "0.292"},
            {"Start Equity", "100000"},
            {"End Equity", "103111.08"},
            {"Net Profit", "3.111%"},
            {"Sharpe Ratio", "0.726"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "45.614%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.58"},
            {"Alpha", "0.036"},
            {"Beta", "-0.003"},
            {"Annual Standard Deviation", "0.049"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "0.064"},
            {"Tracking Error", "0.378"},
            {"Treynor Ratio", "-13.127"},
            {"Total Fees", "$1.42"},
            {"Estimated Strategy Capacity", "$13000000.00"},
            {"Lowest Capacity Asset", "ES XFH59UP5K75W|ES XFH59UK0MYO1"},
            {"Portfolio Turnover", "1.79%"},
            {"OrderListHash", "ea7c72a493aa29a52a5f782fc741d391"}
        };
    }
}

