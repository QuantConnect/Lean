from typing import Optional
from datetime import datetime, timedelta
from AlgorithmImports import * # type: ignore


class MyConstantAlphaModel(AlphaModel):
    """Alpha model that generates insights at regular interval.

    The optimal time to rebalance a portfolio which will avoid overtrading is fortnightly.
    Unlike the default ConstantAlphaModel, this model ensures that insights are generated
    on the first day the program launches, so you don't have to wait two weeks for your
    portfolio to be invested.
    """

    def __init__(self, rebalance_period=timedelta(14), min_share_price=5.0):
        super().__init__()
        self.rebalance_period = rebalance_period
        self.min_share_price = min_share_price
        self.next_rebalance: Optional[datetime] = None
        self.state = 0

    def update(self, algorithm: QCAlgorithm, data: Slice) -> list[Insight]:
        insights = []
        should_generate = False

        # ensure algorithm triggers at start of warmup and start of live trading
        # whereas the rest of the time, it only triggers on the rebalance period
        if self.state == 0:
            should_generate = True
            if algorithm.is_warming_up:
                self.state = 1
            else:
                self.state = 2
        elif self.state == 1:
            if algorithm.is_warming_up:
                assert self.next_rebalance is not None
                should_generate = algorithm.time >= self.next_rebalance
            else:
                should_generate = True
                self.state = 2
        elif self.state == 2:
            assert self.next_rebalance is not None
            should_generate = algorithm.time >= self.next_rebalance

        # generate insights if needed
        if should_generate:
            self.next_rebalance = algorithm.time + self.rebalance_period
            algorithm.log(f"Generating insights for {len(algorithm.active_securities)} securities")
            for security in algorithm.active_securities.values:
                if security.is_tradable and security.price >= self.min_share_price:
                    insights.append(Insight.price(security.symbol, self.rebalance_period, InsightDirection.UP))

        return insights


