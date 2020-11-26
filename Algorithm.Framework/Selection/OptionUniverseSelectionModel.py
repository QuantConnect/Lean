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
from clr import GetClrType as typeof
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm.Framework")

from QuantConnect import *
from QuantConnect.Securities import *
from QuantConnect.Data.Auxiliary import ZipEntryName
from QuantConnect.Data.UniverseSelection import OptionChainUniverse
from Selection.UniverseSelectionModel import UniverseSelectionModel
from datetime import datetime

class OptionUniverseSelectionModel(UniverseSelectionModel):
    '''Provides an implementation of IUniverseSelectionMode that subscribes to option chains'''
    def __init__(self,
                 refreshInterval,
                 optionChainSymbolSelector,
                 universeSettings = None,
                 securityInitializer = None):
        '''Creates a new instance of OptionUniverseSelectionModel
        Args:
            refreshInterval: Time interval between universe refreshes</param>
            optionChainSymbolSelector: Selects symbols from the provided option chain
            universeSettings: Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed
            securityInitializer: [Obsolete, will not be used] Performs extra initialization (such as setting models) after we create a new security object'''
        self.nextRefreshTimeUtc = datetime.min

        self.refreshInterval = refreshInterval
        self.optionChainSymbolSelector = optionChainSymbolSelector
        self.universeSettings = universeSettings
        self.securityInitializer = securityInitializer

    def GetNextRefreshTimeUtc(self):
        '''Gets the next time the framework should invoke the `CreateUniverses` method to refresh the set of universes.'''
        return self.nextRefreshTimeUtc

    def CreateUniverses(self, algorithm):
        '''Creates a new fundamental universe using this class's selection functions
        Args:
            algorithm: The algorithm instance to create universes for
        Returns:
            The universe defined by this model'''
        self.nextRefreshTimeUtc = (algorithm.UtcTime + self.refreshInterval).date()

        uniqueUnderlyingSymbols = set()
        for optionSymbol in self.optionChainSymbolSelector(algorithm.UtcTime):
            if optionSymbol.SecurityType != SecurityType.Option and optionSymbol.SecurityType != SecurityType.FutureOption:
                raise ValueError("optionChainSymbolSelector must return option or futures options symbols.")

            # prevent creating duplicate option chains -- one per underlying
            if optionSymbol.Underlying not in uniqueUnderlyingSymbols:
                uniqueUnderlyingSymbols.add(optionSymbol.Underlying)
                yield self.CreateOptionChain(algorithm, optionSymbol)

    def CreateOptionChain(self, algorithm, symbol):
        '''Creates a OptionChainUniverse for a given symbol
        Args:
            algorithm: The algorithm instance to create universes for
            symbol: Symbol of the option
        Returns:
            OptionChainUniverse for the given symbol'''
        if symbol.SecurityType != SecurityType.Option and symbol.SecurityType != SecurityType.FutureOption:
            raise ValueError("CreateOptionChain requires an option symbol.")

        # rewrite non-canonical symbols to be canonical
        market = symbol.ID.Market
        underlying = symbol.Underlying
        if not symbol.IsCanonical():
            alias = f"?{underlying.Value}"
            symbol = Symbol.Create(underlying.Value, SecurityType.Option, market, alias)

        # resolve defaults if not specified
        settings = self.universeSettings if self.universeSettings is not None else algorithm.UniverseSettings
        initializer = self.securityInitializer if self.securityInitializer is not None else algorithm.SecurityInitializer
        # create canonical security object, but don't duplicate if it already exists
        securities = [s for s in algorithm.Securities if s.Key == symbol]
        if len(securities) == 0:
            optionChain = self.CreateOptionChainSecurity(algorithm, symbol, settings, initializer)
        else:
            optionChain = securities[0]

        # set the option chain contract filter function
        optionChain.SetFilter(self.Filter)

        # force option chain security to not be directly tradable AFTER it's configured to ensure it's not overwritten
        optionChain.IsTradable = False

        return OptionChainUniverse(optionChain, settings, initializer, algorithm.LiveMode)

    def CreateOptionChainSecurity(self, algorithm, symbol, settings, initializer):
        '''Creates the canonical option chain security for a given symbol
        Args:
            algorithm: The algorithm instance to create universes for
            symbol: Symbol of the option
            settings: Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed
            initializer: [Obsolete, will not be used] Performs extra initialization (such as setting models) after we create a new security object
        Returns
            Option for the given symbol'''
        config = algorithm.SubscriptionManager.SubscriptionDataConfigService.Add(typeof(ZipEntryName),
                                                                                 symbol,
                                                                                 settings.Resolution,
                                                                                 settings.FillForward,
                                                                                 settings.ExtendedMarketHours,
                                                                                 False)

        return algorithm.Securities.CreateSecurity(symbol, config, settings.Leverage, False)

    def Filter(self, filter):
        '''Defines the option chain universe filter'''
        # NOP
        return filter
