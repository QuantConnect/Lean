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
    /// Regression test algorithm for scheduled universe selection and warmup GH 3890
    /// </summary>
    public class FundamentalCustomSelectionTimeWarmupRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly TimeSpan _warmupSpan = TimeSpan.FromDays(3);
        private int _specificDateSelection;
        private int _monthStartSelection;
        private readonly Symbol _symbol = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 03, 27);
            SetEndDate(2014, 05, 10);
            UniverseSettings.Resolution = Resolution.Daily;

            AddUniverse(DateRules.MonthStart(), SelectionFunction_MonthStart);

            UniverseSettings.Schedule.On(DateRules.On(
                new DateTime(2013, 05, 9), // really old date will be ignored
                new DateTime(2014, 03, 24), // data for this date will be used to trigger the initial selection
                new DateTime(2014, 03, 26), // date during warmup
                new DateTime(2014, 05, 9), // after warmup
                new DateTime(2020, 05, 9))); // after backtest ends -> wont be executed
            AddUniverse(FundamentalUniverse.USA(SelectionFunction_SpecificDate));

            SetWarmUp(_warmupSpan);
        }

        public IEnumerable<Symbol> SelectionFunction_SpecificDate(IEnumerable<Fundamental> coarse)
        {
            if (_specificDateSelection++ == 0)
            {
                if (Time != StartDate.Add(-_warmupSpan))
                {
                    throw new RegressionTestException($"Month Start unexpected initial selection: {Time}");
                }
            }
            else if (Time != new DateTime(2014, 3, 26)
                && Time != new DateTime(2014, 5, 9))
            {
                throw new RegressionTestException($"SelectionFunction_SpecificDate unexpected selection: {Time}");
            }
            return new[] { _symbol };
        }

        public IEnumerable<Symbol> SelectionFunction_MonthStart(IEnumerable<Fundamental> coarse)
        {
            if (_monthStartSelection++ == 0)
            {
                if (Time != StartDate.Add(-_warmupSpan))
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

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && !IsWarmingUp)
            {
                SetHoldings(_symbol, 1);
                Debug($"Purchased Stock {_symbol}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_monthStartSelection != 3)
            {
                throw new RegressionTestException($"Month start unexpected selection count: {_monthStartSelection}");
            }
            if (_specificDateSelection != 3)
            {
                throw new RegressionTestException($"Specific date unexpected selection count: {_specificDateSelection}");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 14470;

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
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "13.629%"},
            {"Drawdown", "3.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101540.32"},
            {"Net Profit", "1.540%"},
            {"Sharpe Ratio", "0.947"},
            {"Sortino Ratio", "0.896"},
            {"Probabilistic Sharpe Ratio", "49.649%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.019"},
            {"Beta", "0.99"},
            {"Annual Standard Deviation", "0.096"},
            {"Annual Variance", "0.009"},
            {"Information Ratio", "-2.694"},
            {"Tracking Error", "0.007"},
            {"Treynor Ratio", "0.092"},
            {"Total Fees", "$3.09"},
            {"Estimated Strategy Capacity", "$800000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.27%"},
            {"OrderListHash", "8c0997bfe6577a63b266bcf91bce1882"}
        };
    }
}
