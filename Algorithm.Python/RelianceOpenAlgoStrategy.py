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
from datetime import timedelta


class RelianceOpenAlgoStrategy(QCAlgorithm):
    """Simple live strategy: places a single market BUY order for 1 share of
    RELIANCE on NSE (India) via the OpenAlgo brokerage, then logs every order
    event.  Intended to be run with the 'openalgo-live' environment so that
    Lean routes orders to OpenAlgo.

    Order placement is driven by a repeating scheduled event so it does NOT
    depend on streaming market data — the OpenAlgo REST order API is enough.
    The scheduler retries every 30 seconds until the order has been placed
    (and only places it while NSE is open)."""

    def initialize(self):
        self.set_account_currency("INR")
        self.set_start_date(2026, 8, 11)
        self.set_end_date(2030, 12, 31)
        self.set_cash(100000)

        self._ordered = False

        self.reliance = self.add_equity("RELIANCE", Resolution.MINUTE, Market.INDIA)
        self.reliance.set_data_normalization_mode(DataNormalizationMode.RAW)

        # Bypass Lean's buying-power check — actual margin is managed by
        # OpenAlgo / Fyers MIS on the broker side, not by Lean's portfolio.
        self.reliance.set_buying_power_model(NullBuyingPowerModel())

        # Warm up with historical data so the security has a price before
        # we try to place an order (Lean rejects orders when price == 0).
        self.set_warmup(5, Resolution.MINUTE)

        # Evaluate scheduled events in IST so retries land during NSE hours.
        self.set_time_zone(TimeZones.Kolkata)

        # Retry placement every 30 seconds via the real-time scheduler,
        # independent of the (best-effort) market-data stream.
        self.schedule.on(
            self.date_rules.every_day(),
            self.time_rules.every(timedelta(seconds=30)),
            self._place_buy_if_open,
        )

        self.debug("RelianceOpenAlgoStrategy.Initialize: subscribed RELIANCE on India/NSE.")

    def on_data(self, data):
        # Best-effort placement path: fire as soon as the first data point
        # arrives and the market is open.  If streaming is unavailable, the
        # scheduled event will still drive placement.
        self._place_buy_if_open()

    def on_warmup_finished(self):
        self.debug("on_warmup_finished: warm-up complete, ready to place order.")

    def on_order_event(self, order_event):
        self.debug(
            f"OrderEvent id={order_event.order_id} symbol={order_event.symbol.value} "
            f"status={order_event.status} qty={order_event.quantity} "
            f"fill_qty={order_event.fill_quantity} fill_px={order_event.fill_price}"
        )

    def _place_buy_if_open(self):
        if self._ordered:
            return
        if self.is_warming_up:
            return
        if not self.is_market_open(self.reliance.symbol):
            return
        self._place_buy()

    def _place_buy(self):
        # BUY 1 RELIANCE — demonstrates the full Lean → OpenAlgo → broker
        # pipeline. NullBuyingPowerModel lets Lean accept the order regardless
        # of the broker-reported cash; actual margin is managed by Fyers MIS.
        qty = 1

        ticket = self.market_order(self.reliance.symbol, qty)
        # Only mark as ordered if the ticket was accepted (not Invalid).
        if ticket.status != OrderStatus.INVALID:
            self._ordered = True
            self.debug(
                f"_place_buy: submitted MarketOrder qty={qty} RELIANCE; "
                f"ticket_status={ticket.status}; orderId={ticket.order_id}"
            )
        else:
            self.debug(
                f"_place_buy: order rejected (will retry) — ticket_status={ticket.status}"
            )

    def on_end_of_algorithm(self):
        self.debug(
            f"on_end_of_algorithm: invested={self.portfolio.invested} "
            f"total_value={self.portfolio.total_portfolio_value} "
            f"cash={self.portfolio.cash}"
        )