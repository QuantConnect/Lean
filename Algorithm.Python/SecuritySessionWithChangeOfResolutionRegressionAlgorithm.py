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
### Regression algorithm to validate SecurityCache.Session functionality.
### Verifies that daily session bars (Open, High, Low, Close, Volume) are correctly after resolution change
### </summary>
class SecuritySessionWithChangeOfResolutionRegressionAlgorithm(SecuritySessionRegressionAlgorithm):
    def on_securities_changed(self, changes: SecurityChanges):
        if changes.removed_securities:
            self.security = self.add_equity("SPY", Resolution.MINUTE)

    def on_end_of_day(self, symbol: Symbol):
        if self.utc_time.date() == datetime(2013, 10, 7).date():
            session = self.security.session

            # Check before removal
            if (
                session.open != self.open
                or session.high != self.high
                or session.low != self.low
                or session.close != self.close
                or session.volume != self.volume
            ):
                raise RegressionTestException("Mismatch in current session bar (OHLCV)")

            self.remove_security(symbol)
            self.security_was_removed = True