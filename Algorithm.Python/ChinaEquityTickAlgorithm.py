# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.

from AlgorithmImports import *


class ChinaEquityTickAlgorithm(QCAlgorithm):
    """Tick-resolution China A-share smoke test."""

    def initialize(self):
        self.set_start_date(2024, 1, 2)
        self.set_end_date(2024, 1, 3)
        self.set_account_currency(Currencies.CNY)
        self.set_cash(100000)
        self.set_benchmark(lambda _: 0)

        equity = self.add_equity("600000.SH", Resolution.TICK)
        equity.set_fee_model(ConstantFeeModel(0))
        self.symbol = equity.symbol
        self.tick_count = 0
        self.last_price = None

    def on_data(self, data):
        ticks = data.ticks.get(self.symbol)
        if ticks is None:
            return

        for tick in ticks:
            if tick.tick_type != TickType.TRADE:
                continue

            self.tick_count += 1
            self.last_price = tick.last_price

        if self.tick_count >= 20 and not self.portfolio[self.symbol].invested:
            self.market_order(self.symbol, 100)
        elif self.tick_count >= 120 and self.portfolio[self.symbol].invested:
            self.liquidate(self.symbol)

    def on_end_of_algorithm(self):
        if self.tick_count < 100:
            raise Exception(f"Expected at least 100 trade ticks, received {self.tick_count}")
        if self.last_price is None:
            raise Exception("Expected at least one tick price")
