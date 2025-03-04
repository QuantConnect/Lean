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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm tests the functionality of the CompositeIndicator,  
    /// using either a lambda expression or a method reference.
    /// </summary>
    public class CompositeIndicatorWorksAsExpectedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private CompositeIndicator _compositeMinDirect;
        private CompositeIndicator _compositeMinMethod;
        private bool _dataReceived;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 4);
            SetEndDate(2013, 10, 5);
            AddEquity("SPY", Resolution.Minute);

            var closePrice = Identity("SPY", Resolution.Minute, Field.Close);
            var lowPrice = MIN("SPY", 420, Resolution.Minute, Field.Low);

            _compositeMinDirect = new CompositeIndicator("CompositeMinDirect", closePrice, lowPrice, (l, r) => new IndicatorResult(Math.Min(l.Current.Value, r.Current.Value)));
            _compositeMinMethod = new CompositeIndicator("CompositeMinMethod", closePrice, lowPrice, Composer);

            _dataReceived = false;
        }

        private IndicatorResult Composer(IndicatorBase l, IndicatorBase r)
        {
            return new IndicatorResult(Math.Min(l.Current.Value, r.Current.Value));
        }

        public override void OnData(Slice data)
        {
            _dataReceived = true;

            if (_compositeMinDirect.Current.Value != _compositeMinMethod.Current.Value)
            {
                throw new RegressionTestException($"Values of indicators differ: {_compositeMinDirect.Current.Value} | {_compositeMinMethod.Current.Value}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_dataReceived)
            {
                throw new RegressionTestException("No data was processed during the algorithm execution.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 795;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
