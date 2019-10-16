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
from QuantConnect.Data.Custom.SEC import *
from QuantConnect.Data.UniverseSelection import *

class SECReport8KAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2019, 1, 1)
        self.SetEndDate(2019, 8, 21)
        self.SetCash(100000)

        self.UniverseSettings.Resolution = Resolution.Minute
        self.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelector))

        # Request underlying equity data.
        ibm = self.AddEquity("IBM", Resolution.Minute).Symbol
        # Add news data for the underlying IBM asset
        earningsFiling = self.AddData(SECReport10Q, ibm).Symbol
        # Request 120 days of history with the SECReport10Q IBM custom data Symbol
        history = self.History(SECReport10Q, earningsFiling, 120, Resolution.Daily)

        # Count the number of items we get from our history request
        self.Debug(f"We got {len(history)} items from our history request")

    def CoarseSelector(self, coarse):
        # Add SEC data from the filtered coarse selection
        symbols = [i.Symbol for i in coarse if i.HasFundamentalData and i.DollarVolume > 50000000][:10]

        for symbol in symbols:
            self.AddData(SECReport8K, symbol)

        return symbols

    def OnData(self, data):
        # Store the symbols we want to long in a list
        # so that we can have an equal-weighted portfolio
        longEquitySymbols = []

        # Get all SEC data and loop over it
        for report in data.Get(SECReport8K).Values:
            # Get the length of all contents contained within the report
            reportTextLength = sum([len(i.Text) for i in report.Report.Documents])

            if reportTextLength > 20000:
                longEquitySymbols.append(report.Symbol.Underlying)

        for equitySymbol in longEquitySymbols:
            self.SetHoldings(equitySymbol, 1.0 / len(longEquitySymbols))

    def OnSecuritiesChanged(self, changes):
        for r in [i for i in changes.RemovedSecurities if i.Symbol.SecurityType == SecurityType.Equity]:
            # If removed from the universe, liquidate and remove the custom data from the algorithm
            self.Liquidate(r.Symbol)
            self.RemoveSecurity(Symbol.CreateBase(SECReport8K, r.Symbol, Market.USA))
