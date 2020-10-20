from datetime import datetime, timedelta

import clr
from System import *
from System.Reflection import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Future import *
from QuantConnect import Market


class FutureOptionShortPutOTMExpiryRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2020, 9, 22)
        clr.GetClrType(QCAlgorithm).GetField("_endDate", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, DateTime(2021, 3, 30))
        
        # We add AAPL as a temporary workaround for https://github.com/QuantConnect/Lean/issues/4872
        # which causes delisting events to never be processed, thus leading to options that might never
        # be exercised until the next data point arrives.
        self.AddEquity("AAPL", Resolution.Daily)

        self.es19h21 = self.AddFutureContract(
            Symbol.CreateFuture(
                Futures.Indices.SP500EMini,
                Market.CME,
                datetime(2021, 3, 19)),
            Resolution.Minute).Symbol

        # Select a future option expiring ITM, and adds it to the algorithm.
        self.esOption = self.AddFutureOptionContract(
            list(
                sorted(
                    [x for x in self.OptionChainProvider.GetOptionContractList(self.es19h21, self.Time) if x.ID.StrikePrice >= 3200.0 and x.ID.OptionRight == OptionRight.Put],
                    key=lambda x: x.ID.StrikePrice
                )
            )[0], Resolution.Minute).Symbol

        self.expectedContract = Symbol.CreateOption(self.es19h21, Market.CME, OptionStyle.American, OptionRight.Put, 3200.0, datetime(2021, 3, 19))
        if self.esOption != self.expectedContract:
            raise Exception(f"Contract {self.expectedContract} was not found in the chain");

        self.Schedule.On(self.DateRules.Today, self.TimeRules.AfterMarketOpen(self.es19h21, 1), self.ScheduledMarketOrder)

    def ScheduledMarketOrder(self):
        self.MarketOrder(self.esOption, -1)

    def OnData(self, data: Slice):
        # Assert delistings, so that we can make sure that we receive the delisting warnings at
        # the expected time. These assertions detect bug #4872
        for delisting in data.Delistings.Values:
            if delisting.Type == DelistingType.Warning:
                if delisting.Time != datetime(2021, 3, 19):
                    raise Exception(f"Delisting warning issued at unexpected date: {delisting.Time}");

            if delisting.Type == DelistingType.Delisted:
                if delisting.Time != datetime(2021, 3, 20):
                    raise Exception(f"Delisting happened at unexpected date: {delisting.Time}");
        

    def OnOrderEvent(self, orderEvent: OrderEvent):
        if orderEvent.Status != OrderStatus.Filled:
            # There's lots of noise with OnOrderEvent, but we're only interested in fills.
            return

        if not self.Securities.ContainsKey(orderEvent.Symbol):
            raise Exception(f"Order event Symbol not found in Securities collection: {orderEvent.Symbol}")

        security = self.Securities[orderEvent.Symbol]
        if security.Symbol == self.es19h21:
            raise Exception(f"Expected no order events for underlying Symbol {security.Symbol}")

        if security.Symbol == self.expectedContract:
            self.AssertFutureOptionContractOrder(orderEvent, security)

        else:
            raise Exception(f"Received order event for unknown Symbol: {orderEvent.Symbol}")

        self.Log(f"{orderEvent}");

    def AssertFutureOptionContractOrder(self, orderEvent: OrderEvent, optionContract: Security):
        if orderEvent.Direction == OrderDirection.Sell and optionContract.Holdings.Quantity != -1:
            raise Exception(f"No holdings were created for option contract {optionContract.Symbol}")

        if orderEvent.Direction == OrderDirection.Buy and optionContract.Holdings.Quantity != 0:
            raise Exception("Expected no options holdings after closing position")

        if orderEvent.IsAssignment:
            raise Exception(f"Assignment was not expected for {orderEvent.Symbol}")