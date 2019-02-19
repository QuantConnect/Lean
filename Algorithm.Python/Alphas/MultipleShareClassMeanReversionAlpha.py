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
    A number of companies publicly trade two different classes of shares
    in US equity markets. If both assets trade with reasonable volume, then
    the underlying driving forces of each should be similar or the same. Given
    this, we can create a relatively dollar-netural long/short portfolio using
    the dual share classes. Theoretically, any deviation of this portfolio from
    its mean-value should be corrected, and so the motivating idea is based on
    mean-reversion. Using a Simple Moving Average indicator, we can
    compare the value of this portfolio against its SMA and generate insights
    to buy the under-valued symbol and sell the over-valued symbol.
'''


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

from datetime import datetime, timedelta
import pandas as pd
import numpy as np

class ShareClassMeanReversionAlphaModel(QCAlgorithmFramework):

    def Initialize(self):

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2018, 2, 18)   #Set Start Date
        self.SetCash(100000)           #Set Strategy Cash
        self.SetWarmUp(20)
        
        ## Set the universe resolution and select a basket of dual-class shares
        self.UniverseSettings.Resolution = Resolution.Minute
        tickers = ['VIA','VIAB','GOOG','GOOGL','COKE','KO','LEXEA','EXPE','TRIP','LTRPA']
        ticker_pairs = [['VIA','VIAB'],['GOOG','GOOGL'],['COKE','KO'],['LEXEA','EXPE'],['TRIP','LTRPA']]
        
        ## Create Symbols for manual universe selection
        symbols = [ Symbol.Create(ticker, SecurityType.Equity, Market.USA) for ticker in tickers]

        self.SetUniverseSelection( ManualUniverseSelectionModel(symbols) )
        
        self.SetAlpha(ShareClassMeanReversionAlphaModel(ticker_pairs = ticker_pairs, number_pairs = len(ticker_pairs), ticker_list = tickers))
        
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        self.SetExecution(ImmediateExecutionModel())
        
        self.SetRiskManagement(NullRiskManagementModel())
        


class ShareClassMeanReversionAlphaModel(AlphaModel):

    def __init__(self, *args, **kwargs):
        ''' Initialize variables and dictionaries to help with SMA and Symbol Date
        object creation/maintenance'''
        self.symbolDataBySymbol = {}
        self.sma_period = 15
        self.period = timedelta(minutes=5)
        self.number_pairs = kwargs['number_pairs'] if 'number_pairs' in kwargs else None
        self.ticker_pairs = kwargs['ticker_pairs'] if 'ticker_pairs' in kwargs else None
        self.ticker_list = kwargs['ticker_list'] if 'ticker_list' in kwargs else None
        self.initialized = False

    def Update(self, algorithm, data):

        ## Initialize our Symbol Data objects
        if not self.initialized:
            self.Initialize(algorithm, data)
        
        ## Create insights vector to be returned at end of Update() method
        insights = []

        ## Check to see if a security has paid a distribution, split, etc.
        ## If so, this would fail to return a TradeBar object and so skip it
        for security in algorithm.Securities:
            if self.DataEventOccured(data, security.Key):
                return insights
        
        ## Iterate through the Symbol Data dictionary, where each element is
        ## one of the long/short positions
        for symbol, symbolData in self.symbolDataBySymbol.items():
            
            ## Update position value, indicators, and rolling window
            symbolData.Update(data[symbolData.long_symbol], data[symbolData.short_symbol], False, data.Time)

            ## If the portfolio is not invested, then we want to evaluation where the portfolio value is relative
            ## to the SMA and generate insights accordingly
            if not symbolData.Invested:
                if symbolData.PositionAboveMean:
                    insights.append(Insight.Price(symbolData.long_symbol, self.period, InsightDirection.Down, 0.01, None))
                    insights.append(Insight.Price(symbolData.short_symbol, self.period, InsightDirection.Up, 0.01, None))
                    symbolData.UpdateInvestment(True)

                else:
                    insights.append(Insight.Price(symbolData.long_symbol, self.period, InsightDirection.Up, 0.01, None))
                    insights.append(Insight.Price(symbolData.short_symbol, self.period, InsightDirection.Down, 0.01, None))        
                    symbolData.UpdateInvestment(True)
            
            ## If the portfolio is invested and the position value has crossed the mean, then liquidate
            ## and reset the position, so the algorithm emits flat insights
            elif symbolData.Invested and symbolData.CrossedMean:
                insights.append(Insight.Price(symbolData.long_symbol, self.period, InsightDirection.Flat, 0.01, None))
                insights.append(Insight.Price(symbolData.short_symbol, self.period, InsightDirection.Flat, 0.01, None))
                symbolData.UpdateInvestment(False)

        return insights
        
    def DataEventOccured(self, data, symbol):
        if data.Splits.ContainsKey(symbol) or \
           data.Dividends.ContainsKey(symbol) or \
           data.Delistings.ContainsKey(symbol) or \
           data.SymbolChangedEvents.ContainsKey(symbol):
            return True

    def Initialize(self, algorithm, data):
        
        ## History request for all the symbols in the universe
        history = algorithm.History(self.ticker_list, self.sma_period, Resolution.Minute)

        ## The algorithm employs a string indexer to create entries in the Symbol Data dictionary,
        ## and so it iterates through the total number of pairs rather than individual symbols
        for i in range(self.number_pairs):
            indexer = str(i)
            symbols = self.ticker_pairs[i]  ## Get the symbols associated with the i-th pair
            
            symbolData = SymbolData(indexer, symbols, self.sma_period) ## Create the Symbol Data object
            self.symbolDataBySymbol[indexer] = symbolData
            symbolData.CalculateAlphaBeta(algorithm)      ## Make the initial calculations for alpha/beta
            symbolData.WarmUpIndicators(indexer, history) ## Warm up the SMA and rolling window

        self.initialized = True

    def OnSecuritiesChanged(self, algorithm, changes):
        
        ## Set fees to $0 to mimic High-Frequency Trading
        for security in changes.AddedSecurities:
            security.FeeModel = ConstantFeeModel(0)
 

class SymbolData:
    def __init__(self, indexer, symbol_pair, lookback):
        self.Symbol = str(indexer)
        self.alpha = 0
        self.beta = 0
        self.position_value = 0
        self.invested = False
        self.long_symbol = symbol_pair[0]  ## Ticker of the initial 'long' security
        self.short_symbol = symbol_pair[1] ## Ticker of the initial 'short' security
        self.window = RollingWindow[Decimal](2)
        self.SMA = SimpleMovingAverage('{}.SMA({})'.format(indexer, lookback), lookback)
                
    def WarmUpIndicators(self, indexer, history):
        long_data = history.loc[self.long_symbol]    ## Retrieve the historical data for the long-symbol
        short_data = history.loc[self.short_symbol]  ## Retrieve the historical data for the short-symbol
        
        ## Iterate through each entry to update the position value and SMA
        for i in range(long_data.shape[0]):
            position_value = self.Update(long_data.iloc[i], short_data.iloc[i], True, pd.to_datetime(long_data.index[i].to_pydatetime()))
            self.SMA.Update(long_data.iloc[i].name, position_value)

    def Update(self, long_data, short_data, tuple_used, time):
        
        ## Condition check to see if the SymbolData Update() method is using tuples from a historical data request or
        ## data in the Alphs Update() method
        if tuple_used:
            self.position_value = (self.alpha * long_data.close) - (self.beta * short_data.close)  ## Calculate value of the long/short portfolio
        else:
            self.position_value = (self.alpha * long_data.Close) - (self.beta * short_data.Close)  ## Calculate value of the long/short portfolio
        
        self.window.Add(self.position_value)
        self.SMA.Update(time, self.position_value)
        
        return self.position_value

    def CalculateAlphaBeta(self, algorithm):
        
        ## This calculates the number of shares needed to buy/sell of each symbol so that each has a 50% weighting in the total position
        self.alpha = algorithm.CalculateOrderQuantity(self.long_symbol, 0.5)
        self.beta = algorithm.CalculateOrderQuantity(self.short_symbol, 0.5)
        
    def UpdateInvestment(self, boolean):
        ## Simple update to the boolean invested condition
        self.invested = boolean

    @property
    def CrossedMean(self):
        
        ## Return boolean value depending on whether or not the position has crossed over the SMA
        if (self.window[0] >= self.SMA.Current.Value) and (self.window[1] < self.SMA.Current.Value):
            return True
        elif (self.window[0] < self.SMA.Current.Value) and (self.window[1] >= self.SMA.Current.Value):
            return True
        else:
            return False

    @property
    def SMAValue(self):
        return self.SMA.Current.Value

    @property
    def PositionValue(self):
        return self.position_value
        
    @property
    def Invested(self):
        return self.invested
        
    @property
    def PositionAboveMean(self):
        return self.position_value >= self.SMA.Current.Value

<br><br>This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha. You can read the source code for this alpha on Github in  <a href="https://github.com/QuantConnect/Lean/blob/master/Algorithm.CSharp/Alphas/MultipleShareClassMeanReversionAlpha.cs">C#</a> or  <a href="https://github.com/QuantConnect/Lean/blob/master/Algorithm.Python/Alphas/MultipleShareClassMeanReversionAlpha.py">Python</a>.