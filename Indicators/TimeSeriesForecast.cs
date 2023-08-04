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
using System.Collections;
namespace QuantConnect.Indicators;

/// <summary>
/// This indicator predicts the new data in a time-series using the previous data from a window. 
/// Reference: https://tulipindicators.org/tsf
/// </summary>

public class TimeSeriesForecast : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
{

        private readonly int _period;

        private decimal lastVal;
        // This variable represents the last value of the previous window
        // E.g. [1,2,{3,4,5,6},7,8,9] 
        // Suppose the period is 4
        // For above Series of inputs, if the current window is from values 3 to 6 then the last value of the previous window would be 2
        
        private decimal prevBeta;
        private readonly decimal periodAverage, diffSum, squaredDiffSum;
        private readonly SimpleMovingAverage Mean;
        private readonly Sum RollingSum;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            RollingSum.Reset();
            Mean.Reset();
            base.Reset();
        }

        /// <summary>
        /// Initializes a new instance of the TimeSeriesForecast class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the TSF</param>
        public TimeSeriesForecast(string name, int period)
            : base(name, period)
        {
            
            if (period<=0)
            {
                throw new ArgumentException("TimeSeriesForecast: Period must be a positive integer", $"{nameof(period)}");
            }

            _period = period;

            periodAverage = 0;
            diffSum = 0;
            squaredDiffSum = 0;   
            lastVal = 0m;
            prevBeta = 0m;

            // Calculating the average of 1 to n where n is period
            for(int i=1;i<=_period;i++){
                periodAverage += i;
            }
            periodAverage /= _period;

            for(int i=1;i<=_period;i++){

                diffSum += (i-periodAverage);

                squaredDiffSum += (i-periodAverage)*(i-periodAverage);
            }

            RollingSum = new Sum(name + "_Sum", period);
            Mean = new SimpleMovingAverage($"{name}_Mean", period);
        }

        /// <summary>
        /// Initializes a new instance of the TimeSeriesForecast class with the default name and period
        /// </summary>
        /// <param name="period">The period of the SMA</param>
        public TimeSeriesForecast(int period)
            : this($"TSF({period})", period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {

            Mean.Update(input);
            RollingSum.Update(input);

            if(!IsReady){
                return 0;
            }

            decimal numerator = 0;
            decimal beta = 0;
            decimal alpha = 0;

            if(Samples==_period){
                
                for(int i=1;i<=_period;i++){
                    numerator += (i-periodAverage)*(window[_period - i].Value - Mean.Current.Value);
                }

                beta = numerator/squaredDiffSum;
                prevBeta = beta;

                alpha = Mean.Current.Value - beta*periodAverage;

                lastVal = window[_period - 1].Value;
                return alpha + beta*(1+_period);
            }
            
            // Reference for the Linear Regression equation - https://tulipindicators.org/tsf
            // The following equation is for t>n
            // B_t  -> Beta at time t
            // x'   -> The average of numbers between 1 to n
            // y_t  -> Input value at time t
            // n    -> period
            // If we take the difference between B_t and B_(t-1) and simplify it then we get the following equation
            // B_t - B_(t-1) = { (n - x')*y_t - (1 - x')*y_(t-n-1) - [ y_(t-n) + y_(t-n+1) + ... + y_(t-1) ] - [ ∑(i-x') ]*[(y_t - y_(t-n-1))/n]}/{∑(i-x')^2}

            decimal diffNumerator = (_period - periodAverage)*input.Value - (1 - periodAverage)*lastVal 
                               - (RollingSum.Current.Value - input.Value) 
                               - diffSum*((input.Value - lastVal)/_period);

            decimal diff = diffNumerator / squaredDiffSum;
            
            beta = diff + prevBeta;
            prevBeta = beta;

            alpha = Mean.Current.Value - beta*periodAverage;

            decimal finalAns = alpha + beta*(1+_period);

            lastVal = window[_period - 1].Value;

            return finalAns;
        }

}