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
### Tests filtering in coarse selection by shortable quantity
### </summary>
class AllShortableSymbolsCoarseSelectionRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self._20140325 = datetime(2014, 3, 25)
        self._20140326 = datetime(2014, 3, 26)
        self._20140327 = datetime(2014, 3, 27)
        self._20140328 = datetime(2014, 3, 28)
        self._20140329 = datetime(2014, 3, 29)
        self.lastTradeDate = datetime(1,1,1)

        self._aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA)
        self._bac = Symbol.Create("BAC", SecurityType.Equity, Market.USA)
        self._gme = Symbol.Create("GME", SecurityType.Equity, Market.USA)
        self._goog = Symbol.Create("GOOG", SecurityType.Equity, Market.USA)
        self._qqq = Symbol.Create("QQQ", SecurityType.Equity, Market.USA)
        self._spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA)

        self.coarseSelected = { self._20140325: False, self._20140326: False, self._20140327:False, self._20140328:False }
        self.expectedSymbols = { self._20140325: [ self._bac, self._qqq, self._spy ],
                                self._20140326: [ self._spy ],
                                self._20140327: [ self._aapl, self._bac, self._gme, self._qqq, self._spy ],
                                self._20140328: [ self._goog ],
                                self._20140329: []}

        self.SetStartDate(2014, 3, 25)
        self.SetEndDate(2014, 3, 29)
        self.SetCash(10000000)
        self.shortableProvider = RegressionTestShortableProvider();
        self.security = self.AddEquity(self._spy)

        self.AddUniverse(self.CoarseSelection)
        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetBrokerageModel(AllShortableSymbolsRegressionAlgorithmBrokerageModel(self.shortableProvider))

    def OnData(self, data):
        if self.Time.date() == self.lastTradeDate:
            return

        for symbol in sorted(self.ActiveSecurities.Keys, key = lambda x:x.Value):
            if (not self.Portfolio.ContainsKey(symbol)) or (not self.Portfolio[symbol].Invested):
                if not self.Shortable(symbol):
                    raise Exception(f"Expected {symbol} to be shortable on {self.Time.strftime('%Y%m%d')}")

                """
                Buy at least once into all Symbols. Since daily data will always use
                MOO orders, it makes the testing of liquidating buying into Symbols difficult.
                """
                self.MarketOrder(symbol, -self.ShortableQuantity(symbol))
                self.lastTradeDate = self.Time.date()

    def CoarseSelection(self, coarse):
        shortableSymbols = self.shortableProvider.AllShortableSymbols(self.Time)
        selectedSymbols = list(sorted(filter(lambda x: (x in shortableSymbols.keys()) and (shortableSymbols[x] >= 500), map(lambda x: x.Symbol, coarse)), key= lambda x: x.Value))

        expectedMissing = 0
        if self.Time.date() == self._20140327.date():
            gme = Symbol.Create("GME", SecurityType.Equity, Market.USA)
            if gme not in shortableSymbols.keys():
                raise Exception("Expected unmapped GME in shortable symbols list on 2014-03-27")
            if "GME" not in list(map(lambda x: x.Symbol.Value, coarse)):
                raise Exception("Expected mapped GME in coarse symbols on 2014-03-27")

            expectedMissing = 1

        missing = list(filter(lambda x: x not in selectedSymbols, self.expectedSymbols[self.Time]))
        if len(missing) != expectedMissing:
            raise Exception(f"Expected Symbols selected on {self.Time.strftime('%Y%m%d')} to match expected Symbols, but the following Symbols were missing: {', '.join(list(map(lambda x:x.Value, missing)))}")

        self.coarseSelected[self.Time] = True;
        return selectedSymbols

    def OnEndOfAlgorithm(self):
        if not all(x for x in self.coarseSelected.values()):
            raise Exception(f"Expected coarse selection on all dates, but didn't run on: {', '.join(list(map(lambda x: x.Key.strftime('%Y%m%d'), filter(lambda x:not x.Value, self.coarseSelected))))}")

class AllShortableSymbolsRegressionAlgorithmBrokerageModel(DefaultBrokerageModel):
    def __init__(self, shortableProvider):
        self.shortableProvider = shortableProvider
        super().__init__()

    def GetShortableProvider(self, security):
        return self.shortableProvider

class RegressionTestShortableProvider(LocalDiskShortableProvider):
    def __init__(self):
        super().__init__("testbrokerage")

    """
    Gets a list of all shortable Symbols, including the quantity shortable as a Dictionary.
    """
    def AllShortableSymbols(self, localtime):
        shortableDataDirectory = os.path.join(Globals.DataFolder, "equity", Market.USA, "shortable", self.Brokerage)
        allSymbols = {}

        """
        Check backwards up to one week to see if we can source a previous file.
        If not, then we return a list of all Symbols with quantity set to zero.
        """
        i = 0
        while i <= 7:
            shortableListFile = os.path.join(shortableDataDirectory, "dates", f"{(localtime - timedelta(days=i)).strftime('%Y%m%d')}.csv")

            for line in Extensions.ReadLines(self.DataProvider, shortableListFile):
                csv = line.split(',')
                ticker = csv[0]

                symbol = Symbol(SecurityIdentifier.GenerateEquity(ticker, Market.USA, mappingResolveDate = localtime), ticker)
                quantity = int(csv[1])
                allSymbols[symbol] = quantity;

            if len(allSymbols) > 0:
                return allSymbols

            i += 1

        # Return our empty dictionary if we did not find a file to extract
        return allSymbols
