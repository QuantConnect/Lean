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

using Fasterflect;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    public class IndicatorSelectorsWorkWithDifferentOptions: QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Indicator _bidCloseIndicator;
        private Indicator _bidOpenIndicator;
        private Indicator _bidLowIndicator;
        private Indicator _bidHighIndicator;
        private Indicator _askCloseIndicator;
        private Indicator _askOpenIndicator;
        private Indicator _askLowIndicator;
        private Indicator _askHighIndicator;
        private List<Indicator> _indicators;
        private bool _quoteBarsFound;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 08);

            var symbol = AddEquity("SPY", Resolution.Minute).Symbol;
            _bidCloseIndicator = Identity(symbol, Resolution.Minute, Field.BidClose, "Bid.Close.");
            _bidOpenIndicator = Identity(symbol, Resolution.Minute, Field.BidOpen, "Bid.Open.");
            _bidLowIndicator = Identity(symbol, Resolution.Minute, Field.BidLow, "Bid.Low.");
            _bidHighIndicator = Identity(symbol, Resolution.Minute, Field.BidHigh, "Bid.High.");

            _askCloseIndicator = Identity(symbol, Resolution.Minute, Field.AskClose, "Ask.Close.");
            _askOpenIndicator = Identity(symbol, Resolution.Minute, Field.AskOpen, "Ask.Open.");
            _askLowIndicator = Identity(symbol, Resolution.Minute, Field.AskLow, "Ask.Low.");
            _askHighIndicator = Identity(symbol, Resolution.Minute, Field.AskHigh, "Ask.High.");

            _indicators = new List<Indicator>()
            {
                _bidCloseIndicator,
                _bidOpenIndicator,
                _bidLowIndicator,
                _bidHighIndicator,
                _askCloseIndicator,
                _askOpenIndicator,
                _askLowIndicator,
                _askHighIndicator,
            };
        }

        public override void OnData(Slice slice)
        {
            if (slice.QuoteBars.Any())
            {
                _quoteBarsFound = true;
                var wrongIndicators = _indicators.Where(x =>
                {
                    var propertyName = x.Name.Split(".")[0]; // This could be Ask/Bid
                    var secondPropertyName = x.Name.Split(".")[1]; // This could be Open/Close/High/Low
                    var property = slice.QuoteBars["SPY"].GetType().GetProperty(propertyName).GetValue(slice.QuoteBars["SPY"], null);
                    var value = (decimal)property.GetType().GetProperty(secondPropertyName).GetValue(property, null);
                    return x.Current.Value != value;
                });

                if (wrongIndicators.Any())
                {
                    throw new RegressionTestException();
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_quoteBarsFound)
            {
                throw new RegressionTestException("At least one quote bar should have been found, but none was found");
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
        public long DataPoints => 1582;

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
