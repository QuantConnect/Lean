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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Indicators")

from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *


class EmaCrossAlphaModel(AlphaModel):
    '''Alpha model that uses an EMA cross to create insights'''

    def __init__(self,
                 fastPeriod = 12,
                 slowPeriod = 26,
                 resolution = Resolution.Daily):
        '''Initializes a new instance of the EmaCrossAlphaModel class
        Args:
            fastPeriod: The fast EMA period
            slowPeriod: The slow EMA period'''
        self.fastPeriod = fastPeriod
        self.slowPeriod = slowPeriod
        self.resolution = resolution
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(resolution), fastPeriod)
        self.symbolDataBySymbol = {}

        resolutionString = Extensions.GetEnumString(resolution, Resolution)
        self.Name = '{}({},{},{})'.format(self.__class__.__name__, fastPeriod, slowPeriod, resolutionString)


    def Update(self, algorithm, data):
        '''Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        insights = []
        for symbol, symbolData in self.symbolDataBySymbol.items():
            if symbolData.Fast.IsReady and symbolData.Slow.IsReady:

                if symbolData.FastIsOverSlow:
                    if symbolData.Slow > symbolData.Fast:
                        insights.append(Insight.Price(symbolData.Symbol, self.predictionInterval, InsightDirection.Down))

                elif symbolData.SlowIsOverFast:
                    if symbolData.Fast > symbolData.Slow:
                        insights.append(Insight.Price(symbolData.Symbol, self.predictionInterval, InsightDirection.Up))

            symbolData.FastIsOverSlow = symbolData.Fast > symbolData.Slow

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for added in changes.AddedSecurities:
            symbolData = self.symbolDataBySymbol.get(added.Symbol)
            if symbolData is None:
                # create fast/slow EMAs
                symbolData = SymbolData(added)
                symbolData.Fast = algorithm.EMA(added.Symbol, self.fastPeriod, self.resolution)
                symbolData.Slow = algorithm.EMA(added.Symbol, self.slowPeriod, self.resolution)
                self.symbolDataBySymbol[added.Symbol] = symbolData
            else:
                # a security that was already initialized was re-added, reset the indicators
                symbolData.Fast.Reset()
                symbolData.Slow.Reset()


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, security):
        self.Security = security
        self.Symbol = security.Symbol
        self.Fast = None
        self.Slow = None

        # True if the fast is above the slow, otherwise false.
        # This is used to prevent emitting the same signal repeatedly
        self.FastIsOverSlow = False

    @property
    def SlowIsOverFast(self):
        return not self.FastIsOverSlow