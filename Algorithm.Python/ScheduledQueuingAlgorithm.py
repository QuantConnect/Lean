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
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *

from queue import Queue

class ScheduledQueuingAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2020, 9, 1)
        self.SetEndDate(2020, 9, 2)
        self.SetCash(100000)
        
        self.__numberOfSymbols = 2000
        self.__numberOfSymbolsFine = 1000
        self.SetUniverseSelection(FineFundamentalUniverseSelectionModel(self.CoarseSelectionFunction, self.FineSelectionFunction, None, None))
        
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        self.SetExecution(ImmediateExecutionModel())
        
        self.queue = Queue()
        self.dequeue_size = 100
        
        self.AddEquity("SPY", Resolution.Minute)
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.At(0, 0), self.FillQueue)
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.Every(timedelta(minutes=60)), self.TakeFromQueue)

    def CoarseSelectionFunction(self, coarse):
        has_fundamentals = [security for security in coarse if security.HasFundamentalData]
        sorted_by_dollar_volume = sorted(has_fundamentals, key=lambda x: x.DollarVolume, reverse=True)
        return [ x.Symbol for x in sorted_by_dollar_volume[:self.__numberOfSymbols] ]
    
    def FineSelectionFunction(self, fine):
        sorted_by_pe_ratio = sorted(fine, key=lambda x: x.ValuationRatios.PERatio, reverse=True)
        return [ x.Symbol for x in sorted_by_pe_ratio[:self.__numberOfSymbolsFine] ]
        
    def FillQueue(self):
        securities = [security for security in self.ActiveSecurities.Values if security.Fundamentals is not None]
        
        # Fill queue with symbols sorted by PE ratio (decreasing order)
        self.queue.queue.clear()
        sorted_by_pe_ratio = sorted(securities, key=lambda x: x.Fundamentals.ValuationRatios.PERatio, reverse=True)
        for security in sorted_by_pe_ratio:
            self.queue.put(security.Symbol)
        
    def TakeFromQueue(self):
        symbols = [self.queue.get() for _ in range(min(self.dequeue_size, self.queue.qsize()))]
        self.History(symbols, 10, Resolution.Daily)
        
        self.Log(f"Symbols at {self.Time}: {[str(symbol) for symbol in symbols]}")