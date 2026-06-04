# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.

from AlgorithmImports import *


class ChinaEquityMinuteAlgorithm(QCAlgorithm):
    """Minute-resolution China A-share smoke test."""

    def initialize(self):
        self.set_start_date(2024, 1, 2)
        self.set_end_date(2024, 1, 8)
        self.set_account_currency(Currencies.CNY)
        self.set_cash(100000)
        self.set_benchmark(lambda _: 0)

        equity = self.add_equity("600000.SH", Resolution.MINUTE)
        equity.set_fee_model(ConstantFeeModel(0))
        self.symbol = equity.symbol
        self.fast = self.ema(self.symbol, 8, Resolution.MINUTE)
        self.slow = self.ema(self.symbol, 20, Resolution.MINUTE)
        self.bar_count = 0

    def on_data(self, data):
        if not data.bars.contains_key(self.symbol):
            return

        self.bar_count += 1
        if not self.slow.is_ready:
            return

        if not self.portfolio[self.symbol].invested and self.fast.current.value > self.slow.current.value:
            self.set_holdings(self.symbol, 0.8)
        elif self.portfolio[self.symbol].invested and self.fast.current.value < self.slow.current.value:
            self.liquidate(self.symbol)

    def on_end_of_algorithm(self):
        if self.bar_count < 200:
            raise Exception(f"Expected at least 200 minute bars, received {self.bar_count}")
