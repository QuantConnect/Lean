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
using System.Linq;

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
    /// </summary>

    public class CorrelationIndicator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
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
        /// Correlation of the target used in relation with the reference
        /// </summary>
        private decimal _correlation;

        /// <summary>
        /// Period required for calcualte correlation
        /// </summary>
        private decimal _period;
        /// <summary>
        /// Correlation type
        /// </summary>
        private readonly CorrelationIndicatorType _correlationType;


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
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; private set; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _targetDataPoints.Samples >= WarmUpPeriod && _referenceDataPoints.Samples >= WarmUpPeriod;

        /// <summary>
        /// Creates a new Correlation indicator with the specified name, target, reference,  
        /// and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="correlationType">Correlation type</param>
        public CorrelationIndicator(string name, Symbol targetSymbol, Symbol referenceSymbol, int period, CorrelationIndicatorType correlationType = CorrelationIndicatorType.Pearson)
            : base(name)
        {
            // Assert the period is greater than two, otherwise the correlation can not be computed
            if (period < 2)
            {
                throw new ArgumentException($"Period parameter for Correlation indicator must be greater than 2 but was {period}");
            }

            WarmUpPeriod = period + 1;
            _period = period;

            _referenceSymbol = referenceSymbol;
            _targetSymbol = targetSymbol;

            _correlationType = correlationType;

            _targetDataPoints = new RollingWindow<decimal>(period);
            _referenceDataPoints = new RollingWindow<decimal>(period);

            _targetReturns = new RollingWindow<double>(period);
            _referenceReturns = new RollingWindow<double>(period);
        }

        /// <summary>
        /// Creates a new Correlation indicator with the specified target, reference,  
        /// and period values
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="correlationType">Correlation type</param>
        public CorrelationIndicator(Symbol targetSymbol, Symbol referenceSymbol, int period, CorrelationIndicatorType correlationType = CorrelationIndicatorType.Pearson)
            : this($"B({period})", targetSymbol, referenceSymbol, period, correlationType)
        {
        }

        /// <summary>
        /// Creates a new Correlation indicator with the specified name, period, target and 
        /// reference values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="correlationType">Correlation type</param>
        /// <remarks>Constructor overload for backward compatibility.</remarks>
        public CorrelationIndicator(string name, int period, Symbol targetSymbol, Symbol referenceSymbol, CorrelationIndicatorType correlationType = CorrelationIndicatorType.Pearson)
            : this(name, targetSymbol, referenceSymbol, period, correlationType)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// 
        /// As this indicator is receiving data points from two different symbols,
        /// it's going to compute the next value when the amount of data points
        /// of each of them is the same. Otherwise, it will return the last correlation
        /// value computed
        /// </summary>
        /// <param name="input">The input value of this indicator on this time step.
        /// It can be either from the target or the reference symbol</param>
        /// <returns>The correlation value of the target used in relation with the reference</returns>
        protected override decimal ComputeNextValue(TradeBar input)
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

                ComputeCorrelation();
            }
            return _correlation;
        }

        /// <summary>
        /// Computes the returns with the new given data point and the last given data point
        /// </summary>
        /// <param name="rollingWindow">The collection of data points from which we want
        /// to compute the return</param>
        /// <returns>The returns with the new given data point</returns>
        private static double GetNewReturn(RollingWindow<decimal> rollingWindow)
        {
            return (double) ((rollingWindow[0] / rollingWindow[1]) - 1);
        }

        /// <summary>
        /// Computes the correlation value of the target in relation with the reference
        /// using the target and reference returns
        /// </summary>
        private void ComputeCorrelation()
        {

            if (_targetDataPoints.Count < _period || _referenceDataPoints.Count < _period)
            {
                _correlation = 0;
                return;
            }
            double _corr = 0;
           

            if (_correlationType == CorrelationIndicatorType.Pearson)
            {
                _corr = Correlation.Pearson(_targetDataPoints.Select(d=>(double)d).ToList(), _referenceDataPoints.Select(d => (double)d).ToList());
            }
            if (_correlationType == CorrelationIndicatorType.Spearman)
            {
                _corr = Correlation.Spearman(_targetDataPoints.Select(d => (double)d).ToList(), _referenceDataPoints.Select(d => (double)d).ToList());
            }
            if (_corr.IsNaNOrZero())
            {
                _corr = 0;
            }
            _correlation = (decimal)_corr;
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
            _correlation = 0;
            base.Reset();
        }
    }
}
