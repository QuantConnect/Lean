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
from math import ceil
from itertools import groupby

### <summary>
### Demonstration of how to estimate constituents of QC500 index based on the company fundamentals
### The algorithm creates a default tradable and liquid universe containing 500 US equities
### which are chosen at the first trading day of each month.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
### <meta name="tag" content="fine universes" />
class ConstituentsQC500GeneratorAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2018, 1, 1)   # Set Start Date
        self.SetEndDate(2019, 1, 1)     # Set End Date
        self.SetCash(100000)            # Set Strategy Cash

        # this add universe method accepts two parameters:
        # - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
        # - fine selection function: accepts an IEnumerable<FineFundamental> and returns an IEnumerable<Symbol>
        self.AddUniverse(self.CoarseSelectionFunction, self.FineSelectionFunction)

        self.numberOfSymbolsCoarse = 1000
        self.numberOfSymbolsFine = 500
        self.dollarVolumeBySymbol = {}
        self.symbols = []
        self.lastMonth = -1

    def CoarseSelectionFunction(self, coarse):
        if self.Time.month == self.lastMonth: 
            return self.symbols

        # The stocks must have fundamental data
        # The stock must have positive previous-day close price
        # The stock must have positive volume on the previous trading day
        filtered = [x for x in coarse if x.HasFundamentalData and x.Volume > 0 and x.Price > 0]
        sortedByDollarVolume = sorted(filtered, key = lambda x: x.DollarVolume, reverse=True)[:self.numberOfSymbolsCoarse]

        self.symbols.clear()
        self.dollarVolumeBySymbol.clear()
        for x in sortedByDollarVolume:
            self.symbols.append(x.Symbol)
            self.dollarVolumeBySymbol[x.Symbol] = x.DollarVolume

        # return the symbol objects our sorted collection
        return self.symbols

    def FineSelectionFunction(self, fine):
        if self.Time.month == self.lastMonth: 
            return self.symbols
        self.lastMonth = self.Time.month

        # The company's headquarter must in the U.S.
        # The stock must be traded on either the NYSE or NASDAQ
        # At least half a year since its initial public offering
        # The stock's market cap must be greater than 500 million
        filtered = [x for x in fine if x.CompanyReference.CountryId == "USA"
                                    and (x.CompanyReference.PrimaryExchangeID == "NYS" or x.CompanyReference.PrimaryExchangeID == "NAS")
                                    and (self.Time - x.SecurityReference.IPODate).days > 180
                                    and x.EarningReports.BasicAverageShares.ThreeMonths * (x.EarningReports.BasicEPS.TwelveMonths*x.ValuationRatios.PERatio) > 5e8]

        sortedByDollarVolume = []
        sortedBySector = sorted(filtered, key = lambda x: x.CompanyReference.IndustryTemplateCode)

        percent = self.numberOfSymbolsFine/float(len(sortedBySector))

        # select stocks with top dollar volume in every single sector 
        for code, g in groupby(sortedBySector, lambda x: x.CompanyReference.IndustryTemplateCode):
            y = sorted(g, key = lambda x: self.dollarVolumeBySymbol[x.Symbol], reverse = True)
            c = ceil(len(y) * percent)
            sortedByDollarVolume.extend(y[:c])

            self.Log(f"{self.Time} :: {code}-{c}: {','.join([x.Symbol.Value for x in y[:10]])}")

        sortedByDollarVolume = sorted(sortedByDollarVolume, key = lambda x: self.dollarVolumeBySymbol[x.Symbol], reverse=True)
        self.symbols = [x.Symbol for x in sortedByDollarVolume[:self.numberOfSymbolsFine]]
        return self.symbols