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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests Out of The Money (OTM) index option expiry for calls.
    /// We expect 2 orders from the algorithm, which are:
    ///
    ///   * Initial entry, buy SPX Call Option (expiring OTM)
    ///     - contract expires worthless, not exercised, so never opened a position in the underlying
    ///
    ///   * Liquidation of worthless SPX call option (expiring OTM)
    ///
    /// Additionally, we test delistings for index options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    /// </summary>
    /// <remarks>
    /// Total Trades in regression algorithm should be 1, but expiration is counted as a trade.
    /// See related issue: https://github.com/QuantConnect/Lean/issues/4854
    /// </remarks>
    public class IndexOptionCallOTMExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spx;
        private Symbol _spxOption;
        private int _optionOrders;
        private Symbol _expectedContract;

        protected virtual Resolution Resolution => Resolution.Minute;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 31);

            _spx = AddIndex("SPX", Resolution).Symbol;

            // Select a index option call expiring OTM, and adds it to the algorithm.
            _spxOption = AddIndexOptionContract(OptionChain(_spx)
                .Where(x => x.ID.StrikePrice >= 4250m && x.ID.OptionRight == OptionRight.Call && x.ID.Date.Year == 2021 && x.ID.Date.Month == 1)
                .OrderBy(x => x.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution).Symbol;

            _expectedContract = QuantConnect.Symbol.CreateOption(_spx, Market.USA, OptionStyle.European, OptionRight.Call, 4250m, new DateTime(2021, 1, 15));
            if (_spxOption != _expectedContract)
            {
                throw new RegressionTestException($"Contract {_expectedContract} was not found in the chain");
            }

            Schedule.On(DateRules.Tomorrow, TimeRules.AfterMarketOpen(_spx, 1), () =>
            {
                MarketOrder(_spxOption, 1);
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
                    if (delisting.Time != new DateTime(2021, 1, 15))
                    {
                        throw new RegressionTestException($"Delisting warning issued at unexpected date: {delisting.Time}");
                    }
                }
                if (delisting.Type == DelistingType.Delisted)
                {
                    if (delisting.Time != new DateTime(2021, 1, 16))
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
            if (security.Symbol == _spx)
            {
                throw new RegressionTestException("Invalid state: did not expect a position for the underlying to be opened, since this contract expires OTM");
            }
            if (security.Symbol == _expectedContract)
            {
                AssertIndexOptionContractOrder(orderEvent, security);
            }
            else
            {
                throw new RegressionTestException($"Received order event for unknown Symbol: {orderEvent.Symbol}");
            }

            Log($"{orderEvent}");
        }

        private void AssertIndexOptionContractOrder(OrderEvent orderEvent, Security option)
        {
            if (orderEvent.Direction == OrderDirection.Buy && option.Holdings.Quantity != 1)
            {
                throw new RegressionTestException($"No holdings were created for option contract {option.Symbol}");
            }
            if (orderEvent.Direction == OrderDirection.Sell && option.Holdings.Quantity != 0)
            {
                throw new RegressionTestException("Holdings were found after a filled option exercise");
            }
            if (orderEvent.Direction == OrderDirection.Sell && !orderEvent.Message.Contains("OTM"))
            {
                throw new RegressionTestException("Contract did not expire OTM");
            }
            if (orderEvent.Message.Contains("Exercise"))
            {
                throw new RegressionTestException("Exercised option, even though it expires OTM");
            }
            _optionOrders++;
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

            if (_optionOrders != 2)
            {
                throw new RegressionTestException("Option orders were not as expected!");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 15941;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-0.142%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99990"},
            {"Net Profit", "-0.010%"},
            {"Sharpe Ratio", "-15.959"},
            {"Sortino Ratio", "-124989.863"},
            {"Probabilistic Sharpe Ratio", "0.015%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.004"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.334"},
            {"Tracking Error", "0.138"},
            {"Treynor Ratio", "-32.969"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$22000.00"},
            {"Lowest Capacity Asset", "SPX XL80P59H5E6M|SPX 31"},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", "adfa67772fb1a7f40d65922cbe180c8e"}
        };
    }
}

