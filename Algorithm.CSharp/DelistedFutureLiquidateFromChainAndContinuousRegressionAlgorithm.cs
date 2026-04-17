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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a future contract selected by both the continuous future and
    /// the future chain universes gets liquidated on delisting and that the algorithm receives the correct
    /// security addition/removal notifications.
    ///
    /// This partly reproduces GH issue https://github.com/QuantConnect/Lean/issues/9092
    /// </summary>
    public class DelistedFutureLiquidateFromChainAndContinuousRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contractSymbol;

        private Future _continuousFuture;

        private DateTime _internalContractRemovalTime;
        private DateTime _contractRemovalTime;

        protected virtual string FutureTicker => Futures.Indices.SP500EMini;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 12, 30);

            _continuousFuture = AddFuture(FutureTicker);
            _continuousFuture.SetFilter(0, 182);
        }

        public override void OnData(Slice slice)
        {
            if (_contractSymbol == null)
            {
                foreach (var chain in slice.FutureChains)
                {
                    // Make sure the mapped contract is in the chain, that is, is selected by both universes
                    if (chain.Value.Any(x => x.Symbol == _continuousFuture.Mapped))
                    {
                        _contractSymbol = _continuousFuture.Mapped;
                        var ticket = MarketOrder(_contractSymbol, 1);

                        if (ticket.Status != OrderStatus.Filled)
                        {
                            throw new RegressionTestException($"Order should be filled: {ticket}");
                        }
                    }
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.RemovedSecurities.Any(x => x.Symbol == _contractSymbol))
            {
                if (_contractRemovalTime != default)
                {
                    throw new RegressionTestException($"Contract {_contractSymbol} was removed multiple times");
                }
                _contractRemovalTime = Time;
            }
            else
            {
                changes.FilterInternalSecurities = false;
                if (changes.RemovedSecurities.Any(x => x.Symbol == _contractSymbol))
                {
                    if (_internalContractRemovalTime != default)
                    {
                        throw new RegressionTestException($"Contract {_contractSymbol} was removed multiple times as internal subscription");
                    }
                    _internalContractRemovalTime = Time;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_contractSymbol == null)
            {
                throw new RegressionTestException("No contract was ever traded");
            }

            if (_internalContractRemovalTime == default)
            {
                throw new RegressionTestException($"Contract {_contractSymbol} was not removed from the algorithm");
            }

            if (_contractRemovalTime == default)
            {
                throw new RegressionTestException($"Contract {_contractSymbol} was not removed from the algorithm as external subscription");
            }

            // The internal subscription should be removed first (on continuous future mapping),
            // and the regular subscription later (on delisting)
            if (_contractRemovalTime < _internalContractRemovalTime)
            {
                throw new RegressionTestException($"Contract {_contractSymbol} was removed from the algorithm as aregular subscription before internal subscription");
            }

            if (Securities[_contractSymbol].Invested)
            {
                throw new RegressionTestException($"Position should be closed when {_contractSymbol} got delisted {_contractSymbol.ID.Date}");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"{orderEvent}. Delisting on: {_contractSymbol.ID.Date}");
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
        public virtual long DataPoints => 288140;

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
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "7.02%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "34.386%"},
            {"Drawdown", "1.500%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "107016.6"},
            {"Net Profit", "7.017%"},
            {"Sharpe Ratio", "3.217"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "99.828%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.228"},
            {"Beta", "0.108"},
            {"Annual Standard Deviation", "0.084"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "-1.122"},
            {"Tracking Error", "0.112"},
            {"Treynor Ratio", "2.501"},
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$1700000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "2.01%"},
            {"Drawdown Recovery", "16"},
            {"OrderListHash", "640ce720644ff0b580687e80105d0a92"}
        };
    }
}
