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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from System.Collections.Generic import List

### <summary>
### In this algorithm we demonstrate how to perform some technical analysis as
### part of your coarse fundamental universe selection
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
class EmaCrossUniverse(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2019, 12, 23)
        self.SetEndDate(2020,1,15)
        self.SetCash(100000)
        self.UniverseSettings.Resolution = Resolution.Daily
        self.AddUniverse(self.CoarseSelectionFunction)
        self.averages = {}
        
    def CoarseSelectionFunction(self,coarse):
        
        sorted_by_volume = sorted([cf for cf in coarse if cf.Price > 10],
            key = lambda cf: cf.DollarVolume, reverse=True)
            
        selected = {cf.Symbol: cf for cf in sorted_by_volume[:100]}
        symbols = list(selected.keys())
        
        new_symbols = [s for s in symbols if s not in self.averages]
        if new_symbols:
            
            # Get history for all new symbols. If empty, log and return the selected -- WOW!
            history = self.History(new_symbols, 200, Resolution.Daily)
            if history.empty:
                self.Debug(f'Empty history on {self.Time} for {new_symbols}')
                return Universe.Unchanged   # Continue with previous universe
                
            # Only need the closing prices    
            history = history.close.unstack(0)
            
            # Add new item to self.averages
            for symbol in new_symbols:
                if symbol in history:
                    self.averages[symbol] = SelectionData(history[symbol].dropna())
                    
        self.symbols = []
        
        for symbol, cf in selected.items():
            symbolData = self.averages[symbol]
            
            if symbol not in new_symbols:
                symbolData.Update(cf.EndTime, cf.AdjustedPrice) 
                
            if symbolData.IsReady and symbolData.fast > symbolData.slow:
                self.symbols.append(symbol)
                
        return self.symbols
        
        
    def OnSecuritiesChanged(self, changes):
        for security in changes.RemovedSecurities:
            self.Liquidate(security.Symbol, f'Removed from Universe')
            
        self.can_trade = True
        
        
    def OnData(self, data):
        if self.can_trade:
            length = len(self.symbols)
            
            for symbol in self.symbols:
                self.SetHoldings(symbol, 1 / length)
                
            self.can_trade = False
            
            
class SelectionData:
    def __init__(self, history):
        
        self.slow = ExponentialMovingAverage(200)
        self.fast = ExponentialMovingAverage(50)
        
        for time, price in history.items():
            self.Update(time, price)
            
            
    def Update(self, time, price):
        self.fast.Update(time, price)
        self.slow.Update(time, price)
        
        
    @property
    def IsReady(self):
        return self.slow.IsReady and self.fast.IsReady