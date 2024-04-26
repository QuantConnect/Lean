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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests the delisting of the composite Symbol (ETF symbol) and the removal of
    /// the universe and the symbol from the algorithm.
    /// </summary>
    public class ETFConstituentUniverseCompositeDelistingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected virtual bool AddETFSubscription { get; set; } = true;

        private Symbol _gdvd;
        private Symbol _aapl;
        private DateTime _delistingDate;
        private int _universeSymbolCount;
        private bool _universeSelectionDone;
        private bool _universeAdded;
        private bool _universeRemoved;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2020, 12, 1);
            SetEndDate(2021, 1, 31);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Hour;

            _delistingDate = new DateTime(2021, 1, 21);

            _aapl = AddEquity("AAPL", Resolution.Hour).Symbol;
            if (AddETFSubscription)
            {
                Log("Adding ETF constituent universe Symbol by using AddEquity(...)");
                _gdvd = AddEquity("GDVD", Resolution.Hour).Symbol;
            }
            else
            {
                Log("Adding ETF constituent universe Symbol by using Symbol.Create(...)");
                _gdvd = QuantConnect.Symbol.Create("GDVD", SecurityType.Equity, Market.USA);
            }

            AddUniverse(Universe.ETF(_gdvd, universeFilterFunc: FilterETFs));
        }

        private IEnumerable<Symbol> FilterETFs(IEnumerable<ETFConstituentUniverse> constituents)
        {
            _universeSelectionDone = true;

            if (UtcTime.Date > _delistingDate)
            {
                throw new Exception($"Performing constituent universe selection on {UtcTime:yyyy-MM-dd HH:mm:ss.fff} after composite ETF has been delisted");
            }

            var constituentSymbols = constituents.Select(x => x.Symbol);
            _universeSymbolCount = constituentSymbols.Distinct().Count();

            return constituentSymbols;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (UtcTime.Date > _delistingDate && data.Keys.Any(x => x != _aapl))
            {
                throw new Exception($"Received unexpected slice in OnData(...) after universe was deselected");
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_aapl, 0.5m);
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.AddedSecurities.Count != 0 && UtcTime > _delistingDate)
            {
                throw new Exception("New securities added after ETF constituents were delisted");
            }

            // if we added the etf subscription it will get added and delisted and send us a addition/removal event
            var adjusment = AddETFSubscription ? 1 : 0;
            var expectedChangesCount = _universeSymbolCount + adjusment;

            if (_universeSelectionDone)
            {
                // "_universeSymbolCount + 1" because selection is done right away,
                // so AddedSecurities includes all ETF constituents (including APPL) plus GDVD
                _universeAdded |= changes.AddedSecurities.Count == expectedChangesCount;
            }

            // TODO: shouldn't be sending AAPL as a removed security since it was added by another universe
            _universeRemoved |= changes.RemovedSecurities.Count == expectedChangesCount &&
                UtcTime.Date >= _delistingDate &&
                UtcTime.Date < EndDate;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_universeAdded)
            {
                throw new Exception("ETF constituent universe was never added to the algorithm");
            }
            if (!_universeRemoved)
            {
                throw new Exception("ETF constituent universe was not removed from the algorithm after delisting");
            }
            if (ActiveSecurities.Count > 2)
            {
                throw new Exception($"Expected less than 2 securities after algorithm ended, found {Securities.Count}");
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
        public virtual long DataPoints => 692;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "30.084%"},
            {"Drawdown", "5.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "104393.19"},
            {"Net Profit", "4.393%"},
            {"Sharpe Ratio", "1.543"},
            {"Sortino Ratio", "2.111"},
            {"Probabilistic Sharpe Ratio", "58.028%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.166"},
            {"Beta", "0.717"},
            {"Annual Standard Deviation", "0.136"},
            {"Annual Variance", "0.019"},
            {"Information Ratio", "1.254"},
            {"Tracking Error", "0.118"},
            {"Treynor Ratio", "0.293"},
            {"Total Fees", "$2.06"},
            {"Estimated Strategy Capacity", "$160000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.83%"},
            {"OrderListHash", "d38318f2dd0a38f11ef4e4fd704706a7"}
        };
    }
}
