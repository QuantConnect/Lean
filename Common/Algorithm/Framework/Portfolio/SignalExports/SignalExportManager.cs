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

using Python.Runtime;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Python;
using QuantConnect.Securities;
using System.Collections.Generic;
using System;
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Class manager to send portfolio targets to different 3rd party API's
    /// For example, it allows Collective2, CrunchDAO and Numerai signal export providers
    /// </summary>
    public class SignalExportManager
    {
        /// <summary>
        /// Records the time of the first order event of a group of events
        /// </summary>
        private ReferenceWrapper<DateTime> _initialOrderEventTimeUtc = new(Time.EndOfTime);

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
        /// Gets the maximim time span elapsed to export signals after an order event 
        /// If null, disable automatic export.
        /// </summary>
        public TimeSpan? AutomaticExportTimeSpan { get; set; } = TimeSpan.FromSeconds(5);

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
        /// Adds a new signal exports provider
        /// </summary>
        /// <param name="signalExport">Signal export provider</param>
        public void AddSignalExportProvider(ISignalExportTarget signalExport)
        {
            _signalExports ??= [];
            _signalExports.Add(signalExport);
        }

        /// <summary>
        /// Adds a new signal exports provider
        /// </summary>
        /// <param name="signalExport">Signal export provider</param>
        public void AddSignalExportProvider(PyObject signalExport)
        {
            if (!signalExport.TryConvert<ISignalExportTarget>(out var managedSignalExport))
            {
                managedSignalExport = new SignalExportTargetPythonWrapper(signalExport);
            }
            AddSignalExportProvider(managedSignalExport);
        }

        /// <summary>
        /// Adds one or more new signal exports providers
        /// </summary>
        /// <param name="signalExports">One or more signal export provider</param>
        public void AddSignalExportProviders(params ISignalExportTarget[] signalExports)
        {
            signalExports.DoForEach(AddSignalExportProvider);
        }

        /// <summary>
        /// Adds one or more new signal exports providers
        /// </summary>
        /// <param name="signalExports">One or more signal export provider</param>
        public void AddSignalExportProviders(PyObject signalExports)
        {
            using var _ = Py.GIL();
            if (!signalExports.IsIterable())
            {
                AddSignalExportProvider(signalExports);
                return;
            }
            PyList.AsList(signalExports).DoForEach(AddSignalExportProvider);
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
        /// See  <see cref="PortfolioTarget.Percent(IAlgorithm, Symbol, decimal, bool, string)"/> for more
        /// information about how each symbol quantity was calculated
        /// </summary>
        /// <param name="targets">An array of portfolio targets from the algorithm's Portfolio</param>
        /// <returns>True if TotalPortfolioValue was bigger than zero, false otherwise</returns>
        protected bool GetPortfolioTargets(out PortfolioTarget[] targets)
        {
            var totalPortfolioValue = _algorithm.Portfolio.TotalPortfolioValue;
            if (totalPortfolioValue <= 0)
            {
                _algorithm.Error("Total portfolio value was less than or equal to 0");
                targets = Array.Empty<PortfolioTarget>();
                return false;
            }

            targets = GetPortfolioTargets(totalPortfolioValue).ToArray();
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

            if (_signalExports.IsNullOrEmpty())
            { 
                return false;
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

        private IEnumerable<PortfolioTarget> GetPortfolioTargets(decimal totalPortfolioValue)
        {
            foreach (var holding in _algorithm.Portfolio.Values)
            {
                var security = _algorithm.Securities[holding.Symbol];

                // Skip non-tradeable securities except canonical futures as some signal providers
                // like Collective2 accept them.
                // See https://collective2.com/api-docs/latest#Basic_submitsignal_format
                if (!security.IsTradable && !security.Symbol.IsCanonical())
                {
                    continue;
                }

                var marginParameters = new InitialMarginParameters(security, holding.Quantity);
                var adjustedPercent = Math.Abs(security.BuyingPowerModel.GetInitialMarginRequirement(marginParameters) / totalPortfolioValue);

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

                yield return new PortfolioTarget(holding.Symbol, adjustedHoldingPercent);
            }
        }

        /// <summary>
        /// New order event handler: on order status changes (filled, partially filled, cancelled etc).
        /// </summary>
        /// <param name="orderEvent">Event information</param>
        public void OnOrderEvent(OrderEvent orderEvent)
        {
            if (_initialOrderEventTimeUtc.Value == Time.EndOfTime && orderEvent.Status.IsFill())
            {
                _initialOrderEventTimeUtc = new(orderEvent.UtcTime);
            }
        }

        /// <summary>
        /// Set the target portfolio after order events.
        /// </summary>
        /// <param name="currentTimeUtc">The current time of synchronous events</param>
        public void Flush(DateTime currentTimeUtc)
        {
            var initialOrderEventTimeUtc = _initialOrderEventTimeUtc.Value;
            if (_signalExports.IsNullOrEmpty() || initialOrderEventTimeUtc == Time.EndOfTime || !AutomaticExportTimeSpan.HasValue)
            {
                return;
            }

            if (currentTimeUtc - initialOrderEventTimeUtc < AutomaticExportTimeSpan)
            {
                return;
            }

            try
            {
                SetTargetPortfolioFromPortfolio();
            }
            catch (Exception exception)
            {
                // SetTargetPortfolioFromPortfolio logs all known error on LEAN side.
                // Exceptions occurs in the ISignalExportTarget.Send method (user-defined).
                _algorithm.Error($"Failed to send portfolio target(s). Reason: {exception.Message}.{Environment.NewLine}{exception.StackTrace}");
            }
            _initialOrderEventTimeUtc = new(Time.EndOfTime);
        }
    }
}
