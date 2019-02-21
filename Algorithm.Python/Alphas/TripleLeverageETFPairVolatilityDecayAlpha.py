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

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Algorithm.Framework import QCAlgorithmFramework

from datetime import timedelta, datetime

#
# Leveraged ETFs (LETF) promise a fixed leverage ratio with respect to an underlying asset or an index.
# A Triple-Leveraged ETF allows speculators to amplify their exposure to the daily returns of an underlying index by a factor of 3. 
#
# Increased volatility generally decreases the value of a LETF over an extended period of time as daily compounding is amplified.
#
# This alpha emits short-biased insight to capitalize on volatility decay for each listed pair of TL-ETFs, by rebalancing the
# ETFs with equal weights each day.
#
# This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and 
# client funds can see an example of an alpha. 
#

class TripleLeveragedETFPairVolatilityDecayAlphaAlgorithm(QCAlgorithmFramework):

    def Initialize(self):

        self.SetStartDate(2018, 1, 1)
        
        self.SetCash(100000)
        
        # Set zero transaction fees
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))
        
        # 3X ETF pair tickers            
        ultraLong = Symbol.Create("UGLD", SecurityType.Equity, Market.USA) 
        ultraShort = Symbol.Create("DGLD", SecurityType.Equity, Market.USA) 
            
        # Manually curated universe
        self.UniverseSettings.Resolution = Resolution.Daily
        self.SetUniverseSelection(ManualUniverseSelectionModel([ultraLong, ultraShort]))
        
        # Select the demonstration alpha model
        self.SetAlpha(RebalancingTripleLeveragedETFAlphaModel(ultraLong, ultraShort))
        
        # Select our default model types
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())


class RebalancingTripleLeveragedETFAlphaModel(AlphaModel):
    '''
        Rebalance a pair of 3x leveraged ETFs and predict that the value of both ETFs in each pair will decrease.
    '''
    
    def __init__(self, ultraLong, ultraShort): 
        self.Name = "RebalancingTripleLeveragedETFAlphaModel"
        self.ultraLong = ultraLong
        self.ultraShort = ultraShort

    def Update(self, algorithm, data):
        '''Emit an insight each day.'''
        insights = []
        magnitude = 0.001

        # Giving an insight period 1 days.
        period = timedelta(days=1) 
        
        insights.append(Insight.Price(self.ultraLong, period, InsightDirection.Down, magnitude))
        insights.append(Insight.Price(self.ultraShort, period, InsightDirection.Down, magnitude))

        return Insight.Group( insights )
