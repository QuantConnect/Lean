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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm.Framework")

from QuantConnect import Resolution
from QuantConnect.Algorithm.Framework.Alphas import *
from EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from itertools import groupby

class SectorWeightingPortfolioConstructionModel(EqualWeightingPortfolioConstructionModel):
    '''Provides an implementation of IPortfolioConstructionModel that
   generates percent targets based on the CompanyReference.IndustryTemplateCode.
   The target percent holdings of each sector is 1/S where S is the number of sectors and
   the target percent holdings of each security is 1/N where N is the number of securities of each sector.
   For insights of direction InsightDirection.Up, long targets are returned and for insights of direction
   InsightDirection.Down, short targets are returned.
   It will ignore Insight for symbols that have no CompanyReference.IndustryTemplateCode'''

    def __init__(self, rebalance = Resolution.Daily):
        '''Initialize a new instance of InsightWeightingPortfolioConstructionModel
        Args:
            rebalance: Rebalancing parameter. If it is a timedelta, date rules or Resolution, it will be converted into a function.
                              If None will be ignored.
                              The function returns the next expected rebalance time for a given algorithm UTC DateTime.
                              The function returns null if unknown, in which case the function will be called again in the
                              next loop. Returning current time will trigger rebalance.'''
        super().__init__(rebalance)
        self.sectorCodeBySymbol = dict()

    def ShouldCreateTargetForInsight(self, insight):
        '''Method that will determine if the portfolio construction model should create a
        target for this insight
        Args:
            insight: The insight to create a target for'''
        return insight.Symbol in self.sectorCodeBySymbol

    def DetermineTargetPercent(self, activeInsights):
        '''Will determine the target percent for each insight
        Args:
            activeInsights: The active insights to generate a target for'''
        result = dict()

        insightBySectorCode = dict()

        for insight in activeInsights:
            if insight.Direction == InsightDirection.Flat:
                result[insight] = 0
                continue

            sectorCode = self.sectorCodeBySymbol.get(insight.Symbol)
            insights = insightBySectorCode.pop(sectorCode, list())

            insights.append(insight)
            insightBySectorCode[sectorCode] = insights

        # give equal weighting to each sector
        sectorPercent = 0 if len(insightBySectorCode) == 0 else 1.0 / len(insightBySectorCode)

        for _, insights in insightBySectorCode.items():
            # give equal weighting to each security
            count = len(insights)
            percent = 0 if count == 0 else sectorPercent / count
            for insight in insights:
                result[insight] = insight.Direction * percent

        return result

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for security in changes.RemovedSecurities:
            # Removes the symbol from the self.sectorCodeBySymbol dictionary
            # since we cannot emit PortfolioTarget for removed securities
            self.sectorCodeBySymbol.pop(security.Symbol, None)

        for security in changes.AddedSecurities:
            sectorCode = self.GetSectorCode(security)
            if sectorCode:
                self.sectorCodeBySymbol[security.Symbol] = sectorCode

        super().OnSecuritiesChanged(algorithm, changes)

    def GetSectorCode(self, security):
        '''Gets the sector code
        Args:
            security: The security to create a sector code for
        Returns:
            The value of the sector code for the security
        Remarks:
            Other sectors can be defined using AssetClassification'''
        fundamentals = security.Fundamentals
        companyReference = security.Fundamentals.CompanyReference if fundamentals else None
        return companyReference.IndustryTemplateCode if companyReference else None