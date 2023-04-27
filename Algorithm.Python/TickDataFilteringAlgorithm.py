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

# <summary>
# Demonstration of filtering tick data so easier to use. Tick data has lots of glitchy, spikey data which should be filtered out before usagee.
# </summary>
# <meta name="tag" content="filtering" />
# <meta name="tag" content="tick data" />
# <meta name="tag" content="using data" />
# <meta name="tag" content="ticks event" />
class TickDataFilteringAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(25000)
        self.AddSecurity(SecurityType.Equity, "SPY", Resolution.Tick)
        self.Securities["SPY"].DataFilter = ExchangeDataFilter(self)

    # <summary>
    # Data arriving here will now be filtered.
    # </summary>
    # <param name="data">Ticks data array</param>
    def OnData(self, data):
        if self.data.ContainsKey("SPY"): 
            return
        
        self.spyTickList = self.data["SPY"]

        # Ticks return a list of ticks this second
        for tick in self.spyTickList:
            self.Log(tick.Exchange)

        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

# <summary>
# Exchange filter class
# </summary>
class ExchangeDataFilter(SecurityDataFilter):

    # <summary>
    # Save instance of the algorithm namespace
    # </summary>
    # <param name="algo"></param>
    def ExchangeDataFilter(self, algo):
        self.algo = algo

    # <summary>
    # Global Market Short Codes and their full versions: (used in tick objects)
    # https://github.com/QuantConnect/QCAlgorithm/blob/master/QuantConnect.Common/Global.cs
    # </summary>
    class MarketCodesFilter: 
        # US Market Codes
        US = {"A": "American Stock Exchange",
            "B": "Boston Stock Exchange",
            "C": "National Stock Exchange",
            "D": "FINRA ADF",
            "I": "International Securities Exchange",
            "J": "Direct Edge A",
            "K": "Direct Edge X",
            "M": "Chicago Stock Exchange",
            "N": "New York Stock Exchange",
            "P": "Nyse Arca Exchange",
            "Q": "NASDAQ OMX",
            "T": "NASDAQ OMX",
            "U": "OTC Bulletin Board",
            "u": "Over-the-Counter trade in Non-NASDAQ issue",
            "W": "Chicago Board Options Exchange",
            "X": "Philadelphia Stock Exchange",
            "Y": "BATS Y-Exchange, Inc",
            "Z": "BATS Exchange, Inc"}

        # Canada Market Short Codes:
        Canada =  {"T": "Toronto", "V": "Venture"}
            

        # <summary>
        # Select allowed exchanges for this filter: e.g. top 4
        # </summary>
        AllowedExchanges = ["P"]     
        # NYSE ARCA - SPY PRIMARY EXCHANGE
        # https://www.google.com/finance?q=NYSEARCA%3ASPY&ei=XcA2VKCSLs228waMhYCIBg



    # <summary>
    # Filter out a tick from this vehicle, with this new data:
    # </summary>
    # <param name="data">New data packet:</param>
    # <param name="asset">Vehicle of this filter.</param>
    def Filter(self, asset, data):
        # TRUE -->  Accept Tick
        # FALSE --> Reject Tick
        self.tick = data

        if isinstance(data, Tick):
            if self.tick.Exchange == "P":
                return True
        
        return False

