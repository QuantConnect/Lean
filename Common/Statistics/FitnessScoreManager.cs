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
    public class FitnessScoreManager
    {
        private DateTime _startUtcTime;
        private IAlgorithm _algorithm;
        private decimal _riskFreeRate;
        private bool _disabled;
        private decimal _startingPortfolioValue;

        // sortino ratio
        private List<double> _negativeDailyDeltaPortfolioValue;
        private double _profitLossDownsideDeviation;
        private decimal _previousPortfolioValue;

        // return over max drawdown
        private decimal _maxPortfolioValue;
        private decimal _maxDrawdown;

        // portfolio turn over
        private List<Tuple<DateTime, decimal>> _saleVolumes;
        private List<Tuple<DateTime, decimal>> _portfolioValue;
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

            _negativeDailyDeltaPortfolioValue = new List<double>();
            _saleVolumes = new List<Tuple<DateTime, decimal>>();
            _portfolioValue = new List<Tuple<DateTime, decimal>>();
            _riskFreeRate = PortfolioStatistics.GetRiskFreeRate();
        }

        /// <summary>
        /// Score of the strategy's performance, and suitability for the Alpha Stream Market
        /// </summary>
        public decimal FitnessScore { get; private set; }

        /// <summary>
        /// Measurement of the strategies trading activity with respect to the portfolio value.
        /// Calculated as the annual sales volume with respect to the annual average total portfolio value.
        /// </summary>
        public decimal PortfolioTurnOver { get; private set; }

        /// <summary>
        /// Gives a relative picture of the strategy volatility.
        /// It is calculated by taking a portfolio's annualized rate of return and subtracting the risk free rate of return.
        /// </summary>
        public decimal SortinoRatio { get; private set; }

        /// <summary>
        /// Provides a risk adjusted way to factor in the returns and drawdown of the strategy.
        /// It is calculated by dividing the Portfolio Annualized Return by the Maximum Drawdown seen during the backtest.
        /// </summary>
        public decimal ReturnOverMaxDrawdown { get; private set; }


        /// <summary>
        /// Gets the fitness score value for the algorithms current state
        /// </summary>
        public void UpdateScores()
        {
            try
            {
                if (!_disabled)
                {
                    var currentPortfolioValue = _algorithm.Portfolio.TotalPortfolioValue;

                    // calculate portfolio annualized return
                    var currentPortfolioReturn = (currentPortfolioValue - _startingPortfolioValue) / _startingPortfolioValue;
                    var annualFactor = (_algorithm.UtcTime - _startUtcTime).TotalDays / 252;

                    // just in case...
                    if (annualFactor <= 0)
                    {
                        return;
                    }

                    var portfolioAnnualizedReturn = (Math.Pow((double)currentPortfolioReturn + 1, 1 / annualFactor) - 1).SafeDecimalCast();

                    var scaledSortinoRatio = GetScaledSortinoRatio(currentPortfolioValue, portfolioAnnualizedReturn);

                    var scaledReturnOverMaxDrawdown = GetScaledReturnOverMaxDrawdown(currentPortfolioValue, portfolioAnnualizedReturn);

                    var scaledPortfolioTurnover = GetScaledPortfolioTurnover(currentPortfolioValue);

                    var rawFitnessScore = scaledPortfolioTurnover * (scaledReturnOverMaxDrawdown + scaledSortinoRatio);

                    FitnessScore = Math.Round(ScaleToRange(rawFitnessScore, maximumValue: 10, minimumValue: -10), 3);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        private decimal GetScaledSortinoRatio(decimal currentPortfolioValue, decimal portfolioAnnualizedReturn)
        {
            var portfolioValueDelta = (double) (currentPortfolioValue - _previousPortfolioValue);
            _previousPortfolioValue = currentPortfolioValue;
            if (portfolioValueDelta < 0)
            {
                _negativeDailyDeltaPortfolioValue.Add(portfolioValueDelta);
                var variance = _negativeDailyDeltaPortfolioValue.Variance();
                _profitLossDownsideDeviation = Math.Sqrt(variance);
            }

            SortinoRatio = decimal.MaxValue;
            // we need at least 2 samples to calculate the _profitLossDownsideDeviation
            if (_negativeDailyDeltaPortfolioValue.Count > 1)
            {
                SortinoRatio = (portfolioAnnualizedReturn - _riskFreeRate) / (decimal)_profitLossDownsideDeviation;
            }

            return SigmoidalScale(SortinoRatio);
        }

        private decimal GetScaledReturnOverMaxDrawdown(decimal currentPortfolioValue, decimal portfolioAnnualizedReturn)
        {
            if (currentPortfolioValue > _maxPortfolioValue)
            {
                _maxPortfolioValue = currentPortfolioValue;
            }

            var currentDrawdown = currentPortfolioValue / _maxPortfolioValue - 1;

            _maxDrawdown = currentDrawdown < _maxDrawdown ? currentDrawdown : _maxDrawdown;

            ReturnOverMaxDrawdown = decimal.MaxValue;
            if (_maxDrawdown != 0)
            {
                ReturnOverMaxDrawdown = portfolioAnnualizedReturn / Math.Abs(_maxDrawdown);
            }

            return SigmoidalScale(ReturnOverMaxDrawdown);
        }

        private decimal GetScaledPortfolioTurnover(decimal currentPortfolioValue)
        {
            var totalSalesVolume = _algorithm.Portfolio.TotalSaleVolume;

            // save the delta sales volume so we can take into account just the last year
            var salesDelta = totalSalesVolume - _previousSaleVolume;
            if (salesDelta > 0)
            {
                _saleVolumes.Add(new Tuple<DateTime, decimal>(_algorithm.Time, salesDelta));
            }
            _previousSaleVolume = totalSalesVolume;
            // save current portfolio value so we use the annual average
            _portfolioValue.Add(new Tuple<DateTime, decimal>(_algorithm.Time, currentPortfolioValue));

            // remove old values
            var year = TimeSpan.FromDays(365);
            var index = _saleVolumes.FindIndex(tuple => _algorithm.Time - tuple.Item1 < year);
            if (index != -1 && index != 0)
            {
                _saleVolumes.RemoveRange(0, index);
            }
            index = _portfolioValue.FindIndex(tuple => _algorithm.Time - tuple.Item1 < year);
            if (index != -1 && index != 0)
            {
                _portfolioValue.RemoveRange(0, index);
            }

            var averagePortfolioValue = _portfolioValue.Average(tuple => tuple.Item2);
            var saleVolume = _saleVolumes.Sum(tuple => tuple.Item2);

            PortfolioTurnOver = saleVolume / averagePortfolioValue;

            // from 0 to 1 max
            return PortfolioTurnOver > 1 ? 1 : PortfolioTurnOver;
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
