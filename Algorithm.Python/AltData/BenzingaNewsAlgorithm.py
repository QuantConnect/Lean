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
from QuantConnect.Data.Custom.Benzinga import *

from datetime import datetime, timedelta

### <summary>
### Benzinga is a provider of news data. Their news is made in-house
### and covers stock related news such as corporate events.
### </summary>
class BenzingaNewsAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.words = {
            "bad": -0.5, "good": 0.5,
            "negative": -0.5, "great": 0.5,
            "growth": 0.5, "fail": -0.5,
            "failed": -0.5, "success": 0.5,
            "nailed": 0.5, "beat": 0.5,
            "missed": -0.5
        }

        self.lastTrade = datetime(1, 1, 1)

        self.SetStartDate(2018, 6, 5)
        self.SetEndDate(2018, 8, 4)
        self.SetCash(100000)

        aapl = self.AddEquity("AAPL", Resolution.Hour).Symbol
        ibm = self.AddEquity("IBM", Resolution.Hour).Symbol

        self.AddData(BenzingaNews, aapl)
        self.AddData(BenzingaNews, ibm)

    def OnData(self, data):
        if (self.Time - self.lastTrade) < timedelta(days=5):
            return

        # Get rid of our holdings after 5 days, and start fresh
        self.Liquidate()

        # Get all Benzinga data and loop over it
        for article in data.Get(BenzingaNews).Values:
            selectedSymbol = None

            # Use loop instead of list comprehension for clarity purposes

            # Select the same Symbol we're getting a data point for
            # from the articles list so that we can get the sentiment of the article
            # We use the underlying Symbol because the Symbols included in the `Symbols` property
            # are equity Symbols.
            for symbol in article.Symbols:
                if symbol == article.Symbol.Underlying:
                    selectedSymbol = symbol
                    break

            if selectedSymbol is None:
                raise Exception(f"Could not find current Symbol {article.Symbol.Underlying} even though it should exist")

            # The intersection of the article contents and the pre-defined words are the words that are included in both collections
            intersection = set(article.Contents.lower().split(" ")).intersection(list(self.words.keys()))
            # Get the words, then get the aggregate sentiment
            sentimentSum = sum([self.words[i] for i in intersection])

            if sentimentSum >= 0.5:
                self.Log(f"Longing {article.Symbol.Underlying} with sentiment score of {sentimentSum}")
                self.SetHoldings(article.Symbol.Underlying, sentimentSum / 5)

                self.lastTrade = self.Time

            if sentimentSum <= -0.5:
                self.Log(f"Shorting {article.Symbol.Underlying} with sentiment score of {sentimentSum}")
                self.SetHoldings(article.Symbol.Underlying, sentimentSum / 5)

                self.lastTrade = self.Time

    def OnSecuritiesChanged(self, changes):
        for r in changes.RemovedSecurities:
            # If removed from the universe, liquidate and remove the custom data from the algorithm
            self.Liquidate(r.Symbol)
            self.RemoveSecurity(Symbol.CreateBase(BenzingaNews, r.Symbol, Market.USA))
