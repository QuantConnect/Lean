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

from AlgorithmImports import *

### <summary>
### This regression test tests for the loading of futures options contracts with a contract month of 2020-03 can live
### and be loaded from the same ZIP file that the 2020-04 contract month Future Option contract lives in.
### </summary>
class FutureOptionMultipleContractsInDifferentContractMonthsWithSameUnderlyingFutureRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.expectedSymbols = {
            self._createOption(datetime(2020, 3, 26), OptionRight.Call, 1650.0): False,
            self._createOption(datetime(2020, 3, 26), OptionRight.Put, 1540.0): False,
            self._createOption(datetime(2020, 2, 25), OptionRight.Call, 1600.0): False,
            self._createOption(datetime(2020, 2, 25), OptionRight.Put, 1545.0): False
        }


        # Required for FOPs to use extended hours, until GH #6491 is addressed
        self.UniverseSettings.ExtendedMarketHours = True

        self.SetStartDate(2020, 1, 4)
        self.SetEndDate(2020, 1, 6)

        goldFutures = self.AddFuture("GC", Resolution.Minute, Market.COMEX, extendedMarketHours=True)
        goldFutures.SetFilter(0, 365)

        self.AddFutureOption(goldFutures.Symbol)

    def OnData(self, data: Slice):
        for symbol in data.QuoteBars.Keys:
            # Check that we are in regular hours, we can place a market order (on extended hours, limit orders should be used)
            if symbol in self.expectedSymbols and self.IsInRegularHours(symbol):
                invested = self.expectedSymbols[symbol]
                if not invested:
                    self.MarketOrder(symbol, 1)

                self.expectedSymbols[symbol] = True

    def OnEndOfAlgorithm(self):
        notEncountered = [str(k) for k,v in self.expectedSymbols.items() if not v]
        if any(notEncountered):
            raise AggregateException(f"Expected all Symbols encountered and invested in, but the following were not found: {', '.join(notEncountered)}")

        if not self.Portfolio.Invested:
            raise AggregateException("Expected holdings at the end of algorithm, but none were found.")

    def IsInRegularHours(self, symbol):
        return self.Securities[symbol].Exchange.ExchangeOpen

    def _createOption(self, expiry: datetime, optionRight: OptionRight, strikePrice: float) -> Symbol:
        return Symbol.CreateOption(
            Symbol.CreateFuture("GC", Market.COMEX, datetime(2020, 4, 28)),
            Market.COMEX,
            OptionStyle.American,
            optionRight,
            strikePrice,
            expiry
        )
