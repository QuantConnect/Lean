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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Correlation Indicator is a valuable tool in technical analysis, designed to quantify the degree of
    /// relationship between the price movements of a target security (e.g., a stock or ETF) and a reference
    /// market index. It measures how closely the target’s price changes are aligned with the fluctuations of
    /// the index over a specific period of time, providing insights into the target’s susceptibility to market
    /// movements.
    /// A positive correlation indicates that the target tends to move in the same direction as the market index,
    /// while a negative correlation suggests an inverse relationship. A correlation close to 0 implies a weak or
    /// no linear relationship.
    /// Commonly, the SPX index is employed as the benchmark for the overall market when calculating correlation,
    /// ensuring a consistent and reliable reference point. This helps traders and investors make informed decisions
    /// regarding the risk and behavior of the target security in relation to market trends.
    ///
    /// The indicator only updates when both assets have a price for a time step. When a bar is missing for one of the assets,
    /// the indicator value fills forward to improve the accuracy of the indicator.
    /// </summary>
    public class Correlation : DualSymbolIndicator<IBaseDataBar>
    {
        /// <summary>
        /// Correlation type
        /// </summary>
        private readonly CorrelationType _correlationType;

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => TargetDataPoints.IsReady && ReferenceDataPoints.IsReady;

        /// <summary>
        /// Creates a new Correlation indicator with the specified name, target, reference,
        /// and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="correlationType">Correlation type</param>
        public Correlation(string name, Symbol targetSymbol, Symbol referenceSymbol, int period, CorrelationType correlationType = CorrelationType.Pearson)
            : base(name, targetSymbol, referenceSymbol, period)
        {
            // Assert the period is greater than two, otherwise the correlation can not be computed
            if (period < 2)
            {
                throw new ArgumentException($"Period parameter for Correlation indicator must be greater than 2 but was {period}");
            }
            _correlationType = correlationType;
        }

        /// <summary>
        /// Creates a new Correlation indicator with the specified target, reference,
        /// and period values
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="correlationType">Correlation type</param>
        public Correlation(Symbol targetSymbol, Symbol referenceSymbol, int period, CorrelationType correlationType = CorrelationType.Pearson)
            : this($"Correlation({period})", targetSymbol, referenceSymbol, period, correlationType)
        {
        }

        /// <summary>
        /// Computes the correlation value usuing symbols values
        /// correlation values assing into _correlation property
        /// </summary>
        protected override decimal ComputeIndicator()
        {
            var targetDataPoints = TargetDataPoints.Select(x => (double)x.Close);
            var referenceDataPoints = ReferenceDataPoints.Select(x => (double)x.Close);
            var newCorrelation = 0d;
            if (_correlationType == CorrelationType.Pearson)
            {
                newCorrelation = MathNet.Numerics.Statistics.Correlation.Pearson(targetDataPoints, referenceDataPoints);
            }
            if (_correlationType == CorrelationType.Spearman)
            {
                newCorrelation = MathNet.Numerics.Statistics.Correlation.Spearman(targetDataPoints, referenceDataPoints);
            }
            if (newCorrelation.IsNaNOrZero())
            {
                newCorrelation = 0;
            }
            return Extensions.SafeDecimalCast(newCorrelation);
        }
    }
}
