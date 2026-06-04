# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.

from AlgorithmImports import *


class ChinaEquityMovingAverageCrossAlgorithm(QCAlgorithm):
    """Canonical 15/30 day moving average cross on a Wind full-code A-share symbol."""

    def initialize(self):
        self.set_start_date(2024, 1, 2)
        self.set_end_date(2024, 4, 30)
        self.set_account_currency(Currencies.CNY)
        self.set_cash(100000)
        self.set_benchmark(lambda _: 0)

        equity = self.add_equity("600000.SH", Resolution.DAILY)
        equity.set_fee_model(ConstantFeeModel(0))
        self.symbol = equity.symbol
        self.fast = self.ema(self.symbol, 15, Resolution.DAILY)
        self.slow = self.ema(self.symbol, 30, Resolution.DAILY)
        self.previous = None

    def on_data(self, data):
        if not data.bars.contains_key(self.symbol) or not self.slow.is_ready:
            return

        if self.previous is not None and self.previous.date() == self.time.date():
            return

        tolerance = 0.00015
        holdings = self.portfolio[self.symbol].quantity

        if holdings <= 0 and self.fast.current.value > self.slow.current.value * (1 + tolerance):
            self.set_holdings(self.symbol, 0.95)

        if holdings > 0 and self.fast.current.value < self.slow.current.value:
            self.liquidate(self.symbol)

        self.previous = self.time
