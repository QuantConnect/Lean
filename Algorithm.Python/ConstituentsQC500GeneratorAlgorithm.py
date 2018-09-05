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
AddReference("System.Collections")
AddReference("QuantConnect.Common")

from System import *
from System.Collections.Generic import List
from QuantConnect import *
from QuantConnect.Data.UniverseSelection import *
from QCAlgorithm import QCAlgorithm
from math import ceil
import numpy as np
import pandas as pd
import scipy as sp

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

        self.SetStartDate(2018, 1, 1) #Set Start Date
        self.SetEndDate(2018, 1, 3) #Set End Date
        self.SetCash(50000) #Set Strategy Cash
        self.UniverseSettings.Resolution = Resolution.Daily

        # this add universe method accepts two parameters:
        # - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
        # - fine selection function: accepts an IEnumerable<FineFundamental> and returns an IEnumerable<Symbol>
        self.AddUniverse(self.CoarseSelectionFunction, self.FineSelectionFunction)

        self.spy = self.AddEquity("SPY", Resolution.Daily)
        self.Schedule.On(self.DateRules.MonthStart("SPY"), self.TimeRules.At(0, 0), self.monthly_rebalance)
        self.num_coarse = 1000
        self.num_fine = 500
        self.dollar_volume = {}
        self.rebalance = True

    def CoarseSelectionFunction(self, coarse):
        if not self.rebalance: return []
        # The stocks must have fundamental data
        # The stock must have positive previous-day close price
        # The stock must have positive volume on the previous trading day
        filtered = [x for x in coarse if x.HasFundamentalData
                                      and x.Volume > 0
                                      and x.Price > 0]
        # sort the stocks by dollar volume and take the top 1000
        sort_filtered = sorted(filtered, key=lambda x: x.DollarVolume, reverse=True)[:self.num_coarse]
        for i in sort_filtered:
            self.dollar_volume[i.Symbol.Value] = i.DollarVolume

        # return the symbol objects our sorted collection
        return [x.Symbol for x in sort_filtered]

    def FineSelectionFunction(self, fine):
        if not self.rebalance: return []
        self.rebalance = False
        # The company's headquarter must in the U.S.
        # The stock must be traded on either the NYSE or NASDAQ
        # At least half a year since its initial public offering
        # The stock's market cap must be greater than 500 million
        filtered_fine = [x for x in fine if  (x.CompanyReference.CountryId == "USA")
                                        and (x.CompanyReference.PrimaryExchangeID == "NYS" or x.CompanyReference.PrimaryExchangeID == "NAS")
                                        and ((self.Time - x.SecurityReference.IPODate).days > 180)
                                        and x.EarningReports.BasicAverageShares.ThreeMonths * (x.EarningReports.BasicEPS.TwelveMonths*x.ValuationRatios.PERatio) > 5e8]

        count = len(filtered_fine)
        if count == 0: return []
        
        # select stocks with top dollar volume in every single sector
        for i in filtered_fine:
            i.DollarVolume = self.dollar_volume[i.Symbol.Value]
        percent = float(self.num_fine/count)
        group_by_code = {}
        top_list = []
        for code in ["N", "M", "U", "T", "B", "I"]:
            group_by_code[code] = list(filter(lambda x: x.CompanyReference.IndustryTemplateCode == code, filtered_fine))
            top = sorted(group_by_code[code], key=lambda x: x.DollarVolume, reverse = True)[:ceil(len(group_by_code[code])*percent)]
            top_list.append(top)
        joined_list = top_list[0]
        for ls in top_list[1:]:
            joined_list  += ls
        self.symbols = [x.Symbol for x in joined_list][:self.num_fine]
        self.Log(",".join(sorted(i.Value for i in self.symbols)))
        return self.symbols

    def OnData(self, data):
        pass

    def monthly_rebalance(self):
        self.rebalance = True