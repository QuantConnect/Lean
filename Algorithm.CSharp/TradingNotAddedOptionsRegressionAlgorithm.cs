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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// </summary>
    public class TradingNotAddedOptionsRegressionAlgorithm : TradingNotAddedEquitiesRegressionAlgorithm
    {
        private Symbol _optionSymbol;
        private Symbol _deselectedContractSymbol;
        private Symbol _notSelectedContractSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 28);
            SetCash(1000000);

            var option = AddOption("GOOG");
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            // SetFilter method accepts TimeSpan objects or integer for days.
            // The following statements yield the same filtering criteria
            option.SetFilter(u => u.StandardsOnly()
                .Strikes(-2, +2)
                .Expiration(7, 180)
                .Contracts(contracts =>
                {
                    if (_deselectedContractSymbol == null)
                    {
                        var contractsList = contracts.ToList();
                        _deselectedContractSymbol = contractsList.First(x => x.ID.OptionRight == OptionRight.Call && x.ID.StrikePrice == 750m && x.ID.Date.Date == new DateTime(2016, 06, 17));
                        // This contract will never be selected so it's never added to the Securities collection
                        _notSelectedContractSymbol = contractsList.OrderByDescending(x => x.ID.Date).First();

                        return contractsList.Where(x => x != _notSelectedContractSymbol);
                    }

                    // Filter out the contract we selected last time, we don't want it to be selected so it's marked as not tradable
                    return contracts.Where(x => x != _deselectedContractSymbol && x != _notSelectedContractSymbol);
                }));

            // use the underlying equity as the benchmark
            SetBenchmark("GOOG");
        }

        private void AssertTradeContract(Symbol symbol)
        {
            var ticket = Sell(symbol, 1);
            if (ticket.Status == OrderStatus.Invalid)
            {
                throw new RegressionTestException($"Deselected contract {symbol} was not traded when it should have been");
            }

            AssertSecurityIsAdded(symbol);

            // Now let's remove it and try to trade it again, but in a strategy
            RemoveSecurity(symbol);
            AssertSecurityIsNotAdded(symbol);

            var strategy = OptionStrategies.Straddle(_optionSymbol, symbol.ID.StrikePrice, symbol.ID.Date);
            if (strategy.OptionLegs.Count != 2 ||
                strategy.OptionLegs.Any(leg => leg.Symbol == null) ||
                !strategy.OptionLegs.Any(leg => leg.Symbol == symbol) ||
                !strategy.OptionLegs.Any(leg => leg.Symbol == symbol.GetMirrorOptionSymbol()))
            {
                throw new RegressionTestException("Option leg symbols were not set");
            }

            var tickets = Sell(strategy, 1);
            if (tickets.Count == 0 || tickets.Any(x => x.Status == OrderStatus.Invalid))
            {
                throw new RegressionTestException($"Deselected contract {symbol} was not traded as part of the strategy when it should have been");
            }
            AssertSecurityIsAdded(symbol);
        }

        public override void OnData(Slice slice)
        {
            if (Time.Day == 24)
            {
                if (_deselectedContractSymbol == null || _notSelectedContractSymbol == null)
                {
                    throw new RegressionTestException("Trading contracts were not set");
                }

                if (!Securities.TryGetValue(_deselectedContractSymbol, out var deselectedContract))
                {
                    throw new RegressionTestException($"Deselected contract {_deselectedContractSymbol} is tradable");
                }
            }
            else if (Time.Day == 28 && !Portfolio.Invested)
            {
                if (slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
                {
                    if (_deselectedContractSymbol == null || _notSelectedContractSymbol == null)
                    {
                        throw new RegressionTestException("Trading contracts were not set");
                    }

                    AssertSecurityIsNotAdded(_deselectedContractSymbol);
                    AssertSecurityIsNotAdded(_notSelectedContractSymbol);

                    // Now we have _deselectedContractSymbol which was selected in a previous date but deselected for today.
                    // Let's trade it
                    AssertTradeContract(_deselectedContractSymbol);

                    // Let's do the same with _notSelectedContractSymbol which was never selected
                    AssertTradeContract(_notSelectedContractSymbol);

                    // We are done testing
                    Quit();
                }
            }

            foreach (var kpv in slice.Bars)
            {
                Log($"---> OnData: {Time}, {kpv.Key.Value}, {kpv.Value.Close:0.00}");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 14642;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public override AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "1000000"},
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
