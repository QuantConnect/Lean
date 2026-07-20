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
### This regression algorithm verifies automatic option contract assignment behavior.
### </summary>
class OptionAssignmentRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 23)
        self.set_end_date(2015, 12, 28)
        self.set_cash(100000)
        self.stock = self.add_equity("GOOG", Resolution.MINUTE)

        contracts = list(self.option_chain(self.stock.symbol))

        self.put_option_symbol = sorted(
            [c for c in contracts if c.id.option_right == OptionRight.PUT and c.id.strike_price == 800],
            key=lambda c: c.id.date
        )[0]

        self.call_option_symbol = sorted(
            [c for c in contracts if c.id.option_right == OptionRight.CALL and c.id.strike_price == 600],
            key=lambda c: c.id.date
        )[0]

        self.put_option = self.add_option_contract(self.put_option_symbol)
        self.call_option = self.add_option_contract(self.call_option_symbol)

    def on_data(self, data):
        if not self.portfolio.invested and self.stock.price != 0 and self.put_option.price != 0 and self.call_option.price != 0:
            #this gets executed on start and after each auto-assignment, finally ending with expiration assignment
            if self.time < self.put_option_symbol.id.date:
                self.market_order(self.put_option_symbol, -1)

            if self.time < self.call_option_symbol.id.date:
                self.market_order(self.call_option_symbol, -1)

    def get_security(self, symbol):
        if symbol == self.stock.symbol:
            return self.stock
        if symbol == self.call_option_symbol:
            return self.call_option
        if symbol == self.put_option_symbol:
            return self.put_option
        raise RegressionTestException(f"Unexpected symbol: {symbol}")

    def on_end_of_algorithm(self):
        for trade in self.trade_builder.closed_trades:
            symbol, = trade.symbols
            direction = 1 if trade.direction == TradeDirection.LONG else -1
            multiplier = self.get_security(symbol).symbol_properties.contract_multiplier
            expected_profit_loss = round((trade.exit_price - trade.entry_price) * trade.quantity * direction * multiplier, 2)

            if trade.profit_loss != expected_profit_loss:
                raise RegressionTestException(f"Expected underlying trade profit/loss to be {expected_profit_loss}. Actual: {trade.profit_loss}")
