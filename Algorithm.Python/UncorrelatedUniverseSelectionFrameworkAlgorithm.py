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
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Portfolio import EqualWeightingPortfolioConstructionModel
from QuantConnect.Algorithm.Framework.Execution import ImmediateExecutionModel
from Selection.UncorrelatedUniverseSelectionModel import UncorrelatedUniverseSelectionModel

from datetime import timedelta

class UncorrelatedUniverseSelectionFrameworkAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2018,1,1)   # Set Start Date
        self.SetCash(1000000)         # Set Strategy Cash


        benchmark = Symbol.Create("SPY", SecurityType.Equity, Market.USA)
        self.SetUniverseSelection(UncorrelatedUniverseSelectionModel(benchmark))
        self.SetAlpha(UncorrelatedUniverseSelectionAlphaModel())
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())


class UncorrelatedUniverseSelectionAlphaModel(AlphaModel):
    '''Uses ranking of intraday percentage difference between open price and close price to create magnitude and direction prediction for insights'''

    def __init__(self, numberOfStocks = 10, predictionInterval = timedelta(1)):
        self.predictionInterval = predictionInterval
        self.numberOfStocks = numberOfStocks

    def Update(self, algorithm, data):
        symbolsRet = dict()

        for kvp in algorithm.ActiveSecurities:
            security = kvp.Value
            if security.HasData:
                open = security.Open
                if open != 0:
                    symbolsRet[security.Symbol] = security.Close / open - 1

        # Rank on the absolute value of price change
        symbolsRet = dict(sorted(symbolsRet.items(), key=lambda kvp: abs(kvp[1]),reverse=True)[:self.numberOfStocks])

        insights = []
        for symbol, price_change in symbolsRet.items():
            # Emit "up" insight if the price change is positive and "down" otherwise
            direction = InsightDirection.Up if price_change > 0 else InsightDirection.Down
            insights.append(Insight.Price(symbol, self.predictionInterval, direction, abs(price_change), None))

        return insights
