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

constituentData = []

### <summary>
### Alpha model for ETF constituents, where we generate insights based on the weighting
### of the ETF constituent
### </summary>
class ETFConstituentAlphaModel(AlphaModel):
    def OnSecuritiesChanged(self, algorithm, changes):
        pass

    ### <summary>
    ### Creates new insights based on constituent data and their weighting
    ### in their respective ETF
    ### </summary>
    def Update(self, algorithm: QCAlgorithm, data: Slice):
        insights = []

        for constituent in constituentData:
            if constituent.Symbol not in data.Bars and \
                constituent.Symbol not in data.QuoteBars:

                continue

            insightDirection = InsightDirection.Up if constituent.Weight is not None and constituent.Weight >= 0.01 else InsightDirection.Down

            insights.append(Insight(
                algorithm.UtcTime,
                constituent.Symbol,
                timedelta(days=1),
                InsightType.Price,
                insightDirection,
                float(1 * int(insightDirection)),
                1.0,
                weight=float(0 if constituent.Weight is None else constituent.Weight)
            ))

        return insights

### <summary>
### Generates targets for ETF constituents, which will be set to the weighting
### of the constituent in their respective ETF
### </summary>
class ETFConstituentPortfolioModel(PortfolioConstructionModel):
    def __init__(self):
        self.hasAdded = False

    ### <summary>
    ### Securities changed, detects if we've got new additions to the universe
    ### so that we don't try to trade every loop
    ### </summary>
    def OnSecuritiesChanged(self, algorithm: QCAlgorithm, changes: SecurityChanges):
        self.hasAdded = len(changes.AddedSecurities) != 0

    ### <summary>
    ### Creates portfolio targets based on the insights provided to us by the alpha model.
    ### Emits portfolio targets setting the quantity to the weight of the constituent
    ### in its respective ETF.
    ### </summary>
    def CreateTargets(self, algorithm: QCAlgorithm, insights: typing.List[Insight]):
        if not self.hasAdded:
            return []

        finalInsights = []
        for insight in insights:
            finalInsights.append(PortfolioTarget(insight.Symbol, float(0 if insight.Weight is None else insight.Weight)))
            self.hasAdded = False

        return finalInsights

### <summary>
### Executes based on ETF constituent weighting
### </summary>
class ETFConstituentExecutionModel(ExecutionModel):
    ### <summary>
    ### Liquidates if constituents have been removed from the universe
    ### </summary>
    def OnSecuritiesChanged(self, algorithm: QCAlgorithm, changes: SecurityChanges):
        for change in changes.RemovedSecurities:
            algorithm.Liquidate(change.Symbol)

    ### <summary>
    ### Creates orders for constituents that attempts to add
    ### the weighting of the constituent in our portfolio. The
    ### resulting algorithm portfolio weight might not be equal
    ### to the leverage of the ETF (1x, 2x, 3x, etc.)
    ### </summary>
    def Execute(self, algorithm: QCAlgorithm, targets: typing.List[IPortfolioTarget]):
        for target in targets:
            algorithm.SetHoldings(target.Symbol, target.Quantity)

### <summary>
### Tests ETF constituents universe selection with the algorithm framework models (Alpha, PortfolioConstruction, Execution)
### </summary>
class ETFConstituentUniverseFrameworkRegressionAlgorithm(QCAlgorithm):
    ### <summary>
    ### Initializes the algorithm, setting up the framework classes and ETF constituent universe settings
    ### </summary>
    def Initialize(self):
        self.SetStartDate(2020, 12, 1)
        self.SetEndDate(2021, 1, 31)
        self.SetCash(100000)

        self.SetAlpha(ETFConstituentAlphaModel())
        self.SetPortfolioConstruction(ETFConstituentPortfolioModel())
        self.SetExecution(ETFConstituentExecutionModel())

        spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA)

        self.UniverseSettings.Resolution = Resolution.Hour
        universe = self.AddUniverse(self.Universe.ETF(spy, self.UniverseSettings, self.FilterETFConstituents))

        historicalData = self.History(universe, 1)
        if len(historicalData) != 1:
            raise ValueError(f"Unexpected history count {len(historicalData)}! Expected 1");
        for universeDataCollection in historicalData:
            if len(universeDataCollection) < 200:
               raise ValueError(f"Unexpected universe DataCollection count {len(universeDataCollection)}! Expected > 200");

    ### <summary>
    ### Filters ETF constituents
    ### </summary>
    ### <param name="constituents">ETF constituents</param>
    ### <returns>ETF constituent Symbols that we want to include in the algorithm</returns>
    def FilterETFConstituents(self, constituents):
        global constituentData

        constituentDataLocal = [i for i in constituents if i is not None and i.Weight >= 0.001]
        constituentData = list(constituentDataLocal)

        return [i.Symbol for i in constituentDataLocal]

    ### <summary>
    ### no-op for performance
    ### </summary>
    def OnData(self, data):
        pass
