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

from clr import AddReference
AddReference("System.Core")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Data.Custom.Tiingo import *

### <summary>
### Example algorithm of a custom universe selection using coarse data and adding TiingoNews
### If conditions are met will add the underlying and trade it
### </summary>
class CoarseTiingoNewsUniverseSelectionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2014,3,24)
        self.SetEndDate(2014,4,7)
        
        self.UniverseSettings.FillForward = False;

        self.__numberOfSymbols = 3
        
        self.AddUniverse(CustomDataCoarseFundamentalUniverse(self.UniverseSettings, self.SecurityInitializer, self.CoarseSelectionFunction));

        self._symbols = []

    # sort the data by daily dollar volume and take the top 'NumberOfSymbols'
    def CoarseSelectionFunction(self, coarse):
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

        # return the symbol objects of the top entries from our sorted collection
        return [ Symbol.CreateBase(TiingoNews, x.Symbol, x.Symbol.ID.Market) for x in sortedByDollarVolume[:self.__numberOfSymbols] ]

    def OnData(self, data):
        articles = data.Get(TiingoNews)

        for kvp in articles:
            news = kvp.Value
            if "stocks drop" in news.Title.lower():
                if not self.Securities.ContainsKey(kvp.Key.Underlying):
                    # add underlying we want to trade
                    self.AddSecurity(kvp.Key.Underlying)
                    self._symbols.append(kvp.Key.Underlying)

        for symbol in self._symbols:
            if self.Securities[symbol].HasData:
                self.SetHoldings(symbol, 1.0 / len(self._symbols))

    def OnSecuritiesChanged(self,  changes):
        changes.FilterCustomSecurities = False
        self.Log(f"{self.Time} {changes}")

class CustomDataCoarseFundamentalUniverse(CoarseFundamentalUniverse):
    def GetSubscriptionRequests(self, security, currentTimeUtc, maximumEndTimeUtc, subscriptionService):
        us = self.UniverseSettings
        config = subscriptionService.Add(TiingoNews, security.Symbol, us.Resolution, us.FillForward, us.ExtendedMarketHours, True, False, False, us.DataNormalizationMode)
        return [ SubscriptionRequest(False, self, security, config, currentTimeUtc, maximumEndTimeUtc) ]
