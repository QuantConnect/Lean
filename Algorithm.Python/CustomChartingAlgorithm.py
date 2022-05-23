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

### <summary>
### Algorithm demonstrating custom charting support in QuantConnect.
### The entire charting system of quantconnect is adaptable. You can adjust it to draw whatever you'd like.
### Charts can be stacked, or overlayed on each other. Series can be candles, lines or scatter plots.
### Even the default behaviours of QuantConnect can be overridden.
### </summary>
### <meta name="tag" content="charting" />
### <meta name="tag" content="adding charts" />
### <meta name="tag" content="series types" />
### <meta name="tag" content="plotting indicators" />
class CustomChartingAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2020, 11, 3)  # Set Start Date
        self.SetEndDate(2020, 11, 7)
        self.SetCash(100000)  # Set Strategy Cash

        self.symbol = self.AddEquity("SPY", Resolution.Daily).Symbol
        self.sma = self.SMA(self.symbol, 20)
        self.PlotIndicator("PlotIndicator", self.sma)


    def OnData(self, data):
        self.Plot("Plot", self.sma)
        self.Log("SiUUUUuuuu")
