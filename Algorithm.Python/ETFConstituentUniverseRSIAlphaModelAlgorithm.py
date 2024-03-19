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

import typing

from AlgorithmImports import *
from datetime import timedelta


### <summary>
### Example algorithm demonstrating the usage of the RSI indicator
### in combination with ETF constituents data to replicate the weighting
### of the ETF's assets in our own account.
### </summary>
class ETFConstituentUniverseRSIAlphaModelAlgorithm(QCAlgorithm):
    ### <summary>
    ### Initialize the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    ### </summary>
    def Initialize(self):
        self.SetStartDate(2020, 12, 1)
        self.SetEndDate(2021, 1, 31)
        self.SetCash(100000)

        self.SetAlpha(ConstituentWeightedRsiAlphaModel())
        self.SetPortfolioConstruction(InsightWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())

        spy = self.AddEquity("SPY", Resolution.Hour).Symbol

        # We load hourly data for ETF constituents in this algorithm
        self.UniverseSettings.Resolution = Resolution.Hour
        self.Settings.MinimumOrderMarginPortfolioPercentage = 0.01

        self.AddUniverse(self.Universe.ETF(spy, self.UniverseSettings, self.FilterETFConstituents))

    ### <summary>
    ### Filters ETF constituents
    ### </summary>
    ### <param name="constituents">ETF constituents</param>
    ### <returns>ETF constituent Symbols that we want to include in the algorithm</returns>
    def FilterETFConstituents(self, constituents):
        return [i.Symbol for i in constituents if i.Weight is not None and i.Weight >= 0.001]


### <summary>
### Alpha model making use of the RSI indicator and ETF constituent weighting to determine
### which assets we should invest in and the direction of investment
### </summary>
class ConstituentWeightedRsiAlphaModel(AlphaModel):
    def __init__(self, maxTrades=None):
        self.rsiSymbolData = {}

    def Update(self, algorithm: QCAlgorithm, data: Slice):
        algoConstituents = []
        for barSymbol in data.Bars.Keys:
            if not algorithm.Securities[barSymbol].Cache.HasData(ETFConstituentUniverse):
                continue

            constituentData = algorithm.Securities[barSymbol].Cache.GetData[ETFConstituentUniverse]()
            algoConstituents.append(constituentData)

        if len(algoConstituents) == 0 or len(data.Bars) == 0:
            # Don't do anything if we have no data we can work with
            return []

        constituents = {i.Symbol:i for i in algoConstituents}

        for bar in data.Bars.Values:
            if bar.Symbol not in constituents:
                # Dealing with a manually added equity, which in this case is SPY
                continue

            if bar.Symbol not in self.rsiSymbolData:
                # First time we're initializing the RSI.
                # It won't be ready now, but it will be
                # after 7 data points
                constituent = constituents[bar.Symbol]
                self.rsiSymbolData[bar.Symbol] = SymbolData(bar.Symbol, algorithm, constituent, 7)

        allReady = all([sd.rsi.IsReady for sd in self.rsiSymbolData.values()])
        if not allReady:
            # We're still warming up the RSI indicators.
            return []

        insights = []

        for symbol, symbolData in self.rsiSymbolData.items():
            averageLoss = symbolData.rsi.AverageLoss.Current.Value
            averageGain = symbolData.rsi.AverageGain.Current.Value

            # If we've lost more than gained, then we think it's going to go down more
            direction = InsightDirection.Down if averageLoss > averageGain else InsightDirection.Up

            # Set the weight of the insight as the weight of the ETF's
            # holding. The InsightWeightingPortfolioConstructionModel
            # will rebalance our portfolio to have the same percentage
            # of holdings in our algorithm that the ETF has.
            insights.append(Insight.Price(
                symbol,
                timedelta(days=1),
                direction,
                float(averageLoss if direction == InsightDirection.Down else averageGain),
                weight=float(symbolData.constituent.Weight)
            ))
        
        return insights


class SymbolData:
    def __init__(self, symbol, algorithm, constituent, period):
        self.Symbol = symbol
        self.constituent = constituent
        self.rsi = algorithm.RSI(symbol, period, MovingAverageType.Exponential, Resolution.Hour)
