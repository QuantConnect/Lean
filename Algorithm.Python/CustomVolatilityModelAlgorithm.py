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
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from datetime import datetime, timedelta
import numpy as np

### <summary>
### Example of custom volatility model 
### </summary>
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="reality modelling" />
class CustomVolatilityModelAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2015,7,15)     #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.equity = self.AddEquity("SPY", Resolution.Daily)
        self.equity.SetVolatilityModel(CustomVolatilityModel(10))


    def OnData(self, data):
        if not self.Portfolio.Invested and self.equity.VolatilityModel.Volatility > 0:
            self.SetHoldings("SPY", 1)


# Python implementation of StandardDeviationOfReturnsVolatilityModel
# Computes the annualized sample standard deviation of daily returns as the volatility of the security
# https://github.com/QuantConnect/Lean/blob/master/Common/Securities/Volatility/StandardDeviationOfReturnsVolatilityModel.cs
class CustomVolatilityModel():
    def __init__(self, periods):
        self.lastUpdate = datetime.min
        self.lastPrice = 0
        self.needsUpdate = False
        self.periodSpan = timedelta(1)
        self.window = RollingWindow[float](periods)

        # Volatility is a mandatory attribute
        self.Volatility = 0

    # Updates this model using the new price information in the specified security instance
    # Update is a mandatory method
    def Update(self, security, data):
        timeSinceLastUpdate = data.EndTime - self.lastUpdate
        if timeSinceLastUpdate >= self.periodSpan and data.Price > 0:
            if self.lastPrice > 0:
                self.window.Add(float(data.Price / self.lastPrice) - 1.0)
                self.needsUpdate = self.window.IsReady
            self.lastUpdate = data.EndTime
            self.lastPrice = data.Price

        if self.window.Count < 2:
            self.Volatility = 0
            return

        if self.needsUpdate:
            self.needsUpdate = False
            std = np.std([ x for x in self.window ])
            self.Volatility = std * np.sqrt(252.0)

    # Returns history requirements for the volatility model expressed in the form of history request
    # GetHistoryRequirements is a mandatory method
    def GetHistoryRequirements(self, security, utcTime):
        # For simplicity's sake, we will not set a history requirement 
        return None