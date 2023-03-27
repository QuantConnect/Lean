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
using QuantConnect.Logging;
using QuantConnect.Securities;
using System;
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
        /// Flag indicating whether the current algorithm is in live mode or not
        /// </summary>
        private bool _isLiveMode;

        /// <summary>
        /// Adds one or more signal export providers if argument is different than null
        /// </summary>
        /// <param name="signalExports">List of signal export providers</param>
        public SignalExportManager(params ISignalExportTarget[] signalExports)
        {
            if (signalExports.Length != 0)
            {
                AddSignalExportProviders(signalExports);
            }
        }

        /// <summary>
        /// Set live mode state of the algorithm
        /// </summary>
        /// <param name="isLiveMode"></param>
        public void SetLiveMode(bool isLiveMode)
        {
            _isLiveMode = isLiveMode;
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
        /// Sets the portfolio targets from the given algorihtm's Portfolio and sends them with the
        /// algorithm being ran to the signal exports providers already set
        /// </summary>
        /// <param name="algorithm">Algorithm being ran</param>
        /// <returns>True if the target list could be obtained from the algorithm's Portfolio and they
        /// were successfully sent to the signal export providers</returns>
        public bool SetTargetPortfolio(IAlgorithm algorithm)
        {
            if (algorithm.Portfolio == null)
            {
                Log.Trace("SignalExportManager.SetTargetPortfolio(): Portfolio was null");
                return false;
            }

            var result = true;
            result &= GetPortfolioTargets(algorithm, out PortfolioTarget[] targets);
            result &= SetTargetPortfolio(algorithm, targets);
            return result;
        }

        /// <summary>
        /// Obtains an array of portfolio targets from algorithm's Portfolio and returns them
        /// </summary>
        /// <param name="algorithm">Algorithm being ran</param>
        /// <param name="targets">An array of portfolio targets from the algorithm's Portfolio</param>
        /// <returns>True if TotalPortfolioValue was bigger than zero, false otherwise</returns>
        protected bool GetPortfolioTargets(IAlgorithm algorithm, out PortfolioTarget[] targets)
        {
            var portfolio = algorithm.Portfolio;
            targets = new PortfolioTarget[portfolio.Values.Count];
            var index = 0;

            var totalPortfolioValue = portfolio.TotalPortfolioValue;
            if (totalPortfolioValue <= 0)
            {
                Log.Error("SignalExportManager.GetPortfolioTargets(): Total portfolio value was less than or equal to 0");
                return false;
            }

            foreach (var holding in portfolio.Values)
            {
                var security = algorithm.Securities[holding.Symbol];
                var holdingPercent = Math.Abs(security.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(security, holding.Quantity))) / totalPortfolioValue;
                targets[index] = new PortfolioTarget(holding.Symbol, holdingPercent);
                ++index;
            }

            return true;
        }

        /// <summary>
        /// Sets the portfolio targets with the given entries and sends them with the algorithm
        /// being ran to the signal exports providers set, as long as the algorithm is in live mode
        /// </summary>
        /// <param name="algorithm">Algorithm being ran</param>
        /// <param name="portfolioTargets">One or more portfolio targets to be sent to the defined signal export providers</param>
        /// <returns>True if the portfolio targets could be sent to the different signal export providers successfully, false otherwise</returns>
        public bool SetTargetPortfolio(IAlgorithm algorithm, params PortfolioTarget[] portfolioTargets)
        {
            if (!_isLiveMode)
            {
                Log.Trace("SignalExportManager.SetTargetPortfolio(): The algorithm must be in live mode to send signals");
                return false;
            }

            if (portfolioTargets == null)
            {
                Log.Trace("SignalExportManager.SetTargetPortfolio(): No portfolio target given");
                return false;
            }

            var targets = new List<PortfolioTarget>(portfolioTargets);
            var signalExportTargetParameters = new SignalExportTargetParameters
            {
                Targets = targets,
                Algorithm = algorithm
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
