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
### Abstract regression framework algorithm for multiple framework regression tests
### </summary>
class BaseFrameworkRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2014, 6, 1)
        self.SetEndDate(2014, 6, 30)
        
        self.UniverseSettings.Resolution = Resolution.Hour;
        self.UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

        symbols = [Symbol.Create(ticker, SecurityType.Equity, Market.USA)
            for ticker in ["AAPL", "AIG", "BAC", "SPY"]]

        # Manually add AAPL and AIG when the algorithm starts
        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols[:2]))

        # At midnight, add all securities every day except on the last data
        # With this procedure, the Alpha Model will experience multiple universe changes
        self.AddUniverseSelection(ScheduledUniverseSelectionModel(
            self.DateRules.EveryDay(), self.TimeRules.Midnight,
            lambda dt: symbols if dt < self.EndDate.astimezone(dt.tzinfo) - timedelta(1) else []))

        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(31), 0.025, None))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())

    def OnEndOfAlgorithm(self):
        # The base implementation checks for active insights
        insightsCount = len(self.Insights.GetInsights(lambda insight: insight.IsActive(self.UtcTime)))
        if insightsCount != 0:
            raise Exception(f"The number of active insights should be 0. Actual: {insightsCount}")
