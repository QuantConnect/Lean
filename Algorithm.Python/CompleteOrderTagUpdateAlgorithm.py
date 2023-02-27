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
### Algorithm asserting that closed orders can be updated with a new tag
### </summary>
class CompleteOrderTagUpdateAlgorithm(QCAlgorithm):

    TagAfterFill = "This is the tag set after order was filled.";
    TagAfterCanceled = "This is the tag set after order was canceled.";

    def Initialize(self) -> None:
        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)
        self.SetCash(100000)

        self._spy = self.AddEquity("SPY", Resolution.Minute).Symbol

        self._marketOrderTicket = None
        self._limitOrderTicket = None

        self._quantity = 100

    def OnData(self, data: Slice) -> None:
        if not self.Portfolio.Invested:
            if self._limitOrderTicket is None:
                # a limit order to test the tag update after order was canceled.

                # low price, we don't want it to fill since we are canceling it
                self._limitOrderTicket = self.LimitOrder(self._spy, 100, self.Securities[self._spy].Price * 0.1)
                self._limitOrderTicket.Cancel()
            else:
                # a market order to test the tag update after order was filled.
                self.Buy(self._spy, self._quantity)

    def OnOrderEvent(self, orderEvent: OrderEvent) -> None:
        if orderEvent.Status == OrderStatus.Canceled:
            if orderEvent.OrderId != self._limitOrderTicket.OrderId:
                raise Exception("The only canceled order should have been the limit order.")

            # update canceled order tag
            self.UpdateOrderTag(self._limitOrderTicket, self.TagAfterCanceled, "Error updating order tag after canceled")
        elif orderEvent.Status == OrderStatus.Filled:
            self._marketOrderTicket = list(self.Transactions.GetOrderTickets(lambda x: x.OrderType == OrderType.Market))[0]
            if orderEvent.OrderId != self._marketOrderTicket.OrderId:
                raise Exception("The only filled order should have been the market order.")

            # update filled order tag
            self.UpdateOrderTag(self._marketOrderTicket, self.TagAfterFill, "Error updating order tag after fill")

    def OnEndOfAlgorithm(self) -> None:
        # check the filled order
        self.AssertOrderTagUpdate(self._marketOrderTicket, self.TagAfterFill, "filled")
        if self._marketOrderTicket.Quantity != self._quantity or self._marketOrderTicket.QuantityFilled != self._quantity:
            raise Exception("The market order quantity should not have been updated.")

        # check the canceled order
        self.AssertOrderTagUpdate(self._limitOrderTicket, self.TagAfterCanceled, "canceled")

    def AssertOrderTagUpdate(self, ticket: OrderTicket, expectedTag: str, orderAction: str) -> None:
        if ticket is None:
            raise Exception(f"The order ticket was not set for the {orderAction} order")

        if ticket.Tag != expectedTag:
            raise Exception(f"Order ticket tag was not updated after order was {orderAction}")

        order = self.Transactions.GetOrderById(ticket.OrderId)
        if order.Tag != expectedTag:
            raise Exception(f"Order tag was not updated after order was {orderAction}")

    def UpdateOrderTag(self, ticket: OrderTicket, tag: str, errorMessagePrefix: str) -> None:
        updateFields = UpdateOrderFields()
        updateFields.Tag = tag
        response = ticket.Update(updateFields)

        if response.IsError:
            raise Exception(f"{errorMessagePrefix}: {response.ErrorMessage}")
