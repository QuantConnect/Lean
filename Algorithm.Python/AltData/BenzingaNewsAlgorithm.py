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
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Data.Custom.Benzinga import *
from QuantConnect.Data.UniverseSelection import *

### <summary>
### Benzinga is a provider of news data. Their news is made in-house
### and covers stock related news such as corporate events.
### </summary>
class BenzingaNewsAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2018, 10, 12)
        self.SetEndDate(2018, 11, 25)
        self.SetCash(100000)

        self.UniverseSettings.Resolution = Resolution.Daily
        self.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelector))

    def CoarseSelector(self, coarse):
        # Add Benzinga news data from the filtered coarse selection
        symbols = [i.Symbol for i in coarse if i.HasFundamentalData and i.DollarVolume > 50000000][:10]

        for symbol in symbols:
            self.AddData(BenzingaNews, symbol)

        return symbols

    def OnData(self, data):
        # Get all Benzinga data and loop over it
        for article in data.Get(BenzingaNews).Values:
            selectedSymbol = None

            # Use loop instead of list comprehension for clarity purposes

            # Select the same Symbol we're getting a data point for
            # from the articles list so that we can get the sentiment of the article
            # We use the underlying Symbol because the Symbols included in the `Symbols` property
            # are equity Symbols.
            for x in article.Symbols:
                if x.Symbol == article.Symbol.Underlying:
                    selectedSymbol = x
                    break

            if selectedSymbol is None:
                raise Exception(f"Could not find current Symbol {article.Symbol.Underlying} even though it should exist")

            # Sometimes sentiment is not included with the article by Benzinga.
            # We have to check for null values before using it.
            sentimentScore = selectedSymbol = selectedSymbol.Sentiment
            if sentimentScore is None:
                continue

            # Set holdings equal to 1/10th of the sentiment score we get.
            # The sentimentScore value ranges from -1.0 to 1.0
            self.SetHoldings(article.Symbol.Underlying, sentimentScore / 10.0)

    def OnSecuritiesChanged(self, changes):
        for r in changes.RemovedSecurities:
            # If removed from the universe, liquidate and remove the custom data from the algorithm
            self.Liquidate(r.Symbol)
            self.RemoveSecurity(Symbol.CreateBase(BenzingaNews, r.Symbol, Market.USA))
