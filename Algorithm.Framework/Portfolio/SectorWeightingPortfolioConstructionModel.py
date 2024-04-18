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

from AlgorithmImports import *
from EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel

class SectorWeightingPortfolioConstructionModel(EqualWeightingPortfolioConstructionModel):
    '''Provides an implementation of IPortfolioConstructionModel that
   generates percent targets based on the CompanyReference.industry_template_code.
   The target percent holdings of each sector is 1/S where S is the number of sectors and
   the target percent holdings of each security is 1/N where N is the number of securities of each sector.
   For insights of direction InsightDirection.UP, long targets are returned and for insights of direction
   InsightDirection.DOWN, short targets are returned.
   It will ignore Insight for symbols that have no CompanyReference.industry_template_code'''

    def __init__(self, rebalance = Resolution.DAILY):
        '''Initialize a new instance of InsightWeightingPortfolioConstructionModel
        Args:
            rebalance: Rebalancing parameter. If it is a timedelta, date rules or Resolution, it will be converted into a function.
                              If None will be ignored.
                              The function returns the next expected rebalance time for a given algorithm UTC DateTime.
                              The function returns null if unknown, in which case the function will be called again in the
                              next loop. Returning current time will trigger rebalance.'''
        super().__init__(rebalance)
        self.sector_code_by_symbol = dict()

    def should_create_target_for_insight(self, insight):
        '''Method that will determine if the portfolio construction model should create a
        target for this insight
        Args:
            insight: The insight to create a target for'''
        return insight.symbol in self.sector_code_by_symbol

    def determine_target_percent(self, active_insights):
        '''Will determine the target percent for each insight
        Args:
            active_insights: The active insights to generate a target for'''
        result = dict()

        insight_by_sector_code = dict()

        for insight in active_insights:
            if insight.direction == InsightDirection.FLAT:
                result[insight] = 0
                continue

            sector_code = self.sector_code_by_symbol.get(insight.symbol)
            insights = insight_by_sector_code.pop(sector_code, list())

            insights.append(insight)
            insight_by_sector_code[sector_code] = insights

        # give equal weighting to each sector
        sector_percent = 0 if len(insight_by_sector_code) == 0 else 1.0 / len(insight_by_sector_code)

        for _, insights in insight_by_sector_code.items():
            # give equal weighting to each security
            count = len(insights)
            percent = 0 if count == 0 else sector_percent / count
            for insight in insights:
                result[insight] = insight.direction * percent

        return result

    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for security in changes.removed_securities:
            # Removes the symbol from the self.sector_code_by_symbol dictionary
            # since we cannot emit PortfolioTarget for removed securities
            self.sector_code_by_symbol.pop(security.symbol, None)

        for security in changes.added_securities:
            sector_code = self.get_sector_code(security)
            if sector_code:
                self.sector_code_by_symbol[security.symbol] = sector_code

        super().on_securities_changed(algorithm, changes)

    def get_sector_code(self, security):
        '''Gets the sector code
        Args:
            security: The security to create a sector code for
        Returns:
            The value of the sector code for the security
        Remarks:
            Other sectors can be defined using AssetClassification'''
        fundamentals = security.fundamentals
        company_reference = security.fundamentals.company_reference if fundamentals else None
        return company_reference.industry_template_code if company_reference else None
