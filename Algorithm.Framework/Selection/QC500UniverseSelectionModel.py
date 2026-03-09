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
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel
from itertools import groupby
from math import ceil

class QC500UniverseSelectionModel(FundamentalUniverseSelectionModel):
    '''Defines the QC500 universe as a universe selection model for framework algorithm
    For details: https://github.com/QuantConnect/Lean/pull/1663'''

    def __init__(self, filterFineData = True, universeSettings = None):
        '''Initializes a new default instance of the QC500UniverseSelectionModel'''
        super().__init__(filterFineData, universeSettings)
        self.number_of_symbols_coarse = 1000
        self.number_of_symbols_fine = 500
        self.dollar_volume_by_symbol = {}
        self.last_month = -1

    def select_coarse(self, algorithm: QCAlgorithm, fundamental: list[Fundamental]):
        '''Performs coarse selection for the QC500 constituents.
        The stocks must have fundamental data
        The stock must have positive previous-day close price
        The stock must have positive volume on the previous trading day'''
        if algorithm.time.month == self.last_month:
            return Universe.UNCHANGED

        sorted_by_dollar_volume = sorted([x for x in fundamental if x.has_fundamental_data and x.volume > 0 and x.price > 0],
                                     key = lambda x: x.dollar_volume, reverse=True)[:self.number_of_symbols_coarse]

        self.dollar_volume_by_symbol = {x.Symbol:x.dollar_volume for x in sorted_by_dollar_volume}

        # If no security has met the QC500 criteria, the universe is unchanged.
        # A new selection will be attempted on the next trading day as self.lastMonth is not updated
        if len(self.dollar_volume_by_symbol) == 0:
            return Universe.UNCHANGED

        # return the symbol objects our sorted collection
        return list(self.dollar_volume_by_symbol.keys())

    def select_fine(self, algorithm: QCAlgorithm, fundamental: list[Fundamental]):
        '''Performs fine selection for the QC500 constituents
        The company's headquarter must in the U.S.
        The stock must be traded on either the NYSE or NASDAQ
        At least half a year since its initial public offering
        The stock's market cap must be greater than 500 million'''

        sorted_by_sector = sorted([x for x in fundamental if x.company_reference.country_id == "USA"
                                        and x.company_reference.primary_exchange_id in ["NYS","NAS"]
                                        and (algorithm.time - x.security_reference.ipo_date).days > 180
                                        and x.market_cap > 5e8],
                               key = lambda x: x.company_reference.industry_template_code)

        count = len(sorted_by_sector)

        # If no security has met the QC500 criteria, the universe is unchanged.
        # A new selection will be attempted on the next trading day as self.lastMonth is not updated
        if count == 0:
            return Universe.UNCHANGED

        # Update self.lastMonth after all QC500 criteria checks passed
        self.last_month = algorithm.time.month

        percent = self.number_of_symbols_fine / count
        sorted_by_dollar_volume = []

        # select stocks with top dollar volume in every single sector
        for code, g in groupby(sorted_by_sector, lambda x: x.company_reference.industry_template_code):
            y = sorted(g, key = lambda x: self.dollar_volume_by_symbol[x.Symbol], reverse = True)
            c = ceil(len(y) * percent)
            sorted_by_dollar_volume.extend(y[:c])

        sorted_by_dollar_volume = sorted(sorted_by_dollar_volume, key = lambda x: self.dollar_volume_by_symbol[x.Symbol], reverse=True)
        return [x.Symbol for x in sorted_by_dollar_volume[:self.number_of_symbols_fine]]
