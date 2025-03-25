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

from Selection.ManualUniverseSelectionModel import ManualUniverseSelectionModel
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from Execution.ImmediateExecutionModel import ImmediateExecutionModel

### <summary>
### Test algorithm generating insights with custom tags
### </summary>
class InsightTagAlphaRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013,10,7)
        self.set_end_date(2013,10,11)
        self.set_cash(100000)

        self.universe_settings.resolution = Resolution.DAILY

        self.spy = Symbol.create("SPY", SecurityType.EQUITY, Market.USA)
        self.fb = Symbol.create("FB", SecurityType.EQUITY, Market.USA)
        self.ibm = Symbol.create("IBM", SecurityType.EQUITY, Market.USA)

        # set algorithm framework models
        self.set_universe_selection(ManualUniverseSelectionModel([self.spy, self.fb, self.ibm]))
        self.set_portfolio_construction(EqualWeightingPortfolioConstructionModel())
        self.set_execution(ImmediateExecutionModel())

        self.add_alpha(OneTimeAlphaModel(self.spy))
        self.add_alpha(OneTimeAlphaModel(self.fb))
        self.add_alpha(OneTimeAlphaModel(self.ibm))

        self.insights_generated += self.on_insights_generated_verifier

        self._symbols_with_generated_insights = []

    def on_insights_generated_verifier(self, algorithm: IAlgorithm, insights_collection: GeneratedInsightsCollection) -> None:
        for insight in insights_collection.insights:
            if insight.tag != OneTimeAlphaModel.generate_insight_tag(insight.symbol):
                raise AssertionError("Unexpected insight tag was emitted")

            self._symbols_with_generated_insights.append(insight.symbol)

    def on_end_of_algorithm(self) -> None:
        if len(self._symbols_with_generated_insights) != 3:
            raise AssertionError("Unexpected number of symbols with generated insights")

        if not self.spy in self._symbols_with_generated_insights:
            raise AssertionError("SPY symbol was not found in symbols with generated insights")

        if not self.fb in self._symbols_with_generated_insights:
            raise AssertionError("FB symbol was not found in symbols with generated insights")

        if not self.ibm in self._symbols_with_generated_insights:
            raise AssertionError("IBM symbol was not found in symbols with generated insights")

class OneTimeAlphaModel(AlphaModel):
    def __init__(self, symbol):
        self._symbol = symbol
        self.triggered = False

    def update(self, algorithm, data):
        insights = []
        if not self.triggered:
            self.triggered = True
            insights.append(Insight.price(
                self._symbol,
                Resolution.DAILY,
                1,
                InsightDirection.DOWN,
                tag=OneTimeAlphaModel.generate_insight_tag(self._symbol)))
        return insights

    @staticmethod
    def generate_insight_tag(symbol: Symbol) -> str:
        return f"Insight generated for {symbol}";
