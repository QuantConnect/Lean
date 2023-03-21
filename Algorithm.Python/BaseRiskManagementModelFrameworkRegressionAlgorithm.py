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

class BaseRiskManagementModelFrameworkRegressionAlgorithm(QCAlgorithm):
    '''Show example of how to use the MaximumDrawdownPercentPortfolio Risk Management Model'''

    def Initialize(self):

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        # set algorithm framework models
        self.SetUniverseSelection(ManualUniverseSelectionModel([ Symbol.Create("SPY", SecurityType.Equity, Market.USA) ]))
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(5), 0.025, None))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())

        # define risk management model as a composite of several risk management models
        self.SetRiskManagement(NullRiskManagementModel())

    def OnEndOfAlgorithm(self):
        insightsCount = len(self.Insights.GetInsights(lambda insight: insight.IsActive(self.UtcTime)));
        if insightsCount != 0:
            raise Exception(f"The number of active insights should be 0. Actual: {insightsCount}")
