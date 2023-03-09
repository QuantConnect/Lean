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
### Regression algorithm showing how to define a custom insight evaluator
### </summary>
class InsightScoringRegressionAlgorithm(QCAlgorithm):
    '''Regression algorithm showing how to define a custom insight evaluator'''

    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        
        symbols = [ Symbol.Create("SPY", SecurityType.Equity, Market.USA) ]

        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols))
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(minutes = 20), 0.025, None))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(Resolution.Daily))
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.01))
        
        # we specify a custom insight evaluator
        self.Insights.SetInsightScoreFunction(CustomInsightScoreFunction(self.Securities))

    def OnEndOfAlgorithm(self):
        allInsights = self.InsightManager.GetInsights()

        if len(allInsights) != 100:
            raise ValueError(f'Unexpected insight count found {allInsights.Count}')

        if sum(1 for insight in allInsights if insight.Score.Magnitude == 0 or insight.Score.Direction == 0) < 5:
            raise ValueError(f'Insights not scored!')

        if sum(1 for insight in allInsights if insight.Score.IsFinalScore) < 99:
            raise ValueError(f'Insights not finalized!')

class CustomInsightScoreFunction():

    def __init__(self, securities):
        self._securities = securities
        self._openInsights = {}

    def Score(self, insightManager, utcTime):
        openInsights = insightManager.GetOpenInsights()

        for insight in openInsights:
            self._openInsights[insight.Id] = insight

        toRemove = []
        for openInsight in self._openInsights.values():
            security = self._securities[openInsight.Symbol]
            openInsight.ReferenceValueFinal = security.Price

            score = openInsight.ReferenceValueFinal - openInsight.ReferenceValue
            openInsight.Score.SetScore(InsightScoreType.Direction, score, utcTime)
            openInsight.Score.SetScore(InsightScoreType.Magnitude, score * 2, utcTime)
            openInsight.EstimatedValue = score * 100

            if openInsight.IsExpired(utcTime):
                openInsight.Score.Finalize(utcTime)
                toRemove.append(openInsight)

        # clean up
        for insightToRemove in toRemove:
            self._openInsights.pop(insightToRemove.Id)
