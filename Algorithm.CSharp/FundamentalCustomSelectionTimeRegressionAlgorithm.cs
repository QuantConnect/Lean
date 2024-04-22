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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test algorithm for scheduled universe selection GH 3890
    /// </summary>
    public class FundamentalCustomSelectionTimeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _specificDateSelection;
        private int _monthStartSelection;
        private int _monthEndSelection;
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
            AddUniverse(DateRules.MonthStart(), SelectionFunction_MonthStart);

            // Test use case B
            var otherSettings = new UniverseSettings(UniverseSettings);
            otherSettings.Schedule.On(DateRules.MonthEnd());
            AddUniverse(FundamentalUniverse.USA(SelectionFunction_MonthEnd, otherSettings));

            // Test use case C
            UniverseSettings.Schedule.On(DateRules.On(new DateTime(2014, 05, 9)));
            AddUniverse(FundamentalUniverse.USA(SelectionFunction_SpecificDate));
        }

        public IEnumerable<Symbol> SelectionFunction_SpecificDate(IEnumerable<Fundamental> coarse)
        {
            _specificDateSelection++;
            if (Time != new DateTime(2014, 05, 9))
            {
                throw new Exception($"SelectionFunction_SpecificDate unexpected selection: {Time}");
            }
            return new[] { _symbol };
        }

        public IEnumerable<Symbol> SelectionFunction_MonthStart(IEnumerable<Fundamental> coarse)
        {
            if (_monthStartSelection++ == 0)
            {
                if (Time != StartDate)
                {
                    throw new Exception($"Month Start unexpected initial selection: {Time}");
                }
            }
            else if (Time != new DateTime(2014, 4, 1)
                && Time != new DateTime(2014, 5, 1))
            {
                throw new Exception($"Month Start unexpected selection: {Time}");
            }
            return new[] { _symbol };
        }

        public IEnumerable<Symbol> SelectionFunction_MonthEnd(IEnumerable<CoarseFundamental> coarse)
        {
            if (_monthEndSelection++ == 0)
            {
                if (Time != StartDate)
                {
                    throw new Exception($"Month End unexpected initial selection: {Time}");
                }
            }
            else if (Time != new DateTime(2014, 3, 31)
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
            if (_monthEndSelection != 3)
            {
                throw new Exception($"Month End unexpected selection count: {_monthEndSelection}");
            }
            if (_monthStartSelection != 3)
            {
                throw new Exception($"Month start unexpected selection count: {_monthStartSelection}");
            }
            if (_specificDateSelection != 1)
            {
                throw new Exception($"Specific date unexpected selection count: {_specificDateSelection}");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 14467;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "7.010%"},
            {"Drawdown", "3.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100857.57"},
            {"Net Profit", "0.858%"},
            {"Sharpe Ratio", "0.51"},
            {"Sortino Ratio", "0.516"},
            {"Probabilistic Sharpe Ratio", "43.033%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "1.003"},
            {"Annual Standard Deviation", "0.097"},
            {"Annual Variance", "0.009"},
            {"Information Ratio", "0.645"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0.049"},
            {"Total Fees", "$3.08"},
            {"Estimated Strategy Capacity", "$710000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.18%"},
            {"OrderListHash", "472e90ba189aaf55e0edab9087c3d8e7"}
        };
    }
}
