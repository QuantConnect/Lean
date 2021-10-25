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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Indicators;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test to check custom indicators warms up properly
    /// when one of them define WarmUpPeriod parameter and the other doesn't
    /// </summary>
    public class CustomWarmUpPeriodIndicatorAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private CustomSMA custom;
        private CSMAWithWarmUp customWarmUp;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            AddEquity("SPY", Resolution.Second);

            // Create two custom indicators, where one of them defines WarmUpPeriod parameter
            custom = new CustomSMA("custom", 60);
            customWarmUp = new CSMAWithWarmUp("customWarmUp", 60);

            // Register the daily data of "SPY" to automatically update both indicators
            RegisterIndicator("SPY", customWarmUp, Resolution.Minute);
            RegisterIndicator("SPY", custom, Resolution.Minute);

            // Warm up customWarmUp indicator
            WarmUpIndicator("SPY", customWarmUp, Resolution.Minute);

            // Check customWarmUp indicator has already been warmed up with the requested data
            if (!customWarmUp.IsReady)
            {
                throw new Exception("customWarmUp indicator was expected to be ready");
            }
            if (customWarmUp.Samples != 60)
            {
                throw new Exception("customWarmUp was expected to have processed 60 datapoints already");
            }

            // Try to warm up custom indicator. It's expected from LEAN to skip the warm up process
            // because custom indicator doesn't implement IIndicatorWarmUpPeriodProvider
            WarmUpIndicator("SPY", custom, Resolution.Minute);

            // Check custom indicator is not ready, because the warm up process was skipped
            if (custom.IsReady)
            {
                throw new Exception("custom indicator wasn't expected to be warmed up");
            }
        }

        public void OnData(TradeBars data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
            }

            if (Time.Second == 0)
            {
                // Compute the difference between the indicators values
                var diff = Math.Abs(custom.Current.Value - customWarmUp.Current.Value);

                // Check self.custom indicator is ready when the number of samples is bigger than its period
                if (custom.IsReady != (custom.Samples >= 60))
                {
                    throw new Exception("custom indicator was expected to be ready when the number of samples were bigger that its WarmUpPeriod parameter");
                }

                // Check their values are the same when both are ready
                if (diff > 1e-10m && custom.IsReady && customWarmUp.IsReady) 
                {
                    throw new Exception($"The values of the indicators are not the same. The difference is {diff}");
                }
            }
        }

        /// <summary>
        /// Custom implementation of SimpleMovingAverage.
        /// Represents the traditional simple moving average indicator (SMA) without WarmUpPeriod parameter defined
        /// </summary>
        private class CustomSMA : IndicatorBase<IBaseData>
        {
            private Queue<IBaseData> _queue;
            private int _period;
            public CustomSMA(string name, int period)
                : base(name)
            {
                _queue = new Queue<IBaseData>();
                _period = period;
            }

            public override bool IsReady => _queue.Count == _period;

            protected override decimal ComputeNextValue(IBaseData input)
            {
                _queue.Enqueue(input);
                if (_queue.Count > _period)
                {
                    _queue.Dequeue();
                }
                var items = (_queue.ToArray());
                var sum = 0m;
                Array.ForEach(items, i => sum += i.Value);
                return sum / _queue.Count;
            }
        }

        /// <summary>
        /// Custom implementation of SimpleMovingAverage.
        /// Represents the traditional simple moving average indicator (SMA) with WarmUpPeriod defined
        /// </summary>
        private class CSMAWithWarmUp : CustomSMA, IIndicatorWarmUpPeriodProvider
        {
            public CSMAWithWarmUp(string name, int period)
                : base(name, period)
            {
                WarmUpPeriod = period;
            }
            public int WarmUpPeriod { get; private set; }
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
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "272.157%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.694%"},
            {"Sharpe Ratio", "8.897"},
            {"Probabilistic Sharpe Ratio", "67.609%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.003"},
            {"Beta", "0.998"},
            {"Annual Standard Deviation", "0.222"},
            {"Annual Variance", "0.049"},
            {"Information Ratio", "-14.534"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "1.98"},
            {"Total Fees", "$3.45"},
            {"Estimated Strategy Capacity", "$310000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Fitness Score", "0.246"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "9.761"},
            {"Return Over Maximum Drawdown", "107.509"},
            {"Portfolio Turnover", "0.249"},
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
            {"OrderListHash", "e10039d74166b161f3ea2851a5e85843"}
        };
    }
}
