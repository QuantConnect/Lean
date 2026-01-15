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
### Regression algorithm to validate SecurityCache.Session functionality.
### Verifies that daily session bars (Open, High, Low, Close, Volume) are correctly
### </summary>
class SecuritySessionRegressionAlgorithm(QCAlgorithm):
    
    def initialize(self):
        self.add_security_initializer(self.initialize_session_tracking)
        self.initialize_security()

        # Check initial session values
        session = self.security.session
        if session is None:
            raise RegressionTestException("Security.Session is none")
        if (session.open != 0
            or session.high != 0
            or session.low != 0
            or session.close != 0
            or session.volume != 0
            or session.open_interest != 0):
            raise RegressionTestException("Session should start with all zero values.")
            
        self.security_was_removed = False
        self.open = self.close = self.high = self.volume = 0
        self.low = float('inf')
        self.current_date = self.start_date
        self.previous_session_bar = None

        self.schedule.on(
            self.date_rules.every_day(),
            self.time_rules.after_market_close(self.security.symbol, 1),
            self.validate_session_bars
        )

    def initialize_security(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.security = self.add_equity("SPY", Resolution.HOUR)

    def initialize_session_tracking(self, security):
        # activate session tracking
        security.session.size = 3

    def _are_equal(self, value1, value2):
        tolerance = 1e-10
        return abs(value1 - value2) <= tolerance
    
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
            'volume': self.volume
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
            or not self._are_equal(session.volume, self.volume)):
            raise RegressionTestException("Mismatch in current session bar (OHLCV)")

    def is_within_market_hours(self, current_date_time):
        market_open = self.security.exchange.hours.get_next_market_open(current_date_time.date(), False).time()
        market_close = self.security.exchange.hours.get_next_market_close(current_date_time.date(), False).time()
        current_time = current_date_time.time()
        return market_open < current_time <= market_close

    def on_data(self, data):
        if not self.is_within_market_hours(data.time):
            # Skip data outside market hours
            return

        # Accumulate data within regular market hours
        # to later compare against the Session values
        self.accumulate_session_data(data)

    def accumulate_session_data(self, data):
        symbol = self.security.symbol
        if self.current_date.date() == data.time.date():
            # Same trading day
            if self.open == 0:
                self.open = data[symbol].open
            self.high = max(self.high, data[symbol].high)
            self.low = min(self.low, data[symbol].low)
            self.close = data[symbol].close
            self.volume += data[symbol].volume
        else:
            # New trading day

            if self.previous_session_bar is not None:
                session = self.security.session
                if (self.previous_session_bar['open'] != session[1].open
                    or self.previous_session_bar['high'] != session[1].high
                    or self.previous_session_bar['low'] != session[1].low
                    or self.previous_session_bar['close'] != session[1].close
                    or self.previous_session_bar['volume'] != session[1].volume):
                    raise RegressionTestException("Mismatch in previous session bar (OHLCV)")

            # This is the first data point of the new session
            self.open = data[symbol].open
            self.close = data[symbol].close
            self.high = data[symbol].high
            self.low = data[symbol].low
            self.volume = data[symbol].volume
            self.current_date = data.time
