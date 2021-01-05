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
from QuantConnect.Securities import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *
from datetime import timedelta

### <summary>
### Regression algorithm testing portfolio construction model control over rebalancing,
### specifying a custom rebalance function that returns null in some cases, see GH 4075.
### </summary>
class PortfolioRebalanceOnCustomFuncRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2015, 1, 1)
        self.SetEndDate(2018, 1, 1)

        self.Settings.RebalancePortfolioOnInsightChanges = False;
        self.Settings.RebalancePortfolioOnSecurityChanges = False;

        self.SetUniverseSelection(CustomUniverseSelectionModel("CustomUniverseSelectionModel", lambda time: [ "AAPL", "IBM", "FB", "SPY", "AIG", "BAC", "BNO" ]))
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, None));
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(self.RebalanceFunction))
        self.SetExecution(ImmediateExecutionModel())
        self.lastRebalanceTime = self.StartDate

    def RebalanceFunction(self, time):
        # for performance only run rebalance logic once a week, monday
        if time.weekday() != 0:
            return None

        if self.lastRebalanceTime == self.StartDate:
            # initial rebalance
            self.lastRebalanceTime = time;
            return time;

        deviation = 0;
        count = sum(1 for security in self.Securities.Values if security.Invested)
        if count > 0:
            self.lastRebalanceTime = time;
            portfolioValuePerSecurity = self.Portfolio.TotalPortfolioValue / count;
            for security in self.Securities.Values:
                if not security.Invested:
                    continue
                reservedBuyingPowerForCurrentPosition = (security.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                    ReservedBuyingPowerForPositionParameters(security)).AbsoluteUsedBuyingPower
                                                         * security.BuyingPowerModel.GetLeverage(security)) # see GH issue 4107
                # we sum up deviation for each security
                deviation += (portfolioValuePerSecurity - reservedBuyingPowerForCurrentPosition) / portfolioValuePerSecurity;

            # if securities are deviated 2% from their theoretical share of TotalPortfolioValue we rebalance
            if deviation >= 0.02:
                return time
        return None

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Submitted:
            if self.UtcTime != self.lastRebalanceTime or self.UtcTime.weekday() != 0:
                raise ValueError(f"{self.UtcTime} {orderEvent.Symbol}")
