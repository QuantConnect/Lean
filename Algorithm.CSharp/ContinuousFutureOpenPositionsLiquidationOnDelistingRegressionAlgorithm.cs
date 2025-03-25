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
    /// Regression algorithm reproducing GH issue #8386 and other related bugs.
    /// It asserts that open positions are liquidated when a contract is delisted, even if the contract was added as an internal subscription.
    /// It also asserts that the contract is not tradable after being delisted.
    /// </summary>
    public class ContinuousFutureOpenPositionsLiquidationOnDelistingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Symbol _prevContractSymbol;
        private bool _traded;
        private bool _mapped;
        private bool _delistedContractChecked;
        private DateTime _firstMappedContractRemovalTime;
        private int _removalCount;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 12, 30);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.OpenInterest,
                contractDepthOffset: 0
            );
        }

        public override void OnData(Slice slice)
        {
            if (!_traded && _continuousContract.HasData)
            {
                var ticket = MarketOrder(_continuousContract.Mapped, 1);
                if (ticket.Status == OrderStatus.Invalid)
                {
                    throw new RegressionTestException($"Order should be valid: {ticket}");
                }
                _traded = true;
            }

            if (slice.SymbolChangedEvents.Count > 0)
            {
                foreach (var change in slice.SymbolChangedEvents.Values)
                {
                    Debug($"[{Time}] :: Mapping: {change}");
                    _prevContractSymbol = Symbol(change.OldSymbol);
                    _mapped = true;
                }
            }

            if (!_delistedContractChecked &&
                _prevContractSymbol  != null &&
                Time.Date > _prevContractSymbol.ID.Date &&
                IsMarketOpen(_prevContractSymbol))
            {
                _delistedContractChecked = true;
                var delistedContract = Securities.Total.Single(sec => sec.Symbol == _prevContractSymbol);

                if (delistedContract.Invested)
                {
                    throw new RegressionTestException($"Position should be closed when {_prevContractSymbol} got delisted {_prevContractSymbol.ID.Date}");
                }

                if (!delistedContract.IsDelisted)
                {
                    throw new RegressionTestException($"Contract should be delisted: {delistedContract.Symbol}");
                }

                if (delistedContract.IsTradable)
                {
                    throw new RegressionTestException($"Contract should not be tradable: {delistedContract.Symbol}");
                }

                var ticket = MarketOrder(_prevContractSymbol, 1);

                if (ticket.Status != OrderStatus.Invalid)
                {
                    throw new RegressionTestException($"Delisted contract order should be invalid: {ticket}");
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (_prevContractSymbol != null)
            {
                if (changes.RemovedSecurities.Any(x => x.Symbol == _prevContractSymbol))
                {
                    throw new RegressionTestException($"Previous contract symbol {_prevContractSymbol} should not be removed as a non-internal security");
                }

                changes.FilterInternalSecurities = false;

                if (!changes.RemovedSecurities.Any(x => x.Symbol == _prevContractSymbol))
                {
                    throw new RegressionTestException($"Previous contract symbol {_prevContractSymbol} should be removed as an internal security");
                }

                _firstMappedContractRemovalTime = Time;
                _removalCount++;
            }

            changes.FilterInternalSecurities = false;
            Debug($"[{Time}] :: {changes}");
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"[{Time}] :: Order event: {orderEvent}");
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_traded)
            {
                throw new RegressionTestException("No trades have been made");
            }

            if (!_mapped)
            {
                throw new RegressionTestException("No mapping events have been fired");
            }

            if (!_delistedContractChecked)
            {
                throw new RegressionTestException("No delisted contract has been checked");
            }

            if (_prevContractSymbol == null)
            {
                throw new RegressionTestException("No previous contract symbol has been set");
            }

            var tradedContract = Securities.Total.Single(sec => sec.Symbol == _prevContractSymbol);
            if (tradedContract.Invested)
            {
                throw new RegressionTestException($"Position should be closed when {_prevContractSymbol} got delisted on {_prevContractSymbol.ID.Date}");
            }

            if (_firstMappedContractRemovalTime == default || _firstMappedContractRemovalTime >= _prevContractSymbol.ID.Date)
            {
                throw new RegressionTestException($"First mapped contract should have been removed before it's expiry date");
            }

            if (_removalCount != 1)
            {
                throw new RegressionTestException($"The mapped contract should have been removed once only");
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
        public virtual long DataPoints => 159274;

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
            {"Alpha", "0.227"},
            {"Beta", "0.109"},
            {"Annual Standard Deviation", "0.084"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "-1.122"},
            {"Tracking Error", "0.112"},
            {"Treynor Ratio", "2.49"},
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$1700000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "2.01%"},
            {"OrderListHash", "838e662caaa5a385c43ef27df1efbaf4"}
        };
    }
}
