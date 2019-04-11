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
from QuantConnect.Indicators import RollingWindow, IndicatorDataPoint

import pandas as pd

class UncorrelatedUniverseSelectionModel(FundamentalUniverseSelectionModel):
    '''This universe selection model picks stocks that currently have their correlation to a benchmark deviated from the mean.'''

    def __init__(self,
                 benchmark = Symbol.Create("SPY", SecurityType.Equity, Market.USA),
                 numberOfSymbolsCoarse = 400,
                 numberOfSymbols = 10,
                 windowLength = 5,
                 historyLength = 25,
                 threshold = 0.5):
        '''Initializes a new default instance of the OnTheMoveUniverseSelectionModel
        Args:
            benchmark: Symbol of the benchmark
            numberOfSymbolsCoarse: Number of coarse symbols
            numberOfSymbols: Number of symbols selected by the universe model
            windowLength: Rolling window length period for correlation calculation
            historyLength: History length period
            threshold: Threadhold for the minimum mean correlation between security and benchmark'''
        super().__init__(False)

        self.benchmark = benchmark 
        self.numberOfSymbolsCoarse = numberOfSymbolsCoarse
        self.numberOfSymbols = numberOfSymbols
        self.windowLength = windowLength
        self.historyLength = historyLength
        self.threshold = threshold

        self.cache = dict()
        self.symbol = list()

    def SelectCoarse(self, algorithm, coarse):
        '''Select stocks with highest Z-Score with fundamental data and positive previous-day price and volume'''

        # Verify whether the benchmark is present in the Coarse Fundamental
        benchmark = next((x for x in coarse if x.Symbol == self.benchmark), None)
        if benchmark is None:
            return self.symbol

        # Get the symbols with the highest dollar volume
        coarse = sorted([x for x in coarse if x.HasFundamentalData 
                                          and x.Volume * x.Price > 0 
                                          and x.Symbol != self.benchmark],
                        key = lambda x: x.DollarVolume, reverse=True)[:self.numberOfSymbolsCoarse]
        
        newSymbols = list()
        for cf in coarse + [benchmark]:
            symbol = cf.Symbol
            data = self.cache.setdefault(symbol, self.SymbolData(self, symbol))
            data.Update(cf.EndTime, cf.AdjustedPrice)
            if not data.IsReady:
                newSymbols.append(symbol)

        # Warm up the dictionary objects of selected symbols and benchmark that do not have enough data
        if len(newSymbols) > 1:
            history = algorithm.History(newSymbols, self.historyLength, Resolution.Daily)
            if not history.empty:
                history = history.close.unstack(level=0)
                for symbol in newSymbols:
                    self.cache[symbol].Warmup(history)

        # Create a new dictionary with the zScore
        zScore = dict()
        benchmark = self.cache[self.benchmark].GetReturns()
        for cf in coarse:
            symbol = cf.Symbol
            value = self.cache[symbol].CalculateZScore(benchmark)
            if value > 0: zScore[symbol] = value

        # Sort the zScore dictionary by value
        if len(zScore) > self.numberOfSymbols:
            sorted_zScore = sorted(zScore.items(), key=lambda kvp: kvp[1], reverse=True)
            zScore = dict(sorted_zScore[:self.numberOfSymbols])

        # Return the symbols
        self.symbols = list(zScore.keys())
        return self.symbols


    class SymbolData:
        '''Contains data specific to a symbol required by this model'''
        def __init__(self, model, symbol):
            self.symbol = symbol
            self.windowLength = model.windowLength
            self.historyLength = model.historyLength
            self.threshold = model.threshold
            self.history = RollingWindow[IndicatorDataPoint](self.historyLength)
            self.correlation = None

        def Warmup(self, history):
            '''Save the historical data that will be used to compute the correlation'''
            symbol = str(self.symbol)
            if symbol not in history:
                return

            # Save the last point before reset
            last = self.history[0]
            self.history.Reset()

            # Uptade window with historical data
            for time, value in history[symbol].iteritems():
                self.Update(time, value)

            # Re-add the last point if necessary
            if last.EndTime > time:
                self.Update(last.EndTime, last.Value)

        def Update(self, time, value):
            '''Update the historical data'''
            self.history.Add(IndicatorDataPoint(self.symbol, time, value))

        def CalculateZScore(self, benchmark):
            '''Computes the ZScore'''
            # Not enough data to compute zScore
            if not self.IsReady:
                return 0

            returns = pd.DataFrame.from_dict({"A": self.GetReturns(), "B": benchmark})

            if self.correlation is None:
                # Calculate stdev(correlation) using rolling window for all history
                correlation = returns.rolling(self.windowLength, min_periods = self.windowLength).corr()
                self.correlation = correlation["B"].dropna().unstack()
            else:
                last_correlation = returns.tail(self.windowLength).corr()["B"]
                self.correlation = self.correlation.append(last_correlation).tail(self.historyLength)

            # Calculate the mean of correlation and discard low mean correlation
            mean = self.correlation.mean()
            if mean.empty or mean[0] < self.threshold:
                return 0

            # Calculate the standard deviation of correlation
            std = self.correlation.std()

            # Current correlation
            current = self.correlation.tail(1).unstack()

            # Calculate absolute value of Z-Score for stocks in the Coarse Universe.
            return abs(current[0] - mean[0]) / std[0]

        def GetReturns(self):
            '''Get the returns from the rolling window dictionary'''
            historyDict = {x.EndTime: x.Value for x in self.history}
            return pd.Series(historyDict).sort_index().pct_change().dropna()

        @property
        def IsReady(self):
            return self.history.IsReady
