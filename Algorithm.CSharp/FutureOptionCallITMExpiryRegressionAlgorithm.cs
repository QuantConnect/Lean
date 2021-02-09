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
    /// This regression algorithm tests In The Money (ITM) future option expiry for calls.
    /// We expect 3 orders from the algorithm, which are:
    ///
    ///   * Initial entry, buy ES Call Option (expiring ITM)
    ///   * Option exercise, receiving ES future contracts
    ///   * Future contract liquidation, due to impending expiry
    ///
    /// Additionally, we test delistings for future options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    /// </summary>
    public class FutureOptionCallITMExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _es19m20;
        private Symbol _esOption;
        private Symbol _expectedOptionContract;

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
                .Where(x => x.ID.StrikePrice <= 3200m && x.ID.OptionRight == OptionRight.Call)
                .OrderByDescending(x => x.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution.Minute).Symbol;

            _expectedOptionContract = QuantConnect.Symbol.CreateOption(_es19m20, Market.CME, OptionStyle.American, OptionRight.Call, 3200m, new DateTime(2020, 6, 19));
            if (_esOption != _expectedOptionContract)
            {
                throw new Exception($"Contract {_expectedOptionContract} was not found in the chain");
            }

            Schedule.On(DateRules.Tomorrow, TimeRules.AfterMarketOpen(_es19m20, 1), () =>
            {
                MarketOrder(_esOption, 1);
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
                AssertFutureOptionOrderExercise(orderEvent, security, Securities[_expectedOptionContract]);
            }
            else if (security.Symbol == _expectedOptionContract)
            {
                AssertFutureOptionContractOrder(orderEvent, security);
            }
            else
            {
                throw new Exception($"Received order event for unknown Symbol: {orderEvent.Symbol}");
            }

            Log($"{Time:yyyy-MM-dd HH:mm:ss} -- {orderEvent.Symbol} :: Price: {Securities[orderEvent.Symbol].Holdings.Price} Qty: {Securities[orderEvent.Symbol].Holdings.Quantity} Direction: {orderEvent.Direction} Msg: {orderEvent.Message}");
        }

        private void AssertFutureOptionOrderExercise(OrderEvent orderEvent, Security future, Security optionContract)
        {
            var expectedLiquidationTimeUtc = new DateTime(2020, 6, 19, 20, 0, 0);

            if (orderEvent.Direction == OrderDirection.Sell && future.Holdings.Quantity != 0)
            {
                // We expect the contract to have been liquidated immediately
                throw new Exception($"Did not liquidate existing holdings for Symbol {future.Symbol}");
            }
            if (orderEvent.Direction == OrderDirection.Sell && orderEvent.UtcTime != expectedLiquidationTimeUtc)
            {
                throw new Exception($"Liquidated future contract, but not at the expected time. Expected: {expectedLiquidationTimeUtc:yyyy-MM-dd HH:mm:ss} - found {orderEvent.UtcTime:yyyy-MM-dd HH:mm:ss}");
            }

            // No way to detect option exercise orders or any other kind of special orders
            // other than matching strings, for now.
            if (orderEvent.Message.Contains("Option Exercise"))
            {
                if (orderEvent.FillPrice != 3200m)
                {
                    throw new Exception("Option did not exercise at expected strike price (3200)");
                }
                if (future.Holdings.Quantity != 1)
                {
                    // Here, we expect to have some holdings in the underlying, but not in the future option anymore.
                    throw new Exception($"Exercised option contract, but we have no holdings for Future {future.Symbol}");
                }

                if (optionContract.Holdings.Quantity != 0)
                {
                    throw new Exception($"Exercised option contract, but we have holdings for Option contract {optionContract.Symbol}");
                }
            }
        }

        private void AssertFutureOptionContractOrder(OrderEvent orderEvent, Security option)
        {
            if (orderEvent.Direction == OrderDirection.Buy && option.Holdings.Quantity != 1)
            {
                throw new Exception($"No holdings were created for option contract {option.Symbol}");
            }
            if (orderEvent.Direction == OrderDirection.Sell && option.Holdings.Quantity != 0)
            {
                throw new Exception($"Holdings were found after a filled option exercise");
            }
            if (orderEvent.Message.Contains("Exercise") && option.Holdings.Quantity != 0)
            {
                throw new Exception($"Holdings were found after exercising option contract {option.Symbol}");
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "1.22%"},
            {"Average Loss", "-7.42%"},
            {"Compounding Annual Return", "-13.222%"},
            {"Drawdown", "6.300%"},
            {"Expectancy", "-0.417"},
            {"Net Profit", "-6.282%"},
            {"Sharpe Ratio", "-1.345"},
            {"Probabilistic Sharpe Ratio", "0.005%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.17"},
            {"Alpha", "-0.105"},
            {"Beta", "-0.003"},
            {"Annual Standard Deviation", "0.078"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "0.678"},
            {"Tracking Error", "0.191"},
            {"Treynor Ratio", "33.18"},
            {"Total Fees", "$7.40"},
            {"Fitness Score", "0.008"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.217"},
            {"Return Over Maximum Drawdown", "-2.105"},
            {"Portfolio Turnover", "0.024"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "c59d790b89d76f1ad3bb7738b28567c9"}
        };
    }
}

