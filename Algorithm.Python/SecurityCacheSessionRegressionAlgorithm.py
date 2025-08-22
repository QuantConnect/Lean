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
class SecurityCacheSessionRegressionAlgorithm(QCAlgorithm):
    
    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        
        self._equity = self.add_equity("SPY", Resolution.HOUR)
        self._symbol = self._equity.symbol
        self._open = self._close = self._high = self._volume = 0
        self._low = float('inf')
        self._current_date = self.start_date
        self._session_bar = None
        self._previous_session_bar = None

    def on_data(self, data):
        """OnData event is the primary entry point for your algorithm."""
        if self._current_date.date() == data.time.date():
            # Same trading day → update ongoing session
            if self._open == 0:
                self._open = data[self._symbol].open
            self._high = max(self._high, data[self._symbol].high)
            self._low = min(self._low, data[self._symbol].low)
            self._close = data[self._symbol].close
            self._volume += data[self._symbol].volume
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
                0
            )
            
            # This is the first data point of the new session
            self._open = data[self._symbol].open
            self._close = data[self._symbol].close
            self._high = data[self._symbol].high
            self._low = data[self._symbol].low
            self._volume = data[self._symbol].volume
            self._current_date = data.time
    
    def on_end_of_day(self, symbol):
        session = self._equity.session
        if session.is_trading_day_data_ready:
            if session.open != self._open or session.high != self._high or session.low != self._low or session.close != self._close or session.volume != self._volume:
                raise RegressionTestException("Mismatch in current session bar (OHLCV)")