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
### This algorithm demonstrates extended market hours trading.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="assets" />
### <meta name="tag" content="regression test" />
class ExtendedMarketTradingRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.set_start_date(2013,10,7)   #Set Start Date
        self.set_end_date(2013,10,11)    #Set End Date
        self.set_cash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.spy = self.add_equity("SPY", Resolution.MINUTE, Market.USA, True, 1, True)

        self._last_action = None

    def on_data(self, data):
        '''on_data event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if self._last_action is not None and self._last_action.date() == self.time.date():
            return

        spy_bar = data.bars['SPY']

        if not self.in_market_hours():
            self.limit_order("SPY", 10, spy_bar.low)
            self._last_action = self.time

    def on_order_event(self, order_event):
        self.log(str(order_event))
        if self.in_market_hours():
            raise AssertionError("Order processed during market hours.")

    def in_market_hours(self):
        now = self.time.time()
        open = time(9,30,0)
        close = time(16,0,0)
        return (open < now) and (close > now)

