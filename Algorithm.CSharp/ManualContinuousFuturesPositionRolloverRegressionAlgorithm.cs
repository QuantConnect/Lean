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

using QuantConnect.Data;
using QuantConnect.Orders;
using System.Globalization;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.Future;
using Futures = QuantConnect.Securities.Futures;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the new symbol, on a security changed event,
    /// is added to the securities collection and is tradable.
    /// This specific algorithm tests the manual rollover with the symbol changed event
    /// that is received in the slice in <see cref="OnData(Slice)"/>.
    /// </summary>
    public class ManualContinuousFuturesPositionRolloverRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;

        public override void Initialize()
        {
            SetStartDate(2013, 7, 1);
            SetEndDate(2014, 1, 1);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.LastTradingDay,
                contractDepthOffset: 0
            );
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                Order(_continuousContract.Mapped, 1);
            }
            else
            {
                ManualPositionsRollover(slice.SymbolChangedEvents);
            }
        }

        protected void ManualPositionsRollover(SymbolChangedEvents symbolChangedEvents)
        {
            foreach (var changedEvent in symbolChangedEvents.Values)
            {
                Debug($"{Time} - SymbolChanged event: {changedEvent}");

                // This access will throw if any of the symbols are not in the securities collection
                var oldSecurity = Securities[changedEvent.OldSymbol];
                var newSecurity = Securities[changedEvent.NewSymbol];

                if (!oldSecurity.Invested) continue;

                // Rolling over: liquidate any position of the old mapped contract and switch to the newly mapped contract
                var quantity = oldSecurity.Holdings.Quantity;
                var tag = $"Rollover - Symbol changed at {Time.ToString(CultureInfo.GetCultureInfo("en-US"))}: {changedEvent.OldSymbol} -> {changedEvent.NewSymbol}";
                Liquidate(symbol: oldSecurity.Symbol, tag: tag);
                Order(newSecurity.Symbol, quantity, tag: tag);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{orderEvent}");
        }

        public override void OnEndOfAlgorithm()
        {
            if (Transactions.OrdersCount < 3)
            {
                throw new RegressionTestException("Expected at least 3 orders.");
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
        public long DataPoints => 162575;

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
            {"Total Orders", "3"},
            {"Average Win", "7.01%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "15.617%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "107578.9"},
            {"Net Profit", "7.579%"},
            {"Sharpe Ratio", "1.706"},
            {"Sortino Ratio", "0.919"},
            {"Probabilistic Sharpe Ratio", "88.924%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.08"},
            {"Beta", "0.094"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-1.246"},
            {"Tracking Error", "0.094"},
            {"Treynor Ratio", "1.06"},
            {"Total Fees", "$6.45"},
            {"Estimated Strategy Capacity", "$2900000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "1.37%"},
            {"OrderListHash", "7591ea8b91c4aa958b305555fea96862"}
        };
    }
}
