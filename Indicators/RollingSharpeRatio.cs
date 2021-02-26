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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Calculation of the rolling moving average for the Sharpe Ratio (RSR) developed by William F. Sharpe
    /// You can optionally specify a different moving average type to be used in the computation.
    /// 
    /// Notes:
    /// Utilizes QuantConnect.Statistics.Statistics.ObservedSharpeRatio(List<double> values) for sharpe ratio calculation.
    /// </summary>
    public class RollingSharpeRatio : IndicatorBase<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Stores the prior data point used for calculation of the next percent change
        /// </summary>
        private decimal _previousInput;

        /// <summary>
        /// Stores the current value of the indicator
        /// </summary>
        private decimal _indicatorValue;

        /// <summary>
        /// Stores the period of the historical percent changes used for calculating sharpe ratio
        /// </summary>
        public int SharpePeriod { get; }

        /// <summary>
        /// Stores the period of the SharpeRatioMovingAverage
        /// </summary>
        public int MovingAveragePeriod { get; }

        /// <summary>
        /// Used for calculating the moving average of the SharpeRatio
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> SharpeRatioMovingAverage { get; }

        /// <summary>
        /// Stores historical percent change values for calculation of SharpeRatio
        /// </summary>
        private RollingWindow<double> HistoricValues { get; }

        /// <summary>
        /// Returns whether the indicator is properly initalized with data
        /// </summary>
        public override bool IsReady => SharpeRatioMovingAverage.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new RollingSharpeRatio inidcator using the specified periods and moving average type
        /// </summary>
		/// <param name="name">The name of this indicator</param>
		/// <param name="sharpePeriod">Period of historical observation for sharpe ratio calculation</param>
		/// <param name="movingAveragePeriod">Period for the moving average calculation of the sharpe ratio</param>
		/// <param name="movingAverageType">The type of smoothing used to smooth the sharpe ratio values</param>
        public RollingSharpeRatio(string name, int sharpePeriod, int movingAveragePeriod, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : base(name)
        {
            SharpePeriod = sharpePeriod;
            MovingAveragePeriod = movingAveragePeriod;
            SharpeRatioMovingAverage = movingAverageType.AsIndicator(name, movingAveragePeriod);
            HistoricValues = new RollingWindow<double>(sharpePeriod);
            WarmUpPeriod = sharpePeriod + movingAveragePeriod;
            _previousInput = 0.0m;
        }

        /// <summary>
        /// Creates a new RollingSharpeRatio inidcator using the specified periods and moving average type
        /// </summary>
        /// <param name="sharpePeriod">Period of historical observation for sharpe ratio calculation</param>
        /// <param name="movingAveragePeriod">Period for the moving average calculation of the sharpe ratio</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the sharpe ratio values</param>
        public RollingSharpeRatio(int sharpePeriod, int movingAveragePeriod, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : this($"RSR({sharpePeriod}, {movingAveragePeriod})", sharpePeriod, movingAveragePeriod, movingAverageType)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
		/// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            // check to see if _previousInput is set
            // if so calculate percent change and add to HistoricValues
            if (_previousInput != 0.0m)
            {
                // parse to double
                double current = Decimal.ToDouble(input.Value);
                double prior = Decimal.ToDouble(_previousInput);

                // calculate percent change
                double percentChange = ((current - prior) / prior) * 100;

                // add to HistoricValues
                HistoricValues.Add(percentChange);
            }

            // check to see if HistoricValues are loaded
            // if not then perform necessary calculations
            if (HistoricValues.IsReady)
            {
                // turn RollingWindow into array
                List<double> array = new List<double>(SharpePeriod);

                // parse HistoricValues into array
                foreach (decimal d in HistoricValues)
                {
                    double parsedValue = Decimal.ToDouble(d);
                    array.Add(parsedValue);
                }

                // call QuantConnect.Statistics.Statistics.ObservedSharpeRatio to calculate
                decimal sharpeRatio = Convert.ToDecimal(QuantConnect.Statistics.Statistics.ObservedSharpeRatio(array) * Math.Sqrt(SharpePeriod));

                // push to MovingAverage
                SharpeRatioMovingAverage.Update(input.Time, sharpeRatio);
            }

            // update previous input to current
            _previousInput = input.Value;

            // check to see if SharpeRatioMovingAverage is ready
            // if not return 0.0m
            // update _indicatorValue based on conditions
            _indicatorValue = HistoricValues.IsReady && SharpeRatioMovingAverage.IsReady ? SharpeRatioMovingAverage : 0.0m;
            return _indicatorValue;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            SharpeRatioMovingAverage.Reset();
            HistoricValues.Reset();
            _previousInput = 0.0m;
            base.Reset();
        }
    }
}