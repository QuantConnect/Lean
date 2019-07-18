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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Framework.Risk
{
    /// <summary>
    /// Provides an implementation of <see cref="IRiskManagementModel"/> that limits
    /// the sector exposure to the specified percentage
    /// </summary>
    public class MaximumSectorExposureRiskManagementModel : RiskManagementModel
    {
        private readonly decimal _maximumSectorExposure;
        private readonly PortfolioTargetCollection _targetsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaximumSectorExposureRiskManagementModel"/> class
        /// </summary>
        /// <param name="maximumSectorExposure">The maximum exposure for any sector, defaults to 20% sector exposure.</param>
        public MaximumSectorExposureRiskManagementModel(
            decimal maximumSectorExposure = 0.20m
            )
        {
            if (maximumSectorExposure <= 0)
            {
                throw new ArgumentOutOfRangeException("MaximumSectorExposureRiskManagementModel: the maximum sector exposure cannot be a non-positive value.");
            }

            _maximumSectorExposure = maximumSectorExposure;
            _targetsCollection = new PortfolioTargetCollection();
        }

        /// <summary>
        /// Manages the algorithm's risk at each time step
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The current portfolio targets to be assessed for risk</param>
        public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            var maximumSectorExposureValue = algorithm.Portfolio.TotalPortfolioValue * _maximumSectorExposure;

            _targetsCollection.AddRange(targets);

            // Group the securities by their sector
            var groupBySector = algorithm.UniverseManager.ActiveSecurities
                .Where(x => x.Value.Fundamentals != null && x.Value.Fundamentals.HasFundamentalData)
                .GroupBy(x => x.Value.Fundamentals.CompanyReference.IndustryTemplateCode);

            foreach (var securities in groupBySector)
            {
                // Compute the sector absolute holdings value
                // If the construction model has created a target, we consider that
                // value to calculate the security absolute holding value
                var sectorAbsoluteHoldingsValue = 0m;

                foreach (var security in securities)
                {
                    var absoluteHoldingsValue = security.Value.Holdings.AbsoluteHoldingsValue;

                    IPortfolioTarget target;
                    if (_targetsCollection.TryGetValue(security.Value.Symbol, out target))
                    {
                        absoluteHoldingsValue = security.Value.Price * Math.Abs(target.Quantity) *
                            security.Value.SymbolProperties.ContractMultiplier *
                            security.Value.QuoteCurrency.ConversionRate;
                    }

                    sectorAbsoluteHoldingsValue += absoluteHoldingsValue;
                }

                // If the ratio between the sector absolute holdings value and the maximum sector exposure value
                // exceeds the unity, it means we need to reduce each security of that sector by that ratio
                // Otherwise, it means that the sector exposure is below the maximum and there is nothing to do.
                var ratio = sectorAbsoluteHoldingsValue / maximumSectorExposureValue;
                if (ratio > 1)
                {
                    foreach (var security in securities)
                    {
                        var quantity = security.Value.Holdings.Quantity;
                        var symbol = security.Value.Symbol;

                        IPortfolioTarget target;
                        if (_targetsCollection.TryGetValue(symbol, out target))
                        {
                            quantity = target.Quantity;
                        }

                        if (quantity != 0)
                        {
                            yield return new PortfolioTarget(symbol, quantity / ratio);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            var anyFundamentalData = algorithm.ActiveSecurities
                .Any(kvp => kvp.Value.Fundamentals != null && kvp.Value.Fundamentals.HasFundamentalData);

            if (!anyFundamentalData)
            {
                throw new Exception("MaximumSectorExposureRiskManagementModel.OnSecuritiesChanged: Please select a portfolio selection model that selects securities with fundamental data.");
            }
        }
    }
}