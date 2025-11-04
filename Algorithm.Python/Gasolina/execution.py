import math
from datetime import date, timedelta
from AlgorithmImports import * # type: ignore

ALIVE = (OrderStatus.NEW, OrderStatus.SUBMITTED, OrderStatus.UPDATE_SUBMITTED, OrderStatus.PARTIALLY_FILLED)


class LimitExecutionModel(ExecutionModel):
    """Execution model that issues limit orders and falls back to end-of-day."""

    def __init__(self,
                 greed=1.1,
                 lot_size=1.,
                 panic_lot_size=1.):
        super().__init__()
        self.greed: float = greed
        self.lot_size: float = lot_size
        self.panic_lot_size: float = panic_lot_size
        self.pending: dict[Symbol, float] = {}
        self.orders: dict[Symbol, OrderTicket] = {}

    def execute(self, algorithm: QCAlgorithm, targets: list[IPortfolioTarget]) -> None:
        if algorithm.is_warming_up:
            return
        work: dict[Symbol, float] = {}

        # collect targets generated when market was closed
        for symbol, quantity in list(self.pending.items()):
            security = algorithm.securities[symbol]
            if not security.is_tradable:
                continue
            if security.exchange.hours.is_open(algorithm.time, extended_market_hours=False):
                del self.pending[symbol]
                work[symbol] = quantity

        # process list of new targets
        for target in targets:
            symbol = target.symbol
            security = algorithm.securities[symbol]
            if not security.is_tradable:
                continue
            if security.exchange.hours.is_open(algorithm.time, extended_market_hours=False):
                if symbol in self.pending:
                    del self.pending[symbol]
                work[symbol] = target.quantity
            else:
                self.pending[symbol] = target.quantity

        # process work that can be performed
        for symbol, quantity in work.items():
            self.converge(algorithm, symbol, quantity)

        # maintain active order list
        for symbol, order in list(self.orders.items()):
            if order.status not in ALIVE:
                del self.orders[symbol]
            elif order.order_type == OrderType.LIMIT:
                self.maintain(algorithm, symbol, order)

    def converge(self, algorithm: QCAlgorithm, symbol: Symbol, quantity: float) -> None:
        security = algorithm.securities[symbol]
        quantity = self.round_quantity(algorithm, security, quantity - security.holdings.quantity)

        # figure out when this executor will be called next
        if security.resolution == Resolution.HOUR:
            future = algorithm.time + timedelta(hours=1)
        elif security.resolution == Resolution.MINUTE:
            future = algorithm.time + timedelta(minutes=1)
        elif security.resolution in (Resolution.SECOND, Resolution.TICK):
            future = algorithm.time + timedelta(seconds=1)
        else:
            assert False, "unsupported resolution"

        # Cancel existing order if quantity is noop
        if not quantity and symbol in self.orders:
            order = self.orders[symbol]
            if order.status in ALIVE:
                algorithm.log(f"Canceling order for {symbol.value} it is not needed")
                order.cancel()
            del self.orders[symbol]
            return

        # if this is our last chance this trading day, convert it to a market-on-close order
        if not security.exchange.hours.is_open(future, extended_market_hours=False):
            order = self.orders.get(symbol)
            if order is not None:
                if order.order_type == OrderType.MARKET_ON_CLOSE:
                    return
                else:
                    algorithm.log(f"Converting order for {symbol.value} to market-on-close")
                    if order.status in ALIVE:
                        order.cancel()
            elif quantity:
                algorithm.log(f"Issuing market-on-close order for {quantity} of {symbol.value}")
            if quantity:
                self.orders[symbol] = algorithm.market_on_close_order(symbol, quantity, asynchronous=True)
            return

        # get rid of dead order
        if symbol in self.orders:
            order = self.orders[symbol]
            if order.status not in ALIVE:
                del self.orders[symbol]

        # create new limit order at same price as market makers
        price = self.choose_price(algorithm, symbol, quantity)
        if not price:
            algorithm.debug(f"no quote bars for {symbol}")
            return
        if symbol not in self.orders:
            if quantity:
                algorithm.log(f"Creating new limit order for {quantity} of {symbol.value} at ${price:,.2f}")
                self.orders[symbol] = algorithm.limit_order(symbol, quantity, price, asynchronous=True)
            return

        # update quantity if needed
        order = self.orders[symbol]
        if order.quantity_remaining != quantity:
            algorithm.log(f"Updating market-on-close order from {order.quantity} to {quantity} of {symbol.value}")
            update = UpdateOrderFields()
            update.quantity = quantity + order.quantity_filled
            if order.order_type == OrderType.LIMIT:
                update.limit_price = price
            order.update(update)
            return

    def maintain(self, algorithm: QCAlgorithm, symbol: Symbol, order: OrderTicket) -> None:
        security = algorithm.securities[symbol]

        # figure out when this executor will be called next
        if security.resolution == Resolution.HOUR:
            future = algorithm.time + timedelta(hours=1) # technically not supported in production
        elif security.resolution == Resolution.MINUTE:
            future = algorithm.time + timedelta(minutes=1)
        elif security.resolution in (Resolution.SECOND, Resolution.TICK):
            future = algorithm.time + timedelta(seconds=1)
        else:
            assert False, "unsupported resolution"

        # if this is our last chance this trading day, convert it to a market-on-close order
        if not security.exchange.hours.is_open(future, extended_market_hours=False):
            if order.status in ALIVE:
                order.cancel()
            del self.orders[symbol]
            quantity = order.quantity - order.quantity_filled
            if quantity:
                algorithm.log(f"Converting to market-on-close order for {quantity} of {symbol.value}")
                self.orders[symbol] = algorithm.market_on_close_order(symbol, quantity, asynchronous=True)
            return

        # move price along with the market
        price = self.choose_price(algorithm, symbol, order.quantity)
        if price and price != order.get(OrderField.LIMIT_PRICE):
            order.update_limit_price(price)

    def choose_price(self, algorithm: QCAlgorithm, symbol: Symbol, quantity: float) -> float:
        security = algorithm.securities[symbol]
        if algorithm.current_slice.quote_bars.contains_key(symbol):
            quote = algorithm.current_slice.quote_bars[symbol]
            if quantity < 0:  # selling
                price = quote.bid.close + self.greed * (quote.ask.close - quote.bid.close)
            else:  # buying
                price = quote.ask.close - self.greed * (quote.ask.close - quote.bid.close)
        elif not algorithm.live_mode and algorithm.current_slice.bars.contains_key(symbol):
            # only use bars as fallback in backtesting, since in TICK mode
            # the stocks that aren't as liquid won't show up in each slice
            price = algorithm.current_slice.bars[symbol].close
        else:
            return 0.0
        return self.round_price(security, price)

    def round_price(self, security: Security, price: float) -> float:
        mpv = security.symbol_properties.minimum_price_variation
        return round(price / mpv) * mpv

    def round_quantity(self, algorithm: QCAlgorithm, security: Security, quantity: float) -> float:
        lot_size = self.lot_size if algorithm.portfolio.margin_remaining > 0. else self.panic_lot_size
        lot_size = max(lot_size, security.symbol_properties.lot_size)
        return math.floor(quantity / lot_size) * lot_size
