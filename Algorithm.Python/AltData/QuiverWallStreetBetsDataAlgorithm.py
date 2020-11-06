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

from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Custom.Quiver import *
from QuantConnect.Data.UniverseSelection import *
from QuantConnect import *


class QuiverWallStreetBetsDataAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2019, 1, 1)
        self.SetEndDate(2020, 6, 1)
        self.SetCash(100000)

        aapl = self.AddEquity("AAPL", Resolution.Daily).Symbol
        quiverWSBSymbol = self.AddData(QuiverWallStreetBets, aapl).Symbol
        history = self.History(QuiverWallStreetBets, quiverWSBSymbol, 60, Resolution.Daily)

        self.Debug(f"We got {len(history)} items from our history request");

    def OnData(self, data):
        points = data.Get(QuiverWallStreetBets)
        for point in points.Values:
            # Go long in the stock if it was mentioned more than 5 times in the WallStreetBets daily discussion
            if point.Mentions > 5:
                self.SetHoldings(point.Symbol.Underlying, 1)

            # Go short in the stock if it was mentioned less than 5 times in the WallStreetBets daily discussion
            if point.Mentions < 5:
                self.SetHoldings(point.Symbol.Underlying, -1)

    def OnSecuritiesChanged(self, changes):
        for r in changes.RemovedSecurities:
            # If removed from the universe, liquidate and remove the custom data from the algorithm
            self.Liquidate(r.Symbol)
            self.RemoveSecurity(Symbol.CreateBase(QuiverWikipedia, r.Symbol, Market.USA))
