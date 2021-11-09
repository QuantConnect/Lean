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
    /// In technical analysis Beta indicator is used to measure volatility or risk of a stock (ETF) relative to the overall 
    /// risk (volatility) of the stock market (market indexes). The Beta indicators compares stock's price movement to the 
    /// movements of the indexes over the same period of time.
    /// 
    /// It is common practice to use the S&P 500 index as a benchmark of the overall stock market when it comes to Beta 
    /// calculations.
    /// </summary>
    public class BetaIndicator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Dictionary to get the data points collection of the stock symbol
        /// or the Market Index symbol
        /// </summary>
        private Dictionary<Symbol, RollingWindow<decimal>> _symbolDataPoints;

        /// <summary>
        /// Symbol of the Market Index used
        /// </summary>
        private Symbol _marketIndexSymbol;

        /// <summary>
        /// Symbol of the stock used
        /// </summary>
        private Symbol _stockSymbol;

        /// <summary>
        /// RollingWindow of returns of the stock symbol in the given period
        /// </summary>
        private RollingWindow<decimal> _stockReturns;

        /// <summary>
        /// RollingWindow of returns of the Market Index symbol in the given period
        /// </summary>
        private RollingWindow<decimal> _marketIndexReturns;

        /// <summary>
        /// Beta of the stock used in relation with the Market Index
        /// </summary>
        private decimal _beta;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; private set; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _symbolDataPoints[_stockSymbol].Samples == _symbolDataPoints[_marketIndexSymbol].Samples && _symbolDataPoints[_marketIndexSymbol].Count == WarmUpPeriod;

        /// <summary>
        /// Creates a new BetaIndicator with the specified name, period, stock and 
        /// Market Index values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="stockSymbol">The stock symbol of this indicator</param>
        /// <param name="marketIndexSymbol">The Market Index symbol of this indicator</param>
        public BetaIndicator(string name, int period, Symbol stockSymbol, Symbol marketIndexSymbol)
            : base(name)
        {
            WarmUpPeriod = period;
            _marketIndexSymbol = marketIndexSymbol;
            _stockSymbol = stockSymbol;

            _symbolDataPoints = new Dictionary<Symbol, RollingWindow<decimal>>();
            _symbolDataPoints.Add(_marketIndexSymbol, new RollingWindow<decimal>(WarmUpPeriod));
            _symbolDataPoints.Add(_stockSymbol, new RollingWindow<decimal>(WarmUpPeriod));

            _stockReturns = new RollingWindow<decimal>(WarmUpPeriod);
            _marketIndexReturns = new RollingWindow<decimal>(WarmUpPeriod);
            _beta = 0;
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
        /// It can be either from the stock or the Market index symbol</param>
        /// <returns>The beta value of the stock used in relation with the Market Index</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            var inputSymbol = input.Symbol;
            _symbolDataPoints[inputSymbol].Add(input.Close);

            if (_symbolDataPoints[_stockSymbol].Samples == _symbolDataPoints[_marketIndexSymbol].Samples && _symbolDataPoints[_stockSymbol].Samples > 1)
            {
                _stockReturns.Add(GetNewReturn(_symbolDataPoints[_stockSymbol]));
                _marketIndexReturns.Add(GetNewReturn(_symbolDataPoints[_marketIndexSymbol]));

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
        private static decimal GetNewReturn(RollingWindow<decimal> rollingWindow)
        {
            return (rollingWindow[1] / rollingWindow[0]) - 1;
        }

        /// <summary>
        /// Computes the beta value of the stock in relation with the Market Index
        /// using the stock and Market Index returns
        /// </summary>
        private void ComputeBeta()
        {
            // Zip both list into a single one to loop over their values at the same time

            // Initialize the variables used to compute the covariance between the stock returns and the Market Index returns
            // and to compute the variance of the Market Index returns
            var covariance = 0m;
            var variance = 0m;

            // Get the average of both lists
            var stockAverage = _stockReturns.Average();
            var marketIndexAverage = _marketIndexReturns.Average();

            // Compute the covariance of the stock returns and the Market Index returns
            // Compute also the variance of the Market Index returns
            for (int i = 0; i < _stockReturns.Count; i++)
            {
                covariance += (_stockReturns[i] - stockAverage) * (_marketIndexReturns[i] - marketIndexAverage);
                variance += (decimal) Math.Pow((double) (_marketIndexReturns[i] - marketIndexAverage), 2);
            }

            covariance = covariance / _stockReturns.Count;

            // Avoid division by zero
            variance = variance != 0 ? variance / _marketIndexReturns.Count : 1;

            _beta = covariance / variance;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _symbolDataPoints[_stockSymbol].Reset();
            _symbolDataPoints[_marketIndexSymbol].Reset();

            _stockReturns.Reset();
            _marketIndexReturns.Reset();
            _beta = 0;
            base.Reset();
        }
    }
}
