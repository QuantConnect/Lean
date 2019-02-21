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
from QuantConnect.Orders import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Python import PythonQuandl
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Indicators import *
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

# 
# Equity indices exhibit mean reversion in daily returns. The Internal Bar Strength indicator (IBS), 
# which relates the closing price of a security to its daily range can be used to identify overbought
# and oversold securities.
#
# This alpha ranks 33 global equity ETFs on its IBS value the previous day and predicts for the following day
# that the ETF with the highest IBS value will decrease in price, and the ETF with the lowest IBS value 
# will increase in price.
#
# Source: Kakushadze, Zura, and Juan Andrés Serur. “4. Exchange-Traded Funds (ETFs).” 151 Trading Strategies, Palgrave Macmillan, 2018, pp. 90–91.
#
# <br><br>This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha. 
# You can read the source code for this alpha on Github in <a href="https://github.com/QuantConnect/Lean/blob/master/Algorithm.CSharp/Alphas/GlobalEquityMeanReversionIBSAlpha.cs">C#</a>
# or <a href="https://github.com/QuantConnect/Lean/blob/master/Algorithm.Python/Alphas/GlobalEquityMeanReversionIBSAlpha.py">Python</a>.
# 

class GlobalEquityMeanReversionIBSAlphaAlgorithm(QCAlgorithmFramework):

    def Initialize(self):

        self.SetStartDate(2018, 1, 1)

        self.SetCash(100000)
        
        # Set zero transaction fees
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))
        
        # Global Equity ETF tickers
        tickers = ["ECH","EEM","EFA","EPHE","EPP","EWA","EWC","EWG",
                   "EWH","EWI","EWJ","EWL","EWM","EWM","EWO","EWP",
                   "EWQ","EWS","EWT","EWU","EWY","EWZ","EZA","FXI",
                   "GXG","IDX","ILF","EWM","QQQ","RSX","SPY","THD"]           
                   
        symbols = [Symbol.Create(ticker, SecurityType.Equity, Market.USA) for ticker in tickers]
        
        # Manually curated universe
        self.UniverseSettings.Resolution = Resolution.Daily
        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols))
            
        # Use GlobalEquityMeanReversionAlphaModel to establish insights
        self.SetAlpha(MeanReversionIBSAlphaModel())

        # Equally weigh securities in portfolio, based on insights
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        ## Set immediate execution
        self.SetExecution(ImmediateExecutionModel())

        ## Set null risk management
        self.SetRiskManagement(NullRiskManagementModel())

class MeanReversionIBSAlphaModel(AlphaModel):
    '''Uses ranking of Internal Bar Strength (IBS) to create direction prediction for insights'''

    def __init__(self, *args, **kwargs): 
        self.lookback = kwargs['lookback'] if 'lookback' in kwargs else 1
        self.numberOfStocks = kwargs['numberOfStocks'] if 'numberOfStocks' in kwargs else 2
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), self.lookback)
        
    def Update(self, algorithm, data):
        
        insights = []
        symbolsIBS = dict()
        returns = dict()
        
        for security in algorithm.ActiveSecurities.Values:
            if security.HasData:
                high = security.High
                low = security.Low
                hilo = high - low
                
                # Do not consider symbol with zero open and avoid division by zero
                if security.Open * hilo != 0:
                    # Internal bar strength (IBS)
                    symbolsIBS[security.Symbol] = (security.Close-low)/hilo
                    returns[security.Symbol] = security.Close/security.Open-1
        
        # Number of stocks cannot be higher than half of symbolsIBS length
        number_of_stocks = min(int(len(symbolsIBS)/2), self.numberOfStocks)
        if number_of_stocks == 0:
            return []

        # Rank and retrieve the securities with the highest IBS value
        highIBS = dict(sorted(symbolsIBS.items(), key=lambda kv: kv[1],reverse=True)[0:number_of_stocks])

        # Rank and retrieve the securities with the lowest IBS value
        lowIBS = dict(sorted(symbolsIBS.items(), key=lambda kv: kv[1],reverse=False)[0:number_of_stocks])

        # Emit "down" insight for the securities with the highest IBS value
        for key,value in highIBS.items():
            insights.append(Insight.Price(key, self.predictionInterval, InsightDirection.Down, -returns[key], None))

        # Emit "up" insight for the securities with the lowest IBS value
        for key,value in lowIBS.items():
            insights.append(Insight.Price(key, self.predictionInterval, InsightDirection.Up, -returns[key], None))

        return insights
