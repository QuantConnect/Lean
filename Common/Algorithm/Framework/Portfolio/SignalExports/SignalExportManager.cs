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

using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Class manager to send portfolio targets to different 3rd party API's
    /// For example, it allows Collective2, CrunchDAO and Numerai signal export providers
    /// </summary>
    public class SignalExportManager
    {
        /// <summary>
        /// List of signal export providers
        /// </summary>
        private List<ISignalExportTarget> _signalExports;

        /// <summary>
        /// Algorithm being ran
        /// </summary>
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Flag to indicate if the user has tried to send signals with live mode off
        /// </summary>
        private bool _isLiveWarningModeLog;

        /// <summary>
        /// SignalExportManager Constructor, obtains the entry information needed to send signals
        /// and initializes the fields to be used
        /// </summary>
        /// <param name="algorithm">Algorithm being run</param>
        public SignalExportManager(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
            _isLiveWarningModeLog = false;
        }

        /// <summary>
        /// Adds one or more new signal exports providers
        /// </summary>
        /// <param name="signalExports">One or more signal export provider</param>
        public void AddSignalExportProviders(params ISignalExportTarget[] signalExports)
        {
            _signalExports ??= new List<ISignalExportTarget>();

            _signalExports.AddRange(signalExports);
        }

        /// <summary>
        /// Sets the portfolio targets from the algorihtm's Portfolio and sends them with the
        /// algorithm being ran to the signal exports providers already set
        /// </summary>
        /// <returns>True if the target list could be obtained from the algorithm's Portfolio and they
        /// were successfully sent to the signal export providers</returns>
        public bool SetTargetPortfolioFromPortfolio()
        {
            if (!GetPortfolioTargets(out PortfolioTarget[] targets))
            {
                return false;
            }
            var result = SetTargetPortfolio(targets);
            return result;
        }

        /// <summary>
        /// Obtains an array of portfolio targets from algorithm's Portfolio and returns them.
        /// See  <see cref="PortfolioTarget.Percent(IAlgorithm, Symbol, decimal, bool)"/> for more
        /// information about how each symbol quantity was calculated
        /// </summary>
        /// <param name="targets">An array of portfolio targets from the algorithm's Portfolio</param>
        /// <returns>True if TotalPortfolioValue was bigger than zero, false otherwise</returns>
        protected bool GetPortfolioTargets(out PortfolioTarget[] targets)
        {
            var portfolio = _algorithm.Portfolio;
            targets = new PortfolioTarget[portfolio.Values.Count];
            var index = 0;

            var totalPortfolioValue = portfolio.TotalPortfolioValue;
            if (totalPortfolioValue <= 0)
            {
                _algorithm.Error("Total portfolio value was less than or equal to 0");
                return false;
            }

            foreach (var holding in portfolio.Values)
            {
                var security = _algorithm.Securities[holding.Symbol];
                var marginParameters = MaintenanceMarginParameters.ForQuantityAtCurrentPrice(security, holding.Quantity);
                var adjustedPercent = security.BuyingPowerModel.GetMaintenanceMargin(marginParameters) / totalPortfolioValue;
                // See PortfolioTarget.Percent:
                // we normalize the target buying power by the leverage so we work in the land of margin
                var holdingPercent = adjustedPercent * security.BuyingPowerModel.GetLeverage(security);

                // FreePortfolioValue is used for orders not to be rejected due to volatility when using SetHoldings and CalculateOrderQuantity
                // Then, we need to substract its value from the TotalPortfolioValue and obtain again the holding percentage for our holding
                var adjustedHoldingPercent = (holdingPercent * totalPortfolioValue) / _algorithm.Portfolio.TotalPortfolioValueLessFreeBuffer;
                if (holding.Quantity < 0)
                {
                    adjustedHoldingPercent *= -1;
                }

                targets[index] = new PortfolioTarget(holding.Symbol, adjustedHoldingPercent);
                ++index;
            }

            return true;
        }

        /// <summary>
        /// Sets the portfolio targets with the given entries and sends them with the algorithm
        /// being ran to the signal exports providers set, as long as the algorithm is in live mode
        /// </summary>
        /// <param name="portfolioTargets">One or more portfolio targets to be sent to the defined signal export providers</param>
        /// <returns>True if the portfolio targets could be sent to the different signal export providers successfully, false otherwise</returns>
        public bool SetTargetPortfolio(params PortfolioTarget[] portfolioTargets)
        {
            if (!_algorithm.LiveMode)
            {
                if (!_isLiveWarningModeLog)
                {
                    _algorithm.Debug("Portfolio targets are only sent in live mode");
                    _isLiveWarningModeLog = true;
                }

                return true;
            }

            if (portfolioTargets == null || portfolioTargets.Length == 0)
            {
                _algorithm.Debug("No portfolio target given");
                return false;
            }

            var targets = new List<PortfolioTarget>(portfolioTargets);
            var signalExportTargetParameters = new SignalExportTargetParameters
            {
                Targets = targets,
                Algorithm = _algorithm
            };

            var result = true;
            foreach (var signalExport in _signalExports)
            {
                result &= signalExport.Send(signalExportTargetParameters);
            }

            return result;
        }
    }
}
