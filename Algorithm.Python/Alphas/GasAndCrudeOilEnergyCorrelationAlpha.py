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
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *

import numpy as np
from scipy import stats
from scipy.stats import kendalltau 
from datetime import timedelta, datetime

class EnergyETFPairsTradingAlgorithm(QCAlgorithmFramework):

    def Initialize(self):
        
        self.SetStartDate(2018, 1, 1)   #Set Start Date
        self.SetCash(100000)           #Set Strategy Cash
        
        natural_gas = ['UNG','BOIL','FCG']
        crude_oil = ['USO','UCO','DBO']
        symbols = [ Symbol.Create(ticker, SecurityType.Equity, Market.USA) for ticker in natural_gas + crude_oil ]
        
        ## Set Universe Selection
        self.UniverseSettings.Resolution = Resolution.Minute
        self.SetUniverseSelection( ManualUniverseSelectionModel(symbols) )
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
            self.Debug("Purchased Stock: {0}".format(orderEvent.Symbol))
            
    def OnEndOfAlgorithm(self):
        for kvp in self.Portfolio:
            if self.Portfolio[kvp.Key].Invested:
                self.Log('Invested in: ' + str(kvp.Key))


class PairsAlphaModel:
    '''This Alpha model assumes that the ETF for natural gas is a good leading-indicator
        of the price of the crude oil ETF. The model will take in arguments for a threshold
        at which the model triggers an insight, the length of the look-back period for evaluating
        rate-of-change of UNG prices, and the duration of the insight'''
    
    def __init__(self, *args, **kwargs):
        
        self.difference_trigger = kwargs['difference_trigger'] if 'difference_trigger' in kwargs else 0.75
        self.lookback = kwargs['lookback'] if 'lookback' in kwargs else 5
        self.history_days = kwargs['history_days'] if 'history_days' in kwargs else 90 ## In days
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Hour
        self.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), 5) ## Arbitrary
        self.symbolDataBySymbol = {}
        self.next_update = None
        self.leading = kwargs['leading'] if 'leading' in kwargs else None
        self.following = kwargs['following'] if 'following' in kwargs else None
        self.tickers = self.leading + self.following
        self.ticker_list_of_lists = [[],[]]
        for i in range(len(self.leading)):
            for j in range(len(self.following)):
                self.ticker_list_of_lists[0].append(self.leading[i])
        self.ticker_list_of_lists[1] = self.following * 3

    def Update(self, algorithm, data):
        
        if (self.next_update is None) or (algorithm.Time > self.next_update):
            self.pairs = self.CorrelationPairsSelection(algorithm)
            self.next_update = algorithm.Time + (timedelta(days = 30))
            
        ## Build a list to hold our insights
        insights = []
        
        ## These lists hold the Symbol for the following ETF and data from the leading ETF that we need to pass into the Insight() constructor
        leading_data = []       
        following_symbol = []    
        
        for symbol, symbolData in self.symbolDataBySymbol.items():
            if symbol.Value == self.pairs[0]:
                leading_data.append(symbolData)             ## Add symbol data if it's Natural Gas
            elif symbol.Value == self.pairs[1]:
                following_symbol.append(symbol)             ## Stash the Symbol object if it's Crude Oil
        
        for i in range(len(following_symbol)):
            symbolData = leading_data[i]
            if symbolData.Return > self.difference_trigger:      ## Check if Natural Gas returns are greater than the threshold we've set
                ## If so, create and Insight with this information for Crude Oil
                insights.append(Insight(following_symbol[i], self.prediction_interval, InsightType.Price, InsightDirection.Up, symbolData.Return/100, None))

            elif symbolData.Return < -self.difference_trigger:   ## Check if UNG returns are greater than the threshold we've set
                insights.append(Insight(following_symbol[i], self.prediction_interval, InsightType.Price, InsightDirection.Down, symbolData.Return/100, None))

        return insights
    
    def CorrelationPairsSelection(self, algorithm):
        tick_syl = self.ticker_list_of_lists
        tickers = self.tickers
        logreturn={}
        ## Get log returns for each natural gas/oil ETF pair
        unique = list(set([item for sublist in self.ticker_list_of_lists for item in sublist]))
        df = algorithm.History(unique, self.history_days, Resolution.Daily)
        df = df['close'].unstack(level=0)
        df = (np.log(df) - np.log(df.shift(1))).dropna()
        for tick in tickers:
            logreturn[tick] = df[[tick]]
        
        ## Estimate coefficients of different correlation measures 
        tau_coef = []
        for i in range(len(tick_syl[0])):
            tik_x, tik_y= logreturn[tick_syl[0][i]], logreturn[tick_syl[1][i]]
            min_length = min(len(tik_x), len(tik_y))
            tau_coef.append(kendalltau(tik_x[:min_length], tik_y[:min_length])[0])
        index_max = tau_coef.index(max(tau_coef))    
        pair = [tick_syl[0][index_max],tick_syl[1][index_max]]
        
        ## Return the pair with highest historical correlation
        return pair
        
    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for removed in changes.RemovedSecurities:
            algorithm.Log('Removed: ' + str(removed.Symbol))
            symbolData = self.symbolDataBySymbol.pop(removed.Symbol, None)
            if symbolData is not None:
                symbolData.RemoveConsolidators(algorithm)

        # initialize data for added securities
        symbols = [ x.Symbol for x in changes.AddedSecurities ]
        history = algorithm.History(symbols, self.lookback, self.resolution)
        if history.empty: return

        tickers = history.index.levels[0]
        for ticker in tickers:
            symbol = SymbolCache.GetSymbol(ticker)

            if symbol not in self.symbolDataBySymbol:
                symbolData = SymbolData(symbol, self.lookback, self.resolution, algorithm)
                self.symbolDataBySymbol[symbol] = symbolData
                symbolData.WarmUpIndicators(history.loc[ticker])
        
class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, symbol, lookback, resolution, algorithm):
        self.Symbol = symbol
        self.ROCP = RateOfChangePercent('{}.ROCP({})'.format(symbol, lookback), lookback)
        self.Consolidator = algorithm.ResolveConsolidator(self.Symbol, resolution)
        algorithm.RegisterIndicator(symbol, self.ROCP, self.Consolidator)
        self.previous = 0

    def RemoveConsolidators(self, algorithm):
        if self.Consolidator is not None:
            algorithm.SubscriptionManager.RemoveConsolidator(self.Symbol, self.Consolidator)

    def WarmUpIndicators(self, history):
        for tuple in history.itertuples():
            self.ROCP.Update(tuple.Index, tuple.close)

    @property
    def Return(self):
        return float(self.ROCP.Current.Value)

    @property
    def CanEmit(self):
        if self.previous == self.ROCP.Samples:
            return False

        self.previous = self.ROCP.Samples
        return self.ROCP.IsReady

    def __str__(self, **kwargs):
        return '{}: {:.2%}'.format(self.ROCP.Name, (1 + self.Return)**252 - 1)
        

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