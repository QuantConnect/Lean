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
        private bool _disabled;
        private decimal _startingPortfolioValue;

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
            _maxPortfolioValue = _startingPortfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            _startUtcTime = _algorithm.UtcTime;

            // just in case...
            if (_startingPortfolioValue == 0)
            {
                _disabled = true;
                Log.Error("FitnessScore.Initialize(): fitness score will not be calculated because the" +
                    " algorithms starting portfolio value is 0.");
            }

            _dailyPortfolioTurnovers = new List<decimal>();
        }

        /// <summary>
        /// Measurement of the strategies trading activity with respect to the portfolio value.
        /// Calculated as the sales volume with respect to the average total portfolio value.
        /// </summary>
        public decimal PortfolioTurnover { get; private set; }

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

                    SetScaledReturnOverMaxDrawdown(currentPortfolioValue, portfolioAnnualizedReturn);
                    SetScaledPortfolioTurnover(currentPortfolioValue);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        private void SetScaledReturnOverMaxDrawdown(decimal currentPortfolioValue, decimal portfolioAnnualizedReturn)
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
        }

        private void SetScaledPortfolioTurnover(decimal currentPortfolioValue)
        {
            var currentTotalSaleVolume = _algorithm.Portfolio.TotalSaleVolume;

            var todayPortfolioTurnOver = (currentTotalSaleVolume - _previousSalesVolume) / currentPortfolioValue;
            _previousSalesVolume = currentTotalSaleVolume;

            _dailyPortfolioTurnovers.Add(todayPortfolioTurnOver);
            PortfolioTurnover = _dailyPortfolioTurnovers.Average();
        }
    }
}
