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
### Store constituents data when doing universe selection for later use in the alpha model
### </summary>
Constituents = []


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
        self.AddUniverse(self.Universe.ETF(spy, self.UniverseSettings, self.FilterETFConstituents))

    ### <summary>
    ### Filters ETF constituents
    ### </summary>
    ### <param name="constituents">ETF constituents</param>
    ### <returns>ETF constituent Symbols that we want to include in the algorithm</returns>
    def FilterETFConstituents(self, constituents):
        global Constituents

        Constituents = [i for i in constituents if i.Weight is not None and i.Weight >= 0.001]

        return [i.Symbol for i in Constituents]

    ### <summary>
    ### no-op
    ### </summary>
    def OnData(self, data):
        pass


### <summary>
### Alpha model making use of the RSI indicator and ETF constituent weighting to determine
### which assets we should invest in and the direction of investment
### </summary>
class ConstituentWeightedRsiAlphaModel(AlphaModel):
    def __init__(self):
        self.rsiSymbolData = {}

    def Update(self, algorithm: QCAlgorithm, data: Slice):
        if len(Constituents) == 0 or len(data.Bars) == 0:
            # Don't do anything if we have no data we can work with
            return []

        constituents = {i.Symbol:i for i in Constituents}

        allReady = True
        for bar in data.Bars.Values:
            if bar.Symbol not in constituents:
                # Dealing with a manually added equity, which in this case is SPY
                continue

            rsiData = self.rsiSymbolData.get(bar.Symbol)
            if rsiData is None:
                # First time we're initializing the RSI.
                # It won't be ready now, but it will be
                # after 7 data points
                constituent = constituents[bar.Symbol]
                rsiData = SymbolData(bar.Symbol, constituent, 7)
                self.rsiSymbolData[bar.Symbol] = rsiData

            # Let's make sure all RSI indicators are ready before we emit any insights.
            allReady = allReady and rsiData.rsi.Update(IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Close))

        if not allReady:
            # We're still warming up the RSI indicators.
            return []

        emitted = False
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

            emitted = True
        
        if emitted:
            # Prevents us from placing trades before the next
            # ETF constituents universe selection occurs.
            Constituents.clear()

        return insights


class SymbolData:
    def __init__(self, symbol, constituent, period):
        self.Symbol = symbol
        self.constituent = constituent
        self.rsi = RelativeStrengthIndex(period, MovingAverageType.Exponential)