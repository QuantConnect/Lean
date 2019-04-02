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
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Portfolio import EqualWeightingPortfolioConstructionModel
from QuantConnect.Algorithm.Framework.Selection import CoarseFundamentalUniverseSelectionModel

#
# Academic research suggests that stock market participants generally place their orders at the market open and close.
# Intraday trading volume is J-Shaped, where the minimum trading volume of the day is during lunch-break. Stocks become
# more volatile as order flow is reduced and tend to mean-revert during lunch-break.
#
# This alpha aims to capture the mean-reversion effect of ETFs during lunch-break by ranking 20 ETFs
# on their return between the close of the previous day to 12:00 the day after and predicting mean-reversion
# in price during lunch-break.
#
# Source:  Lunina, V. (June 2011). The Intraday Dynamics of Stock Returns and Trading Activity: Evidence from OMXS 30 (Master's Essay, Lund University).
# Retrieved from http://lup.lub.lu.se/luur/download?func=downloadFile&recordOId=1973850&fileOId=1973852
#
# This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha.
#

class MeanReversionLunchBreakAlpha(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2018, 1, 1)

        self.SetCash(100000)

        # Set zero transaction fees
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))

        # Use Hourly Data For Simplicity
        self.UniverseSettings.Resolution = Resolution.Hour
        self.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelectionFunction))

        # Use MeanReversionLunchBreakAlphaModel to establish insights
        self.SetAlpha(MeanReversionLunchBreakAlphaModel())

        # Equally weigh securities in portfolio, based on insights
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        # Set Immediate Execution Model
        self.SetExecution(ImmediateExecutionModel())

        # Set Null Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())

    # Sort the data by daily dollar volume and take the top '20' ETFs
    def CoarseSelectionFunction(self, coarse):
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)
        filtered = [ x.Symbol for x in sortedByDollarVolume if not x.HasFundamentalData ]
        return filtered[:20]


class MeanReversionLunchBreakAlphaModel(AlphaModel):
    '''Uses the price return between the close of previous day to 12:00 the day after to
    predict mean-reversion of stock price during lunch break and creates direction prediction
    for insights accordingly.'''

    def __init__(self, *args, **kwargs):
        lookback = kwargs['lookback'] if 'lookback' in kwargs else 1
        self.resolution = Resolution.Hour
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), lookback)
        self.symbolDataBySymbol = dict()

    def Update(self, algorithm, data):

        for symbol, symbolData in self.symbolDataBySymbol.items():
            if data.Bars.ContainsKey(symbol):
                bar = data.Bars.GetValue(symbol)
                symbolData.Update(bar.EndTime, bar.Close)

        return [] if algorithm.Time.hour != 12 else \
               [x.Insight for x in self.symbolDataBySymbol.values()]

    def OnSecuritiesChanged(self, algorithm, changes):
        for security in changes.RemovedSecurities:
            self.symbolDataBySymbol.pop(security.Symbol, None)

        # Retrieve price history for all securities in the security universe
        # and update the indicators in the SymbolData object
        symbols = [x.Symbol for x in changes.AddedSecurities]
        history = algorithm.History(symbols, 1, self.resolution)
        if history.empty:
            algorithm.Debug(f"No data on {algorithm.Time}")
            return
        history = history.close.unstack(level = 0)

        for ticker, values in history.iteritems():
            symbol = next((x for x in symbols if str(x) == ticker ), None)
            if symbol in self.symbolDataBySymbol or symbol is None: continue
            self.symbolDataBySymbol[symbol] = self.SymbolData(symbol, self.predictionInterval)
            self.symbolDataBySymbol[symbol].Update(values.index[0], values[0])


    class SymbolData:
        def __init__(self, symbol, period):
            self.symbol = symbol
            self.period = period
            # Mean value of returns for magnitude prediction
            self.meanOfPriceChange = IndicatorExtensions.SMA(RateOfChangePercent(1),3)
            # Price change from close price the previous day
            self.priceChange = RateOfChangePercent(3)

        def Update(self, time, value):
            return self.meanOfPriceChange.Update(time, value) and \
                   self.priceChange.Update(time, value)

        @property
        def Insight(self):
            direction = InsightDirection.Down if self.priceChange.Current.Value > 0 else InsightDirection.Up
            margnitude = abs(self.meanOfPriceChange.Current.Value)
            return Insight.Price(self.symbol, self.period, direction, margnitude, None)