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

### <summary>
### Regression algorithm illustrating how to request history data for different data normalization modes.
### </summary>
class HistoryWithDifferentDataMappingModeRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2014, 1, 1)
        self.aaplEquitySymbol = self.AddEquity("AAPL", Resolution.Daily).Symbol
        self.esFutureSymbol = self.AddFuture(Futures.Indices.SP500EMini, Resolution.Daily).Symbol

    def OnEndOfAlgorithm(self):
        equityDataNormalizationModes = [
            DataNormalizationMode.Raw,
            DataNormalizationMode.Adjusted,
            DataNormalizationMode.SplitAdjusted
        ]
        self.CheckHistoryResultsForDataNormalizationModes(self.aaplEquitySymbol, self.StartDate, self.EndDate, Resolution.Daily,
            equityDataNormalizationModes)

        futureDataNormalizationModes = [
            DataNormalizationMode.Raw,
            DataNormalizationMode.BackwardsRatio,
            DataNormalizationMode.BackwardsPanamaCanal,
            DataNormalizationMode.ForwardPanamaCanal
        ]
        self.CheckHistoryResultsForDataNormalizationModes(self.esFutureSymbol, self.StartDate, self.EndDate, Resolution.Daily,
            futureDataNormalizationModes)

    def CheckHistoryResultsForDataNormalizationModes(self, symbol, start, end, resolution, dataNormalizationModes):
        historyResults = [self.History([symbol], start, end, resolution, dataNormalizationMode=x) for x in dataNormalizationModes]
        historyResults = [x.droplevel(0, axis=0) for x in historyResults] if len(historyResults[0].index.levels) == 3 else historyResults
        historyResults = [x.loc[symbol].close for x in historyResults]

        if any(x.size == 0 or x.size != historyResults[0].size for x in historyResults):
            raise Exception(f"History results for {symbol} have different number of bars")

        # Check that, for each history result, close prices at each time are different for these securities (AAPL and ES)
        for j in range(historyResults[0].size):
            closePrices = set(historyResults[i][j] for i in range(len(historyResults)))
            if len(closePrices) != len(dataNormalizationModes):
                raise Exception(f"History results for {symbol} have different close prices at the same time")
