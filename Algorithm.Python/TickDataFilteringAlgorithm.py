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

from datetime import timedelta
from AlgorithmImports import *

# <summary>
# Demonstration of filtering tick data so easier to use. Tick data has lots of glitchy, spikey data which should be filtered out before usagee.
# </summary>
# <meta name="tag" content="filtering" />
# <meta name="tag" content="tick data" />
# <meta name="tag" content="using data" />
# <meta name="tag" content="ticks event" />
class TickDataFilteringAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)
        self.set_cash(25000)
        spy = self.add_equity("SPY", Resolution.TICK)

        #Add our custom data filter.
        spy.set_data_filter(TickExchangeDataFilter(self))

        self._order_time = None

    # <summary>
    # Data arriving here will now be filtered.
    # </summary>
    # <param name="data">Ticks data array</param>
    def on_data(self, data):
        if not data.contains_key("SPY"):
            return

        spy_tick_list = data["SPY"]

        # Ticks return a list of ticks this second
        for tick in spy_tick_list:
            self.debug(tick.exchange)

        if not self.portfolio.invested:
            self.set_holdings("SPY", 1)
            self._order_time = self.time
        # Let's shortcut to reduce regression test duration
        elif self.time - self._order_time > timedelta(minutes=5):
            self.quit()

# <summary>
# Exchange filter class
# </summary>
class TickExchangeDataFilter(SecurityDataFilter):

    # <summary>
    # Save instance of the algorithm namespace
    # </summary>
    # <param name="algo"></param>
    def __init__(self, algo: IAlgorithm):
        self.algo = algo
        super().__init__()

    # <summary>
    # Filter out a tick from this vehicle, with this new data:
    # </summary>
    # <param name="data">New data packet:</param>
    # <param name="asset">Vehicle of this filter.</param>
    def filter(self, asset: Security, data: BaseData):
        # TRUE -->  Accept Tick
        # FALSE --> Reject Tick

        if isinstance(data, Tick):
            if data.exchange == str(Exchange.ARCA):
                return True

        return False
