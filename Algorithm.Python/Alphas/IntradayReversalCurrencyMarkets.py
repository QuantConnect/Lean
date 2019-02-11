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
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import QCAlgorithmFrameworkBridge
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Indicators import *
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Data.Consolidators import *

import numpy as np
import pandas as pd
import decimal as d
from datetime import datetime, timedelta

class IntradayReversalCurrencyMarketsFrameworkAlgorithm(QCAlgorithmFramework):

    def Initialize(self):
    
        ## Set testing timeframe and starting cash
        self.SetStartDate(2019, 1, 1)
        self.SetCash(100000)
        
        # Select resolution
        resolution = Resolution.Hour
        
        # Set requested data resolution
        self.UniverseSettings.Resolution = resolution
        
        # Add currency pair
        self.AddForex("EURUSD", resolution)

        self.SetUniverseSelection(ManualUniverseSelectionModel())
        
        self.SetAlpha(IntradayReversalAlphaModel(5, resolution))
        
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())
        

class IntradayReversalAlphaModel(AlphaModel):
    '''Alpha model that uses a Price/SMA Crossover to create insights on Hourly Frequency.
    Frequency: Hourly data.
    Technical indicator: 5-hour simple moving average.
    Strategy:
    Reversal strategy that goes Long when price crosses below SMA and Short when price crosses above SMA.
    The trading strategy is implemented only between 10AM - 3PM (NY time)'''

    # Initialize variables
    def __init__(self,
                period_sma = 5,
                resolution = Resolution.Hour):
                    
        '''Initializes a new instance of the AlphaModel class
        Args:
            period_sma: The SMA period'''
        
        self.period_sma = period_sma
        self.resolution = resolution

        self.Name = 'IntradayReversalAlphaModel'
    
    def Update(self, algorithm, data):
        '''Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        
        # Set the time to close all positions at 3PM
        self.timeToClose = datetime(algorithm.Time.year, algorithm.Time.month, algorithm.Time.day, 15, 1, 00, tzinfo = algorithm.Time.tzinfo)
        
        insights = []

        for kvp in algorithm.ActiveSecurities:
            security = kvp.Value
            
            if self.ShouldEmitInsight(algorithm, security.Symbol):
                
                direction = InsightDirection.Flat
                
                if self.calculations[security.Symbol].is_uptrend(algorithm.Securities[security.Symbol].Price):
                    direction = InsightDirection.Up
                
                else:
                    direction = InsightDirection.Down
    
                # Ignore signal for same direction as previous signal (when no crossover)
                if direction == self.calculations[security.Symbol].PreviousDirection:
                    continue
                 
                # Update the Prediction Interval so insight goes Flat by timeToClose
                self.predictionInterval = self.timeToClose - algorithm.Time
                
                # Generate insight
                insight = Insight.Price(security.Symbol, self.predictionInterval, direction)
                # Save the current Insight Direction to check when the crossover happens
                self.calculations[security.Symbol].PreviousDirection = insight.Direction
                insights.append(insight)

        return insights
        
        
    def OnSecuritiesChanged(self, algorithm, changes):
        '''
        Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm
        '''
        
        self.calculations = {}
        
        pairs = []
        
        for kvp in algorithm.ActiveSecurities:
            security = kvp.Value
            pairs.append(security.Symbol)
        
        # Get historical data to warm-up our SMA indicator
        history = algorithm.History(pairs, 5, self.resolution)
        
        for ticker in pairs:
            
            if (str(ticker) not in history.index
            or history.loc[str(ticker)].get('close') is None
            or history.loc[str(ticker)].get('close').isna().any()):
                continue
    
            else:
                self.calculations[ticker] = SymbolData(ticker, self.period_sma)
                self.calculations[ticker].RegisterIndicators(algorithm, resolution = self.resolution)
                self.calculations[ticker].WarmUpIndicators(history)
        
    def ShouldEmitInsight(self, algorithm, symbol):

        securityPrice = algorithm.Securities[symbol].Price
        
        # Time to control when to start and finish emitting (10AM to 3PM)
        insightTimeStart = datetime(algorithm.Time.year, algorithm.Time.month, algorithm.Time.day, 10, 00, 00, tzinfo = algorithm.Time.tzinfo).time()
        insightTimeEnd = datetime(algorithm.Time.year, algorithm.Time.month, algorithm.Time.day, 15, 00, 00, tzinfo = algorithm.Time.tzinfo).time()
        
        currentTime = algorithm.Time.time()
        
        if not securityPrice != 0 or currentTime < insightTimeStart or currentTime > insightTimeEnd:
            return False
        else:
            return True

class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    
    def __init__(self, symbol, period_sma):
        
        self.Symbol = symbol
        
        self.priceSMA = SimpleMovingAverage(period_sma)
        self.tolerance = d.Decimal(1.001) # Tolerance level to avoid false signals with tight crossovers

        self.PreviousDirection = None
        
        self.Consolidator = None
        
    def RegisterIndicators(self, algorithm, resolution):
        # Register our SMA indicator
        self.Consolidator = algorithm.ResolveConsolidator(self.Symbol, resolution)
        algorithm.RegisterIndicator(self.Symbol, self.priceSMA, self.Consolidator)
            
    def WarmUpIndicators(self, history):
        # Warm-up our SMA indicator
        for index, row in history.loc[str(self.Symbol)].iterrows():
            if "close" in row:
                self.priceSMA.Update(index, row["close"])
                
    def is_uptrend(self, price):
        # Logic for the Price/SMA Crossover
        if self.priceSMA.IsReady:
            return price < self.priceSMA.Current.Value * self.tolerance
        else:
            return False
