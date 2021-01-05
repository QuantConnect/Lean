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
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *


class PriceGapMeanReversionAlpha(QCAlgorithm):
    '''The motivating idea for this Alpha Model is that a large price gap (here we use true outliers --
    price gaps that whose absolutely values are greater than 3 * Volatility) is due to rebound
    back to an appropriate price or at least retreat from its brief extreme. Using a Coarse Universe selection
    function, the algorithm selects the top x-companies by Dollar Volume (x can be any number you choose)
    to trade with, and then uses the Standard Deviation of the 100 most-recent closing prices to determine
    which price movements are outliers that warrant emitting insights.

    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.'''

    def Initialize(self):

        self.SetStartDate(2018, 1, 1)   #Set Start Date
        self.SetCash(100000)           #Set Strategy Cash

        ## Initialize variables to be used in controlling frequency of universe selection
        self.week = -1

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
        if current_week == self.week:
            return Universe.Unchanged
        self.week = current_week

        ## If its a new week, then re-filter stocks by Dollar Volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

        return [ x.Symbol for x in sortedByDollarVolume[:25] ]


class PriceGapMeanReversionAlphaModel:

    def __init__(self, *args, **kwargs):
        ''' Initialize variables and dictionary for Symbol Data to support algorithm's function '''
        self.lookback = 100
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Minute
        self.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), 5) ## Arbitrary
        self.symbolDataBySymbol = {}

    def Update(self, algorithm, data):
        insights = []

        ## Loop through all Symbol Data objects
        for symbol, symbolData in self.symbolDataBySymbol.items():
            ## Evaluate whether or not the price jump is expected to rebound
            if not symbolData.IsTrend(data):
                continue

            ## Emit insights accordingly to the price jump sign
            direction = InsightDirection.Down if symbolData.PriceJump > 0 else InsightDirection.Up
            insights.append(Insight.Price(symbol, self.prediction_interval, direction, symbolData.PriceJump, None))
            
        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        # Clean up data for removed securities
        for removed in changes.RemovedSecurities:
            symbolData = self.symbolDataBySymbol.pop(removed.Symbol, None)
            if symbolData is not None:
                symbolData.RemoveConsolidators(algorithm)

        symbols = [x.Symbol for x in changes.AddedSecurities
            if x.Symbol not in self.symbolDataBySymbol]

        history = algorithm.History(symbols, self.lookback, self.resolution)
        if history.empty: return

        ## Create and initialize SymbolData objects
        for symbol in symbols:
            symbolData = SymbolData(algorithm, symbol, self.lookback, self.resolution)
            symbolData.WarmUpIndicators(history.loc[symbol])
            self.symbolDataBySymbol[symbol] = symbolData


class SymbolData:
    def __init__(self, algorithm, symbol, lookback, resolution):
        self.symbol = symbol
        self.close = 0
        self.last_price = 0
        self.PriceJump = 0
        self.consolidator = algorithm.ResolveConsolidator(symbol, resolution)
        self.volatility = StandardDeviation(f'{symbol}.STD({lookback})', lookback)
        algorithm.RegisterIndicator(symbol, self.volatility, self.consolidator)

    def RemoveConsolidators(self, algorithm):
        algorithm.SubscriptionManager.RemoveConsolidator(self.symbol, self.consolidator)

    def WarmUpIndicators(self, history):
        self.close = history.iloc[-1].close
        for tuple in history.itertuples():
            self.volatility.Update(tuple.Index, tuple.close)

    def IsTrend(self, data):

        ## Check for any data events that would return a NoneBar in the Alpha Model Update() method
        if not data.Bars.ContainsKey(self.symbol):
            return False

        self.last_price = self.close
        self.close = data.Bars[self.symbol].Close
        self.PriceJump = (self.close / self.last_price) - 1
        return abs(100*self.PriceJump) > 3*self.volatility.Current.Value