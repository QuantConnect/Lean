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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Algorithm.Framework")

from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Indicators import ExponentialMovingAverage
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel
from itertools import chain
from math import ceil

class QC500UniverseSelectionModel(FundamentalUniverseSelectionModel):
    '''Defines the QC500 universe as a universe selection model for framework algorithm
    For details: https://github.com/QuantConnect/Lean/pull/1663'''

    def __init__(self,
                 filterFineData = True,
                 universeSettings = None, 
                 securityInitializer = None):
        '''Initializes a new default instance of the QC500UniverseSelectionModel'''
        super().__init__(filterFineData, universeSettings, securityInitializer)
        self.NumberOfSymbolsCoarse = 1000
        self.NumberOfSymbolsFine = 500
        self.lastMonth = -1
        self.dollarVolumeBySymbol = {}
        self.symbols = []

    def SelectCoarse(self, algorithm, coarse):
        '''Performs coarse selection for the QC500 constituents.
        The stocks must have fundamental data
        The stock must have positive previous-day close price
        The stock must have positive volume on the previous trading day'''
        coarse = list(coarse)

        if len(coarse) == 0:
            return self.symbols

        month = coarse[0].EndTime.month
        if month == self.lastMonth:
            return self.symbols

        self.lastMonth = month

        # The stocks must have fundamental data
        # The stock must have positive previous-day close price
        # The stock must have positive volume on the previous trading day
        filtered = [x for x in coarse if x.HasFundamentalData
                                      and x.Volume > 0
                                      and x.Price > 0]
        # sort the stocks by dollar volume and take the top 1000
        top = sorted(filtered, key=lambda x: x.DollarVolume, reverse=True)[:self.NumberOfSymbolsCoarse]

        self.dollarVolumeBySymbol = { i.Symbol: i.DollarVolume for i in top }

        self.symbols = list(self.dollarVolumeBySymbol.keys())

        return self.symbols


    def SelectFine(self, algorithm, fine):
        '''Performs fine selection for the QC500 constituents
        The company's headquarter must in the U.S.
        The stock must be traded on either the NYSE or NASDAQ
        At least half a year since its initial public offering
        The stock's market cap must be greater than 500 million'''

        # The company's headquarter must in the U.S.
        # The stock must be traded on either the NYSE or NASDAQ
        # At least half a year since its initial public offering
        # The stock's market cap must be greater than 500 million
        filteredFine = [x for x in fine if x.CompanyReference.CountryId == "USA"
                                        and (x.CompanyReference.PrimaryExchangeID == "NYS" or x.CompanyReference.PrimaryExchangeID == "NAS")
                                        and (algorithm.Time - x.SecurityReference.IPODate).days > 180
                                        and x.EarningReports.BasicAverageShares.ThreeMonths * x.EarningReports.BasicEPS.TwelveMonths * x.ValuationRatios.PERatio > 5e8]
        count = len(filteredFine)
        if count == 0: return []

        myDict = dict()
        percent = float(self.NumberOfSymbolsFine / count)

        # select stocks with top dollar volume in every single sector
        for key in ["N", "M", "U", "T", "B", "I"]:
            value = [x for x in filteredFine if x.CompanyReference.IndustryTemplateCode == key]
            value = sorted(value, key=lambda x: self.dollarVolumeBySymbol[x.Symbol], reverse = True)
            myDict[key] = value[:ceil(len(value) * percent)]

        topFine = list(chain.from_iterable(myDict.values()))[:self.NumberOfSymbolsFine]
        self.symbols = [f.Symbol for f in topFine]

        return self.symbols
