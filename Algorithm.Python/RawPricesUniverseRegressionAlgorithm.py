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
AddReference("System.Core")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Orders import OrderStatus
from QuantConnect.Orders.Fees import ConstantFeeModel

### <summary>
### In this algorithm we demonstrate how to use the UniverseSettings
### to define the data normalization mode (raw)
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
### <meta name="tag" content="fine universes" />
class RawPricesUniverseRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        # what resolution should the data *added* to the universe be?
        self.UniverseSettings.Resolution = Resolution.Daily

        # Use raw prices
        self.UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

        self.SetStartDate(2014,3,24)    #Set Start Date
        self.SetEndDate(2014,4,7)      #Set End Date
        self.SetCash(50000)            #Set Strategy Cash

        # Set the security initializer with zero fees
        self.SetSecurityInitializer(lambda x: x.SetFeeModel(ConstantFeeModel(0)))

        self.AddUniverse("MyUniverse", Resolution.Daily, self.SelectionFunction);


    def SelectionFunction(self, dateTime):
        if dateTime.day % 2 == 0:
            return ["SPY", "IWM", "QQQ"]
        else:
            return ["AIG", "BAC", "IBM"]


    # this event fires whenever we have changes to our universe
    def OnSecuritiesChanged(self, changes):
        # liquidate removed securities
        for security in changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)

        # we want 20% allocation in each security in our universe
        for security in changes.AddedSecurities:
            self.SetHoldings(security.Symbol, 0.2)