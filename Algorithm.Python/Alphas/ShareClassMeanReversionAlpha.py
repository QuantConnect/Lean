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
from QuantConnect.Data.Market import TradeBar
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Indicators import RollingWindow, SimpleMovingAverage

from datetime import timedelta, datetime
import numpy as np

#
# A number of companies publicly trade two different classes of shares
# in US equity markets. If both assets trade with reasonable volume, then
# the underlying driving forces of each should be similar or the same. Given
# this, we can create a relatively dollar-netural long/short portfolio using
# the dual share classes. Theoretically, any deviation of this portfolio from
# its mean-value should be corrected, and so the motivating idea is based on
# mean-reversion. Using a Simple Moving Average indicator, we can
# compare the value of this portfolio against its SMA and generate insights
# to buy the under-valued symbol and sell the over-valued symbol.
#
# This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
# sourced so the community and client funds can see an example of an alpha.
#

class ShareClassMeanReversionAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2019, 1, 1)   #Set Start Date
        self.SetCash(100000)           #Set Strategy Cash
        self.SetWarmUp(20)

        ## Setup Universe settings and tickers to be used
        tickers = ['VIA','VIAB']
        self.UniverseSettings.Resolution = Resolution.Minute
        symbols = [ Symbol.Create(ticker, SecurityType.Equity, Market.USA) for ticker in tickers]
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))  ## Set $0 fees to mimic High-Frequency Trading

        ## Set Manual Universe Selection
        self.SetUniverseSelection( ManualUniverseSelectionModel(symbols) )

        ## Set Custom Alpha Model
        self.SetAlpha(ShareClassMeanReversionAlphaModel(tickers = tickers))

        ## Set Equal Weighting Portfolio Construction Model
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        ## Set Immediate Execution Model
        self.SetExecution(ImmediateExecutionModel())

        ## Set Null Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())


class ShareClassMeanReversionAlphaModel(AlphaModel):
    ''' Initialize helper variables for the algorithm'''

    def __init__(self, *args, **kwargs):
        self.sma = SimpleMovingAverage(10)
        self.position_window = RollingWindow[Decimal](2)
        self.alpha = None
        self.beta = None
        if 'tickers' not in kwargs:
            raise Exception('ShareClassMeanReversionAlphaModel: Missing argument: "tickers"')
        self.tickers = kwargs['tickers']
        self.position_value = None
        self.invested = False
        self.liquidate = 'liquidate'
        self.long_symbol = self.tickers[0]
        self.short_symbol = self.tickers[1]
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Minute
        self.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), 5) ## Arbitrary
        self.insight_magnitude = 0.001

    def Update(self, algorithm, data):
        insights = []

        ## Check to see if either ticker will return a NoneBar, and skip the data slice if so
        for security in algorithm.Securities:
            if self.DataEventOccured(data, security.Key):
                return insights

        ## If Alpha and Beta haven't been calculated yet, then do so
        if (self.alpha is None) or (self.beta is None):
           self.CalculateAlphaBeta(algorithm, data)
           algorithm.Log('Alpha: ' + str(self.alpha))
           algorithm.Log('Beta: ' + str(self.beta))

        ## If the SMA isn't fully warmed up, then perform an update
        if not self.sma.IsReady:
            self.UpdateIndicators(data)
            return insights

        ## Update indicator and Rolling Window for each data slice passed into Update() method
        self.UpdateIndicators(data)

        ## Check to see if the portfolio is invested. If no, then perform value comparisons and emit insights accordingly
        if not self.invested:
            if self.position_value >= self.sma.Current.Value:
                insights.append(Insight(self.long_symbol, self.prediction_interval, InsightType.Price, InsightDirection.Down, self.insight_magnitude, None))
                insights.append(Insight(self.short_symbol, self.prediction_interval, InsightType.Price, InsightDirection.Up, self.insight_magnitude, None))

                ## Reset invested boolean
                self.invested = True

            elif self.position_value < self.sma.Current.Value:
                insights.append(Insight(self.long_symbol, self.prediction_interval, InsightType.Price, InsightDirection.Up, self.insight_magnitude, None))
                insights.append(Insight(self.short_symbol, self.prediction_interval, InsightType.Price, InsightDirection.Down, self.insight_magnitude, None))

                ## Reset invested boolean
                self.invested = True

        ## If the portfolio is invested and crossed back over the SMA, then emit flat insights
        elif self.invested and self.CrossedMean():
            ## Reset invested boolean
            self.invested = False

        return Insight.Group(insights)

    def DataEventOccured(self, data, symbol):
        ## Helper function to check to see if data slice will contain a symbol
        if data.Splits.ContainsKey(symbol) or \
           data.Dividends.ContainsKey(symbol) or \
           data.Delistings.ContainsKey(symbol) or \
           data.SymbolChangedEvents.ContainsKey(symbol):
            return True

    def UpdateIndicators(self, data):
        ## Calculate position value and update the SMA indicator and Rolling Window
        self.position_value = (self.alpha * data[self.long_symbol].Close) - (self.beta * data[self.short_symbol].Close)
        self.sma.Update(data[self.long_symbol].EndTime, self.position_value)
        self.position_window.Add(self.position_value)

    def CrossedMean(self):
        ## Check to see if the position value has crossed the SMA and then return a boolean value
        if (self.position_window[0] >= self.sma.Current.Value) and (self.position_window[1] < self.sma.Current.Value):
            return True
        elif (self.position_window[0] < self.sma.Current.Value) and (self.position_window[1] >= self.sma.Current.Value):
            return True
        else:
            return False

    def CalculateAlphaBeta(self, algorithm, data):
        ## Calculate Alpha and Beta, the initial number of shares for each security needed to achieve a 50/50 weighting
        self.alpha = algorithm.CalculateOrderQuantity(self.long_symbol, 0.5)
        self.beta = algorithm.CalculateOrderQuantity(self.short_symbol, 0.5)