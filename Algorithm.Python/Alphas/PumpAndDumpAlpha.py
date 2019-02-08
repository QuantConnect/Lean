from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Algorithm.Framework")

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Python import PythonQuandl
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Indicators import *
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

from itertools import chain
from math import ceil
from datetime import timedelta, datetime
from decimal import Decimal
from collections import deque
import pandas as pd

# Identify "pumped" penny stocks and predict that the price of a "Pumped" penny stock reverts to mean
class PumpAndDumpAlphaAlgorithm(QCAlgorithmFramework):
    ''' Alpha Streams: Benchmark Alpha: Identify "pumped" penny stocks and predict that the price of a "pumped" penny stock reverts to mean'''

    def Initialize(self):

        self.SetStartDate(2018, 1, 1)

        self.SetCash(100000)
        
        self.UniverseSettings.Resolution = Resolution.Daily

        # select stocks using PennyStockUniverseSelectionModel
        self.SetUniverseSelection(PennyStockUniverseSelectionModel())

        # Use PumpAndDumpAlphaModel to establish insights
        self.SetAlpha(PumpAndDumpAlphaModel())

        # Equally weigh securities in portfolio, based on insights
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        

class PumpAndDumpAlphaModel(AlphaModel):
    '''Uses ranking of intraday percentage difference between open price and close price to create magnitude and direction prediction for insights'''

    def __init__(self, *args, **kwargs): 
        self.lookback = kwargs['lookback'] if 'lookback' in kwargs else 1
        self.numberOfStocks = kwargs['numberOfStocks'] if 'numberOfStocks' in kwargs else 10
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), self.lookback)
        self.symbolDataBySymbol = {}

    def Update(self, algorithm, data):
        
        insights = []
        ret = []
        symbols = []
        
        activeSec = [x.Key for x in algorithm.ActiveSecurities]

        for symbol in activeSec:
            if algorithm.ActiveSecurities[symbol].HasData:
                open = algorithm.Securities[symbol].Open
                close = algorithm.Securities[symbol].Close
                if open != 0:
                    openCloseReturn = close/open - 1
                    ret.append(openCloseReturn)
                    symbols.append(symbol)
                    
                    
        # Intraday price change for penny stocks
        symbolsRet = dict(zip(symbols,ret))
        
        # Rank penny stocks on one day price change and retrieve list of ten "pumped" penny stocks
        pumpedStocks = dict(sorted(symbolsRet.items(), key=lambda kv: kv[1],reverse=True)[0:self.numberOfStocks])
        
        
        # Emit "down" insight for "pumped" penny stocks
        for key,value in pumpedStocks.items():
            insights.append(Insight.Price(key, self.predictionInterval, InsightDirection.Down, value, None))


        return insights


class PennyStockUniverseSelectionModel(FundamentalUniverseSelectionModel):
    '''Defines a universe of penny stocks, as a universe selection model for the framework algorithm.'''

    def __init__(self,
                 filterFineData = True,
                 universeSettings = None, 
                 securityInitializer = None):
        '''Initializes a new default instance of the MagicFormulaUniverseSelectionModel'''
        super().__init__(filterFineData, universeSettings, securityInitializer)
        
        # Number of stocks in Coarse and Fine Universe
        self.NumberOfSymbolsCoarse = 500
        
        self.lastMonth = -1
        self.dollarVolumeBySymbol = {}
        self.symbols = []

    def SelectCoarse(self, algorithm, coarse):
        '''Performs coarse selection for constituents.
        The stocks must have fundamental data
        The stock must have positive previous-day close price
        The stock must have volume between $1000000 and $10000 on the previous trading day
        The stock must cost less than $5'''
        coarse = list(coarse)

        if len(coarse) == 0:
            return self.symbols

        month = coarse[0].EndTime.month
        if month == self.lastMonth:
            return self.symbols

        self.lastMonth = month

        # The stocks must have fundamental data
        # The stock must have positive previous-day close price
        # The stock must have volume between $1000000 and $10000 on the previous trading day
        # The stock must cost less than $5

        filtered = [x for x in coarse if x.HasFundamentalData
                                      and  1000000 > x.Volume > 10000
                                      and 5 > x.Price > 0]
                                      
        # sort the stocks by dollar volume and take the top 500
        top = sorted(filtered, key=lambda x: x.DollarVolume, reverse=True)[:self.NumberOfSymbolsCoarse]

        self.dollarVolumeBySymbol = { i.Symbol: i.DollarVolume for i in top }

        self.symbols = list(self.dollarVolumeBySymbol.keys())

        return self.symbols
