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
### Regression algorithm testing portfolio construction model control over rebalancing,
### specifying a date rules, see GH 4075.
### </summary>
class PortfolioRebalanceOnDateRulesRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.UniverseSettings.Resolution = Resolution.Daily

        # Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
        # Commented so regression algorithm is more sensitive
        #self.Settings.MinimumOrderMarginPortfolioPercentage = 0.005

        # let's use 0 minimum order margin percentage so we can assert trades are only submitted immediately after rebalance on Wednesday
        # if not, due to TPV variations happening every day we might no cross the minimum on wednesday but yes another day of the week
        self.Settings.MinimumOrderMarginPortfolioPercentage = 0

        self.SetStartDate(2015,1,1)
        self.SetEndDate(2017,1,1)

        self.Settings.RebalancePortfolioOnInsightChanges = False
        self.Settings.RebalancePortfolioOnSecurityChanges = False

        self.SetUniverseSelection(CustomUniverseSelectionModel("CustomUniverseSelectionModel", lambda time: [ "AAPL", "IBM", "FB", "SPY" ]))
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, None))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(self.DateRules.Every(DayOfWeek.Wednesday)))
        self.SetExecution(ImmediateExecutionModel())

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Submitted:
            self.Debug(str(orderEvent))
            if self.UtcTime.weekday() != 2:
                raise ValueError(str(self.UtcTime) + " " + str(orderEvent.Symbol) + " " + str(self.UtcTime.weekday()))
