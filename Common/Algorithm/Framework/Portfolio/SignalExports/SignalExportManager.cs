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
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

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
        private readonly List<ISignalExportTarget> _signalExports;

        /// <summary>
        /// List of target security holdings to be sent to certain signal export provider
        /// </summary>
        private List<PortfolioTarget> _targets;

        /// <summary>
        /// Initializes the list of signal export providers
        /// </summary>
        /// <param name="signalExports">List of signal export providers</param>
        public SignalExportManager(params ISignalExportTarget[] signalExports)
        {
            _signalExports = new List<ISignalExportTarget>(signalExports);
        }

        /// <summary>
        /// Adds one or more new signal exports providers
        /// </summary>
        /// <param name="signalExports">One or more signal export provider</param>
        public void Add(params ISignalExportTarget[] signalExports)
        {
            _signalExports.AddRange(signalExports);
        }

        /// <summary>
        /// Sets the portfolio targets from the given portfolio current holdings
        /// </summary>
        /// <param name="portfolio">Portfolio containing the current holdings</param>
        /// <exception cref="ArgumentException">If the portfolio is null it will throw an exception</exception>
        public void SetTargetPortfolio(SecurityPortfolioManager portfolio)
        {
            if (portfolio == null)
            {
                throw new ArgumentException("Portfolio was null");
            }
            foreach (var holding in portfolio.Values)
            {
                var holdingPercent = holding.HoldingsValue / portfolio.TotalPortfolioValue;
                _targets.Add(new PortfolioTarget(holding.Symbol, (decimal)holdingPercent));
            }
        }

        /// <summary>
        /// Sets the portfolio targets with the given entries
        /// </summary>
        /// <param name="portfolioTargets">One or more portfolio targets to be sent to the defined signal export providers</param>
        /// <exception cref="ArgumentException">It will throw an exception if there's no portfolio target in the parameters</exception>
        public void SetTargetPortfolio(params PortfolioTarget[] portfolioTargets)
        {
            if (portfolioTargets == null)
            {
                throw new ArgumentException("No portfolio target given");
            }

            _targets.AddRange(portfolioTargets);
        }

        /// <summary>
        /// Sets the portfolio targets with the given list
        /// </summary>
        /// <param name="portfolioTargets">List of portfolio targets to be sent to the defined signal export providers</param>
        /// <exception cref="ArgumentException">It will throw an exception if there's no portfolio target in the parameters</exception>
        public void SetTargetPortfolio(List<PortfolioTarget> portfolioTargets)
        {
            if (portfolioTargets == null)
            {
                throw new ArgumentException("No portfolio target given");
            }

            _targets = portfolioTargets;
        }

        /// <summary>
        /// Sends the defined portfolio targets to the signal exports providers set
        /// </summary>
        public void ExportSignals()
        {
            foreach (var signalExport in _signalExports)
            {
                signalExport.Send(_targets);
            }
        }
    }
}
