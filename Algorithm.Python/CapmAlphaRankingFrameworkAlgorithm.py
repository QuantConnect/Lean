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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Selection import *
from datetime import timedelta
import numpy as np
import pandas as pd

### <summary>
### CapmAlphaRankingFrameworkAlgorithm: example of custom scheduled universe selection model
### Universe Selection inspired by https://www.quantconnect.com/tutorials/strategy-library/capm-alpha-ranking-strategy-on-dow-30-companies
### </summary>
class CapmAlphaRankingFrameworkAlgorithm(QCAlgorithm):
    '''CapmAlphaRankingFrameworkAlgorithm: example of custom scheduled universe selection model'''

    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2016, 1, 1)   #Set Start Date
        self.SetEndDate(2017, 1, 1)     #Set End Date
        self.SetCash(100000)            #Set Strategy Cash

        # set algorithm framework models
        self.SetUniverseSelection(CapmAlphaRankingUniverseSelectionModel())
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(1), 0.025, None))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.01))

from QuantConnect.Data.UniverseSelection import ScheduledUniverse
from Selection.UniverseSelectionModel import UniverseSelectionModel

class CapmAlphaRankingUniverseSelectionModel(UniverseSelectionModel):
    '''This universe selection model picks stocks with the highest alpha: interception of the linear regression against a benchmark.'''

    period = 21;
    benchmark = "SPY"

    # Symbols of Dow 30 companies.
    symbols = [Symbol.Create(x, SecurityType.Equity, Market.USA)
               for x in ["AAPL", "AXP", "BA", "CAT", "CSCO", "CVX", "DD", "DIS", "GE", "GS",
                         "HD", "IBM", "INTC", "JPM", "KO", "MCD", "MMM", "MRK", "MSFT",
                         "NKE","PFE", "PG", "TRV", "UNH", "UTX", "V", "VZ", "WMT", "XOM"]]

    def CreateUniverses(self, algorithm):

        # Adds the benchmark to the user defined universe
        benchmark = algorithm.AddEquity(self.benchmark, Resolution.Daily)

        # Defines a schedule universe that fires after market open when the month starts
        return [ ScheduledUniverse(
            benchmark.Exchange.TimeZone,
            algorithm.DateRules.MonthStart(self.benchmark),
            algorithm.TimeRules.AfterMarketOpen(self.benchmark),
            lambda datetime: self.SelectPair(algorithm, datetime),
            algorithm.UniverseSettings,
            algorithm.SecurityInitializer)]

    def SelectPair(self, algorithm, date):
        '''Selects the pair (two stocks) with the highest alpha'''
        dictionary = dict()
        benchmark = self._getReturns(algorithm, self.benchmark)
        ones = np.ones(len(benchmark))

        for symbol in self.symbols:
            prices = self._getReturns(algorithm, symbol)
            if prices is None: continue
            A = np.vstack([prices, ones]).T

            # Calculate the Least-Square fitting to the returns of a given symbol and the benchmark
            ols = np.linalg.lstsq(A, benchmark)[0]
            dictionary[symbol] = ols[1]

        # Returns the top 2 highest alphas
        orderedDictionary = sorted(dictionary.items(), key= lambda x: x[1], reverse=True)
        return [x[0] for x in orderedDictionary[:2]]

    def _getReturns(self, algorithm, symbol):

        history = algorithm.History([symbol], self.period, Resolution.Daily)
        if history.empty: return None

        window = RollingWindow[float](self.period)
        rateOfChange = RateOfChange(1)

        def roc_updated(s, item):
            window.Add(item.Value)

        rateOfChange.Updated += roc_updated

        history = history.close.reset_index(level=0, drop=True).iteritems()

        for time, value in history:
            rateOfChange.Update(time, value);

        return [ x for x in window]
