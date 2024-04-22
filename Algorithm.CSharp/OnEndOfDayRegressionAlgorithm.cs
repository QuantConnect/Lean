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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm verifying OnEndOfDay callbacks are called as expected. See GH issue 2865.
    /// </summary>
    public class OnEndOfDayRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spySymbol;
        private Symbol _bacSymbol;
        private Symbol _ibmSymbol;
        private int _onEndOfDaySpyCallCount;
        private int _onEndOfDayBacCallCount;
        private int _onEndOfDayIbmCallCount;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _spySymbol = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            _bacSymbol = QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA);
            _ibmSymbol = QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA);

            AddUniverse("my-universe-name", time =>
            {
                if (time.Day == 8)
                {
                    return new List<string> { _spySymbol.Value, _ibmSymbol.Value };
                }
                return new List<string> { _spySymbol.Value };
            });
        }

        /// <summary>
        /// We expect it to be called for the universe selected <see cref="Symbol"/>
        /// and the post initialize manually added equity <see cref="Symbol"/>
        /// </summary>
        public override void OnEndOfDay(Symbol symbol)
        {
            if (symbol == _spySymbol)
            {
                if (_onEndOfDaySpyCallCount == 0)
                {
                    // just the first time
                    SetHoldings(_spySymbol, 0.5);
                    AddEquity("BAC");
                }
                _onEndOfDaySpyCallCount++;
            }
            else if (symbol == _bacSymbol)
            {
                if (_onEndOfDayBacCallCount == 0)
                {
                    // just the first time
                    SetHoldings(_bacSymbol, 0.5);
                }
                _onEndOfDayBacCallCount++;
            }
            else if (symbol == _ibmSymbol)
            {
                _onEndOfDayIbmCallCount++;
            }
            Log($"OnEndOfDay({symbol}) called: {UtcTime}." +
                $" SPY count: {_onEndOfDaySpyCallCount}." +
                $" IBM count: {_onEndOfDayIbmCallCount}." +
                $" BAC count: {_onEndOfDayBacCallCount}");
        }

        /// <summary>
        /// Assert expected behavior
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_onEndOfDaySpyCallCount != 5)
            {
                throw new Exception($"OnEndOfDay(SPY) unexpected count call {_onEndOfDaySpyCallCount}");
            }
            if (_onEndOfDayBacCallCount != 4)
            {
                throw new Exception($"OnEndOfDay(BAC) unexpected count call {_onEndOfDayBacCallCount}");
            }
            if (_onEndOfDayIbmCallCount != 1)
            {
                throw new Exception($"OnEndOfDay(IBM) unexpected count call {_onEndOfDayIbmCallCount}");
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
        public long DataPoints => 7868;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "489.968%"},
            {"Drawdown", "1.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "102295.22"},
            {"Net Profit", "2.295%"},
            {"Sharpe Ratio", "15.661"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "78.483%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.604"},
            {"Beta", "0.954"},
            {"Annual Standard Deviation", "0.223"},
            {"Annual Variance", "0.05"},
            {"Information Ratio", "22.254"},
            {"Tracking Error", "0.068"},
            {"Treynor Ratio", "3.656"},
            {"Total Fees", "$22.11"},
            {"Estimated Strategy Capacity", "$5600000.00"},
            {"Lowest Capacity Asset", "NB R735QTJ8XC9X"},
            {"Portfolio Turnover", "19.96%"},
            {"OrderListHash", "17eb374f011ccb57a28cef4b9a4585d8"}
        };
    }
}
