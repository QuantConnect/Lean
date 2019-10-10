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

class BenzingaNewsAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.words = {
            "bad": -0.5, "good": 0.5,
            "negative": -0.5, "great": 0.5,
            "growth": 0.5, "fail": -0.5,
            "failed": -0.5, "success": 0.5,
            "nailed": 0.5, "beat": 0.5,
            "missed": -0.5, "slipped": -0.5,
            "outperforming": 0.5, "underperforming": -0.5,
            "outperform": 0.5, "underperform": -0.5
        }

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
        for article in data.Get(BenzingaNews).Values:
            # Split the article into words in all lowercase
            articleWords = article.Contents.lower().split(" ")

            # Get the list of matching words we have sentiment definitions for
            intersection = set(self.words.keys()).intersection(articleWords)

            # Get the sentiment score
            sentimentScore = sum([self.words[i] for i in intersection])

            # Set holdings equal to 1/10th of the sentiment score we get
            self.SetHoldings(article.Symbol.Underlying, sentimentScore / 10.0)

    def OnSecuritiesChanged(self, changes):
        for r in [i for i in changes.RemovedSecurities if i.Symbol.SecurityType == SecurityType.Equity]:
            # If removed from the universe, liquidate and remove the custom data from the algorithm
            self.Liquidate(r.Symbol)
            self.RemoveSecurity(Symbol.CreateBase(BenzingaNews, r.Symbol, Market.USA))
