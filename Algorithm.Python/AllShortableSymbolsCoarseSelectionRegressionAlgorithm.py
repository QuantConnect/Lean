### QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
### Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
###
### Licensed under the Apache License, Version 2.0 (the "License");
### you may not use this file except in compliance with the License.
### You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
###
### Unless required by applicable law or agreed to in writing, software
### distributed under the License is distributed on an "AS IS" BASIS,
### WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
### See the License for the specific language governing permissions and
### limitations under the License.

from datetime import date

import QuantConnect 
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Brokerages import *
from QuantConnect.Data import *
from QuantConnect.Data.Shortable import *
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Interfaces import *
from QuantConnect import *


class AllShortableSymbolsRegressionAlgorithmBrokerageModel(DefaultBrokerageModel):
    def __init__(self):
        self.ShortableProvider = LocalDiskShortableProvider(SecurityType.Equity, "testbrokerage", Market.USA)

### <summary>
### Tests filtering in coarse selection by shortable quantity
### </summary>
class AllShortableSymbolsCoarseSelectionRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self._20140325 = date(2014, 3, 25);
        self._20140326 = date(2014, 3, 26);
        self._20140327 = date(2014, 3, 27);
        self._20140328 = date(2014, 3, 28);
        self._20140329 = date(2014, 3, 29);

        self.aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
        self.bac = QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA);
        self.gme = QuantConnect.Symbol.Create("GME", SecurityType.Equity, Market.USA);
        self.goog = QuantConnect.Symbol.Create("GOOG", SecurityType.Equity, Market.USA);
        self.qqq = QuantConnect.Symbol.Create("QQQ", SecurityType.Equity, Market.USA);
        self.spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        self.lastTradeDate = date(1, 1, 1);

        self.coarseSelected = {
            self._20140325: False,
            self._20140326: False,
            self._20140327: False,
            self._20140328: False
        }

        self.expectedSymbols = {
            self._20140325: [self.bac, self.qqq, self.spy],
            self._20140326: [self.spy],
            self._20140327: [self.aapl, self.bac, self.gme, self.qqq, self.spy],
            self._20140328: [self.goog],
            self._20140329: []
        }

        self.SetStartDate(2014, 3, 25);
        self.SetEndDate(2014, 3, 29);
        self.SetCash(10000000);

        self.AddUniverse(self.CoarseSelectionFunc);
        self.UniverseSettings.Resolution = QuantConnect.Resolution.Daily;

        self.SetBrokerageModel(AllShortableSymbolsRegressionAlgorithmBrokerageModel());

    def OnData(self, data):
        if self.Time.date() == self.lastTradeDate:
            return

        for symbol in self.ActiveSecurities.Keys:
            if not symbol in self.Portfolio or not self.Portfolio[symbol].Invested:
                if not self.Shortable(symbol):
                    raise Exception(f"Expected {symbol} to be shortable on {self.Time}")

                # Buy at least once into all Symbols. Since daily data will always use
                # MOO orders, it makes the testing of liquidating buying into Symbols difficult
                self.MarketOrder(symbol, -float(self.ShortableQuantity(symbol)))
                self.lastTradeDate = self.Time.date()

    def CoarseSelectionFunc(self, coarse):
        shortableSymbols = self.AllShortableSymbols();
        selectedSymbols = list(sorted([x.Symbol for x in coarse if x.Symbol in shortableSymbols and shortableSymbols[x.Symbol] >= 500]))

        expectedMissing = 0;
        if self.Time.date() == self._20140327:
            gme = QuantConnect.Symbol.Create("GME", SecurityType.Equity, Market.USA);
            if gme not in shortableSymbols:
                raise Exception("Expected unmapped GME in shortable symbols list on 2014-03-27");
            if len([x.Symbol.Value for x in coarse if x.Symbol.Value == "GME"]) == 0:
                raise Exception("Expected mapped GME in coarse symbols on 2014-03-27");

            expectedMissing = 1;

        missing = [i for i in self.expectedSymbols[self.Time.date()] if i not in selectedSymbols]
        if (len(missing) != expectedMissing):
            raise Exception(f"Expected Symbols selected on {self.Time.date()} to match expected Symbols, but the following Symbols were missing: {', '.join([str(s) for s in missing])}")

        self.coarseSelected[self.Time.date()] = True;
        return selectedSymbols

    def OnEndOfAlgorithm(self):
        if not all(list(self.coarseSelected.values())):
            raise Exception(f"Expected coarse selection on all dates, but didn't run on: {', '.join([str(k) for k, v in self.coarseSelected.items() if not v])}")