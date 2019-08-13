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

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.Custom.Estimize import *

### <summary>
### Basic template algorithm simply initializes the date range and cash. This is a skeleton
### framework you can use for designing an algorithm.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />

class EstimizeUniverseSelection(QCAlgorithm):
    def Initialize(self):

        self.SetStartDate(2017, 1, 1)  #Set Start Date
        self.SetEndDate(2019, 5, 1)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        self.symbols = ["AAPL", "AMZN"]
        self.buyInTime = {}
        self.epsEstimate = {}

        for symbol in self.symbols:
            self.AddEquity(symbol, Resolution.Daily)
            self.AddData(EstimizeRelease, symbol + ".R")
            self.epsEstimate[symbol] = 0

        #self.AddData(EstimizeRelease, "AAPL.R")
        #self.AddData(EstimizeRelease, "AMZN.R")

        

    def OnData(self, data):
        for value in data.Values:
        #for key, value in zip(data.Keys, data.Values):
            if not isinstance(value, EstimizeRelease):
                continue
            
            ticker = value.Symbol.Value[:-2]
            
            if self.Portfolio[ticker].Invested:
                continue

            #self.epsEstimate[ticker] = value.WallStreetEpsEstimate
            self.epsEstimate[ticker] = value[8]
            
            self.Debug(f"{ticker} {self.Time} eps: {self.epsEstimate[ticker]}")
            #self.Debug(f"symbol: {value.Symbol.Value[:-2]}")
            #self.Debug(f"time: {self.Time}")
            #self.Debug(f"eps: {value.Eps}")
            #self.Debug(f"revenue: {value.Revenue}")
            #self.Debug(f"wallstreet_eps_estimate: {value.WallStreetEpsEstimate}")
            #self.Debug(f"wallstreet_revenue_estimate: {value.WallStreetRevenueEstimate}")
            #self.Debug(f"consensus_eps_estimate: {value.ConsensusEpsEstimate}")
            #self.Debug(f"consensus_revenue_estimate: {value.ConsensusRevenueEstimate}")
            #self.Debug(f"consensus_weighted_eps_estimate: {value.ConsensusWeightedEpsEstimate}")
            #self.Debug(f"consensus_weighted_revenue_estimate: {value.ConsensusWeightedRevenueEstimate}")

        for symbol in self.symbols:
            if self.Portfolio[symbol].Invested:
                if (self.Time - self.buyInTime[symbol]).days > 30:
                    self.Debug(f"{symbol} Sell time: {self.Time}")
                    self.Liquidate(symbol)

            else: 
                if self.epsEstimate[symbol] > 2:
                    self.Debug(f"{symbol} Buy time: {self.Time}")
                    self.SetHoldings(symbol, 1/len(self.symbols))
                    self.buyInTime[symbol] = self.Time
                    
                    # reset esp to get ready for next release
                    self.epsEstimate[symbol] = 0