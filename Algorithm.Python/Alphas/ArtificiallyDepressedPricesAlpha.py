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
    Assets whose current trading volume is below its average volume and
    whose price is below its recent average are sometiems viewed as being
    priced artificially low, and as such will rebound in value once trading
    volume picks up again. Using Simple Moving Average indicators, we check
    for these conditions and generate insights acoordingly for the top 50
    assets by Dollar Volume using a Coarse Universe selection function.



    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.
'''

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Selection import * 
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioTarget, EqualWeightingPortfolioConstructionModel

import numpy as np
from datetime import timedelta, datetime


class VolumeValueDirectionAlgorithm(QCAlgorithmFramework):

    def Initialize(self):

        self.SetStartDate(2018, 1, 1)   #Set Start Date
        self.SetCash(100000)            #Set Strategy Cash

        ## To be used later in Universe Selection
        self.symbols = None
        self.week = None

        ## Coarse/Fine Universe Selection
        self.UniverseSettings.Resolution = Resolution.Hour
        self.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelectionFunction) )

        ## Set $0 fees
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))

        ## Set our custom Alpha Model
        self.SetAlpha(VolumeValueAnalysisAlphaModel())
    
        ## Set equal weighting portfolio contruction
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
    
        ## Set immediate execution
        self.SetExecution(ImmediateExecutionModel())
    
        ## Set null risk management
        self.SetRiskManagement(NullRiskManagementModel())
    

    def CoarseSelectionFunction(self, coarse):
    
        ## This control ensures that we only update our universe once per week
        current_week = self.Time.isocalendar()[1]
        if  current_week == self.week:
            return self.symbols
        self.week = current_week
        
        ## Sort by dollar volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

        filtered = [ x.Symbol for x in sortedByDollarVolume ]
        
        ## Take top 25 companies
        self.symbols = filtered[:25]

        return self.symbols


class VolumeValueAnalysisAlphaModel:
    
    def __init__(self, *args, **kwargs):
        '''
            Initialize variables and Symbol Data dictionary to use in our Alpha Model
        '''
        self.lookback = kwargs['lookback'] if 'lookback' in kwargs else 10          ## This will set the SMA period, so the default is a 5-day SMA
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), 3) ## Arbitrary
        self.symbolDataBySymbol = {}
        self.close = {}
        self.volume = {}
        
        
    def Update(self, algorithm, data):
        insights = []
        
        ## Loop through all of the Symbol Data objects in the dictionary
        for symbol, symbolData in self.symbolDataBySymbol.items():

            # Update the symbolData with new data 
            if not symbolData.Update(data): return insights

            ## Check to see if both current asset price and volume are above their respective SMA values.
            ## If so, emit insights accordingly
            if symbolData.IsUpTrend():
                magnitude = symbolData.CloseSMA.Current.Value - data[symbol].Close
                insights.append(Insight(symbolData.Symbol, self.prediction_interval, InsightType.Price, InsightDirection.Up, magnitude, None))

        return insights
        
    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for removed in changes.RemovedSecurities:
            algorithm.Log('Removed: ' + str(removed.Symbol))
            symbolData = self.symbolDataBySymbol.pop(removed.Symbol, None)
            if symbolData is not None:
                symbolData.RemoveConsolidators(algorithm)


        # initialize data for added securities
        symbols = [ x.Symbol for x in changes.AddedSecurities ]
        
        ## Get historical data to warm-up indicators
        history = algorithm.History(symbols, self.lookback, self.resolution)
        if history.empty: return

        tickers = history.index.levels[0]
        for ticker in tickers:
            symbol = SymbolCache.GetSymbol(ticker)

            if symbol not in self.symbolDataBySymbol:
                
                ## Create SymbolData objects for any new assets
                symbolData = SymbolData(symbol, self.lookback)
                self.symbolDataBySymbol[symbol] = symbolData
                symbolData.RegisterIndicators(algorithm, self.resolution)
                symbolData.WarmUpIndicators(history.loc[ticker])


class SymbolData:
    def __init__(self, symbol, lookback):
        self.Symbol = symbol
        self.Close = 0
        self.Volume = 0
        self.CloseSMA = SimpleMovingAverage(f'{symbol}.CloseSMA({lookback})', lookback)
        self.VolumeSMA = SimpleMovingAverage(f'{symbol}.VolumeSMA({lookback})', lookback)
        self.Consolidator = None
        
    def Update(self, data):
        ## Check to see if symbol is in the data dictionary
        if not data.Bars.ContainsKey(self.Symbol) or self.Symbol not in data.Keys:
            return False
        
        ## Update close, volume, and SMAs
        bar = data[self.Symbol]
        self.Close = bar.Close
        self.Volume = bar.Volume
        self.CloseSMA.Update(bar.EndTime, bar.Close)
        self.VolumeSMA.Update(bar.EndTime, bar.Volume)
        return True

    def RegisterIndicators(self, algorithm, resolution):
        self.Consolidator = algorithm.ResolveConsolidator(self.Symbol, resolution)
        algorithm.RegisterIndicator(self.Symbol, self.CloseSMA, self.Consolidator)
        algorithm.RegisterIndicator(self.Symbol, self.VolumeSMA, self.Consolidator)        

    def RemoveConsolidators(self, algorithm):
        if self.Consolidator is not None:
            algorithm.SubscriptionManager.RemoveConsolidator(self.Symbol, self.Consolidator)

    def WarmUpIndicators(self, history):
        for tuple in history.itertuples():
            self.Close = tuple.close
            self.Volume = tuple.volume
            self.CloseSMA.Update(tuple.Index, self.Close)
            self.VolumeSMA.Update(tuple.Index, self.Volume)
            
    def IsUpTrend(self):
        ## UpTrend is when the close and volume are below their respective means,
        ## and so the projection is that the price will rebound
        return self.Close < self.CloseSMA.Current.Value and \
               self.Volume < self.VolumeSMA.Current.Value