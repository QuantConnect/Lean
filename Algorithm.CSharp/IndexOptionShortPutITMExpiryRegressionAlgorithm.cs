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
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests In The Money (ITM) index option expiry for short puts.
    /// We expect 2 orders from the algorithm, which are:
    ///
    ///   * Initial entry, sell SPX Put Option (expiring ITM)
    ///   * Option assignment
    ///
    /// Additionally, we test delistings for index options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    /// </summary>
    public class IndexOptionShortPutITMExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spx;
        private Symbol _spxOption;
        private Symbol _expectedContract;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 31);
            SetCash(1000000);

            Portfolio.SetMarginCallModel(MarginCallModel.Null);

            SetSecurityInitializer(new CompositeSecurityInitializer(SecurityInitializer,
                new FuncSecurityInitializer((security) =>
                {
                    var option = security as Option;
                    // avoid getting assigned
                    option?.SetOptionAssignmentModel(new NullOptionAssignmentModel());
                })));

            _spx = AddIndex("SPX", Resolution.Minute).Symbol;

            // Select a index option expiring ITM, and adds it to the algorithm.
            _spxOption = AddIndexOptionContract(OptionChain(_spx)
                .Where(contractData => contractData.ID.StrikePrice <= 4200m && contractData.ID.OptionRight == OptionRight.Put && contractData.ID.Date.Year == 2021 && contractData.ID.Date.Month == 1)
                .OrderByDescending(contractData => contractData.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution.Minute).Symbol;

            _expectedContract = QuantConnect.Symbol.CreateOption(_spx, Market.USA, OptionStyle.European, OptionRight.Put, 4200m, new DateTime(2021, 1, 15));
            if (_spxOption != _expectedContract)
            {
                throw new RegressionTestException($"Contract {_expectedContract} was not found in the chain");
            }

            Schedule.On(DateRules.Tomorrow, TimeRules.AfterMarketOpen(_spx, 1), () =>
            {
                MarketOrder(_spxOption, -1);
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
                AssertIndexOptionOrderExercise(orderEvent, security, Securities[_expectedContract]);
            }
            else if (security.Symbol == _expectedContract)
            {
                AssertIndexOptionContractOrder(orderEvent, security);
            }
            else
            {
                throw new RegressionTestException($"Received order event for unknown Symbol: {orderEvent.Symbol}");
            }

            Log($"{orderEvent}");
        }

        private void AssertIndexOptionOrderExercise(OrderEvent orderEvent, Security index, Security optionContract)
        {
            if (orderEvent.Message.Contains("Assignment"))
            {
                if (orderEvent.FillPrice != 4200)
                {
                    throw new RegressionTestException("Option was not assigned at expected strike price (4200)");
                }
                if (orderEvent.Direction != OrderDirection.Buy || index.Holdings.Quantity != 0)
                {
                    throw new RegressionTestException($"Expected Qty: 0 index holdings for assigned index option {index.Symbol}, found {index.Holdings.Quantity}");
                }
            }
            else if (index.Holdings.Quantity != 0)
            {
                throw new RegressionTestException($"Expected no holdings in index: {index.Symbol}");
            }
        }

        private void AssertIndexOptionContractOrder(OrderEvent orderEvent, Security option)
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
        public long DataPoints => 19890;

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
            {"Average Win", "0.94%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "14.183%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "1009374"},
            {"Net Profit", "0.937%"},
            {"Sharpe Ratio", "2.997"},
            {"Sortino Ratio", "34.286"},
            {"Probabilistic Sharpe Ratio", "86.840%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.098"},
            {"Beta", "-0.021"},
            {"Annual Standard Deviation", "0.032"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "0.374"},
            {"Tracking Error", "0.144"},
            {"Treynor Ratio", "-4.521"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPX 31KC0UJHC75TA|SPX 31"},
            {"Portfolio Turnover", "0.19%"},
            {"OrderListHash", "5fa3290876b6616eb338fedccd65fd6f"}
        };
    }
}

