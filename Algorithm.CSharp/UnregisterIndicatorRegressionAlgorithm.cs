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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example and regression algorithm asserting the behavior of registering and unregistering an indicator from the engine
    /// </summary>
    public class UnregisterIndicatorRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol[] _symbols;
        private IndicatorBase _trin;
        private IndicatorBase _trin2;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            var spy = AddEquity("SPY");
            var ibm = AddEquity("IBM");

            _symbols = new[] { spy.Symbol, ibm.Symbol };
            _trin = TRIN(_symbols, Resolution.Minute);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if(_trin.IsReady)
            {
                _trin.Reset();
                UnregisterIndicator(_trin);

                // let's create a new one with a differente resolution
                _trin2 = TRIN(_symbols, Resolution.Hour);
            }

            if (_trin2 != null && _trin2.IsReady)
            {
                if (_trin.IsReady)
                {
                    throw new Exception("Indicator should of stop getting updates!");
                }

                if(!Portfolio.Invested)
                {
                    SetHoldings(_symbols[0], 0.5m);
                    SetHoldings(_symbols[1], 0.5m);
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 7843;

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
            {"Compounding Annual Return", "232.884%"},
            {"Drawdown", "2.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101549.48"},
            {"Net Profit", "1.549%"},
            {"Sharpe Ratio", "10.888"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "66.376%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.565"},
            {"Beta", "0.992"},
            {"Annual Standard Deviation", "0.232"},
            {"Annual Variance", "0.054"},
            {"Information Ratio", "7.761"},
            {"Tracking Error", "0.071"},
            {"Treynor Ratio", "2.544"},
            {"Total Fees", "$3.54"},
            {"Estimated Strategy Capacity", "$1200000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "19.99%"},
            {"OrderListHash", "b24d340c2ca279f0b220bad94e946516"}
        };
    }
}
