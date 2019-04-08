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

from clr import GetClrType as typeof
from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm.Framework")

from QuantConnect import Extensions, Resolution, SecurityType, Symbol, SymbolCache
from QuantConnect.Data import SubscriptionDataConfig
from QuantConnect.Data.Market import Tick, TradeBar
from QuantConnect.Securities import MarketHoursDatabase
from QuantConnect.Algorithm.Framework.Selection import ManualUniverse
from Selection.UniverseSelectionModel import UniverseSelectionModel
from itertools import groupby

class ManualUniverseSelectionModel(UniverseSelectionModel):
    '''Provides an implementation of IUniverseSelectionModel that simply subscribes to the specified set of symbols'''

    def __init__(self, symbols = list(), universeSettings = None, securityInitializer = None):
        self.MarketHours = MarketHoursDatabase.FromDataFolder()
        self.symbols = symbols
        self.universeSettings = universeSettings
        self.securityInitializer = securityInitializer

        for symbol in symbols:
            SymbolCache.Set(symbol.Value, symbol)

    def CreateUniverses(self, algorithm):
        '''Creates the universes for this algorithm. Called once after IAlgorithm.Initialize
        Args:
            algorithm: The algorithm instance to create universes for</param>
        Returns:
            The universes to be used by the algorithm'''
        universeSettings = self.universeSettings \
            if self.universeSettings is not None else algorithm.UniverseSettings

        securityInitializer = self.securityInitializer \
            if self.securityInitializer is not None else algorithm.SecurityInitializer

        resolution = universeSettings.Resolution
        type = typeof(Tick) if resolution == Resolution.Tick else typeof(TradeBar);

        universes = list()

        # universe per security type/market
        self.symbols = sorted(self.symbols, key=lambda s: (s.ID.Market, s.SecurityType))
        for key, grp in groupby(self.symbols, lambda s: (s.ID.Market, s.SecurityType)):

            market = key[0]
            securityType = key[1]
            securityTypeString = Extensions.GetEnumString(securityType, SecurityType)
            universeSymbol = Symbol.Create(f"manual-universe-selection-model-{securityTypeString}-{market}", securityType, market)

            if securityType == SecurityType.Base:
                # add an entry for this custom universe symbol -- we don't really know the time zone for sure,
                # but we set it to TimeZones.NewYork in AddData, also, since this is a manual universe, the time
                # zone doesn't actually matter since this universe specifically doesn't do anything with data.
                symbolString = MarketHoursDatabase.GetDatabaseSymbolKey(universeSymbol)
                alwaysOpen = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork)
                entry = self.MarketHours.SetEntry(market, symbolString, securityType, alwaysOpen, TimeZones.NewYork)
            else:
                entry = self.MarketHours.GetEntry(market, None, securityType)

            config = SubscriptionDataConfig(type, universeSymbol, resolution, entry.DataTimeZone, entry.ExchangeHours.TimeZone, False, False, True)
            universes.append( ManualUniverse(config, universeSettings, list(grp)))

        return universes