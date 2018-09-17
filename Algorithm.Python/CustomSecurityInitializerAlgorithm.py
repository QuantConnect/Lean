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
from QuantConnect.Brokerages import *
from QuantConnect.Data import BaseData
from QuantConnect.Data.Market import *
from QuantConnect.Securities import *

### <summary>
### This algorithm shows how to set a custom security initializer.
### A security initializer is run immediately after a new security object
### has been created and can be used to security models and other settings,
### such as data normalization mode
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="securities and portfolio" />
### <meta name="tag" content="trading and orders" />
class CustomSecurityInitializerAlgorithm(QCAlgorithm):

    def Initialize(self):
        
        # set our initializer to our custom type
        self.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage)
        
        func_security_seeder = FuncSecuritySeeder(Func[Security, BaseData](self.custom_seed_function))
        self.SetSecurityInitializer(CustomSecurityInitializer(self.BrokerageModel, func_security_seeder, DataNormalizationMode.Raw))
        
        self.SetStartDate(2013,10,1)
        self.SetEndDate(2013,11,1)

        self.AddEquity("SPY", Resolution.Hour)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

    def custom_seed_function(self, security):

        resolution = Resolution.Hour

        df = self.History(security.Symbol, 1, resolution)
        if df.empty:
            return None

        last_bar = df.unstack(level=0).iloc[-1]
        date_time = last_bar.name.to_pydatetime()
        open = last_bar.open.values[0]
        high = last_bar.high.values[0]
        low = last_bar.low.values[0]
        close = last_bar.close.values[0]
        volume = last_bar.volume.values[0]
        return TradeBar(date_time, security.Symbol, open, high, low, close, volume, Extensions.ToTimeSpan(resolution))


class CustomSecurityInitializer(BrokerageModelSecurityInitializer):
    '''Our custom initializer that will set the data normalization mode.
    We sub-class the BrokerageModelSecurityInitializer so we can also
    take advantage of the default model/leverage setting behaviors'''

    def __init__(self, brokerageModel, securitySeeder, dataNormalizationMode):
        '''Initializes a new instance of the CustomSecurityInitializer class with the specified normalization mode
        brokerageModel -- The brokerage model used to get fill/fee/slippage/settlement models
        securitySeeder -- The security seeder to be used
        dataNormalizationMode -- The desired data normalization mode'''
        self.base = BrokerageModelSecurityInitializer(brokerageModel, securitySeeder)
        self.dataNormalizationMode = dataNormalizationMode

    def Initialize(self, security):
        '''Initializes the specified security by setting up the models
        security -- The security to be initialized
        seedSecurity -- True to seed the security, false otherwise'''
        # first call the default implementation
        self.base.Initialize(security)

        # now apply our data normalization mode
        security.SetDataNormalizationMode(self.dataNormalizationMode)