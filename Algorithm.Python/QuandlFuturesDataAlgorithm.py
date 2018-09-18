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
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Python import PythonQuandl
from datetime import datetime, timedelta

### <summary>
### Futures demonstration algorithm.
### QuantConnect allows importing generic data sources! This example demonstrates importing a futures
### data from the popular open data source Quandl. QuantConnect has a special deal with Quandl giving you access
### to Stevens Continuous Futurs (SCF) for free. If you'd like to download SCF for local backtesting, you can download it through Quandl.com.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="quandl" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="futures" />
class QuandlFuturesDataAlgorithm(QCAlgorithm):

    def Initialize(self):
        ''' Initialize the data and resolution you require for your strategy '''
        self.SetStartDate(2000, 1, 1)
        self.SetEndDate(datetime.now().date() - timedelta(1))
        self.SetCash(25000)

        # Symbol corresponding to the quandl code
        self.crude = "SCF/CME_CL1_ON"
        self.AddData(QuandlFuture, self.crude, Resolution.Daily)


    def OnData(self, data):
        '''Data Event Handler: New data arrives here. "TradeBars" type is a dictionary of strings so you can access it by symbol.'''
        if self.Portfolio.HoldStock: return

        self.SetHoldings(self.crude, 1)
        self.Debug(str(self.Time) + str(" Purchased Crude Oil: ") + self.crude)


class QuandlFuture(PythonQuandl):
    '''Custom quandl data type for setting customized value column name. Value column is used for the primary trading calculations and charting.'''
    def __init__(self):
        # Define ValueColumnName: cannot be None, Empty or non-existant column name
        # If ValueColumnName is "Close", do not use PythonQuandl, use Quandl:
        # self.AddData[QuandlFuture](self.crude, Resolution.Daily)
        self.ValueColumnName = "Settle"