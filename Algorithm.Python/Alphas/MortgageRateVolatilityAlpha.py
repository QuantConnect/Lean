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
    This Alpha Model uses Wells Fargo 30-year Fixed Rate Mortgage data from Quandl to 
    generate Insights about the movement of Real Estate ETFs. Mortgage rates can provide information 
    regarding the general price trend of real estate, and ETFs provide good continuous-time instruments 
    to measure the impact against. Volatility in mortgage rates tends to put downward pressure on real 
    estate prices, whereas stable mortgage rates, regardless of true rate, lead to stable or higher real
    estate prices. This Alpha model seeks to take advantage of this correlation by emitting insights
    based on volatility and rate deviation from its historic mean.

    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.
'''

from clr import AddReference
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Indicators")

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Selection import *


class MortgageRateVolatilityAlgorithm(QCAlgorithmFramework):

    def Initialize(self):

        # Set requested data resolution
        self.SetStartDate(2017, 1, 1)   #Set Start Date
        self.SetCash(100000)           #Set Strategy Cash

        self.UniverseSettings.Resolution = Resolution.Daily
        
        ## Universe of six liquid real estate ETFs
        etfs = ['VNQ', 'REET', 'TAO', 'FREL', 'SRET', 'HIPS']
        symbols = [ Symbol.Create(etf, SecurityType.Equity, Market.USA) for etf in etfs ]
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))
        self.SetUniverseSelection( ManualUniverseSelectionModel(symbols) )
            
        self.SetAlpha(MortgageRateVolatilityAlphaModel(self))
        
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel()) 
        
        self.SetExecution(ImmediateExecutionModel())
        
        self.SetRiskManagement(NullRiskManagementModel())


class MortgageRateVolatilityAlphaModel(AlphaModel):
    
    def __init__(self, algorithm, indicatorPeriod = 15, insightMagnitude = 0.005, deviations = 2):
        ## Add Quandl data for a Well's Fargo 30-year Fixed Rate mortgage
        self.mortgageRate = algorithm.AddData(QuandlMortgagePriceColumns, 'WFC/PR_GOV_30YFIXEDVA_APR').Symbol
        self.indicatorPeriod = indicatorPeriod
        self.insightDuration = TimeSpan.FromDays(indicatorPeriod)
        self.insightMagnitude = insightMagnitude
        self.deviations = deviations
        
        ## Add indicators for the mortgage rate -- Standard Deviation and Simple Moving Average
        self.mortgageRateStd = algorithm.STD(self.mortgageRate.Value, indicatorPeriod)
        self.mortgageRateSma = algorithm.SMA(self.mortgageRate.Value, indicatorPeriod)
        
        ## Use a history call to warm-up the indicators
        self.WarmupIndicators(algorithm)
    
    def Update(self, algorithm, data):
        insights = []
        
        ## Return empty list if data slice doesn't contain monrtgage rate data
        if self.mortgageRate not in data.Keys:
            return []

        ## Extract current mortgage rate, the current STD indicator value, and current SMA value
        mortgageRate = data[self.mortgageRate].Value
        deviation = self.deviations * self.mortgageRateStd.Current.Value
        sma = self.mortgageRateSma.Current.Value
        
        ## If volatility in mortgage rates is high, then we emit an Insight to sell
        if (mortgageRate < sma - deviation) or (mortgageRate > sma + deviation):
            ## Emit insights for all securities that are currently in the Universe,
            ## except for the Quandl Symbol
            insights = [Insight(security, self.insightDuration, InsightType.Price, InsightDirection.Down, self.insightMagnitude, None) \
                        for security in algorithm.ActiveSecurities.Keys if security != self.mortgageRate]
        
        ## If volatility in mortgage rates is low, then we emit an Insight to buy
        if (mortgageRate < sma - deviation/2) or (mortgageRate > sma + deviation/2):
            insights = [Insight(security, self.insightDuration, InsightType.Price, InsightDirection.Up, self.insightMagnitude, None) \
                        for security in algorithm.ActiveSecurities.Keys if security != self.mortgageRate]
        
        return insights
    
    def WarmupIndicators(self, algorithm):
        ## Make a history call and update the indicators
        history = algorithm.History(self.mortgageRate, self.indicatorPeriod, Resolution.Daily)
        for index, row in history.iterrows():
            self.mortgageRateStd.Update(index[1], row['value'])
            self.mortgageRateSma.Update(index[1], row['value'])
    
class QuandlMortgagePriceColumns(PythonQuandl):
    
    def __init__(self):
        ## Rename the Quandl object column to the data we want, which is the 'Value' column
        ## of the CSV that our API call returns
        self.ValueColumnName = "Value"