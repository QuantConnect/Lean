from datetime import timedelta
from AlgorithmImports import * # type: ignore
from System.Collections.Generic import Dictionary

def direction_to_int(direction: InsightDirection) -> int:
    match direction:
        case InsightDirection.UP:
            return 1
        case InsightDirection.DOWN:
            return -1
        case _:
            return 0

class MyEqualWeightingPortfolioConstructionModel(PortfolioConstructionModel):
    """Portfolio construction model that sells high and buys the dip."""

    def __init__(self,
                 leverage=2.0,
                 rebalance=Resolution.HOUR,
                 portfolio_bias=PortfolioBias.LONG_SHORT):
        super().__init__()
        self.leverage = leverage
        self.portfolio_bias: PortfolioBias = portfolio_bias
        rebalancing_func = rebalance
        if isinstance(rebalance, Resolution):
            rebalance = Extensions.to_time_span(rebalance)
        if isinstance(rebalance, timedelta):
            rebalancing_func = lambda dt: dt + rebalance
        if rebalancing_func:
            self.set_rebalancing_func(rebalancing_func)

    def determine_target_percent(self, active_insights): # type: ignore
        count, result = sum(self.respect_portfolio_bias(x) for x in active_insights), {}
        if count == 0:
            return result
        percent = self.leverage / count

        for insight in active_insights:
            direction = InsightDirection.FLAT
            if self.respect_portfolio_bias(insight):
                direction = insight.direction
            # result[insight] = direction_to_int(insight.direction) * percent


            int_direction = int(insight.direction)
            res = int_direction * percent
            result[insight] = res

            for i in range(100):
                idddd = int(insight.direction)

        return result

    def respect_portfolio_bias(self, insight):
        if self.portfolio_bias == PortfolioBias.LONG_SHORT:
            return True
        match insight.direction:
            case InsightDirection.UP:
                return self.portfolio_bias == PortfolioBias.LONG
            case InsightDirection.DOWN:
                return self.portfolio_bias == PortfolioBias.SHORT
        return False

class MyConfidenceWeightedPortfolioConstructionModel(PortfolioConstructionModel):
    """Portfolio construction model that uses insight confidence to bias toward tech or non-tech stocks.

    Based on NDX position relative to 52-week range:
    - When NDX near 52-week low: Bias toward tech stocks (up to 80% allocation)
    - When NDX near 52-week high: Bias toward non-tech stocks (up to 80% allocation)
    """

    def __init__(self,
                 leverage=2.0,
                 rebalance=Resolution.HOUR,
                 portfolio_bias=PortfolioBias.LONG_SHORT):
        super().__init__()
        self.leverage = leverage
        self.portfolio_bias = portfolio_bias

        rebalancing_func = rebalance
        if isinstance(rebalance, Resolution):
            rebalance = Extensions.to_time_span(rebalance)
        if isinstance(rebalance, timedelta):
            rebalancing_func = lambda dt: dt + rebalance
        if rebalancing_func:
            self.set_rebalancing_func(rebalancing_func)

        # No need for tech classification - alpha model handles this via confidence values

    def determine_target_percent(self, active_insights): # type: ignore
        result = {}

        # Filter valid insights
        valid_insights = [x for x in active_insights
                          if x.direction != InsightDirection.FLAT and self.respect_portfolio_bias(x)]

        if not valid_insights:
            for insight in active_insights:
                result[insight] = 0
            return result

        # Calculate confidence-weighted allocations (alpha model sets confidence based on NDX bias)
        total_confidence = sum(insight.confidence or 0.5 for insight in valid_insights)

        if total_confidence == 0:
            # Fallback to equal weighting
            equal_weight = 1.0 / len(valid_insights)
            for insight in valid_insights:
                direction = insight.direction if self.respect_portfolio_bias(insight) else InsightDirection.FLAT
                result[insight] = int(direction) * equal_weight * self.leverage
        else:
            # Allocate based on confidence values (which already reflect NDX bias)
            for insight in valid_insights:
                confidence = insight.confidence or 0.5
                allocation = confidence / total_confidence
                direction = insight.direction if self.respect_portfolio_bias(insight) else InsightDirection.FLAT
                result[insight] = int(direction) * allocation * self.leverage

        # Handle any remaining insights that weren't processed
        for insight in active_insights:
            if insight not in result:
                result[insight] = 0

        return result

    def respect_portfolio_bias(self, insight):
        return self.portfolio_bias == PortfolioBias.LONG_SHORT or insight.direction == self.portfolio_bias
