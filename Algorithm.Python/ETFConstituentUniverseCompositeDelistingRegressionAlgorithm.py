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

### <summary>
### Tests the delisting of the composite Symbol (ETF symbol) and the removal of
### the universe and the symbol from the algorithm.
### </summary>
class ETFConstituentUniverseCompositeDelistingRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2020, 12, 1)
        self.SetEndDate(2021, 1, 31)
        self.SetCash(100000)

        self.universeSymbolCount = 0
        self.universeSelectionDone = False
        self.universeAdded = False
        self.universeRemoved = False

        self.UniverseSettings.Resolution = Resolution.Hour
        self.delistingDate = date(2021, 1, 21)

        self.aapl = self.AddEquity("AAPL", Resolution.Hour).Symbol
        self.gdvd = self.AddEquity("GDVD", Resolution.Hour).Symbol

        self.AddUniverse(self.Universe.ETF(self.gdvd, self.UniverseSettings, self.FilterETFs))

    def FilterETFs(self, constituents):
        self.universeSelectionDone = True

        if self.UtcTime.date() > self.delistingDate:
            raise Exception(f"Performing constituent universe selection on {self.UtcTime.strftime('%Y-%m-%d %H:%M:%S.%f')} after composite ETF has been delisted")

        constituentSymbols = [i.Symbol for i in constituents]
        self.universeSymbolCount = len(set(constituentSymbols))

        return constituentSymbols

    def OnData(self, data):
        if self.UtcTime.date() > self.delistingDate and any([i != self.aapl for i in data.Keys]):
            raise Exception("Received unexpected slice in OnData(...) after universe was deselected")

        if not self.Portfolio.Invested:
            self.SetHoldings(self.aapl, 0.5)

    def OnSecuritiesChanged(self, changes):
        if len(changes.AddedSecurities) != 0 and self.UtcTime.date() > self.delistingDate:
            raise Exception("New securities added after ETF constituents were delisted")

        # Since we added the etf subscription it will get delisted and send us a removal event
        expectedChangesCount = self.universeSymbolCount + 1

        if self.universeSelectionDone:
            # "_universeSymbolCount + 1" because selection is done right away,
            # so AddedSecurities includes all ETF constituents (including APPL) plus GDVD
            self.universeAdded = self.universeAdded or len(changes.AddedSecurities) == expectedChangesCount

        # TODO: shouldn't be sending AAPL as a removed security since it was added by another universe
        self.universeRemoved = self.universeRemoved or (
            len(changes.RemovedSecurities) == expectedChangesCount and
            self.UtcTime.date() >= self.delistingDate and
            self.UtcTime.date() < self.EndDate.date())

    def OnEndOfAlgorithm(self):
        if not self.universeAdded:
            raise Exception("ETF constituent universe was never added to the algorithm")
        if not self.universeRemoved:
            raise Exception("ETF constituent universe was not removed from the algorithm after delisting")
        if len(self.ActiveSecurities) > 2:
            raise Exception(f"Expected less than 2 securities after algorithm ended, found {len(self.Securities)}")
