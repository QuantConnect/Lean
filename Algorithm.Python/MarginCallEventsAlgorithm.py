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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import QCAlgorithm
import numpy as np
from datetime import datetime, timedelta

### <summary>
### This algorithm showcases two margin related event handlers.
### OnMarginCallWarning: Fired when a portfolio's remaining margin dips below 5% of the total portfolio value
### OnMarginCall: Fired immediately before margin call orders are execued, this gives the algorithm a change to regain margin on its own through liquidation
### </summary>
### <meta name="tag" content="securities and portfolio" />
### <meta name="tag" content="margin models" />
class MarginCallEventsAlgorithm(QCAlgorithm):
    """
    This algorithm showcases two margin related event handlers.
    OnMarginCallWarning: Fired when a portfolio's remaining margin dips below 5% of the total portfolio value
    OnMarginCall: Fired immediately before margin call orders are execued, this gives the algorithm a change to regain margin on its own through liquidation
    """

    def Initialize(self):

        self.SetCash(100000)
        self.SetStartDate(2013,10,1)
        self.SetEndDate(2013,12,11)
        self.AddEquity("SPY", Resolution.Second)
        # cranking up the leverage increases the odds of a margin call
        # when the security falls in value
        self.Securities["SPY"].SetLeverage(100)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY",100)

    def OnMarginCall(self, requests):

        # Margin call event handler. This method is called right before the margin call orders are placed in the market.
        # <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        # this code gets called BEFORE the orders are placed, so we can try to liquidate some of our positions
        # before we get the margin call orders executed. We could also modify these orders by changing their quantities
        for order in requests:

            # liquidate an extra 10% each time we get a margin call to give us more padding
            newQuantity = int(np.sign(order.Quantity) * order.Quantity * 1.1)
            requests.remove(order)
            requests.append(SubmitOrderRequest(order.OrderType, order.SecurityType, order.Symbol, newQuantity, order.StopPrice, order.LimitPrice, self.Time, "OnMarginCall"))

        return requests

    def OnMarginCallWarning(self):

        # Margin call warning event handler.
        # This method is called when Portfolio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
        # a chance to prevent a margin call from occurring

        spyHoldings = self.Securities["SPY"].Holdings.Quantity
        shares = int(-spyHoldings * 0.005)
        self.Error("{0} - OnMarginCallWarning(): Liquidating {1} shares of SPY to avoid margin call.".format(self.Time, shares))
        self.MarketOrder("SPY", shares)