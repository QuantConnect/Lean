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
        private int _onEndOfDayCallCount;

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
        /// Obsolete overload to be removed.
        /// </summary>
        public override void OnEndOfDay()
        {
            _onEndOfDayCallCount++;
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
            if (_onEndOfDayCallCount != 5)
            {
                throw new Exception($"OnEndOfDay() unexpected count call {_onEndOfDayCallCount}");
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
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "419.532%"},
            {"Drawdown", "1.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "2.283%"},
            {"Sharpe Ratio", "5.506"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.949"},
            {"Beta", "0.038"},
            {"Annual Standard Deviation", "0.175"},
            {"Annual Variance", "0.031"},
            {"Information Ratio", "2.406"},
            {"Tracking Error", "0.252"},
            {"Treynor Ratio", "25.376"},
            {"Total Fees", "$20.75"}
        };
    }
}
