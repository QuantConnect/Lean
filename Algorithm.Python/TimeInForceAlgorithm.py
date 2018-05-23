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
from QuantConnect.Orders import *
from QuantConnect.Orders.TimeInForces import *
from datetime import datetime

### <summary>
### Demonstration algorithm of time in force order settings.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class TimeInForceAlgorithm(QCAlgorithm):

    # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    def Initialize(self):

        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.SetCash(100000)

        # The default time in force setting for all orders is GoodTilCancelled (GTC),
        # uncomment this line to set a different time in force.
        # We currently only support GTC and DAY.
        # self.DefaultOrderProperties.TimeInForce = TimeInForce.Day;

        self.symbol = self.AddEquity("SPY", Resolution.Second).Symbol

        self.gtcOrderTicket = None
        self.dayOrderTicket = None
        self.gtdOrderTicket = None

    # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    # Arguments:
    #    data: Slice object keyed by symbol containing the stock data
    def OnData(self, data):

        if self.gtcOrderTicket is None:
            # This order has a default time in force of GoodTilCanceled,
            # it will never expire and will not be canceled automatically.

            self.DefaultOrderProperties.TimeInForce = TimeInForce.GoodTilCanceled
            self.gtcOrderTicket = self.LimitOrder(self.symbol, 10, 160)

        if self.dayOrderTicket is None:
            # This order will expire at market close,
            # if not filled by then it will be canceled automatically.

            self.DefaultOrderProperties.TimeInForce = TimeInForce.Day
            self.dayOrderTicket = self.LimitOrder(self.symbol, 10, 160)

        if self.gtdOrderTicket is None:
            # This order will expire on October 10th at market close,
            # if not filled by then it will be canceled automatically.

            self.DefaultOrderProperties.TimeInForce = GoodTilDateTimeInForce(datetime(2013, 10, 10))
            self.gtdOrderTicket = self.LimitOrder(self.symbol, 10, 100)

    # Order event handler. This handler will be called for all order events, including submissions, fills, cancellations.
    # This method can be called asynchronously, ensure you use proper locks on thread-unsafe objects
    def OnOrderEvent(self, orderEvent):
        self.Debug(f"{self.Time} {orderEvent}")
