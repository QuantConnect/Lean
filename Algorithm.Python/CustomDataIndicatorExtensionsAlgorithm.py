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
AddReference("QuantConnect.Indicators")


from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Data.Custom import *
from QuantConnect.Python import PythonQuandl
from QCAlgorithm import QCAlgorithm

### <summary>
### The algorithm creates new indicator value with the existing indicator method by Indicator Extensions
### Demonstration of using the external custom datasource Quandl to request the VIX and VXV daily data
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
### <meta name="tag" content="plotting indicators" />
### <meta name="tag" content="charting" />
class CustomDataIndicatorExtensionsAlgorithm(QCAlgorithm):

    # Initialize the data and resolution you require for your strategy
    def Initialize(self):

        self.SetStartDate(2014,1,1) 
        self.SetEndDate(2018,1,1)  
        self.SetCash(25000)
        
        self.vix = 'CBOE/VIX'
        self.vxv = 'CBOE/VXV'
        
        # Define the symbol and "type" of our generic data
        self.AddData(QuandlVix, self.vix, Resolution.Daily)
        self.AddData(Quandl, self.vxv, Resolution.Daily)
        
        # Set up default Indicators, these are just 'identities' of the closing price
        self.vix_sma = self.SMA(self.vix, 1, Resolution.Daily)
        self.vxv_sma = self.SMA(self.vxv, 1, Resolution.Daily)
        
        # This will create a new indicator whose value is smaVXV / smaVIX
        self.ratio = IndicatorExtensions.Over(self.vxv_sma, self.vix_sma)
        
        # Plot indicators each time they update using the PlotIndicator function
        self.PlotIndicator("Ratio", self.ratio)
        self.PlotIndicator("Data", self.vix_sma, self.vxv_sma)
    
    # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    def OnData(self, data):
        
        # Wait for all indicators to fully initialize
        if not (self.vix_sma.IsReady and self.vxv_sma.IsReady and self.ratio.IsReady): return
        if not self.Portfolio.Invested and self.ratio.Current.Value > 1:
            self.MarketOrder(self.vix, 100)
        elif self.ratio.Current.Value < 1:
                self.Liquidate()

# In CBOE/VIX data, there is a "vix close" column instead of "close" which is the 
# default column namein LEAN Quandl custom data implementation.
# This class assigns new column name to match the the external datasource setting.
class QuandlVix(PythonQuandl):
    
    def __init__(self):
        self.ValueColumnName = "VIX Close"