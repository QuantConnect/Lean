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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm tests and demonstrates EUREX futures subscription and trading:
    /// - It tests contracts rollover by adding a continuous future and asserting that mapping happens at some point.
    /// - It tests basic trading by buying a contract and holding it until expiration.
    /// - It tests delisting and asserts the holdings are liquidated after that.
    /// </summary>
    public class BasicTemplateEurexFuturesAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Symbol _mappedSymbol;
        private Symbol _contractToTrade;
        private int _mappingsCount;
        private decimal _boughtQuantity;
        private decimal _liquidatedQuantity;
        private bool _delisted;

        public override void Initialize()
        {
            SetStartDate(2024, 5, 30);
            SetEndDate(2024, 6, 23);

            SetAccountCurrency(Currencies.EUR);
            SetCash(1000000);

            _continuousContract = AddFuture(Futures.Indices.EuroStoxx50, Resolution.Minute,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.FirstDayMonth,
                contractDepthOffset: 0);
            _continuousContract.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(180));
            _mappedSymbol = _continuousContract.Mapped;

            var benchmark = AddIndex("SX5E");
            SetBenchmark(benchmark.Symbol);

            var seeder = new FuncSecuritySeeder(GetLastKnownPrices);
            SetSecurityInitializer(security => seeder.SeedSecurity(security));
        }

        public override void OnData(Slice slice)
        {
            foreach (var changedEvent in slice.SymbolChangedEvents.Values)
            {
                if (++_mappingsCount > 1)
                {
                    throw new RegressionTestException($"{Time} - Unexpected number of symbol changed events (mappings): {_mappingsCount}. " +
                        $"Expected only 1.");
                }

                Debug($"{Time} - SymbolChanged event: {changedEvent}");

                if (changedEvent.OldSymbol != _mappedSymbol.ID.ToString())
                {
                    throw new RegressionTestException($"{Time} - Unexpected symbol changed event old symbol: {changedEvent}");
                }

                if (changedEvent.NewSymbol != _continuousContract.Mapped.ID.ToString())
                {
                    throw new RegressionTestException($"{Time} - Unexpected symbol changed event new symbol: {changedEvent}");
                }

                // Let's trade the previous mapped contract, so we can hold it until expiration for testing
                // (will be sooner than the new mapped contract)
                _contractToTrade = _mappedSymbol;
                _mappedSymbol = _continuousContract.Mapped;
            }

            // Let's trade after the mapping is done
            if (_contractToTrade != null && _boughtQuantity == 0 && Securities[_contractToTrade].Exchange.ExchangeOpen)
            {
                Buy(_contractToTrade, 1);
            }

            if (_contractToTrade != null && slice.Delistings.TryGetValue(_contractToTrade, out var delisting))
            {
                if (delisting.Type == DelistingType.Delisted)
                {
                    _delisted = true;

                    if (Portfolio.Invested)
                    {
                        throw new RegressionTestException($"{Time} - Portfolio should not be invested after the traded contract is delisted.");
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Symbol != _contractToTrade)
            {
                throw new RegressionTestException($"{Time} - Unexpected order event symbol: {orderEvent.Symbol}. Expected {_contractToTrade}");
            }

            if (orderEvent.Direction == OrderDirection.Buy)
            {
                if (orderEvent.Status == OrderStatus.Filled)
                {
                    if (_boughtQuantity != 0 && _liquidatedQuantity != 0)
                    {
                        throw new RegressionTestException($"{Time} - Unexpected buy order event status: {orderEvent.Status}");
                    }
                    _boughtQuantity = orderEvent.Quantity;
                }
            }
            else if (orderEvent.Direction == OrderDirection.Sell)
            {
                if (orderEvent.Status == OrderStatus.Filled)
                {
                    if (_boughtQuantity <= 0 && _liquidatedQuantity != 0)
                    {
                        throw new RegressionTestException($"{Time} - Unexpected sell order event status: {orderEvent.Status}");
                    }
                    _liquidatedQuantity = orderEvent.Quantity;

                    if (_liquidatedQuantity != -_boughtQuantity)
                    {
                        throw new RegressionTestException($"{Time} - Unexpected liquidated quantity: {_liquidatedQuantity}. Expected: {-_boughtQuantity}");
                    }
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var addedSecurity in changes.AddedSecurities)
            {
                if (addedSecurity.Symbol.SecurityType == SecurityType.Future && addedSecurity.Symbol.IsCanonical())
                {
                    _mappedSymbol = _continuousContract.Mapped;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_mappingsCount == 0)
            {
                throw new RegressionTestException($"Unexpected number of symbol changed events (mappings): {_mappingsCount}. Expected 1.");
            }

            if (!_delisted)
            {
                throw new RegressionTestException("Contract was not delisted");
            }

            // Make sure we traded and that the position was liquidated on delisting
            if (_boughtQuantity <= 0 || _liquidatedQuantity >= 0)
            {
                throw new RegressionTestException($"Unexpected sold quantity: {_boughtQuantity} and liquidated quantity: {_liquidatedQuantity}");
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
        public long DataPoints => 94326;

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
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.11%"},
            {"Compounding Annual Return", "-1.667%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "998849.48"},
            {"Net Profit", "-0.115%"},
            {"Sharpe Ratio", "-34.455"},
            {"Sortino Ratio", "-57.336"},
            {"Probabilistic Sharpe Ratio", "0.002%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-6.176"},
            {"Tracking Error", "0.002"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "€1.02"},
            {"Estimated Strategy Capacity", "€2300000000.00"},
            {"Lowest Capacity Asset", "FESX YJHOAMPYKRS5"},
            {"Portfolio Turnover", "0.40%"},
            {"OrderListHash", "ac9acc478ba1afe53993cdbb92f8ec6e"}
        };
    }
}
