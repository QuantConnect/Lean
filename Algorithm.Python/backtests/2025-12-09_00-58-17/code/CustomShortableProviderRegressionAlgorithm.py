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

    def initialize(self) -> None:
        self.set_cash(10000000)
        self.set_start_date(2013,10,4)
        self.set_end_date(2013,10,6)
        self._spy = self.add_security(SecurityType.EQUITY, "SPY", Resolution.DAILY)
        self._spy.set_shortable_provider(CustomShortableProvider())

    def on_data(self, data: Slice) -> None:
        spy_shortable_quantity = self._spy.shortable_provider.shortable_quantity(self._spy.symbol, self.time)
        if spy_shortable_quantity and spy_shortable_quantity > 1000:
            self._order_id = self.sell("SPY", int(spy_shortable_quantity)).order_id

    def on_end_of_algorithm(self) -> None:
        transactions = self.transactions.orders_count
        if transactions != 1:
            raise AssertionError("Algorithm should have just 1 order, but was " + str(transactions))

        order_quantity = self.transactions.get_order_by_id(self._order_id).quantity
        if order_quantity != -1001:
            raise AssertionError(f"Quantity of order {self._order_id} should be -1001 but was {order_quantity}")
        
        fee_rate = self._spy.shortable_provider.fee_rate(self._spy.symbol, self.time)
        if fee_rate != 0.0025:
            raise AssertionError(f"Fee rate should be 0.0025, but was {fee_rate}")
        rebate_rate = self._spy.shortable_provider.rebate_rate(self._spy.symbol, self.time)
        if rebate_rate != 0.0507:
            raise AssertionError(f"Rebate rate should be 0.0507, but was {rebate_rate}")

class CustomShortableProvider(NullShortableProvider):
    def fee_rate(self, symbol: Symbol, local_time: datetime) -> float:
        return 0.0025
    def rebate_rate(self, symbol: Symbol, local_time: datetime) -> float:
        return 0.0507
    def shortable_quantity(self, symbol: Symbol, local_time: datetime) -> int:
        if local_time < datetime(2013,10,4,16,0,0):
            return 10
        return 1001
