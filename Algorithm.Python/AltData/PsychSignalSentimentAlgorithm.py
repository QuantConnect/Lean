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
AddReference("QuantConnect.Common")

from datetime import datetime, timedelta

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Data import *
from QuantConnect.Data.Custom.PsychSignal import *
from QuantConnect.Data.UniverseSelection import *

### <summary>
### Momentum based strategy that follows bullish rated stocks
### </summary>
class PsychSignalSentimentAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2018, 3, 1)
        self.SetEndDate(2018, 10, 1)
        self.SetCash(100000)
        self.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseUniverse))
        self.timeEntered = datetime(1, 1, 1)

        # Request underlying equity data.
        ibm = self.AddEquity("IBM", Resolution.Minute).Symbol
        # Add sentiment data for the underlying IBM asset
        psy = self.AddData(PsychSignalSentiment, ibm).Symbol
        # Request 120 minutes of history with the PsychSignal IBM Custom Data Symbol
        history = self.History(PsychSignalSentiment, psy, 120, Resolution.Minute)

        # Count the number of items we get from our history request
        self.Debug(f"We got {len(history)} items from our history request")

    # You can use custom data with a universe of assets.
    def CoarseUniverse(self, coarse):
        if (self.Time - self.timeEntered) <= timedelta(days=10):
            return Universe.Unchanged

        # Ask for the universe like normal and then filter it
        symbols = [i.Symbol for i in coarse if i.HasFundamentalData and i.DollarVolume > 50000000][:20]

        # Add the custom data to the underlying security.
        for symbol in symbols:
            self.AddData(PsychSignalSentiment, symbol)

        return symbols

    def OnData(self, data):
        # Scan our last time traded to prevent churn.
        if (self.Time - self.timeEntered) <= timedelta(days=10):
            return

        # Fetch the PsychSignal data for the active securities and trade on any
        for security in self.ActiveSecurities.Values:
            tweets = security.Data.GetAll(PsychSignalSentiment)
            for sentiment in tweets:
                if sentiment.BullIntensity > 2.0 and sentiment.BullScoredMessages > 3:
                    self.SetHoldings(sentiment.Symbol.Underlying, 0.05)
                    self.timeEntered = self.Time

    # When adding custom data from a universe we should also remove the data afterwards.
    def OnSecuritiesChanged(self, changes):
        # Make sure to filter out other security removals (i.e. custom data)
        for r in [i for i in changes.RemovedSecurities if i.Symbol.SecurityType == SecurityType.Equity]:
            self.Liquidate(r.Symbol)
            # Remove the custom data from our algorithm and collection
            self.RemoveSecurity(Symbol.CreateBase(PsychSignalSentiment, r.Symbol, Market.USA))
