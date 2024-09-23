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
                throw new RegressionTestException($"SelectionFunction_SpecificDate unexpected selection: {Time}");
            }
            return new[] { _symbol };
        }

        public IEnumerable<Symbol> SelectionFunction_MonthStart(IEnumerable<Fundamental> coarse)
        {
            if (_monthStartSelection++ == 0)
            {
                if (Time != StartDate)
                {
                    throw new RegressionTestException($"Month Start unexpected initial selection: {Time}");
                }
            }
            else if (Time != new DateTime(2014, 4, 1)
                && Time != new DateTime(2014, 5, 1))
            {
                throw new RegressionTestException($"Month Start unexpected selection: {Time}");
            }
            return new[] { _symbol };
        }

        public IEnumerable<Symbol> SelectionFunction_MonthEnd(IEnumerable<CoarseFundamental> coarse)
        {
            if (_monthEndSelection++ == 0)
            {
                if (Time != StartDate)
                {
                    throw new RegressionTestException($"Month End unexpected initial selection: {Time}");
                }
            }
            else if (Time != new DateTime(2014, 3, 31)
                && Time != new DateTime(2014, 4, 30))
            {
                throw new RegressionTestException($"Month End unexpected selection: {Time}");
            }
            return new[] { _symbol };
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
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
                throw new RegressionTestException($"Month End unexpected selection count: {_monthEndSelection}");
            }
            if (_monthStartSelection != 3)
            {
                throw new RegressionTestException($"Month start unexpected selection count: {_monthStartSelection}");
            }
            if (_specificDateSelection != 1)
            {
                throw new RegressionTestException($"Specific date unexpected selection count: {_specificDateSelection}");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 14466;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "4.334%"},
            {"Drawdown", "3.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100532.22"},
            {"Net Profit", "0.532%"},
            {"Sharpe Ratio", "0.28"},
            {"Sortino Ratio", "0.283"},
            {"Probabilistic Sharpe Ratio", "39.422%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.022"},
            {"Beta", "1.018"},
            {"Annual Standard Deviation", "0.099"},
            {"Annual Variance", "0.01"},
            {"Information Ratio", "-2.462"},
            {"Tracking Error", "0.009"},
            {"Treynor Ratio", "0.027"},
            {"Total Fees", "$3.07"},
            {"Estimated Strategy Capacity", "$920000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.20%"},
            {"OrderListHash", "87438e51988f37757a2d7f97389483ea"}
        };
    }
}
