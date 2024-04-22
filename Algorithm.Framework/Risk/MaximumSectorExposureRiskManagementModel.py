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
from itertools import groupby

class MaximumSectorExposureRiskManagementModel(RiskManagementModel):
    '''Provides an implementation of IRiskManagementModel that that limits the sector exposure to the specified percentage'''

    def __init__(self, maximum_sector_exposure = 0.20):
        '''Initializes a new instance of the MaximumSectorExposureRiskManagementModel class
        Args:
            maximum_drawdown_percent: The maximum exposure for any sector, defaults to 20% sector exposure.'''
        if maximum_sector_exposure <= 0:
            raise ValueError('MaximumSectorExposureRiskManagementModel: the maximum sector exposure cannot be a non-positive value.')

        self.maximum_sector_exposure = maximum_sector_exposure
        self.targets_collection = PortfolioTargetCollection()

    def manage_risk(self, algorithm, targets):
        '''Manages the algorithm's risk at each time step
        Args:
            algorithm: The algorithm instance'''
        maximum_sector_exposure_value = float(algorithm.portfolio.total_portfolio_value) * self.maximum_sector_exposure

        self.targets_collection.add_range(targets)

        risk_targets = list()

        # Group the securities by their sector
        filtered = list(filter(lambda x: x.value.fundamentals is not None and x.value.fundamentals.has_fundamental_data, algorithm.universe_manager.active_securities))
        filtered.sort(key = lambda x: x.value.fundamentals.company_reference.industry_template_code)
        group_by_sector = groupby(filtered, lambda x: x.value.fundamentals.company_reference.industry_template_code)

        for code, securities in group_by_sector:
            # Compute the sector absolute holdings value
            # If the construction model has created a target, we consider that
            # value to calculate the security absolute holding value
            quantities = {}
            sector_absolute_holdings_value = 0

            for security in securities:
                symbol = security.value.symbol
                quantities[symbol] = security.value.holdings.quantity
                absolute_holdings_value = security.value.holdings.absolute_holdings_value

                if self.targets_collection.contains_key(symbol):
                    quantities[symbol] = self.targets_collection[symbol].quantity

                    absolute_holdings_value = (security.value.price * abs(quantities[symbol]) *
                        security.value.symbol_properties.contract_multiplier *
                        security.value.quote_currency.conversion_rate)

                sector_absolute_holdings_value += absolute_holdings_value

            # If the ratio between the sector absolute holdings value and the maximum sector exposure value
            # exceeds the unity, it means we need to reduce each security of that sector by that ratio
            # Otherwise, it means that the sector exposure is below the maximum and there is nothing to do.
            ratio = float(sector_absolute_holdings_value) / maximum_sector_exposure_value

            if ratio > 1:
                for symbol, quantity in quantities.items():
                    if quantity != 0:
                        risk_targets.append(PortfolioTarget(symbol, float(quantity) / ratio))

        return risk_targets

    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        any_fundamental_data = any([
            kvp.value.fundamentals is not None and
            kvp.value.fundamentals.has_fundamental_data for kvp in algorithm.active_securities
            ])

        if not any_fundamental_data:
            raise Exception("MaximumSectorExposureRiskManagementModel.on_securities_changed: Please select a portfolio selection model that selects securities with fundamental data.")
