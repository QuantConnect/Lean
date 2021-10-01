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
from datetime import datetime, timedelta
from collections import deque

### <summary>
### Demonstration algorithm showing how to warm up indicators in a dynamic universe using IndicatorBatchUpdate and ConsolidatorBatchUpdate.
### </summary>

class IndicatorBatchUpdateAlgorithm(QCAlgorithm):
    '''Demonstration algorithm showing how to warm up indicators in a dynamic universe using IndicatorBatchUpdate and ConsolidatorBatchUpdate.'''
    def Initialize(self):
        self.SetStartDate(2013, 10, 9)  
        self.SetEndDate(2013, 10, 11)  
        self.SetCash(100000)  
        self.symbolDataDict = {}
        self.UniverseSettings.Resolution = Resolution.Minute
        self.UniverseSettings.MinimumTimeInUniverse = 10
        self.AddUniverse(self.Universe.Top(10))
        self.AddEquity("SPY", Resolution.Minute)
        self.AddAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(1)))
        self.Settings.RebalancePortfolioOnInsightChanges = False 
        self.Settings.RebalancePortfolioOnSecurityChanges = True
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

    def OnSecuritiesChanged(self, changes):
        for security in changes.RemovedSecurities:
            symbolData = self.symbolDataDict.pop(security.Symbol, None)
            if symbolData is not None:
                symbolData.Clear()

        for security in changes.AddedSecurities:
            if security.Symbol not in self.symbolDataDict:
                self.symbolDataDict[security.Symbol] = SymbolData(self, security)



class SymbolData:
    def __init__(self, algorithm, security, auto_warmup = True):
        self.algorithm = algorithm
        self.Security = security
        self.Symbol = security.Symbol
        resolution = Resolution.Minute 
        n_bars_per_day = security.Exchange.Hours.RegularMarketDuration.seconds//Extensions.ToTimeSpan(resolution).seconds
        period = int(n_bars_per_day*security.Exchange.TradingDaysPerYear) # corresponds to number of bars per year
        sma = SimpleMovingAverage(period)
        atr = AverageTrueRange(period)
        midPoint = CustomMidPointIndicator()
        indicators = [sma, atr, midPoint]
        self.RegisterIndicators(indicators, resolution)

        if auto_warmup:
            # warm up indicators using IndicatorBatchUpdate
            algorithm.IndicatorBatchUpdate(security.Symbol, indicators, period, resolution)
            self.LogIndicatorStates(indicators)

        # add some more indicators to demonstrate the use of ConsolidatorBatchUpdate
        self.ema = ExponentialMovingAverage(period)
        self.avgSpread = AverageBidAskSpreadIndicator(period)
        indicators.extend([self.ema, self.avgSpread])
        self.consolidator = QuoteBarConsolidator(timedelta(minutes=1))
        self.consolidator.DataConsolidated += self.OnDataConsolidated
        algorithm.SubscriptionManager.AddConsolidator(security.Symbol, self.consolidator)

        if auto_warmup:
            # warm up using ConsolidatorBatchUpdate
            algorithm.ConsolidatorBatchUpdate(security.Symbol, self.consolidator, period + 1, resolution)   
            self.LogIndicatorStates(indicators)

      
    def OnDataConsolidated(self, sender, quoteBar):
        self.ema.Update(quoteBar.Time, quoteBar.Ask.Close)
        self.avgSpread.Update(quoteBar)


    def LogIndicatorStates(self, indicators):
        for indicator in indicators:
            self.algorithm.Log(f"Indicator {indicator.Name} for security {self.Symbol.Value} ready ??? --> {indicator.IsReady}.")
    
    def RegisterIndicators(self, indicators, resolution):
        for indicator in indicators:
            self.algorithm.RegisterIndicator(self.Symbol, indicator, resolution)

    def Clear(self):
        self.algorithm.SubscriptionManager.RemoveConsolidator(self.Symbol, self.consolidator)
        for indicator in self.indicators:
            indicator.Reset()



class AverageBidAskSpreadIndicator(PythonIndicator):
    '''Ihe AverageBidAskSpreadIndicator computes the bid-ask spread and smoothes it over a given period.'''
    def __init__(self, period):
        self.period = period
        self.Name = self.__class__.__name__
        self.Time = datetime.min
        self.Value = 0
        self.rollingSum = 0
        self.queue = deque(maxlen=period)
        self.Samples = 0
    
    def Update(self, quoteBar):
        self.Time = quoteBar.EndTime
        spread = quoteBar.Ask.Close - quoteBar.Bid.Close
        if len(self.queue) == self.queue.maxlen:
            self.rollingSum -= self.queue[-1]
        self.queue.appendleft(spread)
        self.rollingSum += spread
        self.Value = self.rollingSum/len(self.queue)
        self.Samples += 1

    @property
    def IsReady(self):
        return len(self.queue) == self.queue.maxlen 


class CustomMidPointIndicator(PythonIndicator):
    '''The CustomMidPointIndicator computes the midpoint of a bar whereas the midpoint is defined as (High - Low)/2.'''
    def __init__(self): 
        self.Name = self.__class__.__name__
        self.Time = datetime.min 
        self.Value = 0 
        self.Samples = 0
    
    def Update(self, data):
        self.Time = data.EndTime 
        self.Value = (data.High - data.Low)/2
        self.Samples += 1
    
    @property
    def IsReady(self):
        return self.Time > datetime.min 



