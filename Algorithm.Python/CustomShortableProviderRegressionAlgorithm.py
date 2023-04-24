### QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
### Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
###
### Licensed under the Apache License, Version 2.0 (the "License");
### you may not use this file except in compliance with the License.
### You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
###
### Unless required by applicable law or agreed to in writing, software
### distributed under the License is distributed on an "AS IS" BASIS,
### WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
### See the License for the specific language governing permissions and
### limitations under the License.

from AlgorithmImports import *

### <summary>
### Regression algorithm asserting we can specify a custom Shortable Provider
### </summary>
class CustomShortableProviderRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetCash(1000000);
        self.SetStartDate(2013,10,4)
        self.SetEndDate(2013,10,6)
        self.spy = self.AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily)
        self.spy.SetShortableProvider(CustomShortableProvider())

    def OnData(self, data):
        spyShortableQuantity = self.spy.ShortableProvider.ShortableQuantity(self.spy.Symbol, self.Time)
        if spyShortableQuantity > 1000:
            self.orderId = self.Sell("SPY", int(spyShortableQuantity))

    def OnEndOfAlgorithm(self):
        transactions = self.Transactions.OrdersCount
        if transactions != 1:
            raise Exception("Algorithm should have just 1 order, but was " + str(transactions))

        orderQuantity = self.Transactions.GetOrderById(self.orderId).Quantity
        if orderQuantity != -1001:
            raise Exception("Quantity of order " + str(_orderId) + " should be " + str(-1001)+", but was {orderQuantity}")

class CustomShortableProvider(NullShortableProvider):
    def ShortableQuantity(self, symbol: Symbol, localTime: DateTime):
        if localTime < datetime(2013,10,5):
            return 10
        else:
            return 1001
