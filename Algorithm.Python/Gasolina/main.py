from datetime import timedelta
from AlgorithmImports import * # type: ignore
from alpha import MyConstantAlphaModel
from execution import LimitExecutionModel
from alpaca import AlpacaMarginInterestRateModel
from portfolio import MyEqualWeightingPortfolioConstructionModel


class HumunculusAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2021, 11, 26)
        self.set_cash(1_000_000)
        # self.set_warm_up(365)
        self.set_benchmark(index("NDX"))
        #self.set_brokerage_model(BrokerageName.ALPACA, AccountType.MARGIN)
        #self.set_security_initializer(self.custom_security_initializer)
        self.settings.minimum_order_margin_portfolio_percentage = 0
        self.universe_settings.resolution = Resolution.MINUTE
        self.universe_settings.leverage = 4.  # alpaca allows 4x leverage during the day
        overnight_leverage = 1/.3  # alpaca allows 3.33x leverage overnight
        rebalance_period = timedelta(14)  # fortnightly rebalancing considered optimal
        min_share_price = 5.0  # alpaca has 100% margin requirement on stocks under $2.50
        margin_rate = 0.0675 + .025  # oct 2025 prole rate
        margin_rate = 0.0425 + .010  # oct 2025 elite rate
        lot_size = 100

        stocks = [

            # 0. Technology, Communications, and Semiconductors
            equity("MSFT"),
            # equity("AVGO"),
            # equity("AMZN"),
            # equity("NVDA"),
            # equity("TSLA"),
            # equity("TSM"),
            # equity("AMD"),
            # equity("ORCL"),
            # equity("NET"),
            # equity("PLTR"),

            # # 1. Gold
            # equity("IAUM"),
            # equity("NEM"),

            # # 2. Treasuries
            # equity("TBT"),

            # # 3. Commodities & Energy (Low correlation to stocks)
            # equity("XOM"),  # Oil major
            # equity("CVX"),  # Oil major
            # equity("SLB"),  # Oil services
            # equity("DBA"),  # Agriculture futures

            # # 4. International Diversification
            # equity("ASML"), # Dutch lithography
            # equity("NVO"),  # Wonder drug maker
            # equity("SAP"),  # German software
            # equity("NVS"),  # Swiss pharma
            # equity("FXI"),  # China ETF
            # equity("BABA"), # Alibaba

            # # 5. Utilities & Defensive (Low volatility, steady dividends)
            # equity("CEG"),  # Constellation Energy
            # equity("SO"),   # Southern Company
            # equity("DUK"),  # Duke Energy
            # equity("JNJ"),  # Healthcare defensive
            # equity("PG"),   # Consumer defensive
            # equity("CAT"),  # Construction

            # # 6. We're going to hell for owning these
            # equity("BTI"),
            # equity("PM"),
            # equity("LYV"),
            # equity("BLK"),

            # # 7. Safe Havens
            # equity("MCK"),  # McKesson (healthcare distributor)
            # equity("UNH"),  # UnitedHealth (healthcare)
        ]

        # # use 100 largest cap stocks in s&p 500 index instead
        # sp100 = """NVDA MSFT AAPL AMZN META AVGO GOOGL GOOG TSLA BRK/B
        # JPM WMT ORCL LLY V MA NFLX XOM COST JNJ HD PLTR ABBV BAC PG UNH
        # CVX GE KO TMUS CSCO WFC PM AMD MS GS IBM ABT CRM AXP LIN MCD T
        # RTX DIS MRK CAT UBER PEP NOW VZ C TMO INTU BKNG MU ANET QCOM BLK
        # GEV SCHW SPGI TXN BA ISRG TJX LOW BSX AMGN ACN ADBE LRCX NEE SYK
        # PGR APH COF ETN GILD BX DHR PFE HON AMAT PANW KKR UNP KLAC DE
        # CMCSA ADI MDT ADP COP WELL INTC MO CB DASH LMT GLD"""
        # stocks = [Symbol.create(t, SecurityType.EQUITY, Market.USA) for t in sp100.split()]

        #self.set_execution(LimitExecutionModel(lot_size=lot_size))
        self.add_universe_selection(ManualUniverseSelectionModel(stocks))
        self.add_alpha(ConstantAlphaModel(InsightType.PRICE, InsightDirection.UP, timedelta(30)))
        #self.add_alpha(MyConstantAlphaModel(rebalance_period=rebalance_period, min_share_price=min_share_price))
        self.set_portfolio_construction(MyEqualWeightingPortfolioConstructionModel(leverage=overnight_leverage, rebalance=self.universe_settings.resolution))
        #self.margin_rate_model = AlpacaMarginInterestRateModel(self, margin_rate)

    def custom_security_initializer(self, security: Security) -> None:
        #security.set_slippage_model(MarketImpactSlippageModel(self))
        security.set_leverage(self.universe_settings.leverage)


def index(ticker: str) -> Symbol:
    return Symbol.create(ticker, SecurityType.INDEX, Market.USA)

def equity(ticker: str) -> Symbol:
    return Symbol.create(ticker, SecurityType.EQUITY, Market.USA)
