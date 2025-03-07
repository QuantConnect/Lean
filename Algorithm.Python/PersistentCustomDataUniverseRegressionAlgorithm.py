import datetime
from AlgorithmImports import *

class PersistentCustomDataUniverseRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.set_start_date(2018, 1, 3)
        self.set_end_date(2018, 1, 20)

        universe = self.add_universe(StockDataSource, "my-stock-data-source", Resolution.DAILY, self.universe_selector)
        self._universe_symbol = universe.symbol
        self._data_received = False

    def universe_selector(self, data):
        return [x.symbol for x in data]
    
    def OnData(self, slice: Slice):
        self._data_received = True
        if self._universe_symbol not in slice:
            raise RegressionTestException("OnData did not receive data for the universe symbol.")
        
    def OnEndOfAlgorithm(self) -> None:
        if not self._data_received:
            raise RegressionTestException("No data was received after the universe selection.")

class StockDataSource(PythonData):

    def __init__(self):
        super().__init__()
        self.Symbols = []

    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live: bool) -> SubscriptionDataSource:
        url = "https://www.dropbox.com/s/ae1couew5ir3z9y/daily-stock-picker-backtest.csv?dl=1"
        return SubscriptionDataSource(url, SubscriptionTransportMedium.RemoteFile)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live: bool) -> BaseData:
        if not line.strip():
            return None

        try:
            csv = line.split(',')
            stocks = StockDataSource()
            stocks.Symbol = config.Symbol

            if is_live:
                # In live mode, the first column does not contain a date, so we use the provided date
                stocks.Time = date
                stocks.Symbols.extend(csv)
            else:
                # In backtest mode, the first column contains the date
                stocks.Time = datetime.strptime(csv[0], "%Y%m%d")
                stocks.Symbols.extend(csv[1:])

            return stocks

        except Exception:
            return None