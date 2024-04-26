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
    /// This regression algorithm tests Out of The Money (OTM) future option expiry for short calls.
    /// We expect 2 orders from the algorithm, which are:
    ///
    ///   * Initial entry, sell ES Call Option (expiring OTM)
    ///     - Profit the option premium, since the option was not assigned.
    ///
    ///   * Liquidation of ES call OTM contract on the last trade date
    ///
    /// Additionally, we test delistings for future options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    /// </summary>
    public class FutureOptionShortCallOTMExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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
            _esOption = AddFutureOptionContract(OptionChainProvider.GetOptionContractList(_es19m20, Time)
                .Where(x => x.ID.StrikePrice >= 3400m && x.ID.OptionRight == OptionRight.Call)
                .OrderBy(x => x.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution.Minute).Symbol;

            _expectedContract = QuantConnect.Symbol.CreateOption(_es19m20, Market.CME, OptionStyle.American, OptionRight.Call, 3400m, new DateTime(2020, 6, 19));
            if (_esOption != _expectedContract)
            {
                throw new Exception($"Contract {_expectedContract} was not found in the chain");
            }

            Schedule.On(DateRules.Tomorrow, TimeRules.AfterMarketOpen(_es19m20, 1), () =>
            {
                MarketOrder(_esOption, -1);
            });
        }

        public override void OnData(Slice data)
        {
            // Assert delistings, so that we can make sure that we receive the delisting warnings at
            // the expected time. These assertions detect bug #4872
            foreach (var delisting in data.Delistings.Values)
            {
                if (delisting.Type == DelistingType.Warning)
                {
                    if (delisting.Time != new DateTime(2020, 6, 19))
                    {
                        throw new Exception($"Delisting warning issued at unexpected date: {delisting.Time}");
                    }
                }
                if (delisting.Type == DelistingType.Delisted)
                {
                    if (delisting.Time != new DateTime(2020, 6, 20))
                    {
                        throw new Exception($"Delisting happened at unexpected date: {delisting.Time}");
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
                throw new Exception($"Order event Symbol not found in Securities collection: {orderEvent.Symbol}");
            }

            var security = Securities[orderEvent.Symbol];
            if (security.Symbol == _es19m20)
            {
                throw new Exception($"Expected no order events for underlying Symbol {security.Symbol}");
            }

            if (security.Symbol == _expectedContract)
            {
                AssertFutureOptionContractOrder(orderEvent, security);
            }
            else
            {
                throw new Exception($"Received order event for unknown Symbol: {orderEvent.Symbol}");
            }

            Log($"{orderEvent}");
        }

        private void AssertFutureOptionContractOrder(OrderEvent orderEvent, Security optionContract)
        {
            if (orderEvent.Direction == OrderDirection.Sell && optionContract.Holdings.Quantity != -1)
            {
                throw new Exception($"No holdings were created for option contract {optionContract.Symbol}");
            }
            if (orderEvent.Direction == OrderDirection.Buy && optionContract.Holdings.Quantity != 0)
            {
                throw new Exception("Expected no options holdings after closing position");
            }
            if (orderEvent.IsAssignment)
            {
                throw new Exception($"Assignment was not expected for {orderEvent.Symbol}");
            }
        }

        /// <summary>
        /// Ran at the end of the algorithm to ensure the algorithm has no holdings
        /// </summary>
        /// <exception cref="Exception">The algorithm has holdings</exception>
        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new Exception($"Expected no holdings at end of algorithm, but are invested in: {string.Join(", ", Portfolio.Keys)}");
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
        public long DataPoints => 212195;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "1.74%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "3.600%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101736.08"},
            {"Net Profit", "1.736%"},
            {"Sharpe Ratio", "0.597"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "49.736%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.015"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.025"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "0.009"},
            {"Tracking Error", "0.375"},
            {"Treynor Ratio", "-11.057"},
            {"Total Fees", "$1.42"},
            {"Estimated Strategy Capacity", "$100000000.00"},
            {"Lowest Capacity Asset", "ES XFH59UPNF7B8|ES XFH59UK0MYO1"},
            {"Portfolio Turnover", "0.01%"},
            {"OrderListHash", "18f4574575cadc142af8f98ae8ba332b"}
        };
    }
}

