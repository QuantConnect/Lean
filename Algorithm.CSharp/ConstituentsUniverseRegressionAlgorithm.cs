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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm using a <see cref="ConstituentsUniverse"/> with test data
    /// </summary>
    public class ConstituentsUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Symbol _appl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
        private readonly Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private readonly Symbol _qqq = QuantConnect.Symbol.Create("QQQ", SecurityType.Equity, Market.USA);
        private readonly Symbol _fb = QuantConnect.Symbol.Create("FB", SecurityType.Equity, Market.USA);
        private int _step;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            UniverseSettings.Resolution = Resolution.Daily;

            AddUniverse(new ConstituentsUniverse(
                QuantConnect.Symbol.Create("constituents-universe-qctest", SecurityType.Equity, Market.USA),
                UniverseSettings));
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            _step++;
            if (_step == 1)
            {
                if (!data.ContainsKey(_qqq)
                    || !data.ContainsKey(_appl))
                {
                    throw new Exception($"Unexpected symbols found, step: {_step}");
                }
                if (data.Count != 2)
                {
                    throw new Exception($"Unexpected data count, step: {_step}");
                }
                // AAPL will be deselected by the ConstituentsUniverse
                // but it shouldn't be removed since we hold it
                SetHoldings(_appl, 0.5);
            }
            else if (_step == 2)
            {
                if (!data.ContainsKey(_appl))
                {
                    throw new Exception($"Unexpected symbols found, step: {_step}");
                }
                if (data.Count != 1)
                {
                    throw new Exception($"Unexpected data count, step: {_step}");
                }
                // AAPL should now be released
                // note: takes one extra loop because the order is executed on market open
                Liquidate();
            }
            else if (_step == 3)
            {
                if (!data.ContainsKey(_fb)
                    || !data.ContainsKey(_spy)
                    || !data.ContainsKey(_appl))
                {
                    throw new Exception($"Unexpected symbols found, step: {_step}");
                }
                if (data.Count != 3)
                {
                    throw new Exception($"Unexpected data count, step: {_step}");
                }
            }
            else if (_step == 4)
            {
                if (!data.ContainsKey(_fb)
                    || !data.ContainsKey(_spy))
                {
                    throw new Exception($"Unexpected symbols found, step: {_step}");
                }
                if (data.Count != 2)
                {
                    throw new Exception($"Unexpected data count, step: {_step}");
                }
            }
            else if (_step == 5)
            {
                if (!data.ContainsKey(_fb)
                    || !data.ContainsKey(_spy))
                {
                    throw new Exception($"Unexpected symbols found, step: {_step}");
                }
                if (data.Count != 2)
                {
                    throw new Exception($"Unexpected data count, step: {_step}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_step != 5)
            {
                throw new Exception($"Unexpected step count: {_step}");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                Log($"AddedSecurities {added}");
            }

            foreach (var removed in changes.RemovedSecurities)
            {
                Log($"RemovedSecurities {removed} {_step}");
                // we are currently notifying the removal of AAPl twice,
                // when deselected and when finally removed (since it stayed pending)
                if (removed.Symbol == _appl && _step != 1 && _step != 2
                    || removed.Symbol == _qqq && _step != 1)
                {
                    throw new Exception($"Unexpected removal step count: {_step}");
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.52%"},
            {"Compounding Annual Return", "-31.636%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.520%"},
            {"Sharpe Ratio", "-3.259"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.275"},
            {"Beta", "0.162"},
            {"Annual Standard Deviation", "0.067"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "-3.4"},
            {"Tracking Error", "0.169"},
            {"Treynor Ratio", "-1.342"},
            {"Total Fees", "$7.82"}
        };
    }
}
