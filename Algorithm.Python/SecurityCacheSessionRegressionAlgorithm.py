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

class SecurityCacheSessionRegressionAlgorithm(QCAlgorithm):
    """Regression algorithm to validate SecurityCache.Session functionality.
    Verifies that daily session bars (Open, High, Low, Close, Volume) are correctly
    built from intraday data and match expected values.
    """
    
    def Initialize(self):
        """Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm."""
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        
        self._equity = self.AddEquity("SPY", Resolution.Hour)
        self._symbol = self._equity.Symbol
        self._open = self._close = self._high = self._volume = 0
        self._low = float('inf')
        self._current_date = self.StartDate
        self._session_bar = None
        self._previous_session_bar = None
        
        self.Schedule.On(self.DateRules.EveryDay(), 
                        self.TimeRules.AfterMarketOpen(self._symbol, 61), 
                        self.ValidateSessionBars)

    def ValidateSessionBars(self):
        """Validate session bar values"""
        session = self._equity.Cache.Session
        
        # Check current session values
        if session.IsTradingDayDataReady:
            if (self._session_bar is None or 
                self._session_bar.Open != session.Open or 
                self._session_bar.High != session.High or 
                self._session_bar.Low != session.Low or 
                self._session_bar.Close != session.Close or 
                self._session_bar.Volume != session.Volume):
                raise RegressionTestException("Mismatch in current session bar (OHLCV)")
        
        # Check previous session values
        if self._previous_session_bar is not None:
            if (self._previous_session_bar.Open != session[1].Open or 
                self._previous_session_bar.High != session[1].High or 
                self._previous_session_bar.Low != session[1].Low or 
                self._previous_session_bar.Close != session[1].Close or 
                self._previous_session_bar.Volume != session[1].Volume):
                raise RegressionTestException("Mismatch in previous session bar (OHLCV)")

    def OnData(self, data):
        """OnData event is the primary entry point for your algorithm."""
        if self._current_date.date() == data.Time.date():
            # Same trading day → update ongoing session
            if self._open == 0:
                self._open = data[self._symbol].Open
            self._high = max(self._high, data[self._symbol].High)
            self._low = min(self._low, data[self._symbol].Low)
            self._close = data[self._symbol].Close
            self._volume += data[self._symbol].Volume
        else:
            # New trading day
            self._current_date = data.Time
            
            # Save previous session bar
            self._previous_session_bar = self._session_bar
            
            # Create new session bar
            self._session_bar = TradeBar(DateTime.MinValue, self._symbol, 
                                        self._open, self._high, self._low, self._close, 
                                        self._volume, timedelta(1))
            
            # Reset for new session
            self._open = data[self._symbol].Open
            self._close = data[self._symbol].Close
            self._high = data[self._symbol].High
            self._low = data[self._symbol].Low
            self._volume = data[self._symbol].Volume