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
        private CSMANotWarmUp _customNotWarmUp;
        private CSMAWithWarmUp _customWarmUp;
        private SimpleMovingAverage _customNotInherit;
        private SimpleMovingAverage _duplicateSMA;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            AddEquity("SPY", Resolution.Second);

            // Create two custom indicators, where one of them defines WarmUpPeriod parameter
            _customNotWarmUp = new CSMANotWarmUp("_customNotWarmUp", 60);
            _customWarmUp = new CSMAWithWarmUp("_customWarmUp", 60);
            _customNotInherit = new SimpleMovingAverage("_customNotInherit", 60);
            // using 2nd SMA to match counterpart python algorithm ( CustomSMA + csharpIndicator )
            // so that AlgorithmHistoryDataPoints are the same in both
            _duplicateSMA = new SimpleMovingAverage("_duplicateSMA", 60);

            // Register the daily data of "SPY" to automatically update both indicators
            RegisterIndicator("SPY", _customWarmUp, Resolution.Minute);
            RegisterIndicator("SPY", _customNotWarmUp, Resolution.Minute);
            RegisterIndicator("SPY", _customNotInherit, Resolution.Minute);
            RegisterIndicator("SPY", _duplicateSMA, Resolution.Minute);

            // Warm up _customWarmUp indicator
            WarmUpIndicator("SPY", _customWarmUp, Resolution.Minute);

            // Check _customWarmUp indicator has already been warmed up with the requested data
            if (!_customWarmUp.IsReady)
            {
                throw new Exception("_customWarmUp indicator was expected to be ready");
            }
            if (_customWarmUp.Samples != 60)
            {
                throw new Exception("_customWarmUp indicator was expected to have processed 60 datapoints already");
            }

            // Try to warm up _customNotWarmUp indicator. It's expected from LEAN to skip the warm up process
            // because this indicator doesn't implement IIndicatorWarmUpPeriodProvider
            WarmUpIndicator("SPY", _customNotWarmUp, Resolution.Minute);

            // Check _customNotWarmUp indicator is not ready, because the warm up process was skipped
            if (_customNotWarmUp.IsReady)
            {
                throw new Exception("_customNotWarmUp indicator wasn't expected to be warmed up");
            }

            WarmUpIndicator("SPY", _customNotInherit, Resolution.Minute);
            // Check _customWarmUp indicator has already been warmed up with the requested data
            if (!_customNotInherit.IsReady)
            {
                throw new Exception("_customNotInherit indicator was expected to be ready");
            }
            if (_customNotInherit.Samples != 60)
            {
                throw new Exception("_customNotInherit indicator was expected to have processed 60 datapoints already");
            }

            WarmUpIndicator("SPY", _duplicateSMA, Resolution.Minute);
            // Check _customWarmUp indicator has already been warmed up with the requested data
            if (!_duplicateSMA.IsReady)
            {
                throw new Exception("_duplicateSMA indicator was expected to be ready");
            }
            if (_duplicateSMA.Samples != 60)
            {
                throw new Exception("_duplicateSMA indicator was expected to have processed 60 datapoints already");
            }
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
            }

            if (Time.Second == 0)
            {
                // Compute the difference between the indicators values
                var diff = Math.Abs(_customNotWarmUp.Current.Value - _customWarmUp.Current.Value);
                diff += Math.Abs(_customNotInherit.Current.Value - _customNotWarmUp.Current.Value);
                diff += Math.Abs(_customNotInherit.Current.Value - _customWarmUp.Current.Value);
                diff += Math.Abs(_duplicateSMA.Current.Value - _customWarmUp.Current.Value);
                diff += Math.Abs(_duplicateSMA.Current.Value - _customNotWarmUp.Current.Value);
                diff += Math.Abs(_duplicateSMA.Current.Value - _customNotInherit.Current.Value);

                // Check _customNotWarmUp indicator is ready when the number of samples is bigger than its period
                if (_customNotWarmUp.IsReady != (_customNotWarmUp.Samples >= 60))
                {
                    throw new Exception("_customNotWarmUp indicator was expected to be ready when the number of samples were bigger that its WarmUpPeriod parameter");
                }

                // Check their values are the same when both are ready
                if (diff > 1e-10m && _customNotWarmUp.IsReady && _customWarmUp.IsReady) 
                {
                    throw new Exception($"The values of the indicators are not the same. The difference is {diff}");
                }
            }
        }

        /// <summary>
        /// Custom implementation of SimpleMovingAverage.
        /// Represents the traditional simple moving average indicator (SMA) without WarmUpPeriod parameter defined
        /// </summary>
        private class CSMANotWarmUp : IndicatorBase<IBaseData>
        {
            private Queue<IBaseData> _queue;
            private int _period;
            public CSMANotWarmUp(string name, int period)
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
        private class CSMAWithWarmUp : CSMANotWarmUp, IIndicatorWarmUpPeriodProvider
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 234043;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 360;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "272.157%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101694.38"},
            {"Net Profit", "1.694%"},
            {"Sharpe Ratio", "8.863"},
            {"Sortino Ratio", "0"},
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
            {"Treynor Ratio", "1.972"},
            {"Total Fees", "$3.45"},
            {"Estimated Strategy Capacity", "$310000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "19.96%"},
            {"OrderListHash", "8c925e7c6c10ff1da3a40669accba91a"}
        };
    }
}
