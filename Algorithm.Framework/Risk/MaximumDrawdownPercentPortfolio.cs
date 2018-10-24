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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Risk
{
    /// <summary>
    /// Provides an implementation of <see cref="IRiskManagementModel"/> that limits the drawdown
    /// of the portfolio to the specified percentage - it terminates the algorithm if this limt is reached
    /// </summary>
    public class MaximumDrawdownPercentPortfolio : RiskManagementModel
    {
        private readonly decimal _maximumDrawdownPercent;
        private decimal _startingValue;
        private bool _initialised = false;
        private string _email;
        private string _phoneNumber;


        /// <summary>
        /// Initializes a new instance of the <see cref="MaximumDrawdownPercentPortfolio"/> class
        /// </summary>
        /// <param name="maximumDrawdownPercent">The maximum percentage drawdown allowed for algorithm portfolio
        /// compared with starting value, defaults to 5% drawdown</param>
        /// <param name="email">Optional - Email address for Algorithm Termination Notifications</param>
        /// <param name="phoneNumber">Optional - Phone Number for Algorithm Termination SMS Notifications</param>
        public MaximumDrawdownPercentPortfolio(
            decimal maximumDrawdownPercent = 0.05m, string email = "", string phoneNumber = ""
            )
        {
            _maximumDrawdownPercent = -Math.Abs(maximumDrawdownPercent);
            _email = email;
            _phoneNumber = phoneNumber;
        }

        /// <summary>
        /// Manages the algorithm's risk at each time step
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The current portfolio targets to be assessed for risk</param>
        public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithmFramework algorithm, IPortfolioTarget[] targets)
        {
            if(!_initialised)
            {
                _startingValue = algorithm.Portfolio.TotalPortfolioValue; // Set inital portfolio value
                _initialised = true;
            }

            var pnl = TotalDrawdownPercent(algorithm.Portfolio);
            if (pnl < _maximumDrawdownPercent)
            {
                var message = $"Maximum Portfolio Drawdown Reached - Drawdown: {pnl.SmartRounding()}%, Drawdown Limit: {_maximumDrawdownPercent.SmartRounding()}%";
                algorithm.Log(message);

                var subject = "Terminating Algorithm";
                algorithm.Error(subject);

                // Inform user
                if (_email != "") algorithm.Notify.Email(_email, subject, message);
                if (_phoneNumber != "") algorithm.Notify.Sms(_phoneNumber, subject + ", " + message);

                KillAlgorithm(algorithm);
            }

            return targets;
        }

        private decimal TotalDrawdownPercent(SecurityPortfolioManager portfolio)
        {
            var currentValue = portfolio.TotalPortfolioValue;
            return (currentValue / _startingValue) - 1.0m;            
        }

        private decimal TotalDrawdownValue(SecurityPortfolioManager portfolio)
        {
            return portfolio.TotalProfit + portfolio.TotalUnrealisedProfit;
        }

        private void KillAlgorithm(QCAlgorithmFramework algorithm)
        {
            // Liqudate
            algorithm.Log($"Liquidating All Holdings");
            algorithm.Liquidate();

            // Stop algorithm
            algorithm.Quit("Terminated Algorithm due to Drawdown");
        }

        
    }
}