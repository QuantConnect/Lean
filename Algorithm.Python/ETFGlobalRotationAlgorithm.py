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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Indicators import *
from System.Collections.Generic import List
from QCAlgorithm import QCAlgorithm
import decimal as d
from datetime import datetime, timedelta
from decimal import Decimal

### <summary>
### Strategy example using a portfolio of ETF Global Rotation
### </summary>
### <meta name="tag" content="strategy example" />
### <meta name="tag" content="momentum" />
### <meta name="tag" content="using data" />

### <summary>
### Strategy example using a portfolio of ETF Global Rotation
### </summary>
### <meta name="tag" content="strategy example" />
### <meta name="tag" content="momentum" />
### <meta name="tag" content="using data" />
class ETFGlobalRotationAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetCash(25000)
        self.SetStartDate(2007,1,1)
        self.LastRotationTime = datetime.min
        self.RotationInterval = timedelta(days=30)
        self.first = True

        # these are the growth symbols we'll rotate through
        GrowthSymbols =["MDY", # US S&P mid cap 400
                        "IEV", # iShares S&P europe 350
                        "EEM", # iShared MSCI emerging markets
                        "ILF", # iShares S&P latin america
                        "EPP" ] # iShared MSCI Pacific ex-Japan

        # these are the safety symbols we go to when things are looking bad for growth
        SafetySymbols = ["EDV", "SHY"] # "EDV" Vangaurd TSY 25yr, "SHY" Barclays Low Duration TSY
        # we'll hold some computed data in these guys
        self.SymbolData = []
        for symbol in list(set(GrowthSymbols) | set(SafetySymbols)):
            self.AddSecurity(SecurityType.Equity, symbol, Resolution.Minute)
            self.oneMonthPerformance = self.MOM(symbol, 30, Resolution.Daily)
            self.threeMonthPerformance = self.MOM(symbol, 90, Resolution.Daily)
            self.SymbolData.append([symbol, self.oneMonthPerformance, self.threeMonthPerformance])
    
        
    def OnData(self, data):

        # the first time we come through here we'll need to do some things such as allocation
        # and initializing our symbol data

        if self.first:
            self.first = False
            self.LastRotationTime = self.Time
            return
        delta = self.Time - self.LastRotationTime
        if delta > self.RotationInterval:
            self.LastRotationTime = self.Time

            orderedObjScores = sorted(self.SymbolData, key=lambda x: Score(x[1].Current.Value,x[2].Current.Value).ObjectiveScore(), reverse=True)
            for x in orderedObjScores:
                self.Log(">>SCORE>>" + x[0] + ">>" + str(Score(x[1].Current.Value,x[2].Current.Value).ObjectiveScore()))
            # pick which one is best from growth and safety symbols
            bestGrowth = orderedObjScores[0]
            if Score(bestGrowth[1].Current.Value,bestGrowth[2].Current.Value).ObjectiveScore() > 0:
                if (self.Portfolio[bestGrowth[0]].Quantity == 0):
                    self.Log("PREBUY>>LIQUIDATE>>")
                    self.Liquidate()
                self.Log(">>BUY>>" + str(bestGrowth[0]) + "@" + str(Decimal(100) * bestGrowth[1].Current.Value))
                qty = self.Portfolio.MarginRemaining / self.Securities[bestGrowth[0]].Close
                self.MarketOrder(bestGrowth[0], int(qty)) 
            else:
            # if no one has a good objective score then let's hold cash this month to be safe
                self.Log(">>LIQUIDATE>>CASH")
                self.Liquidate()
        
class Score(object):
    
    def __init__(self,oneMonthPerformanceValue,threeMonthPerformanceValue):
        self.oneMonthPerformance = oneMonthPerformanceValue
        self.threeMonthPerformance = threeMonthPerformanceValue
    
    def ObjectiveScore(self):
        weight1 = 100
        weight2 = 75
        return (weight1 * self.oneMonthPerformance + weight2 * self.threeMonthPerformance) / (weight1 + weight2)