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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Indicators import *
from datetime import timedelta

### <summary>
### Demonstration algorithm showing how to easily convert an old algorithm into the framework.
###
###  1. When making orders, also create insights for the correct direction (up/down/flat), can also set insight prediction period/magnitude/direction
###  2. Emit insights before placing any trades
###  3. Profit :)
###  </summary>
###  <meta name="tag" content="indicators" />
###  <meta name="tag" content="indicator classes" />
###  <meta name="tag" content="plotting indicators" />
class ConvertToFrameworkAlgorithm(QCAlgorithm):
    '''Demonstration algorithm showing how to easily convert an old algorithm into the framework.'''

    FastEmaPeriod = 12
    SlowEmaPeriod = 26

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2004, 1, 1)
        self.SetEndDate(2015, 1, 1)

        self.symbol = self.AddSecurity(SecurityType.Equity, 'SPY', Resolution.Daily).Symbol

        # define our daily macd(12,26) with a 9 day signal
        self.macd = self.MACD(self.symbol, self.FastEmaPeriod, self.SlowEmaPeriod, 9, MovingAverageType.Exponential, Resolution.Daily)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        Args:
            data: Slice object with your stock data'''
        # wait for our indicator to be ready
        if not self.macd.IsReady or data[self.symbol] is None: return

        holding = self.Portfolio[self.symbol]

        signalDeltaPercent = float(self.macd.Current.Value - self.macd.Signal.Current.Value) / float(self.macd.Fast.Current.Value)
        tolerance = 0.0025

        # if our macd is greater than our signal, then let's go long
        if holding.Quantity <= 0 and signalDeltaPercent > tolerance:
            # 1. Call EmitInsights with insights created in correct direction, here we're going long
            #    The EmitInsights method can accept multiple insights separated by commas
            self.EmitInsights(
                # Creates an insight for our symbol, predicting that it will move up within the fast ema period number of days
                Insight.Price(self.symbol, timedelta(self.FastEmaPeriod), InsightDirection.Up)
            )

            # longterm says buy as well
            self.SetHoldings(self.symbol, 1)

        # if our macd is less than our signal, then let's go short
        elif holding.Quantity >= 0 and signalDeltaPercent < -tolerance:
            # 1. Call EmitInsights with insights created in correct direction, here we're going short
            #    The EmitInsights method can accept multiple insights separated by commas
            self.EmitInsights(
                # Creates an insight for our symbol, predicting that it will move down within the fast ema period number of days
                Insight.Price(self.symbol, timedelta(self.FastEmaPeriod), InsightDirection.Down)
            )

            self.SetHoldings(self.symbol, -1)

        # if we wanted to liquidate our positions
        ## 1. Call EmitInsights with insights create in the correct direction -- Flat
        
        #self.EmitInsights(
            # Creates an insight for our symbol, predicting that it will move down or up within the fast ema period number of days, depending on our current position
            # Insight.Price(self.symbol, timedelta(self.FastEmaPeriod), InsightDirection.Flat)
        #)
        
        # self.Liquidate()

        # plot both lines
        self.Plot("MACD", self.macd, self.macd.Signal)
        self.Plot(self.symbol.Value, self.macd.Fast, self.macd.Slow)
        self.Plot(self.symbol.Value, "Open", data[self.symbol].Open)