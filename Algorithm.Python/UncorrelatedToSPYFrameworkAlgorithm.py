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

from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel
from Selection.UncorrelatedToSPYUniverseSelectionModel import UncorrelatedToSPYUniverseSelectionModel

from datetime import datetime, timedelta

class UncorrelatedToSPYFrameworkAlgorithm(QCAlgorithmFramework):

    def Initialize(self):
        
        self.UniverseSettings.Resolution = Resolution.Daily
        
        self.SetStartDate(2019,2,2)   # Set Start Date
        self.SetEndDate(2019,3,15)    # Set End Date
        self.SetCash(100000)          # Set Strategy Cash

        self.SetUniverseSelection(UncorrelatedToSPYUniverseSelectionModel())
        self.SetAlpha(UncorrelatedToSPYAlphaModel())
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())


class UncorrelatedToSPYAlphaModel(AlphaModel):
    '''Uses ranking of intraday percentage difference between open price and close price to create magnitude and direction prediction for insights'''

    def __init__(self, *args, **kwargs): 
        self.lookback = kwargs['lookback'] if 'lookback' in kwargs else 1
        self.numberOfStocks = kwargs['numberOfStocks'] if 'numberOfStocks' in kwargs else 10
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), self.lookback)
        self.symbolDataBySymbol = {}

    def Update(self, algorithm, data):
        
        insights = []
        ret = []
        symbols = []
        
        activeSec = [x.Key for x in algorithm.ActiveSecurities]
        
        for symbol in activeSec:
            if algorithm.ActiveSecurities[symbol].HasData:
                open = algorithm.Securities[symbol].Open
                close = algorithm.Securities[symbol].Close
                if open != 0:
                    openCloseReturn = close/open - 1
                    ret.append(openCloseReturn)
                    symbols.append(symbol)
                    
                    
        # Intraday price change
        symbolsRet = dict(zip(symbols,ret))
        
        # Rank on price change
        symbolsRanked = dict(sorted(symbolsRet.items(), key=lambda kv: kv[1],reverse=False)[:self.numberOfStocks])
        
        # Emit "up" insight if the price change is positive and "down" insight if the price change is negative
        for key,value in symbolsRanked.items():
            if value > 0:
                insights.append(Insight.Price(key, self.predictionInterval, InsightDirection.Up, value, None))
            else:
                insights.append(Insight.Price(key, self.predictionInterval, InsightDirection.Down, value, None))

        return insights
