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

from QuantConnect.Data.UniverseSelection import *
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

class LiquidGrowthStocks(QCAlgorithm):

    def initialize(self):
        
        # The aim is to have the start date of 2016 to 2021 when the growth stocks were hitting record numbers in the russell 2000 index.
        self.SetStartDate(2016, 10, 1)
        self.SetEndDate(2021, 1, 1)
        self.SetCash(1000000)
        
        # Set to have no transactional cost.
        self.SetSecurityInitializer(lambda security: security.SetFeedModel(ConstantFeeModel(0)))
       
        # Use buying model to purchase low cap growth stocks using LiquidGrowthStocks
        self.SetAlpha(LiquidGrowthStocks())
        
        # Select these equities using LiquidGrowthUniverseSelectionModel
        self.SetUniverseSelection(GrowthStockUniverseSelection())
        self.UniverseSettings.Resolution = Resolution.Weekly
        
        # Use LiquidGrowthStocks to emit insights
        self.setAlpha(LiquidGrowthUniverseSelectionModel())
        
        # Equally weigh equities in portfolio
        self.SetPortfolioConstruction(EqualWeghtingPortfolioConstructionModel())
        
        # Set immediate execution model
        self.SetExecution(ImmediateExecutionModel())

        # Set risk management to null
        self.SetRiskMnagement(NullRiskManagement())

class LiquidGrowthUniverseSelectionModel(FundamentalUniverseSelectionModel):

    def __init__(self):
        super().__init__(True, None, None)
        self.lastMonth = -1

    def selectCoarse(self, algorithm, coarse):

        # If symbol fundamentals are the same, no reason to sell.
        if algorithm.Time.month == self.lastMonth:
            return Universe.Unchanged
        self.lastMonth = algorithm.Time.month

        # Sort symbols by dollar volume
        sortedByDollarVolume = sorted([x for x in coarse if x.HasFundamentalData], key=lambda x: x.DollarVolume, reverse=True)
        return [x.symbol for x in sortedByDollarVolume[:100]]

    def SelectFine(self, algorithm, fine):

        # Sort yields per share
        sortedByYields = sorted(fine, key=lambda f: f.ValuationRatios.EarningYield, reverse=True)
        
        # top 10 most profitable stocks and bottom 10 least profitable stocks.
        # save the variable to self.universe.
        self.universe = sortedByYields[:10] + sortedByYields[-10:]
        
        # return the symbol objects by iterating through self.universe with list comprehension.
        return [f.symbol for f in self.universe]

class GrowthStockUniverseSelection(FundamentalUniverseSelectionModel):

    def __init__(self):
        super().__init_-(False)

        # Number of equity possibilites is 500 in the coarse universe
        self.numberOfSymbolCoarse = 500
        self.lastMonth = -1
    
    def SelectCoarse(self, algorithm, coarse):
        
        # If the security still holds good returns, DO NOT SELL!
        if algorithm.Time.month == self.lastMonth:
            return Universe.Unchanged
        self.lastMonth = algorithm.Time.month

        # Sort the securities by dollar volume and use the upper 500
        upper = sorted([n for n in coarse if n.HasFundamentalData
                                       and 20 > n.Price > 0
                                       and 1000000 > n.Volume > 10000],
                    key=lambda n: n.DollarVolume, reverse = True)[:self.numberOfSymbolsCoarse]

        return [n.symbol for n in upper]
