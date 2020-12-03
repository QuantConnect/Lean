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

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Future import *
from QuantConnect import Market

### <summary>
### This regression algorithm tests that we receive the expected data when
### we add future option contracts individually using <see cref="AddFutureOptionContract"/>
### </summary>
class AddFutureOptionContractDataStreamingRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.onDataReached = False
        self.invested = False
        self.symbolsReceived = []
        self.expectedSymbolsReceived = []
        self.dataReceived = {}

        self.SetStartDate(2020, 1, 5)
        self.SetEndDate(2020, 1, 6)

        self.es20h20 = self.AddFutureContract(
            Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, datetime(2020, 3, 20)),
            Resolution.Minute).Symbol

        self.es19m20 = self.AddFutureContract(
            Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, datetime(2020, 6, 19)),
            Resolution.Minute).Symbol

        optionChains = self.OptionChainProvider.GetOptionContractList(self.es20h20, self.Time)
        optionChains += self.OptionChainProvider.GetOptionContractList(self.es19m20, self.Time)

        for optionContract in optionChains:
            self.expectedSymbolsReceived.append(self.AddFutureOptionContract(optionContract, Resolution.Minute).Symbol)

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

        if data.ContainsKey(self.es20h20) and data.ContainsKey(self.es19m20):
            self.SetHoldings(self.es20h20, 0.2)
            self.SetHoldings(self.es19m20, 0.2)

            self.invested = True

    def OnEndOfAlgorithm(self):
        super().OnEndOfAlgorithm()

        self.symbolsReceived = list(set(self.symbolsReceived))
        self.expectedSymbolsReceived = list(set(self.expectedSymbolsReceived))

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
