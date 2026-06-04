# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.

from AlgorithmImports import *


class ChinaEquityPortfolioAlgorithm(QCAlgorithm):
    """Small China A-share portfolio smoke test using Wind full-code symbols."""

    def initialize(self):
        self.set_start_date(2024, 1, 2)
        self.set_end_date(2024, 4, 30)
        self.set_account_currency(Currencies.CNY)
        self.set_cash(200000)
        self.set_benchmark(lambda _: 0)

        self.symbols = []
        for ticker in ["600000.SH", "000001.SZ"]:
            security = self.add_equity(ticker, Resolution.DAILY)
            security.set_fee_model(ConstantFeeModel(0))
            self.symbols.append(security.symbol)

        self.fast = {symbol: self.ema(symbol, 10, Resolution.DAILY) for symbol in self.symbols}
        self.slow = {symbol: self.ema(symbol, 20, Resolution.DAILY) for symbol in self.symbols}
        self.bar_count = 0

    def on_data(self, data):
        tradable = []
        for symbol in self.symbols:
            if data.bars.contains_key(symbol):
                self.bar_count += 1
            if self.slow[symbol].is_ready and self.fast[symbol].current.value > self.slow[symbol].current.value:
                tradable.append(symbol)

        if not tradable:
            return

        target = 0.45
        for symbol in self.symbols:
            if symbol in tradable:
                self.set_holdings(symbol, target)
            elif self.portfolio[symbol].invested:
                self.liquidate(symbol)

    def on_end_of_algorithm(self):
        if self.bar_count < 120:
            raise Exception(f"Expected at least 120 portfolio bars, received {self.bar_count}")
