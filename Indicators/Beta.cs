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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// In technical analysis Beta indicator is used to measure volatility or risk of a target (ETF) relative to the overall 
    /// risk (volatility) of the reference (market indexes). The Beta indicators compares target's price movement to the 
    /// movements of the indexes over the same period of time.
    /// 
    /// It is common practice to use the SPX index as a benchmark of the overall reference market when it comes to Beta 
    /// calculations.
    /// </summary>
    public class Beta : BarIndicator, IIndicatorWarmUpPeriodProvider
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
        /// RollingWindow of returns of the target symbol in the given period
        /// </summary>
        private readonly RollingWindow<double> _targetReturns;

        /// <summary>
        /// RollingWindow of returns of the reference symbol in the given period
        /// </summary>
        private readonly RollingWindow<double> _referenceReturns;

        /// <summary>
        /// Beta of the target used in relation with the reference
        /// </summary>
        private decimal _beta;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; private set; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _targetDataPoints.Samples >= WarmUpPeriod && _referenceDataPoints.Samples >= WarmUpPeriod;

        /// <summary>
        /// Creates a new Beta indicator with the specified name, target, reference,  
        /// and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        public Beta(string name, Symbol targetSymbol, Symbol referenceSymbol, int period)
            : base(name)
        {
            // Assert the period is greater than two, otherwise the beta can not be computed
            if (period < 2)
            {
                throw new ArgumentException($"Period parameter for Beta indicator must be greater than 2 but was {period}");
            }

            WarmUpPeriod = period + 1;
            _referenceSymbol = referenceSymbol;
            _targetSymbol = targetSymbol;

            _targetDataPoints = new RollingWindow<decimal>(2);
            _referenceDataPoints = new RollingWindow<decimal>(2);

            _targetReturns = new RollingWindow<double>(period);
            _referenceReturns = new RollingWindow<double>(period);
            _beta = 0;
        }

        /// <summary>
        /// Creates a new Beta indicator with the specified target, reference,  
        /// and period values
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        public Beta(Symbol targetSymbol, Symbol referenceSymbol, int period)
            : this($"B({period})", targetSymbol, referenceSymbol, period)
        {
        }

        /// <summary>
        /// Creates a new Beta indicator with the specified name, period, target and 
        /// reference values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <remarks>Constructor overload for backward compatibility.</remarks>
        public Beta(string name, int period, Symbol targetSymbol, Symbol referenceSymbol)
            : this(name, targetSymbol, referenceSymbol, period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// 
        /// As this indicator is receiving data points from two different symbols,
        /// it's going to compute the next value when the amount of data points
        /// of each of them is the same. Otherwise, it will return the last beta
        /// value computed
        /// </summary>
        /// <param name="input">The input value of this indicator on this time step.
        /// It can be either from the target or the reference symbol</param>
        /// <returns>The beta value of the target used in relation with the reference</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var inputSymbol = input.Symbol;
            if (inputSymbol == _targetSymbol)
            {
                _targetDataPoints.Add(input.Close);
            } 
            else if(inputSymbol == _referenceSymbol)
            {
                _referenceDataPoints.Add(input.Close);
            }
            else
            {
                throw new ArgumentException("The given symbol was not target or reference symbol");
            }

            if (_targetDataPoints.Samples == _referenceDataPoints.Samples && _referenceDataPoints.Count > 1)
            {
                _targetReturns.Add(GetNewReturn(_targetDataPoints));
                _referenceReturns.Add(GetNewReturn(_referenceDataPoints));

                ComputeBeta();
            }
            return _beta;
        }

        /// <summary>
        /// Computes the returns with the new given data point and the last given data point
        /// </summary>
        /// <param name="rollingWindow">The collection of data points from which we want
        /// to compute the return</param>
        /// <returns>The returns with the new given data point</returns>
        private static double GetNewReturn(RollingWindow<decimal> rollingWindow)
        {
            return (double) ((rollingWindow[0].SafeDivision(rollingWindow[1]) - 1));
        }

        /// <summary>
        /// Computes the beta value of the target in relation with the reference
        /// using the target and reference returns
        /// </summary>
        private void ComputeBeta()
        {
            var varianceComputed = _referenceReturns.Variance();
            var covarianceComputed = _targetReturns.Covariance(_referenceReturns);

            // Avoid division with NaN or by zero
            var variance = !varianceComputed.IsNaNOrZero() ? varianceComputed : 1;
            var covariance = !covarianceComputed.IsNaNOrZero() ? covarianceComputed : 0;
            _beta = (decimal) (covariance / variance);
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _targetDataPoints.Reset();
            _referenceDataPoints.Reset();

            _targetReturns.Reset();
            _referenceReturns.Reset();
            _beta = 0;
            base.Reset();
        }
    }
}
