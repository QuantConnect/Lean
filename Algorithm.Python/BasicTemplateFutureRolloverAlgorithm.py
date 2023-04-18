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
### Example algorithm for trading continuous future
### </summary>
class BasicTemplateFutureRolloverAlgorithm(QCAlgorithm):

    ### <summary>
    ### Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    ### </summary>
    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2013, 12, 10)
        self.SetCash(1000000)

        self.symbol_data_by_symbol = {}
        
        futures = [
            Futures.Indices.SP500EMini
        ]

        for future in futures:
            # Requesting data
            continuous_contract = self.AddFuture(future,
                resolution = Resolution.Daily,
                extendedMarket = True,
                dataNormalizationMode = DataNormalizationMode.BackwardsRatio,
                dataMappingMode = DataMappingMode.OpenInterest,
                contractDepthOffset = 0
            )
            
            symbol_data = SymbolData(self, continuous_contract)
            self.symbol_data_by_symbol[continuous_contract.Symbol] = symbol_data

    ### <summary>
    ### OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    ### </summary>
    ### <param name="slice">Slice object keyed by symbol containing the stock data</param>
    def OnData(self, slice):
        for symbol, symbol_data in self.symbol_data_by_symbol.items():
            # Call SymbolData.Update() method to handle new data slice received
            symbol_data.Update(slice)
            
            # Check if information in SymbolData class and new slice data are ready for trading
            if not symbol_data.IsReady or not slice.Bars.ContainsKey(symbol):
                return
            
            ema_current_value = symbol_data.EMA.Current.Value
            if ema_current_value < symbol_data.Price and not symbol_data.IsLong:
                self.MarketOrder(symbol_data.Mapped, 1)
            elif ema_current_value > symbol_data.Price and not symbol_data.IsShort:
                self.MarketOrder(symbol_data.Mapped, -1)
    
### <summary>
### Abstracted class object to hold information (state, indicators, methods, etc.) from a Symbol/Security in a multi-security algorithm
### </summary>
class SymbolData:
    
    ### <summary>
    ### Constructor to instantiate the information needed to be hold
    ### </summary>
    def __init__(self, algorithm, future):
        self._algorithm = algorithm
        self._future = future
        self.EMA = algorithm.EMA(future.Symbol, 20, Resolution.Daily)
        self.Price = 0
        self.IsLong = False
        self.IsShort = False

        self.Reset()
    
    @property
    def Symbol(self):
        return self._future.Symbol
    
    @property
    def Mapped(self):
        return self._future.Mapped
    
    @property
    def IsReady(self):
        return self.Mapped is not None and self.EMA.IsReady
    
    ### <summary>
    ### Handler of new slice of data received
    ### </summary>
    def Update(self, slice):
        if slice.SymbolChangedEvents.ContainsKey(self.Symbol):
            changed_event = slice.SymbolChangedEvents[self.Symbol]
            old_symbol = changed_event.OldSymbol
            new_symbol = changed_event.NewSymbol
            tag = f"Rollover - Symbol changed at {self._algorithm.Time}: {old_symbol} -> {new_symbol}"
            quantity = self._algorithm.Portfolio[old_symbol].Quantity

            # Rolling over: to liquidate any position of the old mapped contract and switch to the newly mapped contract
            self._algorithm.Liquidate(old_symbol, tag = tag)
            self._algorithm.MarketOrder(new_symbol, quantity, tag = tag)

            self.Reset()
        
        self.Price = slice.Bars[self.Symbol].Price if slice.Bars.ContainsKey(self.Symbol) else self.Price
        self.IsLong = self._algorithm.Portfolio[self.Mapped].IsLong
        self.IsShort = self._algorithm.Portfolio[self.Mapped].IsShort
        
    ### <summary>
    ### Reset RollingWindow/indicator to adapt to newly mapped contract, then warm up the RollingWindow/indicator
    ### </summary>
    def Reset(self):
        self.EMA.Reset()
        self._algorithm.WarmUpIndicator(self.Symbol, self.EMA, Resolution.Daily)
            
    ### <summary>
    ### Disposal method to remove consolidator/update method handler, and reset RollingWindow/indicator to free up memory and speed
    ### </summary>
    def Dispose(self):
        self.EMA.Reset()