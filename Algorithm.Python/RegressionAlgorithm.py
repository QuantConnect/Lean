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
### Algorithm used for regression tests purposes
### </summary>
### <meta name="tag" content="regression test" />
class RegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.set_start_date(2013,10,7)   #Set Start Date
        self.set_end_date(2013,10,11)    #Set End Date
        self.set_cash(10000000)         #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.add_equity("SPY", Resolution.TICK)
        self.add_equity("BAC", Resolution.MINUTE)
        self.add_equity("AIG", Resolution.HOUR)
        self.add_equity("IBM", Resolution.DAILY)

        self.__last_trade_ticks = self.start_date
        self.__last_trade_trade_bars = self.__last_trade_ticks
        self.__trade_every = timedelta(minutes=1)


    def on_data(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if self.time - self.__last_trade_trade_bars < self.__trade_every:
            return
        self.__last_trade_trade_bars = self.time

        for kvp in data.bars:
            period = kvp.value.period.total_seconds()

            if self.round_time(self.time, period) != self.time:
                pass

            symbol = kvp.key
            holdings = self.portfolio[symbol]

            if not holdings.invested:
                self.market_order(symbol, 10)
            else:
                self.market_order(symbol, -holdings.quantity)


    def round_time(self, dt=None, round_to=60):
        """Round a datetime object to any time laps in seconds
        dt : datetime object, default now.
        roundTo : Closest number of seconds to round to, default 1 minute.
        """
        if dt is None : dt = datetime.now()
        seconds = (dt - dt.min).seconds
        # // is a floor division, not a comment on following line:
        rounding = (seconds+round_to/2) // round_to * round_to
        return dt + timedelta(0,rounding-seconds,-dt.microsecond)
