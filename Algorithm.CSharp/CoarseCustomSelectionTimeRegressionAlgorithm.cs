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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test algorithm for scheduled universe selection GH 3890
    /// </summary>
    public class CoarseCustomSelectionTimeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _monthStartSelection;
        private int _monthEndSelection;
        private int _monthStartFineSelection;
        private readonly Symbol _symbol = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 03, 25);
            SetEndDate(2014, 05, 10);
            UniverseSettings.Resolution = Resolution.Daily;

            // Test use case A
            AddUniverse(CoarseSelectionFunction_MonthStart, DateRules.MonthStart());

            // Test use case B
            var otherSettings = UniverseSettings.Clone();
            otherSettings.Schedule.On(DateRules.MonthEnd());
            AddUniverse(new CoarseFundamentalUniverse(otherSettings, SecurityInitializer, CoarseSelectionFunction_MonthEnd));

            // Test use case C
            AddUniverse(CoarseSelectionFunction_MonthStart, FineSelectionFunction_MonthStart, DateRules.MonthStart());
        }

        public IEnumerable<Symbol> CoarseSelectionFunction_MonthStart(IEnumerable<CoarseFundamental> coarse)
        {
            _monthStartSelection++;
            if (Time != new DateTime(2014, 4, 1)
                && Time != new DateTime(2014, 5, 1))
            {
                throw new Exception($"Month Start unexpected selection: {Time}");
            }
            return new[] { _symbol };
        }

        public IEnumerable<Symbol> FineSelectionFunction_MonthStart(IEnumerable<FineFundamental> fine)
        {
            _monthStartFineSelection++;
            if (Time != new DateTime(2014, 4, 1)
                && Time != new DateTime(2014, 5, 1))
            {
                throw new Exception($"Month Start unexpected selection: {Time}");
            }
            return new[] { _symbol };
        }

        public IEnumerable<Symbol> CoarseSelectionFunction_MonthEnd(IEnumerable<CoarseFundamental> coarse)
        {
            _monthEndSelection++;
            if (Time != new DateTime(2014, 3, 31)
                && Time != new DateTime(2014, 4, 30))
            {
                throw new Exception($"Month End unexpected selection: {Time}");
            }
            return new[] { _symbol };
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_symbol, 1);
                Debug($"Purchased Stock {_symbol}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_monthEndSelection != 2)
            {
                throw new Exception($"Month End unexpected selection count: {_monthEndSelection}");
            }
            if (_monthStartSelection != 4)
            {
                throw new Exception($"Month start unexpected selection count: {_monthStartSelection}");
            }
            if (_monthStartFineSelection != 2)
            {
                throw new Exception($"Month start fine unexpected selection count: {_monthStartFineSelection}");
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
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "1.680%"},
            {"Drawdown", "3.900%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.210%"},
            {"Sharpe Ratio", "0.197"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.074"},
            {"Beta", "0.884"},
            {"Annual Standard Deviation", "0.103"},
            {"Annual Variance", "0.011"},
            {"Information Ratio", "-2.359"},
            {"Tracking Error", "0.037"},
            {"Treynor Ratio", "0.023"},
            {"Total Fees", "$2.89"}
        };
    }
}