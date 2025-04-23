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
### This algorithm sends a list of portfolio targets to Collective2 API every time the ema indicators crosses between themselves.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class Collective2SignalExportDemonstrationAlgorithm(QCAlgorithm):

    def initialize(self):
        ''' Initialize the date and add all equity symbols present in list _symbols '''

        self.set_start_date(2013, 10, 7)   #Set Start Date
        self.set_end_date(2013, 10, 11)    #Set End Date
        self.set_cash(100000)             #Set Strategy Cash

        # Symbols accepted by Collective2. Collective2 accepts stock, future, forex and US stock option symbols
        self.add_equity("GOOG")
        self._symbols = [
            Symbol.create("SPY", SecurityType.EQUITY, Market.USA),
            Symbol.create("EURUSD", SecurityType.FOREX, Market.OANDA),
            Symbol.create_future("ES", Market.CME, datetime(2023, 12, 15)),
            Symbol.create_option("GOOG", Market.USA, OptionStyle.AMERICAN, OptionRight.CALL, 130, datetime(2023, 9, 1))
        ]
        self.targets = []

        # Create a new PortfolioTarget for each symbol, assign it an initial amount of 0.05 and save it in self.targets list
        for item in self._symbols:
            symbol = self.add_security(item).symbol
            if symbol.security_type == SecurityType.EQUITY or symbol.security_type == SecurityType.FOREX:
                self.targets.append(PortfolioTarget(symbol, 0.05))
            else:
                self.targets.append(PortfolioTarget(symbol, 1))

        self.fast = self.ema("SPY", 10)
        self.slow = self.ema("SPY", 100)

        # Initialize these flags, to check when the ema indicators crosses between themselves
        self.ema_fast_is_not_set = True
        self.ema_fast_was_above = False

        # Set Collective2 export provider
        # Collective2 APIv4 KEY: This value is provided by Collective2 in your account section (See https://collective2.com/account-info)
        # See API documentation at https://trade.collective2.com/c2-api
        self.collective2_apikey = "YOUR APIV4 KEY"

        # Collective2 System ID: This value is found beside the system's name (strategy's name) on the main system page
        self.collective2_system_id = 0

        # Disable automatic exports as we manually set them
        self.signal_export.automatic_export_time_span = None

        # If using the Collective2 white-label API, you can specify it in the constructor with the optional parameter `use_white_label_api`:
        # e.g. Collective2SignalExport(self.collective2_apikey, self.collective2_system_id, use_white_label_api=True)
        # The API url can also be overridden by setting the Destination property:
        # e.g. Collective2SignalExport(self.collective2_apikey, self.collective2_system_id) { Destination = new Uri("your url") }
        self.signal_export.add_signal_export_provider(Collective2SignalExport(self.collective2_apikey, self.collective2_system_id))

        self.first_call = True

        self.set_warm_up(100)

    def on_data(self, data):
        ''' Reduce the quantity of holdings for one security and increase the holdings to the another
        one when the EMA's indicators crosses between themselves, then send a signal to Collective2 API '''
        if self.is_warming_up: return

        # Place an order as soon as possible to send a signal.
        if self.first_call:
            self.set_holdings("SPY", 0.1)
            self.targets[0] = PortfolioTarget(self.portfolio["SPY"].symbol, 0.1)
            self.signal_export.set_target_portfolio(self.targets)
            self.first_call = False

        fast = self.fast.current.value
        slow = self.slow.current.value

        # Set the value of flag _ema_fast_was_above, to know when the ema indicators crosses between themselves
        if self.ema_fast_is_not_set == True:
            if fast > slow *1.001:
                self.ema_fast_was_above = True
            else:
                self.ema_fast_was_above = False
            self.ema_fast_is_not_set = False

        # Check whether ema fast and ema slow crosses. If they do, set holdings to SPY
        # or reduce its holdings, change its value in self.targets list and send signals
        #  to Collective2 API from self.targets
        if fast > slow * 1.001 and (not self.ema_fast_was_above):
            self.set_holdings("SPY", 0.1)
            self.targets[0] = PortfolioTarget(self.portfolio["SPY"].symbol, 0.1)
            self.signal_export.set_target_portfolio(self.targets)
        elif fast < slow * 0.999 and (self.ema_fast_was_above):
            self.set_holdings("SPY", 0.01)
            self.targets[0] = PortfolioTarget(self.portfolio["SPY"].symbol, 0.01)
            self.signal_export.set_target_portfolio(self.targets)
