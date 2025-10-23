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
### This algorithm shows how to set a custom security initializer.
### A security initializer is run immediately after a new security object
### has been created and can be used to security models and other settings,
### such as data normalization mode
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="securities and portfolio" />
### <meta name="tag" content="trading and orders" />
class CustomSecurityInitializerAlgorithm(QCAlgorithm):

    def initialize(self):

        # set our initializer to our custom type
        self.set_brokerage_model(BrokerageName.INTERACTIVE_BROKERS_BROKERAGE)

        self.set_security_initializer(CustomSecurityInitializer(self.brokerage_model, DataNormalizationMode.RAW))

        self.set_start_date(2013,10,1)
        self.set_end_date(2013,11,1)

        self.add_equity("SPY", Resolution.HOUR)

    def on_data(self, data):
        if not self.portfolio.invested:
            self.set_holdings("SPY", 1)

    def custom_seed_function(self, security):

        resolution = Resolution.HOUR

        df = self.history(security.symbol, 1, resolution)
        if df.empty:
            return None

        last_bar = df.unstack(level=0).iloc[-1]
        date_time = last_bar.name.to_pydatetime()
        open = last_bar.open.values[0]
        high = last_bar.high.values[0]
        low = last_bar.low.values[0]
        close = last_bar.close.values[0]
        volume = last_bar.volume.values[0]
        return TradeBar(date_time, security.symbol, open, high, low, close, volume, Extensions.to_time_span(resolution))


class CustomSecurityInitializer(BrokerageModelSecurityInitializer):
    '''Our custom initializer that will set the data normalization mode.
    We sub-class the BrokerageModelSecurityInitializer so we can also
    take advantage of the default model/leverage setting behaviors'''

    def __init__(self, brokerage_model, data_normalization_mode):
        '''Initializes a new instance of the CustomSecurityInitializer class with the specified normalization mode
        brokerage_model -- The brokerage model used to get fill/fee/slippage/settlement models
        data_normalization_mode -- The desired data normalization mode'''
        super().__init__(brokerage_model, SecuritySeeder.NULL)
        self.data_normalization_mode = data_normalization_mode

    def initialize(self, security):
        '''Initializes the specified security by setting up the models
        security -- The security to be initialized
        seed_security -- True to seed the security, false otherwise'''
        # first call the default implementation
        super().initialize(security)

        # now apply our data normalization mode
        security.set_data_normalization_mode(self.data_normalization_mode)
