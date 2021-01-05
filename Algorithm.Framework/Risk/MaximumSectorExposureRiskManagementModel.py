# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")

from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioTarget, PortfolioTargetCollection
from QuantConnect.Algorithm.Framework.Risk import RiskManagementModel
from itertools import groupby

class MaximumSectorExposureRiskManagementModel(RiskManagementModel):
    '''Provides an implementation of IRiskManagementModel that that limits the sector exposure to the specified percentage'''

    def __init__(self, maximumSectorExposure = 0.20):
        '''Initializes a new instance of the MaximumSectorExposureRiskManagementModel class
        Args:
            maximumDrawdownPercent: The maximum exposure for any sector, defaults to 20% sector exposure.'''
        if maximumSectorExposure <= 0:
            raise ValueError('MaximumSectorExposureRiskManagementModel: the maximum sector exposure cannot be a non-positive value.')

        self.maximumSectorExposure = maximumSectorExposure
        self.targetsCollection = PortfolioTargetCollection()

    def ManageRisk(self, algorithm, targets):
        '''Manages the algorithm's risk at each time step
        Args:
            algorithm: The algorithm instance'''
        maximumSectorExposureValue = float(algorithm.Portfolio.TotalPortfolioValue) * self.maximumSectorExposure

        self.targetsCollection.AddRange(targets)

        risk_targets = list()

        # Group the securities by their sector
        filtered = list(filter(lambda x: x.Value.Fundamentals is not None and x.Value.Fundamentals.HasFundamentalData, algorithm.UniverseManager.ActiveSecurities))
        filtered.sort(key = lambda x: x.Value.Fundamentals.CompanyReference.IndustryTemplateCode)
        groupBySector = groupby(filtered, lambda x: x.Value.Fundamentals.CompanyReference.IndustryTemplateCode)

        for code, securities in groupBySector:
            # Compute the sector absolute holdings value
            # If the construction model has created a target, we consider that
            # value to calculate the security absolute holding value
            quantities = {}
            sectorAbsoluteHoldingsValue = 0

            for security in securities:
                symbol = security.Value.Symbol
                quantities[symbol] = security.Value.Holdings.Quantity
                absoluteHoldingsValue = security.Value.Holdings.AbsoluteHoldingsValue

                if self.targetsCollection.ContainsKey(symbol):
                    quantities[symbol] = self.targetsCollection[symbol].Quantity

                    absoluteHoldingsValue = (security.Value.Price * abs(quantities[symbol]) *
                        security.Value.SymbolProperties.ContractMultiplier *
                        security.Value.QuoteCurrency.ConversionRate)

                sectorAbsoluteHoldingsValue += absoluteHoldingsValue

            # If the ratio between the sector absolute holdings value and the maximum sector exposure value
            # exceeds the unity, it means we need to reduce each security of that sector by that ratio
            # Otherwise, it means that the sector exposure is below the maximum and there is nothing to do.
            ratio = float(sectorAbsoluteHoldingsValue) / maximumSectorExposureValue

            if ratio > 1:
                for symbol, quantity in quantities.items():
                    if quantity != 0:
                        risk_targets.append(PortfolioTarget(symbol, float(quantity) / ratio))

        return risk_targets

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        anyFundamentalData = any([
            kvp.Value.Fundamentals is not None and 
            kvp.Value.Fundamentals.HasFundamentalData for kvp in algorithm.ActiveSecurities
            ]);

        if not anyFundamentalData:
            raise Exception("MaximumSectorExposureRiskManagementModel.OnSecuritiesChanged: Please select a portfolio selection model that selects securities with fundamental data.")