from datetime import datetime, timedelta

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Future import *


class AddFutureOptionSingleOptionChainSelectedInUniverseFilterRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.invested = False
        self.onDataReached = False
        self.optionFilterRan = False
        self.symbolsReceived = []
        self.expectedSymbolsReceived = []
        self.dataReceived = {}

        self.SetStartDate(2020, 9, 22)
        self.SetEndDate(2020, 9, 23)

        self.es = self.AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME)
        self.es.SetFilter(lambda futureFilter: futureFilter.Expiration(0, 365).ExpirationCycle([3, 12]))

        self.AddFutureOption(self.es.Symbol, self.OptionContractUniverseFilterFunction)

    def OptionContractUniverseFilterFunction(self, optionContracts: OptionFilterUniverse) -> OptionFilterUniverse:
        self.optionFilterRan = True

        expiry = list(set([x.Underlying.ID.Date for x in optionContracts]))
        expiry = None if not any(expiry) else expiry[0]

        symbol = [x.Underlying for x in optionContracts]
        symbol = None if not any(symbol) else symbol[0]

        if expiry is None or symbol is None:
            raise Exception("Expected a single Option contract in the chain, found 0 contracts")

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
            if qb.Symbol.SecurityType != SecurityType.Option:
                continue

            hasOptionQuoteBars = True

            self.symbolsReceived.append(qb.Symbol)
            if qb.Symbol not in self.dataReceived:
                self.dataReceived[qb.Symbol] = []

            self.dataReceived[qb.Symbol].append(qb)

        if self.invested or not hasOptionQuoteBars:
            return

        for chain in data.FutureChains.Values:
            for future in chain.Contracts.Keys:
                if data.ContainsKey(future):
                    self.SetHoldings(future, 0.25)
                    self.invested = True

    def OnEndOfAlgorithm(self):
        super().OnEndOfAlgorithm()
        self.symbolsReceived = list(set(self.symbolsReceived))
        self.expectedSymbolsReceived = list(set(self.expectedSymbolsReceived))

        if not self.optionFilterRan:
            raise Exception("Option chain filter was never ran")
        if not self.onDataReached:
            raise Exception("OnData() was never called.")
        if len(self.symbolsReceived) != len(self.expectedSymbolsReceived):
            raise Exception(f"Expected {len(self.expectedSymbolsReceived)} option contracts Symbols, found {len(self.symbolsReceived)}")

        missingSymbols = [expectedSymbol for expectedSymbol in self.expectedSymbolsReceived if expectedSymbol not in self.symbolsReceived]
        if any(missingSymbols):
            raise Exception(f'Symbols: "{", ".join(missingSymbols)}" were not found in OnData')

        for expectedSymbol in self.expectedSymbolsReceived:
            data = self.dataReceived[expectedSymbol]
            for dataPoint in data:
                dataPoint.EndTime = datetime(1970, 1, 1)

            nonDupeDataCount = len(set(data))
            if nonDupeDataCount < 1000:
                raise Exception(f"Received too few data points. Expected >=1000, found {nonDupeDataCount} for {expectedSymbol}")
