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
# limitations under the License

from AlgorithmImports import *

### <summary>
### This regression algorithm tests In The Money (ITM) index option expiry for calls.
### We expect 2 orders from the algorithm, which are:
###
###   * Initial entry, buy SPX Call Option (expiring ITM)
###   * Option exercise, settles into cash
###
### Additionally, we test delistings for index options and assert that our
### portfolio holdings reflect the orders the algorithm has submitted.
### </summary>
class IndexOptionCallITMExpiryRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2021, 1, 4)
        self.set_end_date(2021, 1, 31)
        self.set_cash(100000)

        self.spx = self.add_index("SPX", Resolution.MINUTE).symbol

        # Select an index option expiring ITM, and adds it to the algorithm.
        self.spx_options = list(self.option_chain(self.spx))
        self.spx_options = [i for i in self.spx_options if i.id.strike_price <= 3200 and i.id.option_right == OptionRight.CALL and i.id.date.year == 2021 and i.id.date.month == 1]
        self.spx_option_contract = list(sorted(self.spx_options, key=lambda x: x.id.strike_price, reverse=True))[0]
        self.spx_option = self.add_index_option_contract(self.spx_option_contract, Resolution.MINUTE).symbol

        self.expected_option_contract = Symbol.create_option(self.spx, Market.USA, OptionStyle.EUROPEAN, OptionRight.CALL, 3200, datetime(2021, 1, 15))
        if self.spx_option != self.expected_option_contract:
            raise AssertionError(f"Contract {self.expected_option_contract} was not found in the chain")

        self.schedule.on(
            self.date_rules.tomorrow,
            self.time_rules.after_market_open(self.spx, 1),
            lambda: self.market_order(self.spx_option, 1)
        )

    def on_data(self, data: Slice):
        # Assert delistings, so that we can make sure that we receive the delisting warnings at
        # the expected time. These assertions detect bug #4872
        for delisting in data.delistings.values():
            if delisting.type == DelistingType.WARNING:
                if delisting.time != datetime(2021, 1, 15):
                    raise AssertionError(f"Delisting warning issued at unexpected date: {delisting.time}")

            if delisting.type == DelistingType.DELISTED:
                if delisting.time != datetime(2021, 1, 16):
                    raise AssertionError(f"Delisting happened at unexpected date: {delisting.time}")

    def on_order_event(self, order_event: OrderEvent):
        if order_event.status != OrderStatus.FILLED:
            # There's lots of noise with OnOrderEvent, but we're only interested in fills.
            return

        if order_event.symbol not in self.securities:
            raise AssertionError(f"Order event Symbol not found in Securities collection: {order_event.symbol}")

        security = self.securities[order_event.symbol]
        if security.symbol == self.spx:
            self.assert_index_option_order_exercise(order_event, security, self.securities[self.expected_option_contract])
        elif security.symbol == self.expected_option_contract:
            self.assert_index_option_contract_order(order_event, security)
        else:
            raise AssertionError(f"Received order event for unknown Symbol: {order_event.symbol}")

        self.log(f"{self.time} -- {order_event.symbol} :: Price: {self.securities[order_event.symbol].holdings.price} Qty: {self.securities[order_event.symbol].holdings.quantity} Direction: {order_event.direction} Msg: {order_event.message}")

    def assert_index_option_order_exercise(self, order_event: OrderEvent, index: Security, option_contract: Security):
        # No way to detect option exercise orders or any other kind of special orders
        # other than matching strings, for now.
        if "Option Exercise" in order_event.message:
            if order_event.fill_price != 3200:
                raise AssertionError("Option did not exercise at expected strike price (3200)")

            if option_contract.holdings.quantity != 0:
                raise AssertionError(f"Exercised option contract, but we have holdings for Option contract {option_contract.symbol}")

    def assert_index_option_contract_order(self, order_event: OrderEvent, option: Security):
        if order_event.direction == OrderDirection.BUY and option.holdings.quantity != 1:
            raise AssertionError(f"No holdings were created for option contract {option.symbol}")
        if order_event.direction == OrderDirection.SELL and option.holdings.quantity != 0:
            raise AssertionError(f"Holdings were found after a filled option exercise")
        if "Exercise" in order_event.message and option.holdings.quantity != 0:
            raise AssertionError(f"Holdings were found after exercising option contract {option.symbol}")

    ### <summary>
    ### Ran at the end of the algorithm to ensure the algorithm has no holdings
    ### </summary>
    ### <exception cref="Exception">The algorithm has holdings</exception>
    def on_end_of_algorithm(self):
        if self.portfolio.invested:
            raise AssertionError(f"Expected no holdings at end of algorithm, but are invested in: {', '.join(self.portfolio.keys())}")
