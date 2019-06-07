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
        private List<decimal> _dailyPortfolioTurnovers;
        private decimal _previousSalesVolume;

        /// <summary>
        /// Initializes the fitness score instance and sets the initial portfolio value
        /// </summary>
        public void Initialize(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
            _maxPortfolioValue = _previousPortfolioValue = _startingPortfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            _startUtcTime = _algorithm.UtcTime;

            // just in case...
            if (_startingPortfolioValue == 0)
            {
                _disabled = true;
                Log.Error("FitnessScore.Initialize(): fitness score will not be calculated because the" +
                    " algorithms starting portfolio value is 0.");
            }

            _negativeDailyDeltaPortfolioValue = new List<double>();
            _dailyPortfolioTurnovers = new List<decimal>();
            _riskFreeRate = PortfolioStatistics.GetRiskFreeRate();
        }

        /// <summary>
        /// Score of the strategy's performance, and suitability for the Alpha Stream Market
        /// </summary>
        public decimal FitnessScore { get; private set; }

        /// <summary>
        /// Measurement of the strategies trading activity with respect to the portfolio value.
        /// Calculated as the sales volume with respect to the average total portfolio value.
        /// </summary>
        public decimal PortfolioTurnover { get; private set; }

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
                    var annualFactor = (decimal)(_algorithm.UtcTime - _startUtcTime).TotalDays / 365m;

                    // just in case...
                    if (annualFactor <= 0)
                    {
                        return;
                    }

                    var portfolioAnnualizedReturn = Statistics.CompoundingAnnualPerformance(_startingPortfolioValue, currentPortfolioValue, annualFactor);

                    var scaledSortinoRatio = GetScaledSortinoRatio(currentPortfolioValue, portfolioAnnualizedReturn);

                    var scaledReturnOverMaxDrawdown = GetScaledReturnOverMaxDrawdown(currentPortfolioValue, portfolioAnnualizedReturn);

                    var scaledPortfolioTurnover = GetScaledPortfolioTurnover(currentPortfolioValue);

                    var rawFitnessScore = scaledPortfolioTurnover * (scaledReturnOverMaxDrawdown + scaledSortinoRatio);

                    FitnessScore = ScaleToRange(rawFitnessScore, maximumValue: 20, minimumValue: 0);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        private decimal GetScaledSortinoRatio(decimal currentPortfolioValue, decimal portfolioAnnualizedReturn)
        {
            var portfolioValueDelta = (double) ((currentPortfolioValue - _previousPortfolioValue) / _previousPortfolioValue);
            _previousPortfolioValue = currentPortfolioValue;
            if (portfolioValueDelta < 0)
            {
                _negativeDailyDeltaPortfolioValue.Add(portfolioValueDelta);
                _profitLossDownsideDeviation = _negativeDailyDeltaPortfolioValue.StandardDeviation();
                // annualize the result:
                _profitLossDownsideDeviation = _profitLossDownsideDeviation * Math.Sqrt(252);
            }

            SortinoRatio = decimal.MaxValue;
            // we need at least 2 samples to calculate the _profitLossDownsideDeviation
            if (_negativeDailyDeltaPortfolioValue.Count > 1)
            {
                if (_profitLossDownsideDeviation == 0)
                {
                    SortinoRatio = (portfolioAnnualizedReturn - _riskFreeRate) > 0 ? decimal.MaxValue : decimal.MinValue;
                }
                else
                {
                    SortinoRatio = ((double) (portfolioAnnualizedReturn - _riskFreeRate) / _profitLossDownsideDeviation).SafeDecimalCast();
                }
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
            var currentTotalSaleVolume = _algorithm.Portfolio.TotalSaleVolume;

            var todayPortfolioTurnOver = (currentTotalSaleVolume - _previousSalesVolume) / currentPortfolioValue;
            _previousSalesVolume = currentTotalSaleVolume;

            _dailyPortfolioTurnovers.Add(todayPortfolioTurnOver);
            PortfolioTurnover = _dailyPortfolioTurnovers.Average();

            // from 0 to 1 max
            return PortfolioTurnover > 1 ? 1 : PortfolioTurnover;
        }

        /// <summary>
        /// Adjusts the input value to a range of 0 to 10 based on a sigmoidal scale
        /// </summary>
        public static decimal SigmoidalScale(decimal valueToScale)
        {
            if (valueToScale == decimal.MaxValue)
            {
                return 10;
            }
            else if(valueToScale == decimal.MinValue)
            {
                return 0;
            }
            return 5 * valueToScale / (decimal)Math.Sqrt(10 + Math.Pow((double)valueToScale, 2)) + 5;
        }

        private decimal ScaleToRange(decimal valueToScale,
            decimal maximumValue,
            decimal minimumValue)
        {
            return (valueToScale - minimumValue) / (maximumValue - minimumValue);
        }
    }
}
