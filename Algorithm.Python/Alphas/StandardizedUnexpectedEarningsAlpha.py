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
    This model applies a momentum strategy to corporate earnings. The idea
    is that companies with strong recent earnings relative to report earnings
    from a year ago have upward momentum. Companies are ranked by Standard
    Unexpected Earnings, which is the difference in recent and previous earnings
    and scaled by the standard deviation of the interveneing quarterly earnings.
    This model takes the top 1000 companies by Dollar Volume who have Fundamental
    Data. Once the SUE ratings have been calculated, the algorithm takes a long 
    position in the top 20 stocks and a short position in the bottom 20 stocks.
    
    
    
    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.
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

from datetime import timedelta, datetime
import numpy as np

class StandardizedUnexpectedEarningsAlgorithm(QCAlgorithmFramework):

    def Initialize(self):

        self.SetStartDate(2018, 1, 1)   #Set Start Date
        self.SetEndDate(2018, 4, 1)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        self.SetWarmUp(20)

        ## Variables to help limit Universe Selection to once-per-month
        self.month = None
        self.symbols = None
        
        ## Set Manual Universe Selection
        self.UniverseSettings.Resolution = Resolution.Daily
        self.SetUniverseSelection(FineFundamentalUniverseSelectionModel(self.CoarseSelectionFunction, self.FineSelectionFunction, None, None))
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))
        
        ## Set our custom SUE Alpha Model
        self.SetAlpha(StandardizedUnexpectedEarningsAlphaModel())
        
        ## Set Equal Weighting Portfolio Construction Model
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        ## Set Immediate Execution Model
        self.SetExecution(ImmediateExecutionModel())
        
        ## Set Null Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())
       

    def CoarseSelectionFunction(self, coarse):
        ## Boolean controls so that our symbol universe is only updated once per month
        if self.Time.month == self.month:
            return self.symbols
        else:
            self.month = self.Time.month

        ## Sort by dollar volume
            sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

        ## Filter for assets with fundamental data and then take the top 1000
            filtered = [ x.Symbol for x in sortedByDollarVolume if x.HasFundamentalData ]
            self.symbols = filtered[:1000]
        
            return self.symbols
    
    
    def FineSelectionFunction(self, fine):
        ## Boolean controls so that our symbol universe is updated only at the beginning of each month
        if self.Time.month == self.month:
            return self.symbols
        else:
            ## Get the symbols from our Coarse Selection function that have the necessary data
            self.month = self.Time.monthfineFilter = sorted(fine, key=lambda x: (x.EarningReports.BasicEPS.TwelveMonths > 0) and
                                                                                (x.EarningReports.BasicEPS.ThreeMonths > 0), reverse=True)

            self.symbols = [ x.Symbol for x in fineFilter ]

            return self.symbols


class StandardizedUnexpectedEarningsAlphaModel(AlphaModel):

    def __init__(self, *args, **kwargs):
        ''' Initialize two dictionaries to help keep track of Fundamental Data
            and SUE scores'''
        self.fundamentalData = {}
        self.sue = {}
        self.insight_magnitude = 0.005
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), 15) ## Arbitrary

    def Update(self, algorithm, data):
        insights = []

        ## Look through the fundamentalData dictionary to calculate SUE for each symbol
        for symbol, fundamentals in self.fundamentalData.items():
            self.CalculateSUE(symbol, fundamentals)
        
        ## Sort the sue dictionary from highest to lowest
        symbols = sorted(self.sue, key=self.sue.get, reverse=True)
        longs = symbols[:20]
        shorts = symbols[-20:]

        ## Emith Down insights for short positions and Up insights for long positions
        for short in shorts:
            insights.append(Insight(short, self.prediction_interval, InsightType.Volatility, InsightDirection.Down, self.insight_magnitude, None))
        for long in longs:
            insights.append(Insight(long, self.prediction_interval, InsightType.Volatility, InsightDirection.Up, self.insight_magnitude, None))

        return insights
     
    def CalculateSUE(self, symbol, fundamentals):
        recent_earnings = fundamentals[0]    ## Earnings from last quarter
        year_ago_earnings = fundamentals[3]  ## Earnings from 1 year ago
        sigma = np.std(fundamentals)         ## Volatility of all earnings over this period

        ## Calculate Standardized Unexpected Earnings and populate the sue dictionary
        self.sue[symbol] = (recent_earnings - year_ago_earnings) / sigma


    def OnSecuritiesChanged(self, algorithm, changes):

        ## We loop through the added securities and extract the Fundamental Data
        ## that we need for our calculations
        symbols = [ x.Symbol for x in changes.AddedSecurities ]
        for symbol in symbols:
            if (algorithm.Securities[symbol].Fundamentals.FinancialStatements is not None) and (algorithm.Securities[symbol].Fundamentals.OperationRatios is not None):
                x = algorithm.Securities[symbol].Fundamentals
                fundamentals = [x.EarningReports.BasicEPS.ThreeMonths,
                                x.EarningReports.BasicEPS.SixMonths,
                                x.EarningReports.BasicEPS.NineMonths,
                                x.EarningReports.BasicEPS.TwelveMonths]
                self.fundamentalData[symbol] = fundamentals


