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

    def Initialize(self):
        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)
        self.SetCash(100000)

        self.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage)

        # Some brokerages like InteractiveBrokers make a difference on CFDs depending on the underlying (equity, index, metal, forex).
        # Depending on this, the market hours can be different. In order to be more specific with the market hours,
        # we can set the MarketHoursDatabase entry for the CFDs.

        # Equity CFDs are usually traded the same hours as the equity market.
        equityMarketHoursEntry = self.MarketHoursDatabase.GetEntry(Market.USA, None, SecurityType.Equity)
        self.MarketHoursDatabase.SetEntry(Market.InteractiveBrokers, "", SecurityType.Cfd, equityMarketHoursEntry.ExchangeHours, equityMarketHoursEntry.DataTimeZone)

        # The same can be done for the symbol properties, in case they are different depending on the underlying
        equitySymbolProperties = self.SymbolPropertiesDatabase.GetSymbolProperties(Market.USA, None, SecurityType.Equity, Currencies.USD)
        self.SymbolPropertiesDatabase.SetEntry(Market.InteractiveBrokers, None, SecurityType.Cfd, equitySymbolProperties)

        spyCfd = self.AddCfd("SPY", market=Market.InteractiveBrokers)

        if json.JsonConvert.SerializeObject(spyCfd.Exchange.Hours) != json.JsonConvert.SerializeObject(equityMarketHoursEntry.ExchangeHours):
            raise Exception("Expected the SPY CFD market hours to be the same as the underlying equity market hours.")

        if json.JsonConvert.SerializeObject(spyCfd.SymbolProperties) != json.JsonConvert.SerializeObject(equitySymbolProperties):
            raise Exception("Expected the SPY CFD symbol properties to be the same as the underlying equity symbol properties.")

        # We can also do it for a specific ticker
        audUsdForexMarketHoursEntry = self.MarketHoursDatabase.GetEntry(Market.Oanda, None, SecurityType.Forex)
        self.MarketHoursDatabase.SetEntry(Market.InteractiveBrokers, "AUDUSD", SecurityType.Cfd, audUsdForexMarketHoursEntry.ExchangeHours, audUsdForexMarketHoursEntry.DataTimeZone)

        audUsdForexSymbolProperties = self.SymbolPropertiesDatabase.GetSymbolProperties(Market.Oanda, "AUDUSD", SecurityType.Forex, Currencies.USD)
        self.SymbolPropertiesDatabase.SetEntry(Market.InteractiveBrokers, "AUDUSD", SecurityType.Cfd, audUsdForexSymbolProperties)

        audUsdCfd = self.AddCfd("AUDUSD", market=Market.InteractiveBrokers)

        if json.JsonConvert.SerializeObject(audUsdCfd.Exchange.Hours) != json.JsonConvert.SerializeObject(audUsdForexMarketHoursEntry.ExchangeHours):
            raise Exception("Expected the AUDUSD CFD market hours to be the same as the underlying forex market hours.")

        if json.JsonConvert.SerializeObject(audUsdCfd.SymbolProperties) != json.JsonConvert.SerializeObject(audUsdForexSymbolProperties):
            raise Exception("Expected the AUDUSD CFD symbol properties to be the same as the underlying forex symbol properties.")
