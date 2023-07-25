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
### Basic algorithm demonstrating how to place trailing stop orders.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="placing orders" />
### <meta name="tag" content="trailing stop order"/>
class TrailingStopOrderRegressionAlgorithm(QCAlgorithm):
    '''Basic algorithm demonstrating how to place trailing stop orders.'''

    BuyTrailingAmount = 2
    SellTrailingAmount = 0.5

    def Initialize(self):

        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)
        self.SetCash(100000)

        self._symbol = self.AddEquity("SPY").Symbol

        self._buyOrderTicket: OrderTicket = None
        self._sellOrderTicket: OrderTicket = None
        self._previousSlice: Slice = None

    def OnData(self, slice: Slice):
        if not slice.ContainsKey(self._symbol):
            return

        if self._buyOrderTicket is None:
            self._buyOrderTicket = self.TrailingStopOrder(self._symbol, 100, trailingAmount=self.BuyTrailingAmount, trailingAsPercentage=False)
        elif self._buyOrderTicket.Status != OrderStatus.Filled:
            stopPrice = self._buyOrderTicket.Get(OrderField.StopPrice)

            # Get the previous bar to compare to the stop price,
            # because stop price update attempt with the current slice data happens after OnData.
            low = self._previousSlice.QuoteBars[self._symbol].Ask.Low if self._previousSlice.QuoteBars.ContainsKey(self._symbol) \
                  else self._previousSlice.Bars[self._symbol].Low

            stopPriceToMarketPriceDistance = stopPrice - low
            if stopPriceToMarketPriceDistance > self.BuyTrailingAmount:
                raise Exception(f"StopPrice {stopPrice} should be within {self.BuyTrailingAmount} of the previous low price {low} at all times.")

        if self._sellOrderTicket is None:
            if self.Portfolio.Invested:
                self._sellOrderTicket = self.TrailingStopOrder(self._symbol, -100, trailingAmount=self.SellTrailingAmount, trailingAsPercentage=False)
        elif self._sellOrderTicket.Status != OrderStatus.Filled:
            stopPrice = self._sellOrderTicket.Get(OrderField.StopPrice)

            # Get the previous bar to compare to the stop price,
            # because stop price update attempt with the current slice data happens after OnData.
            high = self._previousSlice.QuoteBars[self._symbol].Bid.High if self._previousSlice.QuoteBars.ContainsKey(self._symbol) \
                   else self._previousSlice.Bars[self._symbol].High
            stopPriceToMarketPriceDistance = high - stopPrice
            if stopPriceToMarketPriceDistance > self.SellTrailingAmount:
                raise Exception(f"StopPrice {stopPrice} should be within {self.SellTrailingAmount} of the previous high price {high} at all times.")

        self._previousSlice = slice

    def OnOrderEvent(self, orderEvent: OrderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            if orderEvent.Direction == OrderDirection.Buy:
                stopPrice = self._buyOrderTicket.Get(OrderField.StopPrice)
                if orderEvent.FillPrice < stopPrice:
                    raise Exception(f"Buy trailing stop order should have filled with price greater than or equal to the stop price {stopPrice}. "
                                    f"Fill price: {orderEvent.FillPrice}")
            else:
                stopPrice = self._sellOrderTicket.Get(OrderField.StopPrice)
                if orderEvent.FillPrice > stopPrice:
                    raise Exception(f"Sell trailing stop order should have filled with price less than or equal to the stop price {stopPrice}. "
                                    f"Fill price: {orderEvent.FillPrice}")
