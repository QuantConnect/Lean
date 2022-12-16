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

    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2014, 10, 10)
        self.SetCash(1000000)
        
        self.symbolData = {}
        
        futures = [
            Futures.Indices.SP500EMini
        ]

        for future in futures:
            # Requesting data
            continuous_contract = self.AddFuture(future,
                resolution = Resolution.Daily,
                extendedMarketHours = True,
                dataNormalizationMode = DataNormalizationMode.BackwardsRatio,
                dataMappingMode = DataMappingMode.OpenInterest,
                contractDepthOffset = 0
            )
            
            symbolData = SymbolData(self, continuous_contract)
            self.symbolData[continuous_contract] = symbolData

    def OnData(self, slice):
        for future, symbolData in self.symbolData.items():
            symbol = future.Symbol
            
            # Accessing data
            if slice.SymbolChangedEvents.ContainsKey(symbol):
                changedEvent = slice.SymbolChangedEvents[symbol]
                old_symbol = changedEvent.OldSymbol
                new_symbol = changedEvent.NewSymbol
                tag = f"Rollover - Symbol changed at {self.Time}: {old_symbol} -> {new_symbol}"
                quantity = self.Portfolio[old_symbol].Quantity
                
                # Rolling over: to liquidate any position of the old mapped contract and switch to the newly mapped contract
                self.Liquidate(old_symbol, tag=tag)
                self.MarketOrder(new_symbol, quantity, tag=tag)

                symbolData.Reset()
                
            mapped_symbol = future.Mapped
            ema = symbolData.EMA

            if mapped_symbol is not None and slice.Bars.ContainsKey(symbol) and ema.IsReady:
                if ema.Current.Value < slice.Bars[symbol].Price and not self.Portfolio[mapped_symbol].IsLong:
                    self.MarketOrder(mapped_symbol, 1)
                elif ema.Current.Value > slice.Bars[symbol].Price and not self.Portfolio[mapped_symbol].IsShort:
                    self.MarketOrder(mapped_symbol, -1)
    
class SymbolData:
    
    def __init__(self, algorithm, future):
        self._algorithm = algorithm
        self._symbol = future.Symbol
        self.EMA = algorithm.EMA(future.Symbol, 20, Resolution.Daily)

        self.Reset()
        
    def Reset(self):
        self.EMA.Reset()
        self._algorithm.WarmUpIndicator(self._symbol, self.EMA, Resolution.Daily)
