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

from datetime import datetime, timedelta

from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Future import *
from QuantConnect import *

### <summary>
### This regression algorithm tests that we only receive the option chain for a single future contract
### in the option universe filter.
### </summary>
class AddFutureOptionSingleOptionChainSelectedInUniverseFilterRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.invested = False
        self.onDataReached = False
        self.optionFilterRan = False
        self.symbolsReceived = []
        self.expectedSymbolsReceived = []
        self.dataReceived = {}

        self.SetStartDate(2020, 1, 5)
        self.SetEndDate(2020, 1, 6)

        self.es = self.AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME)
        self.es.SetFilter(lambda futureFilter: futureFilter.Expiration(0, 365).ExpirationCycle([3, 6]))

        self.AddFutureOption(self.es.Symbol, self.OptionContractUniverseFilterFunction)

    def OptionContractUniverseFilterFunction(self, optionContracts: OptionFilterUniverse) -> OptionFilterUniverse:
        self.optionFilterRan = True

        expiry = list(set([x.Underlying.ID.Date for x in optionContracts]))
        expiry = None if not any(expiry) else expiry[0]

        symbol = [x.Underlying for x in optionContracts]
        symbol = None if not any(symbol) else symbol[0]

        if expiry is None or symbol is None:
            raise AssertionError("Expected a single Option contract in the chain, found 0 contracts")

        enumerator = optionContracts.GetEnumerator()
        while enumerator.MoveNext():
            self.expectedSymbolsReceived.append(enumerator.Current)

        return optionContracts

    def OnData(self, data: Slice):
        if not data.HasData:
            return

        self.onDataReached = True
        hasOptionQuoteBars = False

        for qb in data.QuoteBars.Values:
            if qb.Symbol.SecurityType != SecurityType.FutureOption:
                continue

            hasOptionQuoteBars = True

            self.symbolsReceived.append(qb.Symbol)
            if qb.Symbol not in self.dataReceived:
                self.dataReceived[qb.Symbol] = []

            self.dataReceived[qb.Symbol].append(qb)

        if self.invested or not hasOptionQuoteBars:
            return

        for chain in data.OptionChains.Values:
            futureInvested = False
            optionInvested = False

            for option in chain.Contracts.Keys:
                if futureInvested and optionInvested:
                    return

                future = option.Underlying

                if not optionInvested and data.ContainsKey(option):
                    self.MarketOrder(option, 1)
                    self.invested = True
                    optionInvested = True

                if not futureInvested and data.ContainsKey(future):
                    self.MarketOrder(future, 1)
                    self.invested = True
                    futureInvested = True

    def OnEndOfAlgorithm(self):
        super().OnEndOfAlgorithm()
        self.symbolsReceived = list(set(self.symbolsReceived))
        self.expectedSymbolsReceived = list(set(self.expectedSymbolsReceived))

        if not self.optionFilterRan:
            raise AssertionError("Option chain filter was never ran")
        if not self.onDataReached:
            raise AssertionError("OnData() was never called.")
        if len(self.symbolsReceived) != len(self.expectedSymbolsReceived):
            raise AssertionError(f"Expected {len(self.expectedSymbolsReceived)} option contracts Symbols, found {len(self.symbolsReceived)}")

        missingSymbols = [expectedSymbol for expectedSymbol in self.expectedSymbolsReceived if expectedSymbol not in self.symbolsReceived]
        if any(missingSymbols):
            raise AssertionError(f'Symbols: "{", ".join(missingSymbols)}" were not found in OnData')

        for expectedSymbol in self.expectedSymbolsReceived:
            data = self.dataReceived[expectedSymbol]
            for dataPoint in data:
                dataPoint.EndTime = datetime(1970, 1, 1)

            nonDupeDataCount = len(set(data))
            if nonDupeDataCount < 1000:
                raise AssertionError(f"Received too few data points. Expected >=1000, found {nonDupeDataCount} for {expectedSymbol}")
