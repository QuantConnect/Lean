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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the indicators and consolidators created through the helper methods are
    /// deregistered when their security is deselected by the universe or removed manually, and that the ones created
    /// after the security is selected or added again receive data while the deregistered ones don't.
    /// See <see cref="IAlgorithmSettings.AutomaticIndicatorDeregistration"/>
    /// </summary>
    public class AutomaticIndicatorDeregistrationOnUniverseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly DateTime _removalDate = new DateTime(2014, 03, 27);
        private readonly DateTime _reAdditionDate = new DateTime(2014, 04, 01);

        private Symbol _spy;
        private Symbol _aig;
        private SymbolData _spyData;
        private SymbolData _aigData;
        private bool _spyRemoved;
        private bool _spyReAdded;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 04, 07);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;
            _aig = QuantConnect.Symbol.Create("AIG", SecurityType.Equity, Market.USA);
            AddUniverse("MyUniverse", Resolution.Daily, SelectionFunction);

            _spy = AddEquity("SPY", Resolution.Daily).Symbol;
            _spyData = new SymbolData(this, _spy);
        }

        private IEnumerable<string> SelectionFunction(DateTime dateTime)
        {
            // AIG leaves the universe and comes back on the same dates SPY is removed and re-added manually
            var selectAig = dateTime < _removalDate || dateTime >= _reAdditionDate;
            return selectAig ? new[] { "IBM", "AIG" } : new[] { "IBM" };
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!_spyRemoved && Time.Date >= _removalDate)
            {
                _spyRemoved = true;
                RemoveSecurity(_spy);
            }
            else if (_spyRemoved && !_spyReAdded && Time.Date >= _reAdditionDate)
            {
                _spyReAdded = true;
                AddEquity("SPY", Resolution.Daily);
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.AddedSecurities)
            {
                if (security.Symbol == _aig)
                {
                    if (_aigData == null)
                    {
                        _aigData = new SymbolData(this, _aig);
                    }
                    else
                    {
                        _aigData.OnReAdded();
                    }
                }
                else if (security.Symbol == _spy && _spyReAdded)
                {
                    _spyData.OnReAdded();
                }
            }

            foreach (var security in changes.RemovedSecurities)
            {
                if (security.Symbol == _aig)
                {
                    _aigData.OnRemoved();
                }
                else if (security.Symbol == _spy)
                {
                    _spyData.OnRemoved();
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_spyRemoved || !_spyReAdded)
            {
                throw new RegressionTestException("SPY should have been removed and re-added");
            }

            _spyData.AssertDeregisteredOnRemoval();
            _aigData.AssertDeregisteredOnRemoval();
        }

        private class SymbolData
        {
            private readonly QCAlgorithm _algorithm;
            private readonly Symbol _symbol;
            private readonly SimpleMovingAverage _sma;
            private SimpleMovingAverage _newSma;
            private int _consolidatedCount;
            private int _newConsolidatedCount;
            private long _smaSamplesAtRemoval;
            private int _consolidatedCountAtRemoval;
            private int _removals;
            private int _reAdditions;

            public SymbolData(QCAlgorithm algorithm, Symbol symbol)
            {
                _algorithm = algorithm;
                _symbol = symbol;
                _sma = algorithm.SMA(symbol, 2, Resolution.Daily);
                algorithm.Consolidate(symbol, Resolution.Daily, (TradeBar bar) => _consolidatedCount++);
            }

            public void OnRemoved()
            {
                _removals++;
                _smaSamplesAtRemoval = _sma.Samples;
                _consolidatedCountAtRemoval = _consolidatedCount;

                if (_sma.Consolidators.Count != 0)
                {
                    throw new RegressionTestException($"{_symbol}: the indicator should have been deregistered when the security was removed");
                }
            }

            public void OnReAdded()
            {
                _reAdditions++;
                _newSma = _algorithm.SMA(_symbol, 2, Resolution.Daily);
                _algorithm.Consolidate(_symbol, Resolution.Daily, (TradeBar bar) => _newConsolidatedCount++);
            }

            public void AssertDeregisteredOnRemoval()
            {
                if (_removals == 0 || _reAdditions == 0)
                {
                    throw new RegressionTestException($"{_symbol}: expected the security to be removed and re-added. Removals: {_removals}, re-additions: {_reAdditions}");
                }
                if (_smaSamplesAtRemoval == 0 || _consolidatedCountAtRemoval == 0)
                {
                    throw new RegressionTestException($"{_symbol}: the indicator and consolidator should have been updated before the security was removed");
                }
                if (_sma.Samples != _smaSamplesAtRemoval || _consolidatedCount != _consolidatedCountAtRemoval)
                {
                    throw new RegressionTestException($"{_symbol}: the deregistered indicator and consolidator should not get updates after the security is removed");
                }
                if (_newSma.Samples == 0 || _newConsolidatedCount == 0)
                {
                    throw new RegressionTestException($"{_symbol}: the indicator and consolidator created after the security is re-added should get updates");
                }
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
        public long DataPoints => 125;

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
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Information Ratio", "0.97"},
            {"Tracking Error", "0.097"},
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