class MyNdxBiasedAlphaModel(AlphaModel):
    """Alpha model that generates insights with NDX-based confidence weighting.

    This model calculates NDX position relative to 52-week range and assigns confidence
    values to bias portfolio toward tech stocks when NDX is near lows and toward non-tech
    when NDX is near highs.

    The optimal time to rebalance a portfolio which will avoid overtrading is fortnightly.
    Unlike the default ConstantAlphaModel, this model ensures that insights are generated
    on the first day the program launches, so you don't have to wait two weeks for your
    portfolio to be invested.
    """

    def __init__(self, rebalance_period=timedelta(14), min_share_price=5.0):
        super().__init__()
        self.rebalance_period = rebalance_period
        self.min_share_price = min_share_price
        self.next_rebalance: Optional[datetime] = None
        self.state = 0

        # NDX symbol for tracking
        self.ndx_symbol = Symbol.create("NDX", SecurityType.INDEX, Market.USA)

    def update(self, algorithm: QCAlgorithm, data: Slice) -> list[Insight]:
        insights = []
        should_generate = False

        # ensure algorithm triggers at start of warmup and start of live trading
        # whereas the rest of the time, it only triggers on the rebalance period
        if self.state == 0:
            should_generate = True
            if algorithm.is_warming_up:
                self.state = 1
            else:
                self.state = 2
        elif self.state == 1:
            if algorithm.is_warming_up:
                assert self.next_rebalance is not None
                should_generate = algorithm.time >= self.next_rebalance
            else:
                should_generate = True
                self.state = 2
        elif self.state == 2:
            assert self.next_rebalance is not None
            should_generate = algorithm.time >= self.next_rebalance

        # generate insights if needed
        if should_generate:
            self.next_rebalance = algorithm.time + self.rebalance_period
            algorithm.log(f"Generating insights for {len(algorithm.active_securities)} securities")

            # Calculate NDX bias factor
            ndx_bias_factor = self._calculate_ndx_bias(algorithm)

            for security in algorithm.active_securities.values:
                if security.is_tradable and security.price >= self.min_share_price and security.symbol != self.ndx_symbol:
                    # Determine if this is a tech stock using Morningstar sector data
                    is_tech = self._is_tech_stock(algorithm, security)

                    # Calculate confidence based on NDX position and stock type
                    confidence = self._calculate_confidence(ndx_bias_factor, is_tech)

                    insights.append(Insight.price(
                        security.symbol,
                        self.rebalance_period,
                        InsightDirection.UP,
                        confidence=confidence
                    ))

        return insights

    def _calculate_ndx_bias(self, algorithm: QCAlgorithm) -> float:
        """Calculate NDX position relative to 52-week range.
        Returns value between 0 (at 52-week low) and 1 (at 52-week high).
        """
        try:
            ndx_security = algorithm.securities.get(self.ndx_symbol)
            if ndx_security is None:
                return 0.5  # Default to neutral if NDX not available

            # Get 52-week (252 trading days) price history
            history = algorithm.history(self.ndx_symbol, 252, Resolution.DAILY)
            if history.empty:
                return 0.5  # Default to neutral if no history

            current_price = ndx_security.price
            week_52_low = history['low'].min()
            week_52_high = history['high'].max()

            if week_52_high == week_52_low:
                return 0.5  # Avoid division by zero

            # Calculate position in 52-week range (0 = low, 1 = high)
            ndx_position = (current_price - week_52_low) / (week_52_high - week_52_low)
            ndx_position = max(0.0, min(1.0, ndx_position))  # Clamp to [0, 1]

            algorithm.log(f"NDX 52-week position: {ndx_position:.3f} (Price: {current_price:.2f}, Low: {week_52_low:.2f}, High: {week_52_high:.2f})")
            return ndx_position

        except BaseException as e:
            algorithm.log(f"Error calculating NDX bias: {e}")
            return 0.5  # Default to neutral on error

    def _calculate_confidence(self, ndx_position: float, is_tech: bool) -> float:
        if is_tech:
            # Tech stocks: High confidence when NDX is low (contrarian)
            # When NDX position is 0 (low), confidence is 1.0
            # When NDX position is 1 (high), confidence is 0.2
            # confidence = 1.0 - (ndx_position * 0.9)
            confidence = 0.25 + ndx_position * 0.75
        else:
            # Non-tech stocks: High confidence when NDX is high
            # When NDX position is 0 (low), confidence is 0.2
            # When NDX position is 1 (high), confidence is 1.0
            # confidence = 0.2 + (ndx_position * 0.9)
            confidence = 0.5

        # Ensure confidence is in reasonable range
        return max(0.1, min(1.0, confidence))

    def _is_tech_stock(self, algorithm: QCAlgorithm, security: Security) -> bool:
        """Determine if a stock is a tech stock using Morningstar sector data and fallback list."""

        # First try to use Morningstar fundamental data
        if hasattr(security, 'fundamentals') and security.fundamentals:
            fundamental = security.fundamentals
            tech_sectors = (
                MorningstarSectorCode.TECHNOLOGY,
                MorningstarSectorCode.COMMUNICATION_SERVICES,
            )
            tech_groups = (
                MorningstarIndustryGroupCode.SOFTWARE,
                MorningstarIndustryGroupCode.SEMICONDUCTORS,
            )
            if hasattr(fundamental, 'asset_classification'):
                if (fundamental.asset_classification.morningstar_sector_code in tech_sectors and
                    fundamental.asset_classification.morningstar_industry_group_code in tech_groups):
                    return True

        # Fallback: Use hardcoded list for companies we know are tech
        # This is necessary because Morningstar data might not always be available
        # or some companies might be misclassified
        tech_symbols = {
            "MSFT", "AVGO", "AMZN", "NVDA", "TSLA", "TSM", "AMD", "ORCL", "NET", "PLTR",
            "SAP", "ASML", "META", "GOOGL", "GOOG", "AAPL", "CRM", "ADBE", "INTU",
            "QCOM", "INTC", "MU", "ANET", "PANW", "NOW"
        }

        return security.symbol.value in tech_symbols
