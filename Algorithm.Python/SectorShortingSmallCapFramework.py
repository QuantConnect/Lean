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

from AlgorithmImports import *
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

class CustomSectorShorting(QCAlgorithm):
    ''' Simple model to short any growth stocks during higher interest rates. The idea is that when interest rates rise, 
    the future cashflows of growth companies can have negative effect on the asset resulting in good short positions. For simplicity,
    I will use technology (small cap) stocks in the example due to the nature of tghe industry.'''

    def Initialize(self):

        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2018, 1, 1)
        self.SetEndDate(2020, 1, 10)
        self.SetCash(1000000)

        # Set zero transaction fees
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))

        # select stocks using PennyStockUniverseSelectionModel
        self.UniverseSettings.Resolution = Resolution.Hourly
        self.SetUniverseSelection(TechnologyStockUniverseSelectionModel())

        # Use CustomWeightingSectorShortModel to gather insights
        self.SetAlpha(TechnologyStockUniverseSelectionModel())

        # Equally weigh securities in portfolio, based on insights
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        # Set Immediate Execution Model
        self.SetExecution(ImmediateExecutionModel())

        # Set Null Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())

class TechnologyStockUniverseSelectionModel(FundamentalUniverseSelectionModel):

    def __init__(self):
        super().__init__(False)
        self.numberOfSymbolsCoarse = 100
        self.lastMonth = -1

    def SelectCoarse(self, algorithm, coarse):
        
        if algorithm.Time.month == self.lastMonth:
            return Universe.Unchanged
        self.lastMonth = algorithm.Time.month
        
        filtered = sorted([x for x in coarse if x.HasFundamentalData
                                       and 20 > x.Price > 0
                                       and 10000000 > x.Volume > 100000],
                    key=lambda x: x.DollarVolume, reverse=True)[:self.numberOfSymbolsCoarse]

        return [x.Symbol for x in filtered]


class CustomWeightingSectorShortModel(AlphaModel):

    def __init__(self, *args, **kwargs):
        lookback = kwargs['lookback'] if 'lookback' in kwargs else 1
        resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(resolution), lookback)
        self.numberOfStocks = kwargs['numberOfStocks'] if 'numberOfStocks' in kwargs else 10

    def Update(self, algorithm, data):
        insights = []
        symbolsRet = dict()

        for security in algorithm.ActiveSecurities.Values:
            if security.HasData:
                open = security.Open
                if open != 0:
                    symbolsRet[security.Symbol] = security.Close / open - 1

        
        growthTechStocks = dict(sorted(symbolsRet.items(),
                                   key = lambda kv: (-round(kv[1], 6), kv[0]))[:self.MorningstarSectorCode.Technology])

        
        for symbol, value in growthTechStocks.items():
            insights.append(Insight.Price(symbol, self.predictionInterval, InsightDirection.Down, abs(value), None))

        return insights


