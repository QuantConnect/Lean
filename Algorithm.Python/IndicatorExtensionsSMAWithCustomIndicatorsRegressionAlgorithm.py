# region imports
from AlgorithmImports import *
# endregion

class IndicatorExtensionsSMAWithCustomIndicatorsRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2020, 2, 20)
        self.qqq = self.add_equity("QQQ", Resolution.DAILY).symbol
        self.range_indicator = RangeIndicator("range")
        self.range_sma = IndicatorExtensions.sma(self.range_indicator, 5)

    def on_data(self, data):
        self.range_indicator.update(data.bars.get(self.qqq))
        self.debug(f"{self.range_indicator.name} {self.range_indicator.value}")
        self.debug(f"{self.range_sma.name} {self.range_sma.current.value}")

class RangeIndicator(PythonIndicator):
    def __init__(self, name):
        self.name = name
        self.time = datetime.min
        self.value = 0
        self._is_ready = False;

    def is_ready(self):
        return self._is_ready

    def update(self, bar: TradeBar):
        if bar is None:
            return False

        self.value = bar.high - bar.low
        self.time = bar.time
        self._is_ready = True
        self.on_updated(IndicatorDataPoint(bar.end_time, self.value))
        return True
