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
from clr import GetClrType as typeof

from Selection.UniverseSelectionModel import UniverseSelectionModel
from itertools import groupby

class ManualUniverseSelectionModel(UniverseSelectionModel):
    '''Provides an implementation of IUniverseSelectionModel that simply subscribes to the specified set of symbols'''

    def __init__(self, symbols = list(), universe_settings = None):
        self.marketHours = MarketHoursDatabase.from_data_folder()
        self.symbols = symbols
        self.universe_settings = universe_settings

        for symbol in symbols:
            SymbolCache.set(symbol.Value, symbol)

    def create_universes(self, algorithm: QCAlgorithm) -> list[Universe]:
        '''Creates the universes for this algorithm. Called once after IAlgorithm.Initialize
        Args:
            algorithm: The algorithm instance to create universes for</param>
        Returns:
            The universes to be used by the algorithm'''
        universe_settings = self.universe_settings \
            if self.universe_settings is not None else algorithm.universe_settings

        resolution = universe_settings.resolution
        type = typeof(Tick) if resolution == Resolution.TICK else typeof(TradeBar)

        universes = list()

        # universe per security type/market
        self.symbols = sorted(self.symbols, key=lambda s: (s.id.market, s.security_type))
        for key, grp in groupby(self.symbols, lambda s: (s.id.market, s.security_type)):

            market = key[0]
            security_type = key[1]
            security_type_str = Extensions.get_enum_string(security_type, SecurityType)
            universe_symbol = Symbol.create(f"manual-universe-selection-model-{security_type_str}-{market}", security_type, market)

            if security_type == SecurityType.BASE:
                # add an entry for this custom universe symbol -- we don't really know the time zone for sure,
                # but we set it to TimeZones.NewYork in AddData, also, since this is a manual universe, the time
                # zone doesn't actually matter since this universe specifically doesn't do anything with data.
                symbol_string = MarketHoursDatabase.get_database_symbol_key(universe_symbol)
                always_open = SecurityExchangeHours.always_open(TimeZones.NEW_YORK)
                entry = self.marketHours.set_entry(market, symbol_string, security_type, always_open, TimeZones.NEW_YORK)
            else:
                entry = self.marketHours.get_entry(market, None, security_type)

            config = SubscriptionDataConfig(type, universe_symbol, resolution, entry.data_time_zone, entry.exchange_hours.time_zone, False, False, True)
            universes.append( ManualUniverse(config, universe_settings, list(grp)))

        return universes
