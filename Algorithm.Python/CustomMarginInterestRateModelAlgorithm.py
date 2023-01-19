# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License")
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
### Demonstration of using custom margin interest rate model in backtesting.
### </summary>
class CustomMarginInterestRateModelAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 1)
        self.SetEndDate(2013, 10, 31)

        security = self.AddEquity("SPY", Resolution.Hour)
        self._spy = security.Symbol

        # set the margin interest rate model
        self._marginInterestRateModel = CustomMarginInterestRateModel()
        security.SetMarginInterestRateModel(self._marginInterestRateModel)

        self._cashAfterOrder = 0

    def OnData(self, data: Slice):
        if not self.Portfolio.Invested:
            self.SetHoldings(self._spy, 1)

    def OnOrderEvent(self, orderEvent: OrderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            self._cashAfterOrder = self.Portfolio.Cash

    def OnEndOfAlgorithm(self):
        if self._marginInterestRateModel.callCount == 0:
            raise Exception("CustomMarginInterestRateModel was not called")

        expectedCash = self._cashAfterOrder * pow(1 + self._marginInterestRateModel.interestRate, self._marginInterestRateModel.callCount)

        if abs(self.Portfolio.Cash - expectedCash) > 1e-10:
            raise Exception(f"Expected cash {expectedCash} but got {self.Portfolio.Cash}")


class CustomMarginInterestRateModel:
    def __init__(self):
        self.interestRate = 0.01
        self.callCount = 0

    def ApplyMarginInterestRate(self, parameters: MarginInterestRateParameters):
        security = parameters.Security
        positionValue = security.Holdings.GetQuantityValue(security.Holdings.Quantity)

        if positionValue.Amount > 0:
            positionValue.Cash.AddAmount(self.interestRate * positionValue.Cash.Amount)
            self.callCount += 1
