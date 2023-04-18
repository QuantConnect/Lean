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
### This regression algorithm asserts that futures have data at extended market hours when this is enabled.
### </summary>
class FutureContractsExtendedMarketHoursRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 6)
        self.SetEndDate(2013, 10, 11)

        esFutureSymbol = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, DateTime(2013, 12, 20))
        self._es = self.AddFutureContract(esFutureSymbol, Resolution.Hour, fillForward=True, extendedMarket=True)

        gcFutureSymbol = Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, DateTime(2013, 10, 29))
        self._gc = self.AddFutureContract(gcFutureSymbol, Resolution.Hour, fillForward=True, extendedMarket=False)

        self._esRanOnRegularHours = False
        self._esRanOnExtendedHours = False
        self._gcRanOnRegularHours = False
        self._gcRanOnExtendedHours = False

    def OnData(self, slice):
        sliceSymbols = set(slice.Keys)
        sliceSymbols.update(slice.Bars.Keys)
        sliceSymbols.update(slice.Ticks.Keys)
        sliceSymbols.update(slice.QuoteBars.Keys)
        sliceSymbols.update([x.Canonical for x in sliceSymbols])

        esIsInRegularHours = self._es.Exchange.Hours.IsOpen(self.Time, False)
        esIsInExtendedHours = not esIsInRegularHours and self._es.Exchange.Hours.IsOpen(self.Time, True)
        sliceHasESData = self._es.Symbol in sliceSymbols
        self._esRanOnRegularHours |= esIsInRegularHours and sliceHasESData
        self._esRanOnExtendedHours |= esIsInExtendedHours and sliceHasESData

        gcIsInRegularHours = self._gc.Exchange.Hours.IsOpen(self.Time, False)
        gcIsInExtendedHours = not gcIsInRegularHours and self._gc.Exchange.Hours.IsOpen(self.Time, True)
        sliceHasGCData = self._gc.Symbol in sliceSymbols
        self._gcRanOnRegularHours |= gcIsInRegularHours and sliceHasGCData
        self._gcRanOnExtendedHours |= gcIsInExtendedHours and sliceHasGCData

    def OnEndOfAlgorithm(self):
        if not self._esRanOnRegularHours:
            raise Exception(f"Algorithm should have run on regular hours for {self._es.Symbol} future, which enabled extended market hours")

        if not self._esRanOnExtendedHours:
            raise Exception(f"Algorithm should have run on extended hours for {self._es.Symbol} future, which enabled extended market hours")

        if not self._gcRanOnRegularHours:
            raise Exception(f"Algorithm should have run on regular hours for {self._gc.Symbol} future, which did not enable extended market hours")

        if self._gcRanOnExtendedHours:
            raise Exception(f"Algorithm should have not run on extended hours for {self._gc.Symbol} future, which did not enable extended market hours")
