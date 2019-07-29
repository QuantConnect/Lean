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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
import numpy as np
from datetime import datetime


class VolumeWeightedAveragePriceExecutionModel(ExecutionModel):
    '''Execution model that submits orders while the current market price is more favorable that the current volume weighted average price.'''

    def __init__(self):
        '''Initializes a new instance of the VolumeWeightedAveragePriceExecutionModel class'''
        self.targetsCollection = PortfolioTargetCollection()
        self.symbolData = {}

        # Gets or sets the maximum order quantity as a percentage of the current bar's volume.
        # This defaults to 0.01m = 1%. For example, if the current bar's volume is 100,
        # then the maximum order size would equal 1 share.
        self.MaximumOrderQuantityPercentVolume = 0.01


    def Execute(self, algorithm, targets):
        '''Executes market orders if the standard deviation of price is more
       than the configured number of deviations in the favorable direction.
       Args:
           algorithm: The algorithm instance
           targets: The portfolio targets'''

        # update the complete set of portfolio targets with the new targets
        self.targetsCollection.AddRange(targets)

        # for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
        if self.targetsCollection.Count > 0:
            for target in self.targetsCollection.OrderByMarginImpact(algorithm):
                symbol = target.Symbol

                # calculate remaining quantity to be ordered
                unorderedQuantity = OrderSizing.GetUnorderedQuantity(algorithm, target)

                # fetch our symbol data containing our VWAP indicator
                data = self.symbolData.get(symbol, None)
                if data is None: return

                # check order entry conditions
                if self.PriceIsFavorable(data, unorderedQuantity):
                    # get the maximum order size based on total order value
                    maxOrderSize = OrderSizing.PercentVolume(data.Security, self.MaximumOrderQuantityPercentVolume)
                    orderSize = np.min([maxOrderSize, np.abs(unorderedQuantity)])

                    remainder = orderSize % data.Security.SymbolProperties.LotSize
                    missingForLotSize = data.Security.SymbolProperties.LotSize - remainder
                    # if the amount we are missing for +1 lot size is 1M part of a lot size
                    # we suppose its due to floating point error and round up
                    # Note: this is required to avoid a diff with C# equivalent
                    if missingForLotSize < (data.Security.SymbolProperties.LotSize / 1000000):
                        remainder -= data.Security.SymbolProperties.LotSize

                    # round down to even lot size
                    orderSize -= remainder
                    if orderSize != 0:
                        algorithm.MarketOrder(symbol, np.sign(unorderedQuantity) * orderSize)

            self.targetsCollection.ClearFulfilled(algorithm)


    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for removed in changes.RemovedSecurities:
            # clean up removed security data
            if removed.Symbol in self.symbolData:
                if self.IsSafeToRemove(algorithm, removed.Symbol):
                    data = self.symbolData.pop(removed.Symbol)
                    algorithm.SubscriptionManager.RemoveConsolidator(removed.Symbol, data.Consolidator)

        for added in changes.AddedSecurities:
            if added.Symbol not in self.symbolData:
                self.symbolData[added.Symbol] = SymbolData(algorithm, added)


    def PriceIsFavorable(self, data, unorderedQuantity):
        '''Determines if the current price is more than the configured
       number of standard deviations away from the mean in the favorable direction.'''
        if unorderedQuantity > 0:
            if data.Security.BidPrice < data.VWAP:
                return True
        else:
            if data.Security.AskPrice > data.VWAP:
                return True

        return False

    def IsSafeToRemove(self, algorithm, symbol):
        '''Determines if it's safe to remove the associated symbol data'''
        # confirm the security isn't currently a member of any universe
        return not any([kvp.Value.ContainsMember(symbol) for kvp in algorithm.UniverseManager])

class SymbolData:
    def __init__(self, algorithm, security):
        self.Security = security
        self.Consolidator = algorithm.ResolveConsolidator(security.Symbol, security.Resolution)
        name = algorithm.CreateIndicatorName(security.Symbol, "VWAP", security.Resolution)
        self.vwap = IntradayVwap(name)
        algorithm.RegisterIndicator(security.Symbol, self.vwap, self.Consolidator)

    @property
    def VWAP(self):
       return self.vwap.Value

class IntradayVwap:
    '''Defines the canonical intraday VWAP indicator'''
    def __init__(self, name):
        self.Name = name
        self.Value = 0.0
        self.lastDate = datetime.min
        self.sumOfVolume = 0.0
        self.sumOfPriceTimesVolume = 0.0

    @property
    def IsReady(self):
        return self.sumOfVolume > 0.0

    def Update(self, input):
        '''Computes the new VWAP'''
        success, volume, averagePrice = self.GetVolumeAndAveragePrice(input)
        if not success:
            return self.IsReady

        # reset vwap on daily boundaries
        if self.lastDate != input.EndTime.date():
            self.sumOfVolume = 0.0
            self.sumOfPriceTimesVolume = 0.0
            self.lastDate = input.EndTime.date()

        # running totals for Σ PiVi / Σ Vi
        self.sumOfVolume += volume
        self.sumOfPriceTimesVolume += averagePrice * volume

        if self.sumOfVolume == 0.0:
           # if we have no trade volume then use the current price as VWAP
           self.Value = input.Value
           return self.IsReady

        self.Value = self.sumOfPriceTimesVolume / self.sumOfVolume
        return self.IsReady

    def GetVolumeAndAveragePrice(self, input):
        '''Determines the volume and price to be used for the current input in the VWAP computation'''

        if type(input) is Tick:
            if input.TickType == TickType.Trade:
                return True, float(input.Quantity), float(input.LastPrice)

        if type(input) is TradeBar:
            if not input.IsFillForward:
                averagePrice = float(input.High + input.Low + input.Close) / 3
                return True, float(input.Volume), averagePrice

        return False, 0.0, 0.0