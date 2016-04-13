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

from datetime import datetime, timedelta

from clr import AddReference
AddReference("System.Core")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Data.Market import *


class ETFGlobalRotationAlgorithm(QCAlgorithm):
    '''ETF Global Rotation Strategy'''
    
    # these are the growth symbols we'll rotate through
    GrowthSymbols = [
    	"MDY",    # US S&P mid cap 400
    	"IEV",    # iShares S&P europe 350
    	"EEM",    # iShared MSCI emerging markets
    	"ILF",    # iShares S&P latin america
    	"EPP" ]   # iShared MSCI Pacific ex-Japan
            
    # these are the safety symbols we go to when things are looking bad for growth
    SafetySymbols = [
    	"EDV",    # Vangaurd TSY 25yr+
    	"SHY" ]   # Barclays Low Duration TSY


    def __init__(self):
        # we'll hold some computed data in these guys
        self.SymbolData = [ ]
		# we'll use this to tell us when the month has ended
        self.__first = True
        self.__lastRotationTime = datetime.min
        self.__rotationInternal = timedelta(days=30)
        

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2007,01,01)  #Set Start Date
        self.SetCash(25000)            #Set Strategy Cash
        
        for symbol in self.GrowthSymbols + self.SafetySymbols:
            # ideally we would use daily data
            self.AddSecurity(SecurityType.Equity, symbol, Resolution.Minute)
            oneMonthPerformance = self.MOM(symbol, 30, Resolution.Daily)
            threeMonthPerformance = self.MOM(symbol, 90, Resolution.Daily)

            self.SymbolData.append(SymbolData(symbol, oneMonthPerformance, threeMonthPerformance))
        
    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        try:
            pyTime = datetime(self.Time)
            # the first time we come through here we'll need to do some 
            # things such as allocation and initializing our symbol data
            if self.__first:
                self.__first = False
                self.__lastRotationTime = pyTime
                return

            delta = pyTime - self.__lastRotationTime
            if delta > self.__rotationInternal:
                self.__lastRotationTime = pyTime
                for x in self.SymbolData: x.Update()
            
                # pick which one is best from growth and safety symbols
                orderedObjScores = sorted(self.SymbolData, key=lambda x: x.ObjectiveScore, reverse = True)
                
                for orderedObjScore in orderedObjScores:
                	self.Log(">>SCORE>>{0}>>{1}".format(orderedObjScore.Symbol, orderedObjScore.ObjectiveScore))
                
                bestGrowth = orderedObjScores[0]
                if bestGrowth.ObjectiveScore > 0:
                    if self.Portfolio[bestGrowth.Symbol].Quantity == 0:
                        self.Log("PREBUY>>LIQUIDATE>>")
                        self.Liquidate()

                    qty = int(self.Portfolio.Cash / self.Securities[bestGrowth.Symbol].Close)
                    self.Log(">>BUY>>{0}@{1}".format(bestGrowth.Symbol, (100.0 * bestGrowth.OneMonthPerformance.Current.Value)))
                    self.MarketOrder(bestGrowth.Symbol, qty)
                else:
                    # if no one has a good objective score then let's hold cash this month to be safe
                    self.Log(">>LIQUIDATE>>CASH");
                    self.Liquidate();

        except:
            self.Error("OnTradeBar: Error")


class SymbolData:
    def __init__(self, symbol, oneMonthPerformance, threeMonthPerformance):
        self.Symbol = symbol
        self.OneMonthPerformance = oneMonthPerformance
        self.ThreeMonthPerformance = threeMonthPerformance
        self.ObjectiveScore = None

    def Update(self):
        # we weight the one month performance higher
        weight1 = 100
        weight2 = 75
        self.ObjectiveScore = (weight1 * self.OneMonthPerformance.Current.Value + weight2 * self.ThreeMonthPerformance.Current.Value) / (weight1 + weight2)