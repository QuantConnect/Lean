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

import Newtonsoft.Json as json

### <summary>
### Algorithm illustrating how to manually set market hours and symbol properties database entries to be picked up by the algorithm's securities.
### This specific case illustrates how to do it for CFDs to match InteractiveBrokers brokerage, which has different market hours
### depending on the CFD underlying asset.
### </summary>
class ManuallySetMarketHoursAndSymbolPropertiesDatabaseEntriesAlgorithm(QCAlgorithm):
    '''
    Algorithm illustrating how to manually set market hours and symbol properties database entries to be picked up by the algorithm's securities.
    This specific case illustrates how to do it for CFDs to match InteractiveBrokers brokerage, which has different market hours
    depending on the CFD underlying asset.
    '''

    def initialize(self):
        self.set_start_date(2013,10, 7)
        self.set_end_date(2013,10,11)
        self.set_cash(100000)

        self.set_brokerage_model(BrokerageName.INTERACTIVE_BROKERS_BROKERAGE)

        # Some brokerages like InteractiveBrokers make a difference on CFDs depending on the underlying (equity, index, metal, forex).
        # Depending on this, the market hours can be different. In order to be more specific with the market hours,
        # we can set the MarketHoursDatabase entry for the CFDs.

        # Equity CFDs are usually traded the same hours as the equity market.
        equity_market_hours_entry = self.market_hours_database.get_entry(Market.USA, None, SecurityType.EQUITY)
        self.market_hours_database.set_entry(Market.INTERACTIVE_BROKERS, "", SecurityType.CFD, equity_market_hours_entry.exchange_hours, equity_market_hours_entry.data_time_zone)

        # The same can be done for the symbol properties, in case they are different depending on the underlying
        equity_symbol_properties = self.symbol_properties_database.get_symbol_properties(Market.USA, None, SecurityType.EQUITY, Currencies.USD)
        self.symbol_properties_database.set_entry(Market.INTERACTIVE_BROKERS, None, SecurityType.CFD, equity_symbol_properties)

        spy_cfd = self.add_cfd("SPY", market=Market.INTERACTIVE_BROKERS)

        if json.JsonConvert.serialize_object(spy_cfd.exchange.hours) != json.JsonConvert.serialize_object(equity_market_hours_entry.exchange_hours):
            raise AssertionError("Expected the SPY CFD market hours to be the same as the underlying equity market hours.")

        if json.JsonConvert.serialize_object(spy_cfd.symbol_properties) != json.JsonConvert.serialize_object(equity_symbol_properties):
            raise AssertionError("Expected the SPY CFD symbol properties to be the same as the underlying equity symbol properties.")

        # We can also do it for a specific ticker
        aud_usd_forex_market_hours_entry = self.market_hours_database.get_entry(Market.OANDA, None, SecurityType.FOREX)
        self.market_hours_database.set_entry(Market.INTERACTIVE_BROKERS, "AUDUSD", SecurityType.CFD, aud_usd_forex_market_hours_entry.exchange_hours, aud_usd_forex_market_hours_entry.data_time_zone)

        aud_usd_forex_symbol_properties = self.symbol_properties_database.get_symbol_properties(Market.OANDA, "AUDUSD", SecurityType.FOREX, Currencies.USD)
        self.symbol_properties_database.set_entry(Market.INTERACTIVE_BROKERS, "AUDUSD", SecurityType.CFD, aud_usd_forex_symbol_properties)

        aud_usd_cfd = self.add_cfd("AUDUSD", market=Market.INTERACTIVE_BROKERS)

        if json.JsonConvert.serialize_object(aud_usd_cfd.exchange.hours) != json.JsonConvert.serialize_object(aud_usd_forex_market_hours_entry.exchange_hours):
            raise AssertionError("Expected the AUDUSD CFD market hours to be the same as the underlying forex market hours.")

        if json.JsonConvert.serialize_object(aud_usd_cfd.symbol_properties) != json.JsonConvert.serialize_object(aud_usd_forex_symbol_properties):
            raise AssertionError("Expected the AUDUSD CFD symbol properties to be the same as the underlying forex symbol properties.")
