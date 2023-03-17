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
### Framework algorithm that uses the <see cref="MacdAlphaModel"/>
### </summary>
class MacdAlphaModelFrameworkAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)    #Set Start Date
        self.SetEndDate(2013, 10, 11)      #Set End Date
        
        symbols = [Symbol.Create(ticker, SecurityType.Equity, Market.USA)
            for ticker in ["SPY", "AIG", "BAC", "IBM"]]

        # Manually add SPY and AIG when the algorithm starts
        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols[:2]))

        # At midnight, add all securities every day except on the last data
        # With this procedure, the Alpha Model will experience multiple universe changes
        self.AddUniverseSelection(ScheduledUniverseSelectionModel(
            self.DateRules.EveryDay(), self.TimeRules.Midnight,
            lambda dt: symbols if dt.day <= (self.EndDate - timedelta(1)).day else []))

        self.SetAlpha(MacdAlphaModel())
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())

    def OnEndOfAlgorithm(self):
        # We have removed all securities from the universe. The Alpha Model should remove the consolidator
        consolidatorCount = sum(s.Consolidators.Count for s in self.SubscriptionManager.Subscriptions)
        if consolidatorCount > 0:
            raise Exception(f"The number of consolidator is should be zero. Actual: {consolidatorCount}")