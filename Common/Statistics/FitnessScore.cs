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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Implements a fitness score calculator needed to account for strategy volatility,
    /// returns, drawdown, and factor in the turnover to ensure the algorithm engagement
    /// is statistically significant
    /// </summary>
    /// <remarks>See https://www.quantconnect.com/research/3bc40ecee68d36a9424fbd1b338eb227 </remarks>
    public class FitnessScore
    {
        private DateTime _startUtcTime;
        private IAlgorithm _algorithm;
        private decimal _riskFreeRate;
        private bool _disabled;
        private decimal _startingPortfolioValue;

        // sortino ratio
        private List<double> _losses;
        private int _previousClosedTradeIndex;
        private double _profitLossDownsideDeviation;

        // return over max drawdown
        private decimal _maxPortfolioValue;
        private decimal _maxDrawdown;

        // portfolio turn over
        private Dictionary<DateTime, decimal> _saleVolumes;
        private Dictionary<DateTime, decimal> _portfolioValue;
        private decimal _previousSaleVolume;

        /// <summary>
        /// Initializes the fitness score instance and sets the initial portfolio value
        /// </summary>
        public void Initialize(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
            _maxPortfolioValue = _startingPortfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            _startUtcTime = _algorithm.UtcTime;

            // just in case...
            if (_startingPortfolioValue == 0)
            {
                _disabled = true;
                Log.Error("FitnessScore.Initialize(): fitness score will not be calculated because the" +
                    " algorithms starting portfolio value is 0.");
            }

            _losses = new List<double>();
            _saleVolumes = new Dictionary<DateTime, decimal>();
            _portfolioValue = new Dictionary<DateTime, decimal>();
            _riskFreeRate = PortfolioStatistics.GetRiskFreeRate();
        }

        /// <summary>
        /// Gets the fitness score value for the algorithms current state
        /// </summary>
        public decimal GetFitnessScore()
        {
            try
            {
                if (!_disabled)
                {
                    var currentPortfolioValue = _algorithm.Portfolio.TotalPortfolioValue;

                    // calculate portfolio annualized return
                    var currentPortfolioReturn = (currentPortfolioValue - _startingPortfolioValue) / _startingPortfolioValue;
                    var annualFactor = (_algorithm.UtcTime - _startUtcTime).TotalDays.SafeDecimalCast() / 365;

                    // just in case...
                    if (annualFactor == 0)
                    {
                        return 0;
                    }

                    var portfolioAnnualizedReturn = currentPortfolioReturn / annualFactor;

                    var sortinoRatio = GetSortinoRatio(portfolioAnnualizedReturn);

                    var returnOverMaxDrawdown = GetReturnOverMaxDrawdown(currentPortfolioValue, portfolioAnnualizedReturn);

                    var portfolioTurnover = GetPortfolioTurnover(currentPortfolioValue);

                    var rawFitnessScore = portfolioTurnover * (returnOverMaxDrawdown + sortinoRatio);

                    return Math.Round(ScaleToRange(rawFitnessScore, maximumValue: 10, minimumValue: -10), 3);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }

            return 0;
        }

        private decimal GetSortinoRatio(decimal portfolioAnnualizedReturn)
        {
            var closedTrades = _algorithm.TradeBuilder.ClosedTrades;
            if (_previousClosedTradeIndex < closedTrades.Count)
            {
                var updateStandardDeviation = false;
                for (; _previousClosedTradeIndex < closedTrades.Count; _previousClosedTradeIndex++)
                {
                    if (closedTrades[_previousClosedTradeIndex].ProfitLoss < 0)
                    {
                        updateStandardDeviation = true;
                        _losses.Add((double)closedTrades[_previousClosedTradeIndex].ProfitLoss);
                    }
                }

                // we update the profit loss downside deviation only when a new loss trade was closed
                if (updateStandardDeviation)
                {
                    var variance = _losses.Variance();
                    _profitLossDownsideDeviation = Math.Sqrt(variance);
                }
            }

            var sortinoRatio = decimal.MaxValue;
            if (_losses.Count > 1)
            {
                sortinoRatio = (portfolioAnnualizedReturn - _riskFreeRate) / (decimal)_profitLossDownsideDeviation;
            }

            return SigmoidalScale(sortinoRatio);
        }

        private decimal GetReturnOverMaxDrawdown(decimal currentPortfolioValue, decimal portfolioAnnualizedReturn)
        {
            if (currentPortfolioValue > _maxPortfolioValue)
            {
                _maxPortfolioValue = currentPortfolioValue;
            }

            var currentDrawdown = currentPortfolioValue / _maxPortfolioValue - 1;

            _maxDrawdown = currentDrawdown < _maxDrawdown ? currentDrawdown : _maxDrawdown;

            var returnOverMaxDrawdown = decimal.MaxValue;
            if (_maxDrawdown != 0)
            {
                returnOverMaxDrawdown = portfolioAnnualizedReturn / Math.Abs(_maxDrawdown);
            }

            return SigmoidalScale(returnOverMaxDrawdown);
        }

        private decimal GetPortfolioTurnover(decimal currentPortfolioValue)
        {
            var totalSalesVolume = _algorithm.Portfolio.TotalSaleVolume;

            // save the delta sales volume so we can take into account just the last year
            var salesDelta = totalSalesVolume - _previousSaleVolume;
            if (salesDelta > 0)
            {
                _saleVolumes[_algorithm.Time] = salesDelta;
            }
            _previousSaleVolume = totalSalesVolume;
            // save current portfolio value so we use the annual average
            _portfolioValue[_algorithm.Time] = currentPortfolioValue;

            // remove old values
            var year = TimeSpan.FromDays(365);
            _saleVolumes = _saleVolumes
                .Where(pair => (_algorithm.Time - pair.Key) < year)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            _portfolioValue = _portfolioValue
                .Where(pair => (_algorithm.Time - pair.Key) < year)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            var averagePortfolioValue = _portfolioValue.Values.Average();
            var saleVolume = _saleVolumes.Values.Sum();

            var rawPortfolioTurnOver = saleVolume / averagePortfolioValue;

            // from 0 to 1 max
            return rawPortfolioTurnOver > 1 ? 1 : rawPortfolioTurnOver;
        }

        private decimal SigmoidalScale(decimal valueToScale)
        {
            if (valueToScale == decimal.MaxValue)
            {
                return 5;
            }
            return 5 * valueToScale / (decimal)Math.Sqrt(10 + Math.Pow((double)valueToScale, 2));
        }

        private decimal ScaleToRange(decimal valueToScale,
            decimal maximumValue,
            decimal minimumValue)
        {
            return (valueToScale - minimumValue) / (maximumValue - minimumValue);
        }
    }
}
