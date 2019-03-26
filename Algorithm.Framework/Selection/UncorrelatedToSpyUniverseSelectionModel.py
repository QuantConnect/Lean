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

class UncorrelatedToBenchmarkUniverseSelectionModel(FundamentalUniverseSelectionModel):
    '''This universe selection model picks stocks that currently have their correlation to a benchmark deviated from the mean.'''

    def __init__(self,
                 benchmark = Symbol.Create("SPY", SecurityType.Equity, Market.USA),
                 numberOfSymbolsCoarse = 40,
                 numberOfSymbols = 10,
                 windowLength = 5,
                 historyLength = 25):
        '''Initializes a new default instance of the OnTheMoveUniverseSelectionModel
        Args:
            benchmark: Symbol of the benchmark
            numberOfSymbolsCoarse: Number of coarse symbols
            numberOfSymbols: Number of symbols selected by the universe model
            windowLength: Rolling window length period for correlation calculation
            historyLength: History length period'''
        super().__init__(False)

        self.benchmark = benchmark 
        self.numberOfSymbolsCoarse = numberOfSymbolsCoarse
        self.numberOfSymbols = numberOfSymbols
        self.windowLength = windowLength
        self.historyLength = historyLength
        
        # Symbols in universe
        self.symbols = []
        self.coarseSymbols = []
        self.cor = None

    def SelectCoarse(self, algorithm, coarse):

        if not self.coarseSymbols:
            # The stocks must have fundamental data
            # The stock must have positive previous-day close price
            # The stock must have positive volume on the previous trading day
            filtered = [x for x in coarse if x.HasFundamentalData and x.Volume > 0 and x.Price > 0]
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
        hist = algorithm.History(symbols + [self.benchmark], self.historyLength, Resolution.Daily)

        # Calculate returns
        returns=hist.close.unstack(level=0).pct_change()
      
        # Calculate stdev(correlation) using rolling window for all history
        if self.cor is None:
            corMat=returns.rolling(self.windowLength,min_periods = self.windowLength).corr().dropna()
            
            # Correlation of all securities against SPY
            self.cor = corMat[str(self.benchmark)].unstack()

        # Calculate stdev(correlation) for last period and append a new row
        else:
            corRow=returns.tail(self.windowLength).corr()[str(self.benchmark)]

            # Correlation of all securities against SPY
            self.cor = self.cor.append(corRow).tail(self.historyLength)

        # Calculate the mean of correlation.
        # Only include stocks with significantly positive or negative correlation to benchmark.
        corMu = self.cor.mean()
        corMu = corMu[abs(corMu) > 0.5].drop(str(self.benchmark))

        # Calculate the standard deviation of correlation
        corStd = self.cor.std()

        # Current correlation
        corCur = self.cor.tail(1).unstack()

        # Calculate absolute value of Z-Score for stocks in the Coarse Universe. 
        zScore = (abs(corCur - corMu) / corStd).dropna()

        # Rank stocks on Z-Score
        zScore = zScore.sort_values(ascending=False).head(self.numberOfSymbols)

        return zScore.index.levels[0].tolist()
