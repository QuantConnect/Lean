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

### <summary>
### This algorithm demonstrates how to submit orders to a Financial Advisor account group, allocation profile or a single managed account.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="financial advisor" />
class FinancialAdvisorDemoAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must be initialized.

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        self.symbol = self.AddEquity("SPY", Resolution.Second).Symbol

        # The default order properties can be set here to choose the FA settings
        # to be automatically used in any order submission method (such as SetHoldings, Buy, Sell and Order)

        # Use a default FA Account Group with an Allocation Method
        self.DefaultOrderProperties = InteractiveBrokersOrderProperties()
        # account group created manually in IB/TWS
        self.DefaultOrderProperties.FaGroup = "TestGroupEQ"
        # supported allocation methods are: EqualQuantity, NetLiq, AvailableEquity, PctChange
        self.DefaultOrderProperties.FaMethod = "EqualQuantity"

        # set a default FA Allocation Profile
        # DefaultOrderProperties = InteractiveBrokersOrderProperties()
        # allocation profile created manually in IB/TWS
        # self.DefaultOrderProperties.FaProfile = "TestProfileP"

        # send all orders to a single managed account
        # DefaultOrderProperties = InteractiveBrokersOrderProperties()
        # a sub-account linked to the Financial Advisor master account
        # self.DefaultOrderProperties.Account = "DU123456"

    def OnData(self, data):
        # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        if not self.Portfolio.Invested:
            # when logged into IB as a Financial Advisor, this call will use order properties
            # set in the DefaultOrderProperties property of QCAlgorithm
            self.SetHoldings("SPY", 1)
