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
### Demonstration of how to define a universe using the fundamental data
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
### <meta name="tag" content="regression test" />
class FundamentalRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2014, 3, 25)
        self.SetEndDate(2014, 4, 7)

        self.UniverseSettings.Resolution = Resolution.Daily

        self.AddEquity("SPY")
        self.AddEquity("AAPL")

        # Request fundamental data for symbols at current algorithm time
        ibm = Symbol.Create("IBM", SecurityType.Equity, Market.USA)
        ibmFundamental = self.Fundamentals(ibm)
        if self.Time != self.StartDate or self.Time != ibmFundamental.EndTime:
            raise ValueError(f"Unexpected Fundamental time {ibmFundamental.EndTime}");
        if ibmFundamental.Price == 0:
            raise ValueError(f"Unexpected Fundamental IBM price!");
        nb = Symbol.Create("NB", SecurityType.Equity, Market.USA)
        fundamentals = self.Fundamentals([ nb, ibm ])
        if len(fundamentals) != 2:
            raise ValueError(f"Unexpected Fundamental count {len(fundamentals)}! Expected 2")

        # Request historical fundamental data for symbols
        history = self.History(Fundamental, TimeSpan(1, 0, 0, 0))
        if len(history) != 2:
            raise ValueError(f"Unexpected Fundamental history count {len(history)}! Expected 2")

        for ticker in [ "AAPL", "SPY" ]:
            data = history.loc[ticker]
            if data["value"][0] == 0:
                raise ValueError(f"Unexpected {data} fundamental data")

        # Request historical fundamental data for all symbols
        history2 = self.History(Fundamentals, TimeSpan(1, 0, 0, 0))
        if len(history2) != 1:
            raise ValueError(f"Unexpected Fundamentals history count {len(history2)}! Expected 1")
        data = history2["data"][0]
        if len(data) < 7000:
            raise ValueError(f"Unexpected Fundamentals data count {len(data)}! Expected > 7000")
        for fundamental in data:
            if type(fundamental) is not Fundamental:
                raise ValueError(f"Unexpected Fundamentals data type! {fundamental}")

        self.AddUniverse(self.SelectionFunction)

        self.changes = None
        self.numberOfSymbolsFundamental = 2

    # return a list of three fixed symbol objects
    def SelectionFunction(self, fundamental):
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted([x for x in fundamental if x.Price > 1],
            key=lambda x: x.DollarVolume, reverse=True)

        # sort descending by P/E ratio
        sortedByPeRatio = sorted(sortedByDollarVolume, key=lambda x: x.ValuationRatios.PERatio, reverse=True)

        # take the top entries from our sorted collection
        return [ x.Symbol for x in sortedByPeRatio[:self.numberOfSymbolsFundamental] ]

    def OnData(self, data):
        # if we have no changes, do nothing
        if self.changes is None: return

        # liquidate removed securities
        for security in self.changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)
                self.Debug("Liquidated Stock: " + str(security.Symbol.Value))

        # we want 50% allocation in each security in our universe
        for security in self.changes.AddedSecurities:
            self.SetHoldings(security.Symbol, 0.02)

        self.changes = None

    # this event fires whenever we have changes to our universe
    def OnSecuritiesChanged(self, changes):
        self.changes = changes
