from QuantConnect.Algorithm import *
from QuantConnect.Orders import SubmitOrderRequest, OrderType, UpdateOrderRequest, UpdateOrderFields, OrderEvent
from QuantConnect import SecurityType, Resolution
from collections import deque
from datetime import timedelta


class LimitIfTouchedRegressionAlgorithm(QCAlgorithm):
    _expectedEvents = deque([
        "Time: 10/10/2013 13:31:00 OrderID: 72 EventID: 11 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: "
        "-1 FillPrice: 152.8807 USD LimitPrice: 152.519 TriggerPrice: 151.769 OrderFee: 1 USD",
        "Time: 10/10/2013 15:55:00 OrderID: 73 EventID: 11 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: "
        "-1 FillPrice: 153.9225 USD LimitPrice: 153.8898 TriggerPrice: 153.1398 OrderFee: 1 USD",
        "Time: 10/11/2013 14:02:00 OrderID: 74 EventID: 11 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: "
        "-1 FillPrice: 154.9643 USD LimitPrice: 154.9317 TriggerPrice: 154.1817 OrderFee: 1 USD "
    ])

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 15)
        self.SetCash(100000)
        self.AddSecurity(SecurityType.Equity, "SPY", Resolution.Minute)

    def OnData(self, data):
        if self.Transactions.GetOpenOrders().Count < 1:
            self._negative = 1 if self.Time < self.StartDate.__add__(timedelta(days=2)) else -1
            orderRequest = self.SubmitOrderRequest(OrderType.LimitIfTouched, SecurityType.Equity, "SPY",
                                                   data["SPY"].Price - self._negative,
                                                   data["SPY"].Price - 0.25 * self._negative, self.UtcTime,
                                                   f"LIT - {self.UtcTime}, Quantity: {self._negative * 10}")
            self._request = self.Transactions.AddOrder(orderRequest)

        if self._request is not None:
            if self._request.Quantity == 1:
                self.Transactions.CancelOpenOrders()
                del self._request
                return

            new_quantity = self._request.Quantity - self._negative
            self._request.UpdateQuantity(new_quantity,
                                         f"LIT - Time: {self.UtcTime}, Quantity: {new_quantity}")
    
    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderEvent.Filled:
            expected = self._expectedEvents.pop()
            if orderEvent.ToString() != expected:
                raise Exception(f"orderEvent {orderEvent.Id} differed from {expected}")
