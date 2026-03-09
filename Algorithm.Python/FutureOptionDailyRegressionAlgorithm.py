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
### This regression algorithm tests using FutureOptions daily resolution
### </summary>
class FutureOptionDailyRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2020, 1, 7)
        self.set_end_date(2020, 1, 8)
        resolution = Resolution.DAILY

        # Add our underlying future contract
        self.es = self.add_future_contract(
            Symbol.create_future(
                Futures.Indices.SP_500_E_MINI,
                Market.CME,
                datetime(2020, 3, 20)
            ),
            resolution).symbol

        # Attempt to fetch a specific ITM future option contract
        es_options = [
            self.add_future_option_contract(x, resolution).symbol
            for x in self.option_chain(self.es)
            if x.id.strike_price == 3200 and x.id.option_right == OptionRight.CALL
        ]
        self.es_option = es_options[0]

        # Validate it is the expected contract
        expected_contract = Symbol.create_option(self.es, Market.CME, OptionStyle.AMERICAN, OptionRight.CALL, 3200, datetime(2020, 3, 20))
        if self.es_option != expected_contract:
            raise AssertionError(f"Contract {self.es_option} was not the expected contract {expected_contract}")

        # Schedule a purchase of this contract tomorrow at 10AM when the market is open
        self.schedule.on(self.date_rules.tomorrow, self.time_rules.at(10,0,0), self.schedule_callback_buy)

        # Schedule liquidation at 2pm tomorrow when the market is open
        self.schedule.on(self.date_rules.tomorrow, self.time_rules.at(14,0,0), self.schedule_callback_liquidate)

    def schedule_callback_buy(self):
        self.market_order(self.es_option, 1)

    def on_data(self, slice):
        # Assert we are only getting data at 4PM NY, for ES future market closes at 16pm NY
        if slice.time.hour != 17:
            raise AssertionError(f"Expected data at 5PM each day; instead was {slice.time}")

    def schedule_callback_liquidate(self):
        self.liquidate()

    def on_end_of_algorithm(self):
        if self.portfolio.invested:
            raise AssertionError(f"Expected no holdings at end of algorithm, but are invested in: {', '.join([str(i.id) for i in self.portfolio.keys()])}")
