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
### Test algorithm generating insights with custom tags
### </summary>
class InsightTagAlphaRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.SetCash(100000)

        self.UniverseSettings.Resolution = Resolution.Daily

        self.spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA)
        self.fb = Symbol.Create("FB", SecurityType.Equity, Market.USA)
        self.ibm = Symbol.Create("IBM", SecurityType.Equity, Market.USA)

        # set algorithm framework models
        self.SetUniverseSelection(ManualUniverseSelectionModel([self.spy, self.fb, self.ibm]))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())

        self.AddAlpha(OneTimeAlphaModel(self.spy))
        self.AddAlpha(OneTimeAlphaModel(self.fb))
        self.AddAlpha(OneTimeAlphaModel(self.ibm))

        self.InsightsGenerated += self.OnInsightsGeneratedVerifier

        self.symbols_with_generated_insights = []

    def OnInsightsGeneratedVerifier(self, algorithm: IAlgorithm, insightsCollection: GeneratedInsightsCollection) -> None:
        for insight in insightsCollection.Insights:
            if insight.Tag != OneTimeAlphaModel.GenerateInsightTag(insight.Symbol):
                raise Exception("Unexpected insight tag was emitted")

            self.symbols_with_generated_insights.append(insight.Symbol)

    def OnEndOfAlgorithm(self) -> None:
        if len(self.symbols_with_generated_insights) != 3:
            raise Exception("Unexpected number of symbols with generated insights")

        if not self.spy in self.symbols_with_generated_insights:
            raise Exception("SPY symbol was not found in symbols with generated insights")

        if not self.fb in self.symbols_with_generated_insights:
            raise Exception("FB symbol was not found in symbols with generated insights")

        if not self.ibm in self.symbols_with_generated_insights:
            raise Exception("IBM symbol was not found in symbols with generated insights")

class OneTimeAlphaModel(AlphaModel):
    def __init__(self, symbol):
        self.symbol = symbol
        self.triggered = False

    def Update(self, algorithm, data):
        insights = []
        if not self.triggered:
            self.triggered = True
            insights.append(Insight.Price(
                self.symbol,
                Resolution.Daily,
                1,
                InsightDirection.Down,
                tag=OneTimeAlphaModel.GenerateInsightTag(self.symbol)))
        return insights

    @staticmethod
    def GenerateInsightTag(symbol: Symbol) -> str:
        return f"Insight generated for {symbol}";
