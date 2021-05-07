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
from QuantConnect.Securities import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *
from datetime import *

class MarketOnCloseOrderBufferRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013,10,4)   #Set Start Date
        self.SetEndDate(2013,10,4)    #Set End Date

        self.AddEquity("SPY", Resolution.Minute)

        # Modify our submission buffer time to 10 minutes
        MarketOnCloseOrder.SubmissionTimeBuffer = timedelta(minutes=10)

    def OnData(self, data):
        # Test our ability to submit MarketOnCloseOrders
        # Because we set our buffer to 10 minutes, any order placed
        # before 3:50PM should be accepted, any after marked invalid

        # Will not throw an order error and execute
        if self.Time.hour == 15 and self.Time.minute == 49:
            self.validOrderTicket = self.MarketOnCloseOrder("SPY", 2)

        # Will throw an order error and be marked invalid
        if self.Time.hour == 15 and self.Time.minute == 51:
            self.invalidOrderTicket = self.MarketOnCloseOrder("SPY", 2)

    def OnEndOfAlgorithm(self):
        # Set it back to default for other regressions
        MarketOnCloseOrder.SubmissionTimeBuffer = MarketOnCloseOrder.DefaultSubmissionTimeBuffer;

        if self.validOrderTicket.Status != OrderStatus.Filled:
            raise Exception("Valid order failed to fill")

        if self.invalidOrderTicket.Status != OrderStatus.Invalid:
            raise Exception("Invalid order was not rejected")
