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

from AlgorithmImports import *

### <summary>
### Regression algorithm asserting that an equity held through its delisting is liquidated with a market on close order
### at the close of its last trading day, instead of at midnight once delisted
### </summary>
class DelistingLiquidationAtMarketCloseRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2007, 5, 15)
        self.set_end_date(2007, 5, 25)
        self.set_cash(100000)

        self._symbol = self.add_equity("AAA.1", Resolution.DAILY).symbol

    def on_data(self, slice):
        # buy once and hold through the delisting, so the engine has to liquidate the position
        if self.transactions.orders_count == 0 and slice.bars.contains_key(self._symbol):
            self.set_holdings(self._symbol, 0.5)

    def on_end_of_algorithm(self):
        liquidations = list(self.transactions.get_orders(lambda order: order.tag == "Liquidate from delisting"))
        if len(liquidations) != 1:
            raise AssertionError(f"Expected a single delisting liquidation, got {len(liquidations)}")

        liquidation = liquidations[0]
        if liquidation.type != OrderType.MARKET_ON_CLOSE or liquidation.status != OrderStatus.FILLED:
            raise AssertionError(f"Expected a filled MarketOnClose delisting liquidation, got a {liquidation.status} {liquidation.type} order")

        security = self.securities[self._symbol]
        fill_time = Extensions.convert_from_utc(liquidation.last_fill_time, security.exchange.time_zone)
        expected_time = datetime(2007, 5, 18, 16, 0, 0)
        if fill_time != expected_time:
            raise AssertionError(f"Expected the delisting liquidation to fill at {expected_time}, the close of the last trading day, but it filled at {fill_time}")
        if not security.exchange.hours.is_open(fill_time - timedelta(microseconds=1), False):
            raise AssertionError(f"Expected the delisting liquidation to fill during regular market hours, but it filled at {fill_time}")
        if self.portfolio[self._symbol].invested:
            raise AssertionError(f"Expected the {self._symbol} position to be liquidated")
