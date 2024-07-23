from AlgorithmImports import *

class IndicatorSelectorWorksWithDifferentOptions(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 8)
        
        symbol = self.add_equity("SPY", Resolution.MINUTE).symbol
        self.bid_close_indicator = self.identity(symbol, Resolution.MINUTE, Field.BID_CLOSE, "Bid.Close.")
        self.bid_open_indicator = self.identity(symbol, Resolution.MINUTE, Field.BID_OPEN, "Bid.Open.")
        self.bid_low_indicator = self.identity(symbol, Resolution.MINUTE, Field.BID_LOW, "Bid.Low.")
        self.bid_high_indicator = self.identity(symbol, Resolution.MINUTE, Field.BID_HIGH, "Bid.High.")
        
        self.ask_close_indicator = self.identity(symbol, Resolution.MINUTE, Field.ASK_CLOSE, "Ask.Close.")
        self.ask_open_indicator = self.identity(symbol, Resolution.MINUTE, Field.ASK_OPEN, "Ask.Open.")
        self.ask_low_indicator = self.identity(symbol, Resolution.MINUTE, Field.ASK_LOW, "Ask.Low.")
        self.ask_high_indicator = self.identity(symbol, Resolution.MINUTE, Field.ASK_HIGH, "Ask.High.")

        self.quotebars_found = False

    def on_data(self, slice):
        if len(slice.quote_bars) != 0:
            self.quotebars_found = True
            if slice.quote_bars["SPY"].bid.close != self.bid_close_indicator.current.value:
                close_value = slice.quote_bars["SPY"].bid.close
                raise Exception(f"{self.bid_close_indicator.__name__} should have been {close_value}, but was {self.bid_close_indicator.current.value}")

            if slice.quote_bars["SPY"].bid.open != self.bid_open_indicator.current.value:
                open_value = slice.quote_bars["SPY"].bid.open
                raise Exception(f"{self.bid_open_indicator.__name__} should have been {open_value}, but was {self.bid_open_indicator.current.value}")

            if slice.quote_bars["SPY"].bid.low != self.bid_low_indicator.current.value:
                low_value = slice.quote_bars["SPY"].bid.low
                raise Exception(f"{self.bid_low_indicator.__name__} should have been {low_value}, but was {self.bid_low_indicator.current.value}")

            if slice.quote_bars["SPY"].bid.high != self.bid_high_indicator.current.value:
                high_value = slice.quote_bars["SPY"].bid.high
                raise Exception(f"{self.bid_high_indicator.__name__} should have been {high_value}, but was {self.bid_high_indicator.current.value}")

            if slice.quote_bars["SPY"].ask.close != self.ask_close_indicator.current.value:
                close_value = slice.quote_bars["SPY"].ask.close
                raise Exception(f"{self.ask_close_indicator.__name__} should have been {close_value}, but was {self.ask_close_indicator.current.value}")

            if slice.quote_bars["SPY"].ask.open != self.ask_open_indicator.current.value:
                open_value = slice.quote_bars["SPY"].bid.open
                raise Exception(f"{self.ask_open_indicator.__name__} should have been {open_value}, but was {self.ask_open_indicator.current.value}")

            if slice.quote_bars["SPY"].ask.low != self.ask_low_indicator.current.value:
                low_value = slice.quote_bars["SPY"].bid.low
                raise Exception(f"{self.ask_low_indicator.__name__} should have been {low_value}, but was {self.ask_low_indicator.current.value}")

            if slice.quote_bars["SPY"].ask.high != self.ask_high_indicator.current.value:
                high_value = slice.quote_bars["SPY"].bid.high
                raise Exception(f"{self.ask_high_indicator.__name__} should have been {high_value}, but was {self.ask_high_indicator.current.value}")
            
    def on_end_of_algorithm(self):
        if not self.quotebars_found:
            raise Exception("At least one quote bar should have been found, but none was found")
