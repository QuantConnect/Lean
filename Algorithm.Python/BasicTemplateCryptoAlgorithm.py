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

from System import *
from QuantConnect import *
from QuantConnect.Brokerages import *
from QuantConnect.Orders import *
from QCAlgorithm import QCAlgorithm

import decimal as d

### <summary>
### The demonstration algorithm shows some of the most common order methods when working with Crypto assets.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class BasicTemplateCryptoAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2018, 4, 4)  #Set Start Date
        self.SetEndDate(2018, 4, 4)    #Set End Date

        # Although typically real brokerages as GDAX only support a single account currency,
        # here we add both USD and EUR to demonstrate how to handle non-USD account currencies.
        # Set Strategy Cash (USD)
        self.SetCash(10000)

        # Set Strategy Cash (EUR)
        # EUR/USD conversion rate will be updated dynamically
        self.SetCash("EUR", 10000, 1.23)

        # Add some coins as initial holdings
        # When connected to a real brokerage, the amount specified in SetCash
        # will be replaced with the amount in your actual account.
        self.SetCash("BTC", 1, 7300)
        self.SetCash("ETH", 5, 400)

        # Note: the conversion rates above are required in backtesting (for now) because of this issue:
        # https://github.com/QuantConnect/Lean/issues/1859

        self.SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash)

        # You can uncomment the following lines when live trading with GDAX,
        # to ensure limit orders will only be posted to the order book and never executed as a taker (incurring fees).
        # Please note this statement has no effect in backtesting or paper trading.
        # self.DefaultOrderProperties = GDAXOrderProperties()
        # self.DefaultOrderProperties.PostOnly = True

        # Find more symbols here: http://quantconnect.com/data
        self.AddCrypto("BTCUSD", Resolution.Minute)
        self.AddCrypto("ETHUSD", Resolution.Minute)
        self.AddCrypto("BTCEUR", Resolution.Minute)
        symbol = self.AddCrypto("LTCUSD", Resolution.Minute).Symbol

        # create two moving averages
        self.fast = self.EMA(symbol, 30, Resolution.Minute)
        self.slow = self.EMA(symbol, 60, Resolution.Minute)

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''

        # Note: all limit orders in this algorithm will be paying taker fees,
        # they shouldn't, but they do (for now) because of this issue:
        # https://github.com/QuantConnect/Lean/issues/1852

        if self.Time.hour == 1 and self.Time.minute == 0:
            # Sell all ETH holdings with a limit order at 1% above the current price
            limitPrice = round(self.Securities["ETHUSD"].Price * d.Decimal(1.01), 2)
            quantity = self.Portfolio.CashBook["ETH"].Amount
            self.LimitOrder("ETHUSD", -quantity, limitPrice)

        elif self.Time.hour == 2 and self.Time.minute == 0:
            # Submit a buy limit order for BTC at 5% below the current price
            usdTotal = self.Portfolio.CashBook["USD"].Amount
            limitPrice = round(self.Securities["BTCUSD"].Price * d.Decimal(0.95), 2)
            # use only half of our total USD
            quantity = usdTotal * d.Decimal(0.5) / limitPrice
            self.LimitOrder("BTCUSD", quantity, limitPrice)

        elif self.Time.hour == 2 and self.Time.minute == 1:
            # Get current USD available, subtracting amount reserved for buy open orders
            usdTotal = self.Portfolio.CashBook["USD"].Amount
            usdReserved = sum(x.Quantity * x.LimitPrice for x
                in [x for x in self.Transactions.GetOpenOrders()
                    if x.Direction == OrderDirection.Buy
                        and x.Type == OrderType.Limit
                        and (x.Symbol.Value == "BTCUSD" or x.Symbol.Value == "ETHUSD")])
            usdAvailable = usdTotal - usdReserved
            self.Debug("usdAvailable: {}".format(usdAvailable))

            # Submit a marketable buy limit order for ETH at 1% above the current price
            limitPrice = round(self.Securities["ETHUSD"].Price * d.Decimal(1.01), 2)

            # use all of our available USD
            quantity = usdAvailable / limitPrice

            # this order will be rejected (for now) because of this issue:
            # https://github.com/QuantConnect/Lean/issues/1852
            self.LimitOrder("ETHUSD", quantity, limitPrice)

            # use only half of our available USD
            quantity = usdAvailable * d.Decimal(0.5) / limitPrice
            self.LimitOrder("ETHUSD", quantity, limitPrice)

        elif self.Time.hour == 11 and self.Time.minute == 0:
            # Liquidate our BTC holdings (including the initial holding)
            self.SetHoldings("BTCUSD", 0)

        elif self.Time.hour == 12 and self.Time.minute == 0:
            # Submit a market buy order for 1 BTC using EUR
            self.Buy("BTCEUR", 1)

            # Submit a sell limit order at 10% above market price
            limitPrice = round(self.Securities["BTCEUR"].Price * d.Decimal(1.1), 2)
            self.LimitOrder("BTCEUR", -1, limitPrice)

        elif self.Time.hour == 13 and self.Time.minute == 0:
            # Cancel the limit order if not filled
            self.Transactions.CancelOpenOrders("BTCEUR")

        elif self.Time.hour > 13:
            # To include any initial holdings, we read the LTC amount from the cashbook
            # instead of using Portfolio["LTCUSD"].Quantity

            if self.fast > self.slow:
                if self.Portfolio.CashBook["LTC"].Amount == 0:
                    self.Buy("LTCUSD", 10)
            else:
                if self.Portfolio.CashBook["LTC"].Amount > 0:
                    # The following two statements currently behave differently if we have initial holdings:
                    # https://github.com/QuantConnect/Lean/issues/1860

                    self.Liquidate("LTCUSD")
                    # self.SetHoldings("LTCUSD", 0)

    def OnOrderEvent(self, orderEvent):
        self.Debug("{} {}".format(self.Time, orderEvent.ToString()))

    def OnEndOfAlgorithm(self):
        self.Log("{} - TotalPortfolioValue: {}".format(self.Time, self.Portfolio.TotalPortfolioValue))
        self.Log("{} - CashBook: {}".format(self.Time, self.Portfolio.CashBook))
