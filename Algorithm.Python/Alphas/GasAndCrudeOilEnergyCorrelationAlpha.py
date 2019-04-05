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
    Energy prices, especially Oil and Natural Gas, are in general fairly correlated,
    meaning they typically move in the same direction as an overall trend. This Alpha
    uses this idea and implements an Alpha Model that takes Natural Gas ETF price
    movements as a leading indicator for Crude Oil ETF price movements. We take the
    Natural Gas/Crude Oil ETF pair with the highest historical price correlation and
    then create insights for Crude Oil depending on whether or not the Natural Gas ETF price change
    is above/below a certain threshold that we set (arbitrarily).



    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.
'''

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Algorithm.Framework")

from System import *
from QuantConnect import *
from QuantConnect.Orders import OrderStatus
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *
import pandas as pd
from datetime import timedelta

class GasAndCrudeOilEnergyCorrelationAlpha(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2018, 1, 1)   #Set Start Date
        self.SetCash(100000)            #Set Strategy Cash

        natural_gas = [Symbol.Create(x, SecurityType.Equity, Market.USA) for x in ['UNG','BOIL','FCG']]
        crude_oil = [Symbol.Create(x, SecurityType.Equity, Market.USA) for x in ['USO','UCO','DBO']]

        ## Set Universe Selection
        self.UniverseSettings.Resolution = Resolution.Minute
        self.SetUniverseSelection( ManualUniverseSelectionModel(natural_gas + crude_oil) )
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))

        ## Custom Alpha Model
        self.SetAlpha(PairsAlphaModel(leading = natural_gas, following = crude_oil, history_days = 90, resolution = Resolution.Minute))

        ## Equal-weight our positions, in this case 100% in USO
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(resolution = Resolution.Minute))

        ## Immediate Execution Fill Model
        self.SetExecution(CustomExecutionModel())

        ## Null Risk-Management Model
        self.SetRiskManagement(NullRiskManagementModel())

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            self.Debug(f'Purchased Stock: {orderEvent.Symbol}')

    def OnEndOfAlgorithm(self):
        for kvp in self.Portfolio:
            if kvp.Value.Invested:
                self.Log(f'Invested in: {kvp.Key}')


class PairsAlphaModel:
    '''This Alpha model assumes that the ETF for natural gas is a good leading-indicator
        of the price of the crude oil ETF. The model will take in arguments for a threshold
        at which the model triggers an insight, the length of the look-back period for evaluating
        rate-of-change of UNG prices, and the duration of the insight'''

    def __init__(self, *args, **kwargs):
        self.leading = kwargs.get('leading', [])
        self.following = kwargs.get('following', [])
        self.history_days = kwargs.get('history_days', 90) ## In days
        self.lookback = kwargs.get('lookback', 5)
        self.resolution = kwargs.get('resolution', Resolution.Hour)
        self.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), 5) ## Arbitrary
        self.difference_trigger = kwargs.get('difference_trigger', 0.75)
        self.symbolDataBySymbol = {}
        self.next_update = None

    def Update(self, algorithm, data):

        if (self.next_update is None) or (algorithm.Time > self.next_update):
            self.CorrelationPairsSelection()
            self.next_update = algorithm.Time + timedelta(30)

        magnitude = round(self.pairs[0].Return / 100, 6)

        ## Check if Natural Gas returns are greater than the threshold we've set
        if self.pairs[0].Return > self.difference_trigger:
            return [Insight.Price(self.pairs[1].Symbol, self.prediction_interval, InsightDirection.Up, magnitude)]
        if self.pairs[0].Return < -self.difference_trigger:
            return [Insight.Price(self.pairs[1].Symbol, self.prediction_interval, InsightDirection.Down, magnitude)]

        return []

    def CorrelationPairsSelection(self):
        ## Get returns for each natural gas/oil ETF
        daily_return = {}
        for symbol, symbolData in self.symbolDataBySymbol.items():
            daily_return[symbol] = symbolData.DailyReturnArray

        ## Estimate coefficients of different correlation measures
        tau = pd.DataFrame.from_dict(daily_return).corr(method='kendall')

        ## Calculate the pair with highest historical correlation
        max_corr = -1
        for x in self.leading:
            df = tau[[x]].loc[self.following]
            corr = float(df.max())
            if corr > max_corr:
                self.pairs = (
                    self.symbolDataBySymbol[x],
                    self.symbolDataBySymbol[df.idxmax()[0]])
                max_corr = corr

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for removed in changes.RemovedSecurities:
            symbolData = self.symbolDataBySymbol.pop(removed.Symbol, None)
            if symbolData is not None:
                symbolData.RemoveConsolidators(algorithm)

        # initialize data for added securities
        symbols = [ x.Symbol for x in changes.AddedSecurities ]
        history = algorithm.History(symbols, self.history_days + 1, Resolution.Daily)
        if history.empty: return

        tickers = history.index.levels[0]
        for ticker in tickers:
            symbol = SymbolCache.GetSymbol(ticker)
            if symbol not in self.symbolDataBySymbol:
                symbolData = SymbolData(symbol, self.history_days, self.lookback, self.resolution, algorithm)
                self.symbolDataBySymbol[symbol] = symbolData
                symbolData.UpdateDailyRateOfChange(history.loc[ticker])

        history = algorithm.History(symbols, self.lookback, self.resolution)
        if history.empty: return
        for ticker in tickers:
            symbol = SymbolCache.GetSymbol(ticker)
            if symbol in self.symbolDataBySymbol:
                self.symbolDataBySymbol[symbol].UpdateRateOfChange(history.loc[ticker])

class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, symbol, dailyLookback, lookback, resolution, algorithm):
        self.Symbol = symbol

        self.dailyReturn = RateOfChangePercent('f{symbol}.DailyROCP({1})', 1)
        self.dailyConsolidator = algorithm.ResolveConsolidator(symbol, Resolution.Daily)
        self.dailyReturnHistory = RollingWindow[IndicatorDataPoint](dailyLookback)

        def updatedailyReturnHistory(s, e):
            self.dailyReturnHistory.Add(e)

        self.dailyReturn.Updated += updatedailyReturnHistory
        algorithm.RegisterIndicator(symbol, self.dailyReturn, self.dailyConsolidator)

        self.rocp = RateOfChangePercent(f'{symbol}.ROCP({lookback})', lookback)
        self.consolidator = algorithm.ResolveConsolidator(symbol, resolution)
        algorithm.RegisterIndicator(symbol, self.rocp, self.consolidator)

    def RemoveConsolidators(self, algorithm):
        algorithm.SubscriptionManager.RemoveConsolidator(self.Symbol, self.consolidator)
        algorithm.SubscriptionManager.RemoveConsolidator(self.Symbol, self.dailyConsolidator)

    def UpdateRateOfChange(self, history):
        for tuple in history.itertuples():
            self.rocp.Update(tuple.Index, tuple.close)

    def UpdateDailyRateOfChange(self, history):
        for tuple in history.itertuples():
            self.dailyReturn.Update(tuple.Index, tuple.close)

    @property
    def Return(self):
        return float(self.rocp.Current.Value)

    @property
    def DailyReturnArray(self):
        return pd.Series({x.EndTime: x.Value for x in self.dailyReturnHistory})

    def __repr__(self):
        return f"{self.rocp.Name} - {Return}"


class CustomExecutionModel(ExecutionModel):
    '''Provides an implementation of IExecutionModel that immediately submits market orders to achieve the desired portfolio targets'''

    def __init__(self):
        '''Initializes a new instance of the ImmediateExecutionModel class'''
        self.targetsCollection = PortfolioTargetCollection()
        self.previous_symbol = None

    def Execute(self, algorithm, targets):
        '''Immediately submits orders for the specified portfolio targets.
        Args:
            algorithm: The algorithm instance
            targets: The portfolio targets to be ordered'''

        self.targetsCollection.AddRange(targets)

        for target in self.targetsCollection.OrderByMarginImpact(algorithm):
            open_quantity = sum([x.Quantity for x in algorithm.Transactions.GetOpenOrders(target.Symbol)])
            existing = algorithm.Securities[target.Symbol].Holdings.Quantity + open_quantity
            quantity = target.Quantity - existing
            ## Liquidate positions in Crude Oil ETF that is no longer part of the highest-correlation pair
            if (str(target.Symbol) != str(self.previous_symbol)) and (self.previous_symbol is not None):
                algorithm.Liquidate(self.previous_symbol)
            if quantity != 0:
                algorithm.MarketOrder(target.Symbol, quantity)
                self.previous_symbol = target.Symbol
        self.targetsCollection.ClearFulfilled(algorithm)