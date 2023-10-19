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
### Algorithm demonstrating and ensuring that Bybit crypto brokerage model works as expected
### </summary>
class BybitCryptoRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2022, 12, 13)
        self.SetEndDate(2022, 12, 13)

        # Set account currency (USDT)
        self.SetAccountCurrency("USDT")

        # Set strategy cash (USD)
        self.SetCash(100000)

        # Add some coin as initial holdings
        # When connected to a real brokerage, the amount specified in SetCash
        # will be replaced with the amount in your actual account.
        self.SetCash("BTC", 1)

        try:
            self.SetBrokerageModel(BrokerageName.Bybit, AccountType.Margin)
            raise Exception("Expected Margin accounts to not be supported by Bybit")
        except NotSupportedException as e:
            # Expected, only cash accounts are supported by Bytbit brokerage model
            pass

        self.SetBrokerageModel(BrokerageName.Bybit, AccountType.Cash)

        self.btcUsdt = self.AddCrypto("BTCUSDT").Symbol

        # create two moving averages
        self.fast = self.EMA(self.btcUsdt, 30, Resolution.Minute)
        self.slow = self.EMA(self.btcUsdt, 60, Resolution.Minute)

        self.liquidated = False

    def OnData(self, data):
        if self.Portfolio.CashBook["USDT"].ConversionRate == 0 or self.Portfolio.CashBook["BTC"].ConversionRate == 0:
            self.Log(f"USDT conversion rate: {self.Portfolio.CashBook['USDT'].ConversionRate}")
            self.Log(f"BTC conversion rate: {self.Portfolio.CashBook['BTC'].ConversionRate}")

            raise Exception("Conversion rate is 0")

        if not self.slow.IsReady:
            return

        btcAmount = self.Portfolio.CashBook["BTC"].Amount
        if self.fast > self.slow:
            if btcAmount == 1 and not self.liquidated:
                self.Buy(self.btcUsdt, 1)
        else:
            if btcAmount > 1:
                self.Liquidate(self.btcUsdt)
                self.liquidated = True
            elif btcAmount > 0 and self.liquidated and len(self.Transactions.GetOpenOrders()) == 0:
                # Place a limit order to sell our initial BTC holdings at 1% above the current price
                limitPrice = round(self.Securities[self.btcUsdt].Price * 1.01, 2)
                self.LimitOrder(self.btcUsdt, -btcAmount, limitPrice)

    def OnOrderEvent(self, orderEvent):
        self.Debug("{} {}".format(self.Time, orderEvent.ToString()))

    def OnEndOfAlgorithm(self):
        self.Log(f"{self.Time} - TotalPortfolioValue: {self.Portfolio.TotalPortfolioValue}")
        self.Log(f"{self.Time} - CashBook: {self.Portfolio.CashBook}")

        btcAmount = self.Portfolio.CashBook["BTC"].Amount
        if btcAmount > 0:
            raise Exception(f"BTC holdings should be zero at the end of the algorithm, but was {btcAmount}")
