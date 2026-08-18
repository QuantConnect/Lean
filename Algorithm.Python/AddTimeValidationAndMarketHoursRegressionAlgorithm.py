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
### Regression algorithm asserting that adding a security with an unknown ticker/market combination fails fast
### at add time naming the markets that do have the ticker, and that market_hours() works without a subscription
### </summary>
class AddTimeValidationAndMarketHoursRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)

        self.add_equity("SPY", Resolution.MINUTE)

        # BNBUSD is not a coinbase pair: the add must fail naming the markets that do have it
        self.assert_throws(lambda: self.add_crypto("BNBUSD", market=Market.COINBASE),
            ["Crypto 'BNBUSD' symbol could not be found in the database", "Markets with a 'BNBUSD' Crypto entry:", Market.KRAKEN])

        # oanda has no crypto entries at all: the exchange hours failure must also name the valid markets
        self.assert_throws(lambda: self.add_crypto("BTCUSD", market=Market.OANDA),
            ["Unable to locate exchange hours for Crypto-oanda-BTCUSD", "Markets with a 'BTCUSD' Crypto entry:", Market.COINBASE])

        # exchange hours lookup must not require a subscription
        hours = self.market_hours("IBM")
        if str(hours.time_zone) != "America/New_York":
            raise AssertionError(f"Unexpected time zone for IBM market hours: {hours.time_zone}")

        crypto_hours = self.market_hours(Symbol.create("BTCUSD", SecurityType.CRYPTO, Market.COINBASE))
        if not crypto_hours.is_market_always_open:
            raise AssertionError("Expected coinbase BTCUSD market to be always open")

        # and the lookups must not have added any securities
        if any(symbol.value in ("IBM", "BTCUSD", "BNBUSD") for symbol in self.securities.keys()):
            raise AssertionError("No security should have been added by the market hours lookups or the failed adds")

    def assert_throws(self, add_security, expected_message_parts):
        try:
            add_security()
        except Exception as exception:
            message = str(exception)
            for expected_message_part in expected_message_parts:
                if expected_message_part not in message:
                    raise AssertionError(f"Expected message to contain '{expected_message_part}' but was: {message}")
            return

        raise AssertionError("Expected an exception to be thrown at add time")
