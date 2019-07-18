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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Algorithm.Framework")

from System import *
from QuantConnect import *
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Data.Consolidators import TradeBarConsolidator
from QuantConnect.Data.Market import TradeBar
from QuantConnect.Indicators import RollingWindow
from QuantConnect.Brokerages import BrokerageName
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Selection import ManualUniverseSelectionModel
from QuantConnect.Algorithm.Framework.Portfolio import EqualWeightingPortfolioConstructionModel
from QuantConnect.Algorithm.Framework.Execution import ImmediateExecutionModel
from QuantConnect.Algorithm.Framework.Risk import MaximumDrawdownPercentPerSecurity
from datetime import timedelta

#
# This is a demonstration algorithm. It trades UVXY.
# Dual Thrust alpha model is used to produce insights.
# Those input parameters have been chosen that gave acceptable results on a series
# of random backtests run for the period from Oct, 2016 till Feb, 2019.
#

class VIXDualThrustAlpha(QCAlgorithm):

    def Initialize(self):

        # -- STRATEGY INPUT PARAMETERS --
        self.k1 = 0.63
        self.k2 = 0.63
        self.rangePeriod = 20
        self.consolidatorBars = 30

        # Settings
        self.SetStartDate(2018, 10, 1)
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))
        self.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

        # Universe Selection
        self.UniverseSettings.Resolution = Resolution.Minute   # it's minute by default, but lets leave this param here
        symbols = [Symbol.Create("SPY", SecurityType.Equity, Market.USA)]
        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols))

        # Warming up
        resolutionInTimeSpan =  Extensions.ToTimeSpan(self.UniverseSettings.Resolution)
        warmUpTimeSpan = Time.Multiply(resolutionInTimeSpan, self.consolidatorBars)
        self.SetWarmUp(warmUpTimeSpan)

        # Alpha Model
        self.SetAlpha(DualThrustAlphaModel(self.k1, self.k2, self.rangePeriod, self.UniverseSettings.Resolution, self.consolidatorBars))

        ## Portfolio Construction
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        ## Execution
        self.SetExecution(ImmediateExecutionModel())

        ## Risk Management
        self.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.03))


class DualThrustAlphaModel(AlphaModel):
    '''Alpha model that uses dual-thrust strategy to create insights
    https://medium.com/@FMZ_Quant/dual-thrust-trading-strategy-2cc74101a626
    or here:
    https://www.quantconnect.com/tutorials/strategy-library/dual-thrust-trading-algorithm'''

    def __init__(self,
                 k1,
                 k2,
                 rangePeriod,
                 resolution = Resolution.Daily,
                 barsToConsolidate = 1):
        '''Initializes a new instance of the class
        Args:
            k1: Coefficient for upper band
            k2: Coefficient for lower band
            rangePeriod: Amount of last bars to calculate the range
            resolution: The resolution of data sent into the EMA indicators
            barsToConsolidate: If we want alpha to work on trade bars whose length is different
                from the standard resolution - 1m 1h etc. - we need to pass this parameters along
                with proper data resolution'''

        # coefficient that used to determinte upper and lower borders of a breakout channel
        self.k1 = k1
        self.k2 = k2

        # period the range is calculated over
        self.rangePeriod = rangePeriod

        # initialize with empty dict.
        self.symbolDataBySymbol = dict()

        # time for bars we make the calculations on
        resolutionInTimeSpan =  Extensions.ToTimeSpan(resolution)
        self.consolidatorTimeSpan = Time.Multiply(resolutionInTimeSpan, barsToConsolidate)

        # in 5 days after emission an insight is to be considered expired
        self.period = timedelta(5)

    def Update(self, algorithm, data):
        insights = []

        for symbol, symbolData in self.symbolDataBySymbol.items():
            if not symbolData.IsReady:
                continue

            holding = algorithm.Portfolio[symbol]
            price = algorithm.Securities[symbol].Price

            # buying condition
            # - (1) price is above upper line
            # - (2) and we are not long. this is a first time we crossed the line lately
            if price > symbolData.UpperLine and not holding.IsLong:
                insightCloseTimeUtc = algorithm.UtcTime + self.period
                insights.append(Insight.Price(symbol, insightCloseTimeUtc, InsightDirection.Up))

            # selling condition
            # - (1) price is lower that lower line
            # - (2) and we are not short. this is a first time we crossed the line lately
            if price < symbolData.LowerLine and not holding.IsShort:
                insightCloseTimeUtc = algorithm.UtcTime + self.period
                insights.append(Insight.Price(symbol, insightCloseTimeUtc, InsightDirection.Down))

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        # added
        for symbol in [x.Symbol for x in changes.AddedSecurities]:
            if symbol not in self.symbolDataBySymbol:
                # add symbol/symbolData pair to collection
                symbolData = self.SymbolData(symbol, self.k1, self.k2, self.rangePeriod, self.consolidatorTimeSpan)
                self.symbolDataBySymbol[symbol] = symbolData
                # register consolidator
                algorithm.SubscriptionManager.AddConsolidator(symbol, symbolData.GetConsolidator())

        # removed
        for symbol in [x.Symbol for x in changes.RemovedSecurities]:
            symbolData = self.symbolDataBySymbol.pop(symbol, None)
            if symbolData is None:
                algorithm.Error("Unable to remove data from collection: DualThrustAlphaModel")
            else:
                # unsubscribe consolidator from data updates
                algorithm.SubscriptionManager.RemoveConsolidator(symbol, symbolData.GetConsolidator())


    class SymbolData:
        '''Contains data specific to a symbol required by this model'''
        def __init__(self, symbol, k1, k2, rangePeriod, consolidatorResolution):

            self.Symbol = symbol
            self.rangeWindow = RollingWindow[TradeBar](rangePeriod)
            self.consolidator = TradeBarConsolidator(consolidatorResolution);

            def onDataConsolidated(sender, consolidated):
                # add new tradebar to
                self.rangeWindow.Add(consolidated)

                if self.rangeWindow.IsReady:
                    hh = max([x.High for x in self.rangeWindow])
                    hc = max([x.Close for x in self.rangeWindow])
                    lc = min([x.Close for x in self.rangeWindow])
                    ll = min([x.Low for x in self.rangeWindow])

                    range = max([hh - lc, hc - ll])
                    self.UpperLine = consolidated.Close + k1 * range
                    self.LowerLine = consolidated.Close - k2 * range

            # event fired at new consolidated trade bar
            self.consolidator.DataConsolidated += onDataConsolidated

        # Returns the interior consolidator
        def GetConsolidator(self):
            return self.consolidator

        @property
        def IsReady(self):
            return self.rangeWindow.IsReady