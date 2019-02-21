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
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Indicators import *
from QuantConnect.Orders.Fees import ConstantFeeModel

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
# This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community 
# and client funds can see an example of an alpha. 
#

class MeanReversionLunchBreakAlphaAlgorithm(QCAlgorithmFramework):

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
        
        ## Set immediate execution
        self.SetExecution(ImmediateExecutionModel())

        ## Set null risk management
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
        self.lookback = kwargs['lookback'] if 'lookback' in kwargs else 1
        self.resolution = Resolution.Hour
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), self.lookback)

    def Update(self, algorithm, data):
        
        insights = []
        
        if algorithm.Time.hour != 12:
            return []
        
        # Retrieve symbols for active securities that have data
        symbols = [x.Key for x in algorithm.ActiveSecurities]

        # Retrieve price history for all securities in the security universe
        hist = algorithm.History(symbols, 4, self.resolution)
        
        # Return 'None' if no history exists
        if hist.empty:
            algorithm.Log(f"No data on {algorithm.Time}")
            return []
    
        # Get close price for securities 
        hist = hist.close.unstack(level=0)

        # Retrieve the price change from close price the previous day
        returns=hist.pct_change(periods=3).tail(1).reset_index(drop=True).to_dict()
        
        # Retrieve the mean value of returns for magnitude prediction
        mean=hist.pct_change().mean().to_dict()
        
        for symbol in list(returns):
            # Emit "down" insight for the securities that increased in value and
            # emit "up" insight for securities that have decreased in value
            direction = InsightDirection.Down if returns[symbol][0] > 0 else InsightDirection.Up
            insights.append(Insight.Price(symbol, self.predictionInterval, direction, -mean[symbol], None))
        
        return insights
