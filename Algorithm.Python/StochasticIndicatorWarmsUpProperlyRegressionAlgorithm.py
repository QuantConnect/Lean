from datetime import timedelta
from AlgorithmImports import *


class StochasticIndicatorWarmsUpProperlyRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2020, 1, 1)  # monday = holiday..
        self.set_end_date(2020, 2, 1)
        self.set_cash(100000)

        self.data_points_received = False;
        self.spy = self.add_equity("SPY", Resolution.HOUR).symbol

        self.daily_consolidator = TradeBarConsolidator(timedelta(days=1))

        self._rsi = RelativeStrengthIndex(14, MovingAverageType.WILDERS)
        self._sto = Stochastic("FIRST", 14, 3, 3)
        self.register_indicator(self.spy, self._rsi, self.daily_consolidator)
        self.register_indicator(self.spy, self._sto, self.daily_consolidator)

        # warm_up indicator
        self.warm_up_indicator(self.spy, self._rsi, timedelta(days=1))
        self.warm_up_indicator(self.spy, self._sto, timedelta(days=1))
        

        self._rsi_history = RelativeStrengthIndex(14, MovingAverageType.WILDERS)
        self._sto_history = Stochastic("SECOND", 14, 3, 3)
        self.register_indicator(self.spy, self._rsi_history, self.daily_consolidator)
        self.register_indicator(self.spy, self._sto_history, self.daily_consolidator)

        # history warm up
        history = self.history[TradeBar](self.spy, 15, Resolution.DAILY)
        count = 0
        for bar in history:
            count+=1
            if count == 15:
                break
            self._rsi_history.update(bar.end_time, bar.close)
            self._sto_history.update(bar)

    def on_data(self, data: Slice):
        if self.is_warming_up:
            return

        if data.contains_key(self.spy):
            self.data_points_received = True
            if self._rsi.current.value != self._rsi_history.current.value:
                raise Exception(f"Values of indicators differ: {self._rsi.name}: {self._rsi.current.value} | {self._rsi_history.name}: {self._rsi_history.current.value}")
            
            if self._sto.stoch_k.current.value != self._sto_history.stoch_k.current.value:
                raise Exception(f"Stoch K values of indicators differ: {self._sto.name}.StochK: {self._sto.stoch_k.current.value} | {self._sto_history.name}.StochK: {self._sto_history.stoch_k.current.value}")
            
            if self._sto.stoch_d.current.value != self._sto_history.stoch_d.current.value:
                raise Exception(f"Stoch D values of indicators differ: {self._sto.name}.StochD: {self._sto.stoch_d.current.value} | {self._sto_history.name}.StochD: {self._sto_history.stoch_d.current.value}")

    def on_end_of_algorithm(self):
        if not self.data_points_received:
            raise Exception("No data points received")
