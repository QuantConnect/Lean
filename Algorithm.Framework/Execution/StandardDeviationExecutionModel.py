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

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Data.Market import TradeBar
from QuantConnect.Orders import *
from QuantConnect.Algorithm.Framework.Execution import ExecutionModel, OrderSizing
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioTargetCollection
import numpy as np


class StandardDeviationExecutionModel(ExecutionModel):
    '''Execution model that submits orders while the current market prices is at least the configured number of standard
     deviations away from the mean in the favorable direction (below/above for buy/sell respectively)'''

    def __init__(self, 
                 period = 60,
                 deviations = 2,
                 resolution = Resolution.Minute):
        '''Initializes a new instance of the StandardDeviationExecutionModel class
        Args:
            period: Period of the standard deviation indicator
            deviations: The number of deviations away from the mean before submitting an order
            resolution: The resolution of the STD and SMA indicators'''
        self.period = period
        self.deviations = deviations
        self.resolution = resolution
        self.targetsCollection = PortfolioTargetCollection()
        self.symbolData = {}

        # Gets or sets the maximum order value in units of the account currency.
        # This defaults to $20,000. For example, if purchasing a stock with a price
        # of $100, then the maximum order size would be 200 shares.
        self.MaximumOrderValue = 20000


    def Execute(self, algorithm, targets):
        '''Executes market orders if the standard deviation of price is more
       than the configured number of deviations in the favorable direction.
       Args:
           algorithm: The algorithm instance
           targets: The portfolio targets'''
        self.targetsCollection.AddRange(targets)

        for target in self.targetsCollection:
            symbol = target.Symbol

            # calculate remaining quantity to be ordered
            unorderedQuantity = OrderSizing.GetUnorderedQuantity(algorithm, target)

            # fetch our symbol data containing our STD/SMA indicators
            data = self.symbolData.get(symbol, None)
            if data is None: return

            # ensure we're receiving price data before submitting orders
            if data.Security.Price == 0: return

            # check order entry conditions
            if data.STD.IsReady and self.PriceIsFavorable(data, unorderedQuantity):
                # get the maximum order size based on total order value
                maxOrderSize = OrderSizing.Value(data.Security, self.MaximumOrderValue)
                orderSize = np.min([maxOrderSize, np.abs(unorderedQuantity)])

                # round down to even lot size
                orderSize -= orderSize % data.Security.SymbolProperties.LotSize
                if orderSize != 0:
                    algorithm.MarketOrder(symbol, np.sign(unorderedQuantity) * orderSize)

            # check to see if we're done with this target
            unorderedQuantity = OrderSizing.GetUnorderedQuantity(algorithm, target)
            if unorderedQuantity == 0:
                self.targetsCollection.Remove(target.Symbol)


    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for removed in changes.RemovedSecurities:
            # clean up data from removed securities
            if removed.Symbol in self.symbolData:
                if self.IsSafeToRemove(algorithm, removed.Symbol):
                    data = self.symbolData.pop(removed.Symbol)
                    algorithm.SubscriptionManager.RemoveConsolidator(removed.Symbol, data.Consolidator)

        addedSymbols = []
        for added in changes.AddedSecurities:
            if added.Symbol not in self.symbolData:
                self.symbolData[added.Symbol] = SymbolData(algorithm, added, self.period, self.resolution)
                addedSymbols.append(added.Symbol)

        if len(addedSymbols) > 0:
            # warmup our indicators by pushing history through the consolidators
            history = algorithm.History(addedSymbols, self.period, self.resolution)
            if history.empty: return

            tickers = history.index.levels[0]
            for ticker in tickers:
                symbol = SymbolCache.GetSymbol(ticker)
                symbolData = self.symbolData[symbol]

                for tuple in history.loc[ticker].itertuples():
                    bar = TradeBar(tuple.Index, symbol, tuple.open, tuple.high, tuple.low, tuple.close, tuple.volume)
                    symbolData.Consolidator.Update(bar)

    def PriceIsFavorable(self, data, unorderedQuantity):
        '''Determines if the current price is more than the configured
       number of standard deviations away from the mean in the favorable direction.'''
        deviations = self.deviations * data.STD.Current.Value
        if unorderedQuantity > 0:
            if data.Security.BidPrice < data.SMA.Current.Value - deviations:
                return True
        else:
            if data.Security.AskPrice > data.SMA.Current.Value + deviations:
                return True

        return False

    def IsSafeToRemove(self, algorithm, symbol):
        '''Determines if it's safe to remove the associated symbol data'''
        # confirm the security isn't currently a member of any universe
        return not any([kvp.Value.ContainsMember(symbol) for kvp in algorithm.UniverseManager])

class SymbolData:
    def __init__(self, algorithm, security, period, resolution):
        self.Security = security
        self.Consolidator = algorithm.ResolveConsolidator(security.Symbol, resolution)
        smaName = algorithm.CreateIndicatorName(security.Symbol, "SMA{}".format(period), resolution)
        self.SMA = SimpleMovingAverage(smaName, period)
        algorithm.RegisterIndicator(security.Symbol, self.SMA, self.Consolidator)
        stdName = algorithm.CreateIndicatorName(security.Symbol, "STD{}".format(period), resolution)
        self.STD = StandardDeviation(stdName, period)
        algorithm.RegisterIndicator(security.Symbol, self.STD, self.Consolidator)