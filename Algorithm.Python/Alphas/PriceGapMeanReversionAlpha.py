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


'''
    The motivating idea for this Alpha Model is that a large price gap (here we use true outliers --
    price gaps that whose absolutely values are greater than 3 * Volatility) is due to rebound
    back to an appropriate price or at least retreat from its brief extreme. Using a Coarse Universe selection
    function, the algorithm selects the top x-companies by Dollar Volume (x can be any number you choose)
    to trade with, and then uses the Standard Deviation of the 100 most-recent closing prices to determine
    which price movements are outliers that warrant emitting insights.

    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.
'''

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Data.Market import TradeBar
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioTarget, EqualWeightingPortfolioConstructionModel

import numpy as np
from datetime import timedelta, datetime

class PriceGapMeanReversionAlpha(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2018, 1, 1)   #Set Start Date
        self.SetCash(100000)           #Set Strategy Cash

        ## Initialize variables to be used in controlling frequency of universe selection
        self.week = None
        self.symbols = None

        self.SetWarmUp(100)

        ## Manual Universe Selection
        self.UniverseSettings.Resolution = Resolution.Minute
        self.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelectionFunction))

        ## Set trading fees to $0
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))

        ## Set custom Alpha Model
        self.SetAlpha(PriceGapMeanReversionAlphaModel())

        ## Set equal-weighting Portfolio Construction Model
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        ## Set Execution Model
        self.SetExecution(ImmediateExecutionModel())

        ## Set Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())


    def CoarseSelectionFunction(self, coarse):
        ## If it isn't a new week, return the same symbols
        current_week = self.Time.isocalendar()[1]
        if  current_week == self.week:
            return self.symbols
        self.week = current_week
        ## If its a new month, then re-filter stocks by Dollar Volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

        self.symbols = [ x.Symbol for x in sortedByDollarVolume[:25] ]
        return self.symbols



class PriceGapMeanReversionAlphaModel:

    def __init__(self, *args, **kwargs):
        ''' Initialize variables and dictionary for Symbol Data to support algorithm's function '''
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Minute
        self.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), 5) ## Arbitrary
        self.symbolDataBySymbol = {}

    def Update(self, algorithm, data):
        insights = []

        ## Loop through all Symbol Data objects
        for symbol, symbolData in self.symbolDataBySymbol.items():
            if symbol not in data.Keys:   ## Skip this slice if the data dictionary doesn't contain the symbol
                continue

            security = algorithm.Securities[symbol]

            ## Update the symbolData properties
            if not symbolData.Update(data, security): return insights

            ## Evaluate whether or not the price jump is expected to rebound up or return down, and emit insights accordingly
            if symbolData.DownTrend:
                insights.append(Insight(symbol, self.prediction_interval, InsightType.Price, InsightDirection.Down, symbolData.PriceJump, None))
            elif symbolData.UpTrend:
                insights.append(Insight(symbol, self.prediction_interval, InsightType.Price, InsightDirection.Up, symbolData.PriceJump, None))

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        for security in changes.RemovedSecurities:
            if security.Symbol in self.symbolDataBySymbol.keys():
                self.symbolDataBySymbol.pop(security.Symbol)
                algorithm.Log(f'{security.Symbol.Value} removed from Universe')
        
        history_request_symbols = [ x.Symbol for x in changes.AddedSecurities ]
        history_df = algorithm.History(history_request_symbols, 100, self.resolution)

        for security in changes.AddedSecurities:
            algorithm.Log(f'{security.Symbol.Value} added to Universe')
            if str(security.Symbol) not in history_df.index.get_level_values(0):
                continue
            history = history_df.loc[str(security.Symbol)]

             ## Create and initialize SymbolData objects
            symbolData = SymbolData(algorithm, security)
            self.symbolDataBySymbol[security.Symbol] = symbolData
            for tuple in history.itertuples():
                bar = TradeBar(tuple.Index, security.Symbol, tuple.open, tuple.high, tuple.low, tuple.close, tuple.volume)
                symbolData.Initialize(bar, security)

class SymbolData:
    def __init__(self, algorithm, security):
        self.symbol = security.Symbol
        self.close = 0
        self.last_price = 0
        self.volatility = algorithm.STD(self.symbol, 100)
        self.price_jump = 0

    def Update(self, data, security):

        ## Check for any data events that would return a NoneBar in the Alpha Model Update() method
        if not data.Bars.ContainsKey(self.symbol) or data.Bars[self.symbol].Close == 0:
            return False

        price = data.Bars[self.symbol].Close
        self.last_price = self.close
        self.close = price
        self.price_jump = (self.close / self.last_price) - 1

        return True

    def Initialize(self, data, security):

        self.volatility.Update(data.Time, data.Close)
        price = data.Close
        if self.last_price == 0:
            self.last_price = price
            self.close = price
        else:
            self.last_price = self.close
            self.close = price


    @property
    def PriceJump(self):
        return (self.close / self.last_price) - 1

    @property
    def DownTrend(self):
        return (abs(100*self.price_jump) > 3*self.volatility.Current.Value) and (self.price_jump > 0)

    @property
    def UpTrend(self):
        return (abs(100*self.price_jump) > 3*self.volatility.Current.Value) and (self.price_jump < 0)