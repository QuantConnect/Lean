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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to use custom security properties.
    /// In this algorithm we trade a security based on the values of a slow and fast EMAs which are stored in the security itself.
    /// </summary>
    public class SecurityCustomPropertiesAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Equity _spy;
        private dynamic _dynamicSpy;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _spy = AddEquity("SPY", Resolution.Minute);

            // Using the dynamic interface to store our indicator as a custom property
            _dynamicSpy = _spy;
            _dynamicSpy.SlowEma = EMA(_spy.Symbol, 30, Resolution.Minute);

            // Using the generic interface to store our indicator as a custom property
            _spy.Add("FastEma", EMA(_spy.Symbol, 60, Resolution.Minute));

            // Using the indexer to store our indicator as a custom property
            _spy["BB"] = BB(_spy.Symbol, 20, 1, MovingAverageType.Simple, Resolution.Minute);

        }

        public override void OnData(Slice data)
        {
            if (!_dynamicSpy.FastEma.IsReady)
            {
                return;
            }

            if (!Portfolio.Invested)
            {
                // Using the dynamic interface to access the custom properties
                if (_dynamicSpy.SlowEma > _dynamicSpy.FastEma)
                {
                    SetHoldings(_spy.Symbol, 1);
                }
            }
            // Using the generic interface to access the custom properties
            else if (_spy.Get<IndicatorBase>("SlowEma") < _spy.Get<IndicatorBase>("FastEma"))
            {
                Liquidate(_spy.Symbol);
            }

            // Using the indexer to access the custom properties
            var bb = _spy["BB"] as BollingerBands;
            Plot("BB", bb.UpperBand, bb.MiddleBand, bb.LowerBand);
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
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "30"},
            {"Average Win", "0.42%"},
            {"Average Loss", "-0.09%"},
            {"Compounding Annual Return", "78.519%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "0.574"},
            {"Net Profit", "0.744%"},
            {"Sharpe Ratio", "11.684"},
            {"Probabilistic Sharpe Ratio", "88.148%"},
            {"Loss Rate", "73%"},
            {"Win Rate", "27%"},
            {"Profit-Loss Ratio", "4.90"},
            {"Alpha", "0.227"},
            {"Beta", "0.341"},
            {"Annual Standard Deviation", "0.077"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-7.329"},
            {"Tracking Error", "0.147"},
            {"Treynor Ratio", "2.648"},
            {"Total Fees", "$103.09"},
            {"Estimated Strategy Capacity", "$7300000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "597.39%"},
            {"OrderListHash", "5c28d5191259b2eb26f8e9694200c5c6"}
        };
    }
}
