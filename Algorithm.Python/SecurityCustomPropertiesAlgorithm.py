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

from cmath import isclose
from AlgorithmImports import *

### <summary>
### Demonstration of how to use custom security properties.
### In this algorithm we trade a security based on the values of a slow and fast EMAs which are stored in the security itself.
### </summary>
class SecurityCustomPropertiesAlgorithm(QCAlgorithm):
    '''Demonstration of how to use custom security properties.
    In this algorithm we trade a security based on the values of a slow and fast EMAs which are stored in the security itself.'''

    def Initialize(self):
        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)
        self.SetCash(100000)

        self.spy = self.AddEquity("SPY", Resolution.Minute)

        # Using the dynamic interface to store our indicator as a custom property.
        self.spy.SlowEma = self.EMA(self.spy.Symbol, 30, Resolution.Minute)

        # Using the generic interface to store our indicator as a custom property.
        self.spy.Add("FastEma", self.EMA(self.spy.Symbol, 60, Resolution.Minute))

        # Using the indexer to store our indicator as a custom property
        self.spy["BB"] = self.BB(self.spy.Symbol, 20, 1, MovingAverageType.Simple, Resolution.Minute);

        # Fee factor to be used by the custom fee model
        self.spy["FeeFactor"] = 0.00002
        self.spy.SetFeeModel(CustomFeeModel())

        # This property will be used to store the prices used to calculate the fees in order to assert the correct fee factor is used.
        self.spy["OrdersFeesPrices"] = {}

    def OnData(self, data):
        if not self.spy.Get[IndicatorBase]("FastEma").IsReady:
            return

        if not self.Portfolio.Invested:
            # Using the property and the generic interface to access our indicator
            if self.spy.SlowEma > self.spy.Get[IndicatorBase]("FastEma"):
                self.SetHoldings(self.spy.Symbol, 1)
        else:
            if self.spy.SlowEma < self.spy.Get[IndicatorBase]("FastEma"):
                self.Liquidate(self.spy.Symbol)

        # Using the indexer to access our indicator
        bb: BollingerBands = self.spy["BB"]
        self.Plot("BB", bb.UpperBand, bb.MiddleBand, bb.LowerBand)

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            fee = orderEvent.OrderFee
            expectedFee = self.spy["OrdersFeesPrices"][orderEvent.OrderId] * orderEvent.AbsoluteFillQuantity * self.spy["FeeFactor"]
            if not isclose(fee.Value.Amount, expectedFee, rel_tol=1e-15):
                raise Exception(f"Custom fee model failed to set the correct fee. Expected: {expectedFee}. Actual: {fee.Value.Amount}")

    def OnEndOfAlgorithm(self):
        if self.Transactions.OrdersCount == 0:
            raise Exception("No orders executed")

class CustomFeeModel(FeeModel):
    '''This custom fee is implemented for demonstration purposes only.'''

    def GetOrderFee(self, parameters):
        security = parameters.Security
        # custom fee math using the fee factor stored in security instance
        feeFactor = security["FeeFactor"]
        if feeFactor is None:
            feeFactor = 0.00001

        # Store the price used to calculate the fee for this order
        security["OrdersFeesPrices"][parameters.Order.Id] = security.Price

        fee = max(1.0, security.Price * parameters.Order.AbsoluteQuantity * feeFactor)

        return OrderFee(CashAmount(fee, "USD"))
