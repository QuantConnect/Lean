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
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Selection import *

### <summary>
### Expiry Helper algorithm uses Expiry helper class in an Alpha Model
### </summary>
class ExpiryHelperAlphaModelFrameworkAlgorithm(QCAlgorithm):
    '''Expiry Helper framework algorithm uses Expiry helper class in an Alpha Model'''

    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Hour

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2014,1,1)      #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        symbols = [ Symbol.Create("SPY", SecurityType.Equity, Market.USA) ]

        # set algorithm framework models
        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols))
        self.SetAlpha(self.ExpiryHelperAlphaModel())
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.01))

        self.InsightsGenerated += self.OnInsightsGenerated

    def OnInsightsGenerated(self, s, e):
        for insight in e.Insights:
            self.Log(f"{e.DateTimeUtc.isoweekday()}: Close Time {insight.CloseTimeUtc} {insight.CloseTimeUtc.isoweekday()}")

    class ExpiryHelperAlphaModel(AlphaModel):
        nextUpdate = None
        direction = InsightDirection.Up

        def Update(self, algorithm, data):

            if self.nextUpdate is not None and self.nextUpdate > algorithm.Time:
                return []

            expiry = Expiry.EndOfDay

            # Use the Expiry helper to calculate a date/time in the future
            self.nextUpdate = expiry(algorithm.Time)

            weekday = algorithm.Time.isoweekday()

            insights = []
            for symbol in data.Bars.Keys:
                # Expected CloseTime: next month on the same day and time
                if weekday == 1:
                    insights.append(Insight.Price(symbol, Expiry.OneMonth, self.direction))
                # Expected CloseTime: next month on the 1st at market open time
                elif weekday == 2:
                    insights.append(Insight.Price(symbol, Expiry.EndOfMonth, self.direction))
                # Expected CloseTime: next Monday at market open time
                elif weekday == 3:
                    insights.append(Insight.Price(symbol, Expiry.EndOfWeek, self.direction))
                # Expected CloseTime: next day (Friday) at market open time
                elif weekday == 4:
                    insights.append(Insight.Price(symbol, Expiry.EndOfDay, self.direction))

            return insights