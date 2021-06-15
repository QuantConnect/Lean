import asyncio
import ccxtpro
import ccxt
import json
import logging
import sys
from threading import Thread

class AsyncLoopThread(Thread):
    def __init__(self):
        super().__init__(daemon=True)
        self.loop = asyncio.new_event_loop()

    def run(self):
        asyncio.set_event_loop(self.loop)
        #self.loop.set_debug(True)
        self.loop.run_forever()
        return self.loop

class CcxtPythonBridge:
    #def __init__(self):
    #    logging.basicConfig(level=logging.DEBUG)

    def Initialize(self, exchange_name, config_json, order_event_handler, trade_data_handler, quote_data_handler):
        self.loop_handler = AsyncLoopThread()
        self.loop_handler.start()

        config = json.loads(config_json)
        config['asyncio_loop'] = self.loop_handler.loop

        self.exchange_name = exchange_name
        self.exchange = getattr(sys.modules['ccxtpro'], exchange_name)(config)

        self.log(f'Loaded exchange: ' + exchange_name)

        self.exchange.options['warnOnFetchOpenOrdersWithoutSymbol'] = False
        #self.exchange.verbose = True  # for debugging

        if 'watchOrders' in self.exchange.has and self.exchange.has['watchOrders']:
            self.run_async(self.watch_orders(order_event_handler))
        else:
            raise Exception(f'Exchange "{exchange_name}" does not support method "watchOrders "')

        self.subscriptions = {}
        self.trade_data_handler = trade_data_handler
        self.quote_data_handler = quote_data_handler

    def GetVersionInformation(self):
        return { 'ccxt': ccxt.__version__, 'ccxtPro': ccxtpro.__version__ }

    def Terminate(self):
        # exchange.close() requires Python 3.7 --> AttributeError : module 'asyncio' has no attribute 'create_task' (error in 3.6)
        # https://github.com/kroitor/ccxt.pro/blob/master/python/ccxtpro/base/exchange.py#L166
        # self.run_sync(self.exchange.close())

        self.loop_handler.loop.stop()

        self.log(f'Terminated exchange: ' + self.exchange_name)

    def GetBalances(self):
        return self.run_sync(self.exchange.fetch_balance())

    def GetOpenOrders(self):
        return self.run_sync(self.exchange.fetchOpenOrders())

    def PlaceMarketOrder(self, symbol, side, amount):
        # gateio - not supported by exchange
        if self.exchange_name == 'gateio':
            raise Exception(f'PlaceStopMarketOrder(): stop market orders are not supported for exchange: {self.exchange_name}')

        return self.run_sync(self.exchange.createOrder(symbol, 'market', side, amount))

    def PlaceLimitOrder(self, symbol, side, amount, limit_price):
        return self.run_sync(self.exchange.createOrder(symbol, 'limit', side, amount, limit_price))

    def PlaceStopMarketOrder(self, symbol, side, amount, stop_price):
        # stop market orders in ccxt are not unified yet

        # binance - not supported by exchange
        # bittrex - not supported by ccxt yet
        # coinbasepro - not supported by exchange
        # gateio - not supported by exchange

        if self.exchange_name == 'ftx':
            return self.run_sync(self.exchange.createOrder(symbol, 'stop', side, amount, None, { 'stopPrice': stop_price }))

        if self.exchange_name == 'kraken':
            return self.run_sync(self.exchange.createOrder(symbol, 'stop-loss', side, amount, None, { 'stopPrice': stop_price }))

        raise Exception(f'PlaceStopMarketOrder(): stop market orders are not supported for exchange: {self.exchange_name }')

    def PlaceStopLimitOrder(self, symbol, side, amount, stop_price, limit_price):
        # stop limit orders in ccxt are not unified yet

        if self.exchange_name == 'binance':
            return self.run_sync(self.exchange.createOrder(symbol, 'stop_loss_limit', side, amount, limit_price, { 'stopPrice': stop_price }))

        # bittrex - not supported by ccxt yet

        if self.exchange_name == 'coinbasepro':
            return self.run_sync(self.exchange.createOrder(symbol, 'limit', side, amount, limit_price, { 'stop_price': stop_price, 'stop': 'loss' }))

        if self.exchange_name == 'ftx':
            return self.run_sync(self.exchange.createOrder(symbol, 'stop', side, amount, limit_price, { 'stopPrice': stop_price }))

        # gateio - not supported by ccxt yet

        if self.exchange_name == 'kraken':
            return self.run_sync(self.exchange.createOrder(symbol, 'stop-loss-limit', side, amount, None, { 'stopPrice': stop_price, 'price2': limit_price }))

        raise Exception(f'PlaceStopLimitOrder(): stop limit orders are not supported for exchange: {self.exchange_name}')

    def CancelOrder(self, id, symbol):
        return self.run_sync(self.exchange.cancelOrder(id, symbol))

    def Subscribe(self, symbol):
        self.subscriptions[symbol] = True

        self.log(f"Subscribing trades: {symbol}")
        futureTrades = self.run_async(self.watch_trades(symbol))

        self.log(f"Subscribing book: {symbol}")
        futureOrderBook = self.run_async(self.watch_order_book(symbol))

    def Unsubscribe(self, symbol):
        self.subscriptions[symbol] = False

    def log(self, text):
        print(text)

    def run_async(self, coroutine):
        return asyncio.run_coroutine_threadsafe(coroutine, self.loop_handler.loop)

    def run_sync(self, coroutine):
        return self.run_async(coroutine).result(None)

    async def watch_orders(self, order_event_handler):
        while True:
            result = await self.exchange.watch_orders()
            order_event_handler(json.dumps(result))

    async def watch_trades(self, symbol):
        try:
            print('start watch_trades')
            #raise Exception('test watch_trades exception')
            while self.subscriptions[symbol]:
                result = await self.exchange.watch_trades(symbol)
                #self.log(result)
                self.trade_data_handler(result)
        except e:
            self.log(e)

    async def watch_order_book(self, symbol):
        while self.subscriptions[symbol]:
            result = await self.exchange.watch_order_book(symbol)
            #self.log(result)
            self.quote_data_handler(symbol, result)

