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
### This regression algorithm tests that we can use selectors in the indicators
### that need quote data
### </summary>
class IndicatorSelectorWorksWithDifferentOptions(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 6, 7)
        self.set_end_date(2013, 11, 8)
        
        self.equity = self.add_equity("SPY", Resolution.MINUTE).symbol
        self.option = self.add_option("NWSA", Resolution.MINUTE).symbol
        self.option = Symbol.create_option("NWSA", Market.USA, OptionStyle.AMERICAN, OptionRight.PUT, 33, datetime(2013, 7, 20))
        self.add_option_contract(self.option, Resolution.MINUTE)
        
        self.option_indicator = self.identity(self.option, Resolution.MINUTE, Field.VOLUME, "Volume.")

        self.bid_close_indicator = self.identity(self.equity, Resolution.MINUTE, Field.BID_CLOSE, "Bid.Close.")
        self.bid_open_indicator = self.identity(self.equity, Resolution.MINUTE, Field.BID_OPEN, "Bid.Open.")
        self.bid_low_indicator = self.identity(self.equity, Resolution.MINUTE, Field.BID_LOW, "Bid.Low.")
        self.bid_high_indicator = self.identity(self.equity, Resolution.MINUTE, Field.BID_HIGH, "Bid.High.")
        
        self.ask_close_indicator = self.identity(self.equity, Resolution.MINUTE, Field.ASK_CLOSE, "Ask.Close.")
        self.ask_open_indicator = self.identity(self.equity, Resolution.MINUTE, Field.ASK_OPEN, "Ask.Open.")
        self.ask_low_indicator = self.identity(self.equity, Resolution.MINUTE, Field.ASK_LOW, "Ask.Low.")
        self.ask_high_indicator = self.identity(self.equity, Resolution.MINUTE, Field.ASK_HIGH, "Ask.High.")

        self.quotebars_found = False
        self.tradebars_found = False

    def on_data(self, slice):
        if self.equity in slice.quote_bars.keys():
            self.quotebars_found = True
            if slice.quote_bars["SPY"].bid.close != self.bid_close_indicator.current.value:
                close_value = slice.quote_bars["SPY"].bid.close
                raise Exception(f"{self.bid_close_indicator.__name__} should have been {close_value}, but was {self.bid_close_indicator.current.value}")

            if slice.quote_bars["SPY"].bid.open != self.bid_open_indicator.current.value:
                open_value = slice.quote_bars["SPY"].bid.open
                raise Exception(f"{self.bid_open_indicator.__name__} should have been {open_value}, but was {self.bid_open_indicator.current.value}")

            if slice.quote_bars["SPY"].bid.low != self.bid_low_indicator.current.value:
                low_value = slice.quote_bars["SPY"].bid.low
                raise Exception(f"{self.bid_low_indicator.__name__} should have been {low_value}, but was {self.bid_low_indicator.current.value}")

            if slice.quote_bars["SPY"].bid.high != self.bid_high_indicator.current.value:
                high_value = slice.quote_bars["SPY"].bid.high
                raise Exception(f"{self.bid_high_indicator.__name__} should have been {high_value}, but was {self.bid_high_indicator.current.value}")

            if slice.quote_bars["SPY"].ask.close != self.ask_close_indicator.current.value:
                close_value = slice.quote_bars["SPY"].ask.close
                raise Exception(f"{self.ask_close_indicator.__name__} should have been {close_value}, but was {self.ask_close_indicator.current.value}")

            if slice.quote_bars["SPY"].ask.open != self.ask_open_indicator.current.value:
                open_value = slice.quote_bars["SPY"].bid.open
                raise Exception(f"{self.ask_open_indicator.__name__} should have been {open_value}, but was {self.ask_open_indicator.current.value}")

            if slice.quote_bars["SPY"].ask.low != self.ask_low_indicator.current.value:
                low_value = slice.quote_bars["SPY"].bid.low
                raise Exception(f"{self.ask_low_indicator.__name__} should have been {low_value}, but was {self.ask_low_indicator.current.value}")

            if slice.quote_bars["SPY"].ask.high != self.ask_high_indicator.current.value:
                high_value = slice.quote_bars["SPY"].bid.high
                raise Exception(f"{self.ask_high_indicator.__name__} should have been {high_value}, but was {self.ask_high_indicator.current.value}")
        
        if (self.option.canonical in slice.option_chains.keys()) and (self.option in slice.option_chains[self.option.canonical].trade_bars.keys()):
            self.tradebars_found = True
            if self.option_indicator.current.value != slice.option_chains[self.option.canonical].trade_bars[self.option].volume:
                volume = slice.option_chains[self.option.canonical].trade_bars[self.option].volume
                raise Exception(f"{self.option_indicator.__name__} should have been {volume}, but was {self.option_indicator.current.value}")
            
    def on_end_of_algorithm(self):
        if not self.quotebars_found:
            raise Exception("At least one quote bar should have been found, but none was found")
        
        if not self.tradebars_found:
            raise Exception("At least one trade bar should have been found, but none was found")
