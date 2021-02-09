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
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm which reproduces GH issue 3861, where in some cases 2 consolidators were added when
    /// using the automatic indicator warmup feature
    /// </summary>
    public class AutomaticIndicatorWarmupRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            EnableAutomaticIndicatorWarmUp = true;

            // Test case 1
            _spy = AddEquity("SPY").Symbol;
            var sma = SMA(_spy, 10);
            if (!sma.IsReady)
            {
                throw new Exception("Expected SMA to be warmed up");
            }

            // Test case 2
            var indicator = new CustomIndicator(10);
            RegisterIndicator(_spy, indicator, Resolution.Minute, (Func<IBaseData, decimal>) null);

            if (indicator.IsReady)
            {
                throw new Exception("Expected CustomIndicator Not to be warmed up");
            }
            WarmUpIndicator(_spy, indicator);
            if (!indicator.IsReady)
            {
                throw new Exception("Expected CustomIndicator to be warmed up");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                var subscription = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(_spy).First(config => config.TickType == TickType.Trade);

                // we expect 1 consolidator per indicator
                if (subscription.Consolidators.Count != 2)
                {
                    throw new Exception($"Unexpected consolidator count for subscription: {subscription.Consolidators.Count}");
                }
                SetHoldings(_spy, 1);
            }
        }

        private class CustomIndicator : SimpleMovingAverage
        {
            private IndicatorDataPoint _previous;
            public CustomIndicator(int period) : base(period)
            {
            }
            protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
            {
                if (_previous != null && input.EndTime == _previous.EndTime)
                {
                    throw new Exception($"Unexpected indicator double data point call: {_previous}");
                }
                _previous = input;
                return base.ComputeNextValue(window, input);
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
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "264.819%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.668%"},
            {"Sharpe Ratio", "8.749"},
            {"Probabilistic Sharpe Ratio", "67.311%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.005"},
            {"Beta", "0.996"},
            {"Annual Standard Deviation", "0.219"},
            {"Annual Variance", "0.048"},
            {"Information Ratio", "-14.189"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "1.922"},
            {"Total Fees", "$3.26"},
            {"Fitness Score", "0.248"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "93.761"},
            {"Portfolio Turnover", "0.248"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "25885f979ca8c7b44f5d0f7daf00b241"}
        };
    }
}
