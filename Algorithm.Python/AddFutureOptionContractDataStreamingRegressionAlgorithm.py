from datetime import datetime, timedelta

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Future import *


class AddFutureOptionContractDataStreamingRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.onDataReached = False
        self.invested = False
        self.symbolsReceived = []
        self.expectedSymbolsReceived = []
        self.dataReceived = {}

        self.SetStartDate(2020, 9, 22)
        self.SetEndDate(2020, 9, 23)

        self.es18z20 = self.AddFutureContract(
            Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, datetime(2020, 12, 18)), 
            Resolution.Minute).Symbol

        self.es19h21 = self.AddFutureContract(
            Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, datetime(2021, 3, 19)), 
            Resolution.Minute).Symbol

        optionChains = self.OptionChainProvider.GetOptionContractList(self.es18z20, self.Time)
        optionChains += self.OptionChainProvider.GetOptionContractList(self.es19h21, self.Time)

        for optionContract in optionChains:
            self.expectedSymbolsReceived.append(self.AddFutureOptionContract(optionContract, Resolution.Minute).Symbol)

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

        if data.ContainsKey(self.es18z20) and data.ContainsKey(self.es19h21):
            self.SetHoldings(self.es18z20, 0.2)
            self.SetHoldings(self.es19h21, 0.2)

            self.invested = True

    def OnEndOfAlgorithm(self):
        super().OnEndOfAlgorithm()

        self.symbolsReceived = list(set(self.symbolsReceived))
        self.expectedSymbolsReceived = list(set(self.expectedSymbolsReceived))

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
