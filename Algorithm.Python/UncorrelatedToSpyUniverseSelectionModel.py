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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *

from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

class UncorrelatedToSPYUniverseSelectionModel(FundamentalUniverseSelectionModel):
    '''
        This universe selection model picks stocks that currently have their correlation to SPY deviated from the mean. 
    '''

    def __init__(self, filterFineData = False, universeSettings = None, securityInitializer = None):
        '''Initializes a new default instance of the OnTheMoveUniverseSelectionModel'''
        super().__init__(filterFineData, universeSettings, securityInitializer)
        
        # Add SPY to the universe
        self.spySymbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA)
        
        # Number of coarse symbols
        self.numberOfSymbolsCoarse = 400
        
        # Number of symbols selected by the universe model
        self.numberOfSymbols = 10
        
        # Rolling window length period for correlation calculation
        self.windowLength = 5
        
        # History length period
        self.historyLength = 25
        
        # Symbols in universe
        self.symbols = []

        # Set True when initial history has been retrieved.
        self.initialHistory = False
        
        self.coarseSymbols = []
        self.cor = None

    def SelectCoarse(self, algorithm, coarse):
        
        if not self.coarseSymbols:
            # The stocks must have fundamental data
            # The stock must have positive previous-day close price
            # The stock must have positive volume on the previous trading day
            filtered = [x for x in coarse if x.HasFundamentalData and x.Volume ¨ 0 and x.Price > 0]
            sortedByDollarVolume = sorted(filtered, key = lambda x: x.DollarVolume, reverse=True)[:self.numberOfSymbolsCoarse]
    
            self.coarseSymbols = [x.Symbol for x in sortedByDollarVolume]
            
        # return the symbol objects our sorted collection
        self.symbols = self.corRanked(algorithm,self.coarseSymbols)

        return self.symbols 
    
    def corRanked(self, algorithm, symbols):
        
        # Not enough symbols to filter
        if len(symbols) <= self.numberOfSymbols:
            return symbols
        
        # Retrieve history of prices
        hist = algorithm.History(symbols + [self.spySymbol], self.historyLength, Resolution.Daily)

        # Calculate returns
        returns=hist.close.unstack(level=0).pct_change()
      
        # Calculate stdev(correlation) using rolling window for all history
        if not self.initialHistory:
            corMat=returns.rolling(self.windowLength,min_periods = self.windowLength).corr().dropna()
            
            # Correlation of all securities against SPY
            self.cor = corMat[str(self.spySymbol)].unstack()
            
            self.initialHistory = True
            
        # Calculate stdev(correlation) for last period and append a new row
        if self.initialHistory:
            corRow=returns.tail(self.windowLength).corr()[str(self.spySymbol)]
            
            # Correlation of all securities against SPY
            self.cor = self.cor.append(corRow).tail(self.historyLength)
        
        # Calculate the mean of correlation
        corMu = self.cor.mean()
        
        # Calculate the standard deviation of correlation
        corStd = self.cor.std()
        
        # Calculate absolute value of Z-Score for stocks in the Coarse Universe. Only include stocks with significantly positive or negative correlation to SPY.
        zScore = {}
        for symbol in corStd.index:
            if not symbol == "SPY":
                if abs(corMu[symbol]) > 0.5:
                    zScore.update({symbol : abs((self.cor[symbol].tail(1).values-corMu[symbol])/corStd[symbol])})


        # Rank stocks on Z-Score
        symbols=sorted(zScore, key=lambda symbol: zScore[symbol],reverse=True)[:self.numberOfSymbols]

        return symbols
