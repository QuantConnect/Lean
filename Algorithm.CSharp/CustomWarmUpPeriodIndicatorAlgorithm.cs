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
using Python.Runtime;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test to check Python indicator is keeping backwards compatibility 
    /// with indicators that do not set WarmUpPeriod.
    /// </summary>
    public class CustomWarmUpPeriodIndicatorAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        PyObject rawCustom;
        PyObject rawCustomWarmUp;
        PythonIndicator custom;
        PythonIndicator customWarmUp;
        RollingWindow<IndicatorDataPoint> customWarmUpWindow;
        RollingWindow<IndicatorDataPoint> customWindow;
        int samples;

    public override void Initialize()
        {
            using (Py.GIL())
            {
                // Get the Python module of the custom indicators
                var module = PythonEngine.ModuleFromString(
                    Guid.NewGuid().ToString(),
                    @"
# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) With Warm Up Period parameter defined
from AlgorithmImports import *
from collections import deque

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) With Warm Up Period parameter defined
class CSMAWithWarmUp(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)
        self.WarmUpPeriod = period

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) without Warm Up Period parameter defined
class CustomSMA(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen
"
                );

                SetStartDate(2013, 10, 7);
                SetEndDate(2013, 10, 11);
                AddEquity("SPY", Resolution.Second);

                // Create two python indicators, one defines Warm Up
                rawCustom = module.GetAttr("CustomSMA")
                    .Invoke("custom".ToPython(), 60.ToPython());
                custom = new PythonIndicator(rawCustom);

                rawCustomWarmUp = module.GetAttr("CSMAWithWarmUp")
                    .Invoke("customWarmUp".ToPython(), 60.ToPython());
                customWarmUp = new PythonIndicator(rawCustomWarmUp);

                // The python custom class must inherit from PythonIndicator to enable Updated event handler
                customWarmUp.Updated += CustomWarmUpUpdated;
                custom.Updated += CustomUpdated;

                // Register the indicators
                customWarmUpWindow = new RollingWindow<IndicatorDataPoint>(5);
                customWindow = new RollingWindow<IndicatorDataPoint>(5);
                RegisterIndicator("SPY", customWarmUp, Resolution.Minute);
                RegisterIndicator("SPY", custom, Resolution.Minute);

                // Try to warm up both indicators
                WarmUpIndicator("SPY", customWarmUp, Resolution.Minute);

                // Check customWarmUp indicator has already warmed up the data
                if (!customWarmUp.IsReady)
                {
                    throw new Exception("customWarmUp indicator was expected to be ready");
                }
                if (customWarmUp.Samples != 60)
                {
                    throw new Exception("customWarmUp was expected to have processed datapoints 60 already");
                }

                WarmUpIndicator("SPY", custom, Resolution.Minute);

                // Check custom indicator is not ready and is using the default WarmUpPeriod value
                if (custom.IsReady)
                {
                    throw new Exception("custom indicator wasn't expected to be warmed up");
                }
                if (custom.WarmUpPeriod != 0)
                {
                    throw new Exception("custom indicator WarmUpPeriod parameter was expected to be 0");
                }

                // Helper variable to save the number of samples processed
                samples = 0;
            }
        }

        public void CustomUpdated(object sender, IndicatorDataPoint updated)
        {
            customWindow.Add(updated);
        }

        public void CustomWarmUpUpdated(object sender, IndicatorDataPoint updated)
        {
            customWarmUpWindow.Add(updated);
        }

        public void OnData(TradeBars data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
            }

            if (Time.Second == 0)
            {
                samples += 1;
                Log($"   customWarmUp -> IsReady: {customWarmUp.IsReady}. Value: {customWarmUp.Current.Value}");
                Log($"custom -> IsReady: {custom.IsReady}. Value: {custom.Current.Value}");

                var diff = Math.Abs(custom.Current.Value - customWarmUp.Current.Value);
                Log($"Samples: {samples}");

                // Check self.custom indicator is ready when the number of samples is bigger than its WarmUpPeriod
                if (custom.IsReady != (samples >= 60))
                {
                    throw new Exception("custom indicator was expected to be ready when the number of samples were bigger that its WarmUpPeriod parameter");
                }

                // Check the value of the two custom indicators is the same when both are ready
                if (diff > 1e-10m && custom.IsReady && customWarmUp.IsReady) 
                {
                    throw new Exception($"indicators difference is {diff}");
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
