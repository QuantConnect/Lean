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
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Data.Consolidators import *
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Selection import ManualUniverseSelectionModel
from QuantConnect.Algorithm.Framework.Portfolio import EqualWeightingPortfolioConstructionModel
from datetime import datetime, timedelta, time

#
# Reversal strategy that goes long when price crosses below SMA and Short when price crosses above SMA.
# The trading strategy is implemented only between 10AM - 3PM (NY time). Research suggests this is due to
# institutional trades during market hours which need hedging with the USD. Source paper:
# LeBaron, Zhao: Intraday Foreign Exchange Reversals
# http://people.brandeis.edu/~blebaron/wps/fxnyc.pdf
# http://www.fma.org/Reno/Papers/ForeignExchangeReversalsinNewYorkTime.pdf
#
# This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha.
#

class IntradayReversalCurrencyMarketsAlpha(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2015, 1, 1)
        self.SetCash(100000)

        # Set zero transaction fees
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))

        # Select resolution
        resolution = Resolution.Hour

        # Reversion on the USD.
        symbols = [Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda)]

        # Set requested data resolution
        self.UniverseSettings.Resolution = resolution
        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols))
        self.SetAlpha(IntradayReversalAlphaModel(5, resolution))

        # Equally weigh securities in portfolio, based on insights
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        # Set Immediate Execution Model
        self.SetExecution(ImmediateExecutionModel())

        # Set Null Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())

        #Set WarmUp for Indicators
        self.SetWarmUp(20)


class IntradayReversalAlphaModel(AlphaModel):
    '''Alpha model that uses a Price/SMA Crossover to create insights on Hourly Frequency.
    Frequency: Hourly data with 5-hour simple moving average.
    Strategy:
    Reversal strategy that goes Long when price crosses below SMA and Short when price crosses above SMA.
    The trading strategy is implemented only between 10AM - 3PM (NY time)'''

    # Initialize variables
    def __init__(self, period_sma = 5, resolution = Resolution.Hour):
        self.period_sma = period_sma
        self.resolution = resolution
        self.cache = {} # Cache for SymbolData
        self.Name = 'IntradayReversalAlphaModel'

    def Update(self, algorithm, data):
        # Set the time to close all positions at 3PM
        timeToClose = algorithm.Time.replace(hour=15, minute=1, second=0)

        insights = []
        for kvp in algorithm.ActiveSecurities:

            symbol = kvp.Key

            if self.ShouldEmitInsight(algorithm, symbol) and symbol in self.cache:

                price = kvp.Value.Price
                symbolData = self.cache[symbol]

                direction = InsightDirection.Up if symbolData.is_uptrend(price) else InsightDirection.Down

                # Ignore signal for same direction as previous signal (when no crossover)
                if direction == symbolData.PreviousDirection:
                    continue

                # Save the current Insight Direction to check when the crossover happens
                symbolData.PreviousDirection = direction

                # Generate insight
                insights.append(Insight.Price(symbol, timeToClose, direction))

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Handle creation of the new security and its cache class.
        Simplified in this example as there is 1 asset.'''
        for security in changes.AddedSecurities:
            self.cache[security.Symbol] = SymbolData(algorithm, security.Symbol, self.period_sma, self.resolution)

    def ShouldEmitInsight(self, algorithm, symbol):
        '''Time to control when to start and finish emitting (10AM to 3PM)'''
        timeOfDay = algorithm.Time.time()
        return algorithm.Securities[symbol].HasData and timeOfDay >= time(10) and timeOfDay <= time(15)


class SymbolData:

    def __init__(self, algorithm, symbol, period_sma, resolution):
        self.PreviousDirection = InsightDirection.Flat
        self.priceSMA = algorithm.SMA(symbol, period_sma, resolution)

    def is_uptrend(self, price):
        return self.priceSMA.IsReady and price < round(self.priceSMA.Current.Value * 1.001, 6)