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
using QuantConnect.Data.Market;
using MathNet.Numerics.Statistics;
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Alpha is a measure of residual risk of an investment relative to some market index 
    /// </summary>
    public class Alpha : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// RollingWindow to store the data points of the target symbol
        /// </summary>
        private readonly RollingWindow<decimal> _targetDataPoints;

        /// <summary>
        /// RollingWindow to store the data points of the reference symbol
        /// </summary>
        private readonly RollingWindow<decimal> _referenceDataPoints;

        /// <summary>
        /// Alpha of the target used in relation with the reference
        /// </summary>
        private decimal _alpha;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; private set; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _targetDataPoints.Samples >= WarmUpPeriod && _referenceDataPoints.Samples >= WarmUpPeriod;

        /// <summary>
        /// risk free constant
        /// </summary>
        public float RiskFree = 0.025f;

        /// <summary>
        /// Beta indicator
        /// </summary>
        public Beta BetaIndicator { get; }

        /// <summary>
        /// Rate of change indicator
        /// </summary>
        public RateOfChange ROC { get; }

        /// <summary>
        /// Creates a new Alpha indicator with the specified arguments  
        /// and period values
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int period)
            : base(name)
        {
            // Assert the period is greater than two, otherwise the Alpha can not be computed
            if (period < 2)
            {
                throw new ArgumentException($"Period parameter for Alpha indicator must be greater than 2 but was {period}");
            }

            WarmUpPeriod = period + 1;

            _targetDataPoints = new RollingWindow<decimal>(2);
            _referenceDataPoints = new RollingWindow<decimal>(2);

            BetaIndicator = new Beta(targetSymbol, referenceSymbol, period);
            ROC = new RateOfChange(1);

            _alpha = 0;
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified args
        /// reference values
        /// </summary>
        /// <param name="period">The period of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <remarks>Constructor overload for backward compatibility.</remarks>
        public Alpha(Symbol targetSymbol, Symbol referenceSymbol, int period)
            : this($"A(${period})", targetSymbol, referenceSymbol, period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value of this indicator on this time step.
        /// It can be either from the target or the reference symbol</param>
        /// <returns>The Alpha value of the target used in relation with the reference</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            ROC.Update(input.Time, input.Value);
            BetaIndicator.Update(input);

            _alpha = ROC.Current.Value - (decimal) RiskFree - (BetaIndicator.Current.Value * input.Value);
           
            return _alpha;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _alpha = 0;
            base.Reset();
        }
    }
}
