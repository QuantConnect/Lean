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
### Demonstration of using the Delisting event in your algorithm. Assets are delisted on their last day of trading, or when their contract expires.
### This data is not included in the open source project.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="data event handlers" />
### <meta name="tag" content="delisting event" />
class DelistingEventsAlgorithm(QCAlgorithm):

    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.set_start_date(2007, 5, 15)  #Set Start Date
        self.set_end_date(2007, 5, 25)    #Set End Date
        self.set_cash(100000)             #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.add_equity("AAA.1", Resolution.DAILY)
        self.add_equity("SPY", Resolution.DAILY)


    def on_data(self, data):
        '''on_data event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if self.transactions.orders_count == 0:
            self.set_holdings("AAA.1", 1)
            self.debug("Purchased stock")

        for kvp in data.bars:
            symbol = kvp.key
            value = kvp.value

            self.log("OnData(Slice): {0}: {1}: {2}".format(self.time, symbol, value.close))

        # the slice can also contain delisting data: data.delistings in a dictionary string->Delisting

        aaa = self.securities["AAA.1"]
        if aaa.is_delisted and aaa.is_tradable:
            raise Exception("Delisted security must NOT be tradable")

        if not aaa.is_delisted and not aaa.is_tradable:
            raise Exception("Securities must be marked as tradable until they're delisted or removed from the universe")

        for kvp in data.delistings:
            symbol = kvp.key
            value = kvp.value

            if value.type == DelistingType.WARNING:
                self.log("OnData(Delistings): {0}: {1} will be delisted at end of day today.".format(self.time, symbol))

                # liquidate on delisting warning
                self.set_holdings(symbol, 0)

            if value.type == DelistingType.DELISTED:
                self.log("OnData(Delistings): {0}: {1} has been delisted.".format(self.time, symbol))

                # fails because the security has already been delisted and is no longer tradable
                self.set_holdings(symbol, 1)


    def on_order_event(self, order_event):
        self.log("OnOrderEvent(OrderEvent): {0}: {1}".format(self.time, order_event))
