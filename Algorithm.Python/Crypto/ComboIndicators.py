# For moving avg types: https://github.com/QuantConnect/Lean/blob/bc9af8784b02715000a2030e9757ef63b484378e/Indicators/MovingAverageType.cs
import QuantConnect.Indicators as ind
from QuantConnect.Orders import OrderDirection
from configs import configs

class ComboIndicators(QCAlgorithm):
    def Initialize(self):
        #####################
        # Backtest Settings #
        #####################
        self.SetStartDate(2017, 7, 7)  # Set Start Date
        self.SetEndDate(2018, 1, 2)  # Set End Date
        self.SetCash(1000)  # Set Strategy Cash
        self.SetBrokerageModel(BrokerageName.GDAX)

        ###########################
        # Configurable parameters #
        ###########################
        self.target_crypto = configs["target_crypto"] # Can be ETHUSD, LTCUSD, BTCUSD, or BCCUSD
        self.indicator_name = configs["indicator_name"]  # bollinger, momentum, or MACD
        self.warmup_lookback = configs["warmup_lookback"]  # Number of time periods resolution to load
        self.time_resolution = configs["time_resolution"]  # Resolution of periods/data to use
        self.resubmit_order_threshold = configs["resubmit_order_threshold"]  # Percent at which we will update the limit order to cause a fill
        self.bar_size = configs["bar_size"]

        # Bollinger Band Variables
        self.moving_average_type = configs["moving_average_type"]
        self.bollinger_period = configs["bollinger_period"]
        self.k = configs["k"]

        # Volume
        self.momentum_period = configs["momentum_period"]
        self.momentum_buy_threshold = configs["momentum_buy_threshold"]
        self.momentum_sell_threshold = configs["momentum_sell_threshold"]

        # MACD Variables
        self.MACD_fast_period = configs["MACD_fast_period"]
        self.MACD_slow_period = configs["MACD_slow_period"]
        self.MACD_signal_period = configs["MACD_signal_period"]
        self.MACD_moving_average_type = configs["MACD_moving_average_type"]
        self.MACD_tolerance = configs["MACD_tolerance"]

        # Ichimoku Variables
        self.tenkanPeriod = configs["tenkanPeriod"]
        self.kijunPeriod = configs["kijunPeriod"]
        self.senkouAPeriod = configs["senkouAPeriod"]
        self.senkouBPeriod = configs["senkouBPeriod"]
        self.senkouADelayedPeriod = configs["senkouADelayedPeriod"]
        self.senkouBDelayedPeriod = configs["senkouBDelayedPeriod"]

        ############################
        # Indicators and processes #
        ############################
        # Add Symbol
        self.AddCrypto(self.target_crypto, self.time_resolution)

        barConsolidator = TradeBarConsolidator(TimeSpan.FromMinutes(self.bar_size))
        barConsolidator.DataConsolidated += self.barHandler
        self.SubscriptionManager.AddConsolidator(self.target_crypto, barConsolidator)

        # Create charts
        pricePlot = Chart('%s Price Plot' % self.target_crypto)
        pricePlot.AddSeries(Series('Price', SeriesType.Line, 0))
        self.AddChart(pricePlot)

        holdingsPlot = Chart('%s Holdings Plot' % self.target_crypto)
        holdingsPlot.AddSeries(Series('Holdings', SeriesType.Line, 0))
        self.AddChart(holdingsPlot)

        # Create the different indicators
        if self.indicator_name == "bollinger":
            # Create bollinger band
            self.Bolband = self.BB(self.target_crypto, self.bollinger_period, self.k, self.moving_average_type,
                                   self.time_resolution)
        elif self.indicator_name == "momentum":
            # Create a momentum indicator over 3 days
            self.mom = self.MOM(self.target_crypto, self.momentum_period, self.time_resolution)
        elif self.indicator_name == "macd":
            # Create the MACD
            self.macd = self.MACD(self.target_crypto, self.MACD_fast_period, self.MACD_slow_period,
                                  self.MACD_signal_period, self.MACD_moving_average_type, self.time_resolution)
            self.RegisterIndicator(self.target_crypto, self.macd, barConsolidator)
        elif self.indicator_name == "ichimoku":
            self.ichimoku = self.ICHIMOKU(self.target_crypto, self.tenkanPeriod, self.kijunPeriod, self.senkouAPeriod,
                                          self.senkouBPeriod, self.senkouADelayedPeriod, self.senkouBDelayedPeriod,
                                          self.time_resolution * self.time_resolution)
        # Processing variables
        self.pending_limit_price = 0

        #####################
        # Scheduled Actions #
        #####################
        self.Schedule.On(self.DateRules.EveryDay(self.target_crypto), self.TimeRules.At(12, 0),
                         Action(self.PlotCryptoIndicator))

    def OnData(self, data):
        ##########################
        # OnData Processing Vars #
        ##########################
        last_price = self.Securities[self.target_crypto].Close
        buy_price = self.Securities[self.target_crypto].BidPrice
        sell_price = self.Securities[self.target_crypto].AskPrice

        ###############################
        # OnData Processing Functions #
        ###############################
        # Update limit order if the price moves
        if len(self.Transactions.GetOpenOrders(self.target_crypto)) > 0:
            open_order = self.Transactions.GetOpenOrders(self.target_crypto)[0]
            if abs(float(self.pending_limit_price - last_price) / float(
                    self.pending_limit_price)) > self.resubmit_order_threshold:
                # self.Debug("Open Order Price: %s last_price: %s percent change: %s resubmit_order_threshold: %s order direction:%s" % (self.pending_limit_price, last_price,abs(float(self.pending_limit_price - last_price) / float(self.pending_limit_price)),self.resubmit_order_threshold,open_order.Direction))
                limit_price = buy_price if open_order.Direction == 0 else sell_price
                # self.Debug("Updating order to limit price of %s, amount of %s, and direction of %s" % (str(limit_price),str(amount),str(open_order.Direction)))
                self.Transactions.CancelOrder(open_order.Id)
                self.LimitOrder(self.target_crypto, open_order.Quantity, limit_price)
                self.pending_limit_price = limit_price
            return

    def PlotCryptoIndicator(self):
        # Chart the crypto price
        self.Plot('%s Price Plot' % self.target_crypto, 'Price', self.Securities[self.target_crypto].Close)
        self.Plot('%s Holdings Plot' % self.target_crypto, 'Holdings',
                  float(self.Portfolio[self.target_crypto].Quantity))

    def barHandler(self, sender, bar):
        ##########################
        # OnData Processing Vars #
        ##########################
        holdings = self.Portfolio[self.target_crypto].Quantity
        last_price = self.Securities[self.target_crypto].Close
        buy_price = self.Securities[self.target_crypto].BidPrice
        sell_price = self.Securities[self.target_crypto].AskPrice
        amount = float(self.Portfolio.GetBuyingPower(self.target_crypto, OrderDirection.Buy) / last_price)

        if len(self.Transactions.GetOpenOrders(self.target_crypto)) > 0:
            return

        ###################
        # Indicator Logic #
        ###################
        if self.indicator_name == "bollinger":
            # buy if price closes above upper bollinger band
            # sell if price closes below middle bollinger band
            if holdings == 0 and last_price > self.Bolband.UpperBand.Current.Value:
                self.LimitOrder(self.target_crypto, amount, buy_price)
                self.pending_limit_price = buy_price
            elif holdings > 0 and last_price < self.Bolband.MiddleBand.Current.Value:
                self.LimitOrder(self.target_crypto, -holdings, sell_price)
                self.pending_limit_price = sell_price
        elif self.indicator_name == "momentum":
            mom = self.mom.Current.Value
            if holdings == 0 and mom > self.momentum_buy_threshold:
                self.LimitOrder(self.target_crypto, amount, buy_price)
                self.pending_limit_price = buy_price
            elif holdings > 0 and mom < self.momentum_sell_threshold:
                self.LimitOrder(self.target_crypto, -holdings, sell_price)
                self.pending_limit_price = sell_price
        elif self.indicator_name == "macd":
            if not self.macd.IsReady:
                return
            signalDeltaPercent = (self.macd.Current.Value - self.macd.Signal.Current.Value) / self.macd.Fast.Current.Value

            if holdings == 0 and signalDeltaPercent > self.MACD_tolerance:
                self.LimitOrder(self.target_crypto, amount, buy_price)
                self.pending_limit_price = buy_price
            elif holdings > 0 and signalDeltaPercent < -self.MACD_tolerance:
                self.LimitOrder(self.target_crypto, -holdings, sell_price)
                self.pending_limit_price = sell_price
        elif self.indicator_name == "ichimoku":
            if not self.ichimoku.IsReady:
                return
            # self.Debug("TenkanMax: %s Tenkan: %s" % (str(self.ichimoku.TenkanMaximum.Current.Value), str(self.ichimoku.Tenkan.Current.Value)))
            if holdings == 0 and (self.ichimoku.Tenkan.Current.Value > self.ichimoku.Kijun.Current.Value
                                  and self.ichimoku.SenkouA.Current.Value > self.ichimoku.SenkouB.Current.Value
                                  and last_price > self.ichimoku.SenkouA.Current.Value):
                self.LimitOrder(self.target_crypto, amount, buy_price)
                self.pending_limit_price = buy_price
            elif holdings > 0 and (self.ichimoku.Tenkan.Current.Value <= self.ichimoku.Kijun.Current.Value
                                   and self.ichimoku.SenkouA.Current.Value < self.ichimoku.SenkouB.Current.Value
                                   and last_price < self.ichimoku.SenkouA.Current.Value):
                self.LimitOrder(self.target_crypto, -holdings, sell_price)
                self.pending_limit_price = sell_price

    # def OnOrderEvent(self, orderEvent):
    #    order = self.Transactions.GetOrderById(orderEvent.OrderId)
    #    self.Debug("{0}: {1}: {2}".format(self.Time, order.Type, orderEvent))