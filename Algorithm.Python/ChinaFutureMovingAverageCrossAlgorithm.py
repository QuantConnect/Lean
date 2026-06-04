# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.

from AlgorithmImports import *


class ChinaFutureMovingAverageCrossAlgorithm(QCAlgorithm):
    """Canonical 15/30 day moving average cross on a concrete SHFE rebar contract."""

    def initialize(self):
        self.set_start_date(2024, 1, 2)
        self.set_end_date(2024, 4, 30)
        self.set_account_currency(Currencies.CNY)
        self.set_cash(1000000)
        self.set_benchmark(lambda _: 0)

        self.contract = Symbol.create_future("RB", Market.SHF, datetime(2025, 1, 15))
        future = self.add_future_contract(self.contract, Resolution.DAILY)
        future.set_fee_model(ConstantFeeModel(0))
        self.fast = self.ema(self.contract, 15, Resolution.DAILY)
        self.slow = self.ema(self.contract, 30, Resolution.DAILY)
        self.previous = None

    def on_data(self, data):
        if not data.bars.contains_key(self.contract) or not self.slow.is_ready:
            return

        if self.previous is not None and self.previous.date() == self.time.date():
            return

        tolerance = 0.00015
        holdings = self.portfolio[self.contract].quantity

        if holdings <= 0 and self.fast.current.value > self.slow.current.value * (1 + tolerance):
            self.market_order(self.contract, 1)

        if holdings > 0 and self.fast.current.value < self.slow.current.value:
            self.liquidate(self.contract)

        self.previous = self.time
