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

from SecuritySessionRegressionAlgorithm import SecuritySessionRegressionAlgorithm

### <summary>
### Regression algorithm to validate SecurityCache.Session with Futures.
### Ensures OHLCV are consistent with Tick data.
### </summary>
class SecuritySessionWithFuturesRegressionAlgorithm(SecuritySessionRegressionAlgorithm):
    def initialize_security(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 8)
        self.security = self.add_future(Futures.Metals.GOLD, Resolution.TICK)
        self.bid_price = 0
        self.ask_price = 0
        self.bid_high = 0
        self.bid_low = float('inf')
        self.ask_low = float('inf')
        self.ask_high = 0
        self.previous_open_interest = 0

    def is_within_market_hours(self, current_date_time):
        return self.security.exchange.hours.is_open(current_date_time, False)

    def accumulate_session_data(self, data):
        symbol = self.security.symbol
            
        for tick in data.ticks[symbol]:
            if tick.tick_type == TickType.TRADE:
                self.volume += tick.quantity
                
            if self.current_date.date() == tick.time.date():
                # Same trading day
                if tick.bid_price != 0:
                    self.bid_price = tick.bid_price
                    self.bid_low = min(self.bid_low, tick.bid_price)
                    self.bid_high = max(self.bid_high, tick.bid_price)
                    
                if tick.ask_price != 0:
                    self.ask_price = tick.ask_price
                    self.ask_low = min(self.ask_low, tick.ask_price)
                    self.ask_high = max(self.ask_high, tick.ask_price)
                    
                if self.bid_price != 0 and self.ask_price != 0:
                    mid_price = (self.bid_price + self.ask_price) / 2
                    if self.open == 0:
                        self.open = mid_price
                    self.close = mid_price
                    
                if self.bid_high != 0 and self.ask_high != 0:
                    self.high = max(self.high, (self.bid_high + self.ask_high) / 2)
                    
                if self.bid_low != float('inf') and self.ask_low != float('inf'):
                    self.low = min(self.low, (self.bid_low + self.ask_low) / 2)
                    
            else:
                # New trading day
                if self.previous_session_bar is not None:
                    session = self.security.session
                    if (self.previous_session_bar['open'] != session[1].open
                        or self.previous_session_bar['high'] != session[1].high
                        or self.previous_session_bar['low'] != session[1].low
                        or self.previous_session_bar['close'] != session[1].close
                        or self.previous_session_bar['volume'] != session[1].volume
                        or self.previous_session_bar['open_interest'] != session[1].open_interest):
                        raise RegressionTestException("Mismatch in previous session bar (OHLCV)")

                # This is the first data point of the new session
                self.open = (self.bid_price + self.ask_price) / 2
                self.low = float('inf')
                self.bid_low = float('inf')
                self.ask_low = float('inf')
                self.volume = 0
                self.current_date = tick.time
        
    def validate_session_bars(self):
        session = self.security.session
        # At this point the data was consolidated (market close)

        # Save previous session bar
        self.previous_session_bar = {
            'date': self.current_date,
            'open': self.open,
            'high': self.high,
            'low': self.low,
            'close': self.close,
            'volume': self.volume,
            'open_interest': self.security.open_interest
        }

        if self.security_was_removed:
            self.previous_session_bar = None
            self.security_was_removed = False
            return

        # Check current session values
        if (not self._are_equal(session.open, self.open)
            or not self._are_equal(session.high, self.high)
            or not self._are_equal(session.low, self.low)
            or not self._are_equal(session.close, self.close)
            or not self._are_equal(session.volume, self.volume)
            or not self._are_equal(session.open_interest, self.security.open_interest)):
            raise RegressionTestException("Mismatch in current session bar (OHLCV)")