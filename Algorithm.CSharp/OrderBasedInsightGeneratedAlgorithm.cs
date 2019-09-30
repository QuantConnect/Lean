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
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm that asserts on insights automatically emitted based on order fills
    /// </summary>
    public class OrderBasedInsightGeneratedAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private int _step;
        private double _expectedConfidence;
        private InsightDirection _expectedInsightDirection;
        private bool _emitted;
        private List<Insight> _insights;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 20);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            _spy = AddEquity("SPY", Resolution.Daily).Symbol;
            InsightsGenerated += OnInsightsGeneratedVerifier;
            _insights = new List<Insight>();
        }

        private void OnInsightsGeneratedVerifier(IAlgorithm algorithm,
            GeneratedInsightsCollection insightsCollection)
        {
            _emitted = true;
            var insight = insightsCollection.Insights.First();

            if (Math.Abs(insight.Confidence.Value - _expectedConfidence) > 0.02)
            {
                throw new Exception($"Unexpected insight Confidence: {insight.Confidence.Value}." +
                    $" Expected: {_expectedConfidence}. Step {_step}");
            }
            if (insight.Direction != _expectedInsightDirection)
            {
                throw new Exception($"Unexpected insight Direction: {insight.Direction}." +
                    $" Expected: {_expectedInsightDirection}. Step {_step}");
            }
            _insights.Add(insight);
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_emitted)
            {
                throw new Exception("No insight was emitted!");
            }
            if (_step != 7)
            {
                throw new Exception($"Unexpected final step value: {_step}. Expected 7");
            }
            if (_insights.Take(_insights.Count - 1) // the last is not closed yet
                .Any(insight => insight.CloseTimeUtc == QuantConnect.Time.EndOfTime
                 || insight.Period == QuantConnect.Time.EndOfTimeTimeSpan))
            {
                throw new Exception("Found insight with invalid close or period value");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_step == 0)
            {
                _expectedConfidence = 1;
                _expectedInsightDirection = InsightDirection.Up;
                _step++;
                SetHoldings(_spy, 0.75);
            }
            else if (_step == 1)
            {
                _step++;
                _expectedConfidence = 1;
                _expectedInsightDirection = InsightDirection.Up;
                SetHoldings(_spy, 0.80);
            }
            else if (_step == 2)
            {
                _step++;
                _expectedConfidence = 0.5;
                _expectedInsightDirection = InsightDirection.Up;
                SetHoldings(_spy, 0.40);
            }
            else if (_step == 3)
            {
                _step++;
                _expectedConfidence = 0.25;
                _expectedInsightDirection = InsightDirection.Up;
                SetHoldings(_spy, 0.20);
            }
            else if (_step == 4)
            {
                _step++;
                _expectedConfidence = 1;
                _expectedInsightDirection = InsightDirection.Flat;
                SetHoldings(_spy, 0);
            }
            else if (_step == 5)
            {
                _step++;
                _expectedConfidence = 1;
                _expectedInsightDirection = InsightDirection.Down;
                SetHoldings(_spy, -0.5);
            }
            else if (_step == 6)
            {
                _step++;
                _expectedConfidence = 1;
                _expectedInsightDirection = InsightDirection.Up;
                SetHoldings(_spy, 1);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "7"},
            {"Average Win", "0.21%"},
            {"Average Loss", "-0.04%"},
            {"Compounding Annual Return", "112.371%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "2.423"},
            {"Net Profit", "2.507%"},
            {"Sharpe Ratio", "6.553"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "5.85"},
            {"Alpha", "0.23"},
            {"Beta", "0.456"},
            {"Annual Standard Deviation", "0.087"},
            {"Annual Variance", "0.008"},
            {"Information Ratio", "-1.785"},
            {"Tracking Error", "0.099"},
            {"Treynor Ratio", "1.253"},
            {"Total Fees", "$13.18"},
            {"Total Insights Generated", "7"},
            {"Total Insights Closed", "6"},
            {"Total Insights Analysis Completed", "6"},
            {"Long Insight Count", "5"},
            {"Short Insight Count", "1"},
            {"Long/Short Ratio", "500%"},
            {"Estimated Monthly Alpha Value", "$8015512.7828"},
            {"Total Accumulated Estimated Alpha Value", "$3250735.7397"},
            {"Mean Population Estimated Insight Value", "$541789.2899"},
            {"Mean Population Direction", "83.3333%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "98.0198%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}
