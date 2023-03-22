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
    /// So far it only allows Collective2, CrunchDAO and Numerai signal export providers
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
        /// Set live mode state of the algorithm
        /// </summary>
        /// <param name="isLiveMode"></param>
        public void SetLiveMode(bool isLiveMode)
        {
            _isLiveMode = isLiveMode;
        }

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
        /// Adds one or more new signal exports providers
        /// </summary>
        /// <param name="signalExports">One or more signal export provider</param>
        public void AddSignalExportProviders(params ISignalExportTarget[] signalExports)
        {
            _signalExports ??= new List<ISignalExportTarget>();

            _signalExports.AddRange(signalExports);
        }

        /// <summary>
        /// Sets the portfolio targets from the given portfolio current holdings and sends them with the
        /// algorithm being ran to the signal exports providers already set
        /// </summary>
        /// <param name="algorithm">Algorithm being ran</param>
        /// <param name="portfolio">Portfolio containing the current holdings</param>
        /// <exception cref="ArgumentException">If the portfolio is null it will throw an exception as well as if the
        /// TotalPortfolioValue is negative or zero</exception>
        public void SetTargetPortfolio(IAlgorithm algorithm, SecurityPortfolioManager portfolio)
        {
            if (portfolio == null)
            {
                throw new ArgumentException("Portfolio was null");
            }

            var targets = new PortfolioTarget[portfolio.Values.Count];
            var index = 0;

            foreach (var holding in portfolio.Values)
            {
                if (portfolio.TotalPortfolioValue <= 0)
                {
                    Log.Error("Total portfolio value was less than or equal to 0");
                    return;
                }

                var security = algorithm.Securities[holding.Symbol];
                var holdingPercent = Math.Abs(security.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(security, holding.Quantity))) / portfolio.TotalPortfolioValue;
                targets[index] = new PortfolioTarget(holding.Symbol, holdingPercent);
                ++index;
            }

            SetTargetPortfolio(algorithm, targets);
        }

        /// <summary>
        /// Sets the portfolio targets with the given entries and sends them with the algorithm
        /// being ran to the signal exports providers set, as long as the algorithm is in live mode
        /// </summary>
        /// <param name="algorithm">Algorithm being ran</param>
        /// <param name="portfolioTargets">One or more portfolio targets to be sent to the defined signal export providers</param>
        /// <exception cref="ArgumentException">It will throw an exception if there's no portfolio target in the parameters</exception>
        public void SetTargetPortfolio(IAlgorithm algorithm, params PortfolioTarget[] portfolioTargets)
        {
            if (!_isLiveMode)
            {
                return;
            }

            if (portfolioTargets == null)
            {
                throw new ArgumentException("No portfolio target given");
            }

            var targets = new List<PortfolioTarget>(portfolioTargets);
            var signalExportTargetParameters = new SignalExportTargetParameters
            {
                Targets = targets,
                Algorithm = algorithm
            };

            foreach (var signalExport in _signalExports)
            {
                signalExport.Send(signalExportTargetParameters);
            }
        }
    }
}
