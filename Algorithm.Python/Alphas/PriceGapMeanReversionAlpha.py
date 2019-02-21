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


'''
    The motivating idea for this Alpha Model is that a large price gap (here we use true outliers --
    price gaps that whose absolutely values are greater than 3 * Volatility) is due to rebound 
    back to an appropriate price or at least retreat from its brief extreme. Using a Coarse Universe selection
    function, the algorithm selects the top x-companies by Dollar Volume (x can be any number you choose)
    to trade with, and then uses the Standard Deviation of the 100 most-recent closing prices to determine
    which price movements are outliers that warrant emitting insights.



    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.
'''

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.Market import TradeBar
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Algorithm.Framework.Selection import * 
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Indicators import RollingWindow, SimpleMovingAverage
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioTarget, EqualWeightingPortfolioConstructionModel

import numpy as np
from datetime import timedelta, datetime

class PriceGapMeanReversionAlgorithm(QCAlgorithmFramework):

    def Initialize(self):
        
        self.SetStartDate(2018, 1, 1)   #Set Start Date
        self.SetCash(100000)           #Set Strategy Cash

        ## Initialize variables to be used in controlling frequency of universe selection
        self.week = None
        self.symbols = None
        
        
        ## Manual Universe Selection
        self.UniverseSettings.Resolution = Resolution.Minute
        self.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelectionFunction))
        
        ## Set trading fees to $0
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))
        
        ## Set custom Alpha Model
        self.SetAlpha(PriceGapMeanReversionAlphaModel())
        
        ## Set equal-weighting Portfolio Construction Model
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        ## Set Execution Model
        self.SetExecution(ImmediateExecutionModel())
        
        ## Set Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())
        
    
    
    def CoarseSelectionFunction(self, coarse):
        ## If it isn't a new week, return the same symbols
        current_week = self.Time.isocalendar()[1]
        if  current_week == self.week:
            return self.symbols
        self.week = current_week
        ## If its a new month, then re-filter stocks by Dollar Volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

        self.symbols = [ x.Symbol for x in sortedByDollarVolume[:25] ]
        return self.symbols

        
        
class PriceGapMeanReversionAlphaModel:

    def __init__(self, *args, **kwargs):
        ''' Initialize variables and dictionary for Symbol Data to support algorithm's function '''
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Minute
        self.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), 5) ## Arbitrary
        self.symbolDataBySymbol = {}
        
    def Update(self, algorithm, data):
        insights = []
        
        ## Loop through all Symbol Data objects
        for symbol, symbolData in self.symbolDataBySymbol.items():
            if symbol not in data.Keys:   ## Skip this slice if the data dictionary doesn't contain the symbol
                continue
            
            security = algorithm.Securities[symbol]
            
            ## Update the symbolData properties
            if not symbolData.Update(data, security): return insights
            
            ## Evaluate whether or not the price jump is expected to rebound up or return down, and emit insights accordingly
            if symbolData.DownTrend:
                insights.append(Insight(symbol, self.prediction_interval, InsightType.Volatility, InsightDirection.Down, -symbolData.PriceJump, None))
            elif symbolData.UpTrend:
                insights.append(Insight(symbol, self.prediction_interval, InsightType.Volatility, InsightDirection.Up, abs(symbolData.PriceJump), None))
            
        return insights


    def OnSecuritiesChanged(self, algorithm, changes):
        
        ## Make a history request for all securities that have been added
        history_request_symbols = [ x.Symbol for x in changes.AddedSecurities ]
        history_df = algorithm.History(history_request_symbols, 100, Resolution.Minute)
        

        for security in changes.AddedSecurities:
            if str(security.Symbol) not in history_df.index.get_level_values(0):
                continue
            history = history_df.loc[str(security.Symbol)]

            ## Create and initialize SymbolData objects
            symbolData = SymbolData(security)
            self.symbolDataBySymbol[security.Symbol] = symbolData
            for tuple in history.itertuples():
                bar = TradeBar(tuple.Index, security.Symbol, tuple.open, tuple.high, tuple.low, tuple.close, tuple.volume)
                symbolData.Initialize(bar, security)
                
    

class SymbolData:
    def __init__(self, security):
        self.Symbol = security.Symbol
        self.volatility = 0
        self.close = 0
        self.window = RollingWindow[TradeBar](2)
        self.delta = []

    def Update(self, data, security):

        ## Check for any data events that would return a NoneBar in the Alpha Model Update() method
        if not data.Bars.ContainsKey(self.Symbol) or self.Symbol not in data.Keys:
            return False
        
        ## Update Rolling Window and volatility
        self.window.Add(data[security.Symbol])
        self.close = data[security.Symbol].Close
        if self.window.Count > 1:
            self.delta.append((self.window[0].Open / self.window[1].Close) - 1)
            if len(self.delta) > 100:
                self.delta.pop(0)
            self.volatility = np.std(self.delta)
            
        return True

    # Initialize Rolling Window and volatility
    def Initialize(self, bar, security):
        self.window.Add(bar)
        self.close = bar.Close
        if self.window.Count > 1:
            self.delta.append((self.window[0].Open / self.window[1].Close) - 1)
            if len(self.delta) > 100:
                self.delta.pop(0)
        else:
            self.delta.append(0)
        self.volatility = np.std(self.delta)



    @property
    def Volatility(self):
        return self.volatility

    @property
    def PriceJump(self):
        return (self.window[0].Open / self.window[1].Close) - 1

    @property
    def DownTrend(self):
        price_jump = (self.window[0].Open / self.window[1].Close) - 1
        return (abs(price_jump) > 3*self.volatility) and (price_jump > 0)

    @property
    def UpTrend(self):
        price_jump = (self.window[0].Open / self.window[1].Close) - 1
        return (abs(price_jump) > 3*self.volatility) and (price_jump < 0)