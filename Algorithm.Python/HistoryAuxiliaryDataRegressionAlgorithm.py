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
### Regression algorithm asserting the behavior of auxiliary data history requests
### </summary>
class HistoryAuxiliaryDataRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2021, 1, 1)
        self.SetEndDate(2021, 1, 5)

        aapl = self.AddEquity("AAPL", Resolution.Daily).Symbol

        dividend = self.History(Dividend, aapl, 360)
        self.Debug(str(dividend))
        if len(dividend) != 6:
            raise ValueError(f"Unexpected dividend count: {len(dividend)}")
        for distribution in dividend.distribution:
            if distribution == 0:
                raise ValueError(f"Unexpected distribution: {distribution}")

        split = self.History(Split, aapl, 360)
        self.Debug(str(split))
        if len(split) != 2:
            raise ValueError(f"Unexpected split count: {len(split)}")
        for splitfactor in split.splitfactor:
            if splitfactor == 0:
                raise ValueError(f"Unexpected splitfactor: {splitfactor}")

        symbol = Symbol.Create("BTCUSD", SecurityType.CryptoFuture, Market.Binance)
        marginInterest = self.History(MarginInterestRate, symbol, 24 * 3, Resolution.Hour)
        self.Debug(str(marginInterest))
        if len(marginInterest) != 8:
            raise ValueError(f"Unexpected margin interest count: {len(marginInterest)}")
        for interestrate in marginInterest.interestrate:
            if interestrate == 0:
                raise ValueError(f"Unexpected interestrate: {interestrate}")

        # last trading date on 2007-05-18
        delistedSymbol = Symbol.Create("AAA.1", SecurityType.Equity, Market.USA)
        delistings = self.History(Delisting, delistedSymbol, datetime(2007, 5, 15), datetime(2007, 5, 21))
        self.Debug(str(delistings))
        if len(delistings) != 2:
            raise ValueError(f"Unexpected delistings count: {len(delistings)}")
        if delistings.iloc[0].type != DelistingType.Warning:
            raise ValueError(f"Unexpected delisting: {delistings.iloc[0]}")
        if delistings.iloc[1].type != DelistingType.Delisted:
            raise ValueError(f"Unexpected delisting: {delistings.iloc[1]}")

        # get's remapped:
        # 2008-09-30 spwr -> spwra
        # 2011-11-17 spwra -> spwr
        remappedSymbol = Symbol.Create("SPWR", SecurityType.Equity, Market.USA)
        symbolChangedEvents = self.History(SymbolChangedEvent, remappedSymbol, datetime(2007, 1, 1), datetime(2012, 1, 1))
        self.Debug(str(symbolChangedEvents))
        if len(symbolChangedEvents) != 2:
            raise ValueError(f"Unexpected SymbolChangedEvents count: {len(symbolChangedEvents)}")
        firstEvent = symbolChangedEvents.iloc[0]
        if firstEvent.oldsymbol != "SPWR" or firstEvent.newsymbol != "SPWRA" or symbolChangedEvents.index[0][1] != datetime(2008, 9, 30):
            raise ValueError(f"Unexpected SymbolChangedEvents: {firstEvent}")
        secondEvent = symbolChangedEvents.iloc[1]
        if secondEvent.newsymbol != "SPWR" or secondEvent.oldsymbol != "SPWRA" or symbolChangedEvents.index[1][1] != datetime(2011, 11, 17):
            raise ValueError(f"Unexpected SymbolChangedEvents: {secondEvent}")

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings("AAPL", 1)
