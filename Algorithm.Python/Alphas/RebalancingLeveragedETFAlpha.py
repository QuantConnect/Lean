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
from datetime import timedelta, datetime
from decimal import Decimal

### <summary>
### Alpha Benchmark Strategy capitalizing on ETF rebalancing causing momentum during trending markets.
### </summary>
### <meta name="tag" content="alphastream" />
### <meta name="tag" content="etf" />
### <meta name="tag" content="algorithm framework" />
class RebalancingLeveragedETFAlpha(QCAlgorithm):
    ''' Alpha Streams: Benchmark Alpha: Leveraged ETF Rebalancing
        Strategy by Prof. Shum, reposted by Ernie Chan.
        Source: http://epchan.blogspot.com/2012/10/a-leveraged-etfs-strategy.html'''

    def Initialize(self):

        self.SetStartDate(2017, 6, 1)
        self.SetEndDate(2018, 8, 1)
        self.SetCash(100000)

        underlying = ["SPY","QLD","DIA","IJR","MDY","IWM","QQQ","IYE","EEM","IYW","EFA","GAZB","SLV","IEF","IYM","IYF","IYH","IYR","IYC","IBB","FEZ","USO","TLT"]
        ultraLong =  ["SSO","UGL","DDM","SAA","MZZ","UWM","QLD","DIG","EET","ROM","EFO","BOIL","AGQ","UST","UYM","UYG","RXL","URE","UCC","BIB","ULE","UCO","UBT"]
        ultraShort = ["SDS","GLL","DXD","SDD","MVV","TWM","QID","DUG","EEV","REW","EFU","KOLD","ZSL","PST","SMN","SKF","RXD","SRS","SCC","BIS","EPV","SCO","TBT"]

        groups = []
        for i in range(len(underlying)):
            group = ETFGroup(self.AddEquity(underlying[i], Resolution.Minute).Symbol,
                              self.AddEquity(ultraLong[i], Resolution.Minute).Symbol,
                              self.AddEquity(ultraShort[i], Resolution.Minute).Symbol)
            groups.append(group)

        # Manually curated universe
        self.SetUniverseSelection(ManualUniverseSelectionModel())
        # Select the demonstration alpha model
        self.SetAlpha(RebalancingLeveragedETFAlphaModel(groups))

        # Equally weigh securities in portfolio, based on insights
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        # Set Immediate Execution Model
        self.SetExecution(ImmediateExecutionModel())

        # Set Null Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())


class RebalancingLeveragedETFAlphaModel(AlphaModel):

    '''
        If the underlying ETF has experienced a return >= 1% since the previous day's close up to the current time at 14:15,
        then buy it's ultra ETF right away, and exit at the close. If the return is <= -1%, sell it's ultra-short ETF.
    '''

    def __init__(self, ETFgroups):

        self.ETFgroups = ETFgroups
        self.date = datetime.min.date
        self.Name = "RebalancingLeveragedETFAlphaModel"

    def Update(self, algorithm, data):
        '''Scan to see if the returns are greater than 1% at 2.15pm to emit an insight.'''

        insights = []
        magnitude = 0.0005
        # Paper suggests leveraged ETF's rebalance from 2.15pm - to close
        # giving an insight period of 105 minutes.
        period = timedelta(minutes=105)

        # Get yesterday's close price at the market open
        if algorithm.Time.date() != self.date:
            self.date = algorithm.Time.date()
            # Save yesterday's price and reset the signal
            for group in self.ETFgroups:
                history = algorithm.History([group.underlying], 1, Resolution.Daily)
                group.yesterdayClose = None if history.empty else Decimal(history.loc[str(group.underlying)]['close'][0])

        # Check if the returns are > 1% at 14.15
        if algorithm.Time.hour == 14 and algorithm.Time.minute == 15:
            for group in self.ETFgroups:
                if group.yesterdayClose == 0 or group.yesterdayClose is None: continue
                returns = round((algorithm.Portfolio[group.underlying].Price - group.yesterdayClose) / group.yesterdayClose, 10)
                if returns > 0.01:
                    insights.append(Insight.Price(group.ultraLong, period, InsightDirection.Up, magnitude))
                elif returns < -0.01:
                    insights.append(Insight.Price(group.ultraShort, period, InsightDirection.Down, magnitude))

        return insights

class ETFGroup:
    '''
    Group the underlying ETF and it's ultra ETFs
    Args:
        underlying: The underlying index ETF
        ultraLong: The long-leveraged version of underlying ETF
        ultraShort: The short-leveraged version of the underlying ETF
    '''
    def __init__(self,underlying, ultraLong, ultraShort):
        self.underlying = underlying
        self.ultraLong = ultraLong
        self.ultraShort = ultraShort
        self.yesterdayClose = 0