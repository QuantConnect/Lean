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
class ShareClassMeanReversionAlphaModel(QCAlgorithmFramework):

    def Initialize(self):

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2018, 1, 1)   #Set Start Date
        self.SetEndDate(2018, 4, 1)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        self.SetWarmUp(20)

        self.UniverseSettings.Resolution = Resolution.Minute
        tickers = ['VIA','VIAB']
        symbols = [ Symbol.Create(ticker, SecurityType.Equity, Market.USA) for ticker in tickers]

        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))

        self.SetUniverseSelection( ManualUniverseSelectionModel(symbols) )
        
        self.SetAlpha(ShareClassMeanReversionAlphaModel(long_ticker = tickers[0], short_ticker = tickers[1]))
        
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        self.SetExecution(NullExecutionModel())
        
        self.SetRiskManagement(NullRiskManagementModel())
        


class ShareClassMeanReversionAlphaModel(AlphaModel):

    def __init__(self, *args, **kwargs):
        self.SMA = SimpleMovingAverage(10)
        self.position_window = RollingWindow[Decimal](2)
        self.alpha = None
        self.beta = None
        self.position_value = None
        self.invested = False
        self.liquidate = 'liquidate'
        self.long_symbol = kwargs['long_ticker'] if 'long_ticker' in kwargs else None
        self.short_symbol = kwargs['short_ticker'] if 'short_ticker' in kwargs else None

    def Update(self, algorithm, data):
        insights = []

        for security in algorithm.Securities:
            if self.DataEventOccured(data, security.Key):
                return insights
        
        if (self.alpha is None) or (self.beta is None):
           self.calculate_alpha_beta(algorithm, data)
           algorithm.Log('Alpha: ' + str(self.alpha))
           algorithm.Log('Beta: ' + str(self.beta))
        if not self.SMA.IsReady:
            self.update_indicators(data)
            return insights
            
        self.update_indicators(data)

        if not self.invested:
            if self.position_value >= self.SMA.Current.Value:
                insights.append(Insight(self.long_symbol, timedelta(minutes=5), InsightType.Price, InsightDirection.Down, 0.01, None))
                insights.append(Insight(self.short_symbol, timedelta(minutes=5), InsightType.Price, InsightDirection.Up, 0.01, None))
                ## short long_symbol 50%
                ## long short_symbol 50%
                self.invested = True

            elif self.position_value < self.SMA.Current.Value:
                insights.append(Insight(self.long_symbol, timedelta(minutes=5), InsightType.Price, InsightDirection.Up, 0.01, None))
                insights.append(Insight(self.short_symbol, timedelta(minutes=5), InsightType.Price, InsightDirection.Down, 0.01, None))        
                ## long long_symbol 50%
                ## short short_symbol 50%
                self.invested = True
                
        elif self.invested and self.crossed_mean():
            insights.append(Insight(self.short_symbol, timedelta(minutes=5), InsightType.Price, InsightDirection.Flat, 0.01, None))
            insights.append(Insight(self.short_symbol, timedelta(minutes=5), InsightType.Price, InsightDirection.Flat, 0.01, None))
            ## Liquidate
            self.invested = False

        return insights
        
    def DataEventOccured(self, data, symbol):
        if data.Splits.ContainsKey(symbol) or \
           data.Dividends.ContainsKey(symbol) or \
           data.Delistings.ContainsKey(symbol) or \
           data.SymbolChangedEvents.ContainsKey(symbol):
            return True
            
    def update_indicators(self, data):
        self.position_value = (self.alpha * data[self.long_symbol].Close) - (self.beta * data[self.short_symbol].Close)
        self.SMA.Update(data[self.long_symbol].EndTime, self.position_value)
        self.position_window.Add(self.position_value)

    def crossed_mean(self):
        if (self.position_window[0] >= self.SMA.Current.Value) and (self.position_window[1] < self.SMA.Current.Value):
            return True
        elif (self.position_window[0] < self.SMA.Current.Value) and (self.position_window[1] >= self.SMA.Current.Value):
            return True
        else:
            return False
        
    def calculate_alpha_beta(self, algorithm, data):
        self.alpha = algorithm.CalculateOrderQuantity(self.long_symbol, 0.5)
        self.beta = algorithm.CalculateOrderQuantity(self.short_symbol, 0.5)

    def OnSecuritiesChanged(self, algorithm, changes):
        pass