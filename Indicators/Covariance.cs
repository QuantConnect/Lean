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
using MathNet.Numerics.Statistics;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// TODO: figure out what to write here
    /// </summary>
    public class Covariance : BarIndicator, IIndicatorWarmUpPeriodProvider
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
        /// Symbol of the reference used
        /// </summary>
        private readonly Symbol _referenceSymbol;

        /// <summary>
        /// Symbol of the target used
        /// </summary>
        private readonly Symbol _targetSymbol;

        /// <summary>
        /// Covariance of the target used in relation with the reference
        /// </summary>
        private decimal _covariance;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; private set; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _targetDataPoints.Samples >= WarmUpPeriod && _referenceDataPoints.Samples >= WarmUpPeriod;


        /// <summary>
        /// Creates a new Covariance indicator with the specified name, target, reference,  
        /// and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        public Covariance(string name, Symbol targetSymbol, Symbol referenceSymbol, int period)
            : base(name)
        {
            // Assert the period is greater than two, otherwise the covariance can not be computed
            if (period < 2)
            {
                throw new ArgumentException($"Period parameter for Covariance indicator must be greater than 2 but was {period}");
            }

            WarmUpPeriod = period + 1;
            _referenceSymbol = referenceSymbol;
            _targetSymbol = targetSymbol;

            _targetDataPoints = new RollingWindow<decimal>(period);
            _referenceDataPoints = new RollingWindow<decimal>(period);

            _covariance = 0;
        }

        /// <summary>
        /// Creates a new Covariance indicator with the specified target, reference,  
        /// and period values
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        public Covariance(Symbol targetSymbol, Symbol referenceSymbol, int period)
            : this($"COV({period})", targetSymbol, referenceSymbol, period)
        {
        }

        /// <summary>
        /// Creates a new Covariance indicator with the specified name, period, target and 
        /// reference values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <remarks>Constructor overload for backward compatibility.</remarks>
        public Covariance(string name, int period, Symbol targetSymbol, Symbol referenceSymbol)
            : this(name, targetSymbol, referenceSymbol, period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// 
        /// As this indicator is receiving data points from two different symbols,
        /// it's going to compute the next value when the amount of data points
        /// of each of them is the same. Otherwise, it will return the last covariance
        /// value computed
        /// </summary>
        /// <param name="input">The input value of this indicator on this time step.
        /// It can be either from the target or the reference symbol</param>
        /// <returns>The covariance value of the target used in relation with the reference</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var inputSymbol = input.Symbol;
            if (inputSymbol == _targetSymbol)
            {
                _targetDataPoints.Add(input.Value);
            }
            else if (inputSymbol == _referenceSymbol)
            {
                _referenceDataPoints.Add(input.Value);
            }
            else
            {
                throw new ArgumentException("The given symbol was not target or reference symbol");
            }

            if (_targetDataPoints.Samples == _referenceDataPoints.Samples && _referenceDataPoints.Count > 1)
            {
                ComputeCovariance();
            }
            
            return _covariance;

        }

        /// <summary>
        /// Computes the covariance value of the target in relation with the reference
        /// using the target and reference returns
        /// </summary>
        private void ComputeCovariance()
        {

            if (!IsReady)
            {
                _covariance = 0m;
                return;
            }

            decimal mean1 = 0;
            decimal mean2 = 0;
            decimal sumProduct = 0;

            int windowSize = _targetDataPoints.Count;
            for (int i = 0; i < windowSize; i++)
            {
                mean1 += _targetDataPoints[i] / windowSize;
                mean2 += _referenceDataPoints[i] / windowSize;
            }

            for (int i = 0; i < windowSize; i++)
            {
                sumProduct += (_targetDataPoints[i] - mean1) * (_referenceDataPoints[i] - mean2);
            }

            _covariance = sumProduct / (windowSize - 1);

        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _targetDataPoints.Reset();
            _referenceDataPoints.Reset();

            _covariance = 0;
            base.Reset();
        }

    }
}
