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
    ///
    /// The indicator only updates when both assets have a price for a time step. When a bar is missing for one of the assets,
    /// the indicator value fills forward to improve the accuracy of the indicator.
    /// </summary>
    public class Beta : DualSymbolIndicator<IBaseDataBar>
    {
        /// <summary>
        /// RollingWindow of returns of the target symbol in the given period
        /// </summary>
        private readonly RollingWindow<double> _targetReturns;

        /// <summary>
        /// RollingWindow of returns of the reference symbol in the given period
        /// </summary>
        private readonly RollingWindow<double> _referenceReturns;

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _targetReturns.IsReady && _referenceReturns.IsReady;

        /// <summary>
        /// Creates a new Beta indicator with the specified name, target, reference,
        /// and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        public Beta(string name, Symbol targetSymbol, Symbol referenceSymbol, int period)
            : base(name, targetSymbol, referenceSymbol, 2)
        {
            // Assert the period is greater than two, otherwise the beta can not be computed
            if (period < 2)
            {
                throw new ArgumentException($"Period parameter for Beta indicator must be greater than 2 but was {period}.");
            }

            _targetReturns = new RollingWindow<double>(period);
            _referenceReturns = new RollingWindow<double>(period);
            WarmUpPeriod += (period - 2) + 1;
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
        /// Computes the returns with the new given data point and the last given data point
        /// </summary>
        /// <param name="rollingWindow">The collection of data points from which we want
        /// to compute the return</param>
        /// <returns>The returns with the new given data point</returns>
        private static double GetNewReturn(IReadOnlyWindow<IBaseDataBar> rollingWindow)
        {
            return (double)(rollingWindow[0].Close.SafeDivision(rollingWindow[1].Close) - 1);
        }

        /// <summary>
        /// Computes the beta value of the target in relation with the reference
        /// using the target and reference returns
        /// </summary>
        protected override decimal ComputeIndicator()
        {
            if (TargetDataPoints.IsReady)
            {
                _targetReturns.Add(GetNewReturn(TargetDataPoints));
            }

            if (ReferenceDataPoints.IsReady)
            {
                _referenceReturns.Add(GetNewReturn(ReferenceDataPoints));
            }

            var varianceComputed = _referenceReturns.Variance();
            var covarianceComputed = _targetReturns.Covariance(_referenceReturns);

            // Avoid division with NaN or by zero
            var variance = !varianceComputed.IsNaNOrZero() ? varianceComputed : 1;
            var covariance = !covarianceComputed.IsNaNOrZero() ? covarianceComputed : 0;
            return (decimal)(covariance / variance);
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _targetReturns.Reset();
            _referenceReturns.Reset();
            base.Reset();
        }
    }
}
