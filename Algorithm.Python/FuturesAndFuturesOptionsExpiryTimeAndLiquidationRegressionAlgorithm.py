from datetime import datetime, timedelta

from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *
from QuantConnect import *

### <summary>
### Tests delistings for Futures and Futures Options to ensure that they are delisted at the expected times.
### </summary>
class FuturesAndFuturesOptionsExpiryTimeAndLiquidationRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.invested = False
        self.liquidated = 0
        self.delistingsReceived = 0

        self.expectedExpiryWarningTime = datetime(2020, 6, 19)
        self.expectedExpiryDelistingTime = datetime(2020, 6, 20)
        self.expectedLiquidationTime = datetime(2020, 6, 19, 16, 0, 0)

        self.SetStartDate(2020, 1, 5)
        self.SetEndDate(2020, 12, 1)
        self.SetCash(100000)

        es = Symbol.CreateFuture(
            "ES",
            Market.CME,
            datetime(2020, 6, 19)
        )

        esOption = Symbol.CreateOption(
            es,
            Market.CME,
            OptionStyle.American,
            OptionRight.Put,
            3400.0,
            datetime(2020, 6, 19)
        )

        self.esFuture = self.AddFutureContract(es, Resolution.Minute).Symbol
        self.esFutureOption = self.AddFutureOptionContract(esOption, Resolution.Minute).Symbol

    def OnData(self, data: Slice):
        for delisting in data.Delistings.Values:
            self.delistingsReceived += 1

            if delisting.Type == DelistingType.Warning and delisting.Time != self.expectedExpiryWarningTime:
                raise AssertionError(f"Expiry warning with time {delisting.Time} but is expected to be {self.expectedExpiryWarningTime}")

            if delisting.Type == DelistingType.Warning and delisting.Time != datetime(self.Time.year, self.Time.month, self.Time.day):
                raise AssertionError(f"Delisting warning received at an unexpected date: {self.Time} - expected {delisting.Time}")

            if delisting.Type == DelistingType.Delisted and delisting.Time != self.expectedExpiryDelistingTime:
                raise AssertionError(f"Delisting occurred at unexpected time: {delisting.Time} - expected: {self.expectedExpiryDelistingTime}")

            if delisting.Type == DelistingType.Delisted and delisting.Time != datetime(self.Time.year, self.Time.month, self.Time.day):
                raise AssertionError(f"Delisting notice received at an unexpected date: {self.Time} - expected {delisting.Time}")

        if not self.invested and \
            (self.esFuture in data.Bars or self.esFuture in data.QuoteBars) and \
            (self.esFutureOption in data.Bars or self.esFutureOption in data.QuoteBars):

            self.invested = True

            self.MarketOrder(self.esFuture, 1)
            self.MarketOrder(self.esFutureOption, 1)

    def OnOrderEvent(self, orderEvent: OrderEvent):
        if orderEvent.Direction != OrderDirection.Sell or orderEvent.Status != OrderStatus.Filled:
            return

        # * Future Liquidation
        # * Future Option Exercise
        # * We expect NO Underlying Future Liquidation because we already hold a Long future position so the FOP Put selling leaves us breakeven
        self.liquidated += 1
        if orderEvent.Symbol.SecurityType == SecurityType.FutureOption and self.expectedLiquidationTime != self.Time:
            raise AssertionError(f"Expected to liquidate option {orderEvent.Symbol} at {self.expectedLiquidationTime}, instead liquidated at {self.Time}")

        if orderEvent.Symbol.SecurityType == SecurityType.Future and \
            (self.expectedLiquidationTime - timedelta(minutes=1)) != self.Time and \
            self.expectedLiquidationTime != self.Time:

            raise AssertionError(f"Expected to liquidate future {orderEvent.Symbol} at {self.expectedLiquidationTime} (+1 minute), instead liquidated at {self.Time}")


    def OnEndOfAlgorithm(self):
        if not self.invested:
            raise AssertionError("Never invested in ES futures and FOPs")

        if self.delistingsReceived != 4:
            raise AssertionError(f"Expected 4 delisting events received, found: {self.delistingsReceived}")

        if self.liquidated != 2:
            raise AssertionError(f"Expected 3 liquidation events, found {self.liquidated}")
