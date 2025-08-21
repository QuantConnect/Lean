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
### Regression algorithm to validate SecurityCache.Session with Futures.
### Ensures OHLCV + OpenInterest are consistent with Tick data.
### </summary>
class SecurityCacheSessionWithFuturesRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 9)
        
        self._future = self.add_future(Futures.Metals.Gold, Resolution.TICK)
        self._symbol = self._future.symbol

        self._open = 0
        self._high = 0
        self._low = float('inf')
        self._close = 0
        self._volume = 0
        self._bid_price = 0
        self._ask_price = 0
        self._bid_high = 0
        self._bid_low = float('inf')
        self._ask_low = float('inf')
        self._ask_high = 0
        self._open_interest = 0
        self._session_bar = None
        self._previous_session_bar = None
        self._current_date = self.start_date
        
        self.schedule.on(self.date_rules.every_day(), 
                        self.time_rules.after_market_open(self._future.symbol, 1), 
                        self.validate_session_bars)

    def _are_equal(self, value1, value2):
        tolerance = 1e-10
        return abs(value1 - value2) <= tolerance

    def validate_session_bars(self):
        session = self._future.session

        # Adding tolerance to compare floats
        # Check current session values
        if session.is_trading_day_data_ready:
            if (self._session_bar is None or
                not self._are_equal(self._session_bar.open, session.open) or
                not self._are_equal(self._session_bar.high, session.high) or
                not self._are_equal(self._session_bar.low, session.low) or
                not self._are_equal(self._session_bar.close, session.close) or
                not self._are_equal(self._session_bar.volume, session.volume) or
                not self._are_equal(self._session_bar.open_interest, session.open_interest)):
                raise RegressionTestException("Mismatch in current session bar (OHLCV)")

        # Check previous session values
        if self._previous_session_bar is not None:
            if (not self._are_equal(self._previous_session_bar.open, session[1].open) or
                not self._are_equal(self._previous_session_bar.high, session[1].high) or
                not self._are_equal(self._previous_session_bar.low, session[1].low) or
                not self._are_equal(self._previous_session_bar.close, session[1].close) or
                not self._are_equal(self._previous_session_bar.volume, session[1].volume) or
                not self._are_equal(self._previous_session_bar.open_interest, session[1].open_interest)):
                raise RegressionTestException("Mismatch in previous session bar (OHLCV)")

    def on_data(self, slice):
        if self._symbol not in slice.ticks:
            return
            
        for tick in slice.ticks[self._symbol]:
            if tick.tick_type == TickType.TRADE:
                self._volume += tick.quantity
            elif tick.tick_type == TickType.OPEN_INTEREST:
                self._open_interest = tick.value
                
            if self._current_date.date() == tick.time.date():
                if tick.bid_price != 0:
                    self._bid_price = tick.bid_price
                    self._bid_low = min(self._bid_low, tick.bid_price)
                    self._bid_high = max(self._bid_high, tick.bid_price)
                    
                if tick.ask_price != 0:
                    self._ask_price = tick.ask_price
                    self._ask_low = min(self._ask_low, tick.ask_price)
                    self._ask_high = max(self._ask_high, tick.ask_price)
                    
                if self._bid_price != 0 and self._ask_price != 0:
                    mid_price = (self._bid_price + self._ask_price) / 2
                    if self._open == 0:
                        self._open = mid_price
                    self._close = mid_price
                    
                if self._bid_high != 0 and self._ask_high != 0:
                    self._high = max(self._high, (self._bid_high + self._ask_high) / 2)
                    
                if self._bid_low != float('inf') and self._ask_low != float('inf'):
                    self._low = min(self._low, (self._bid_low + self._ask_low) / 2)
                    
            else:
                # New trading day

                # Save previous session bar
                self._previous_session_bar = self._session_bar

                # Create new session bar
                self._session_bar = SessionBar(
                    self._current_date,
                    self._open,
                    self._high,
                    self._low,
                    self._close,
                    self._volume,
                    self._open_interest
                )
                
                # This is the first data point of the new session
                self._open = (self._bid_price + self._ask_price) / 2
                self._low = float('inf')
                self._bid_low = float('inf')
                self._ask_low = float('inf')
                self._volume = 0
                self._current_date = tick.time