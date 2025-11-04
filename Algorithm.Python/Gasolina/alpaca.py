from AlgorithmImports import * # type: ignore
from datetime import time, timedelta


class AlpacaMarginInterestRateModel:
    """
    Alpaca margin interest rate model following their exact specifications:
    - 7.0% annual rate (varies by date and account type)
    - Charged only on end-of-day (overnight) debit balance
    - Formula: daily_margin_interest_charge = (settlement_date_debit_balance * 0.070) / 360
    - Interest accrues daily and posts monthly
    - Weekend charges: Friday EOD balance incurs 3 days of interest (Fri, Sat, Sun)
    See https://docs.alpaca.markets/docs/margin-and-short-selling
    """

    def __init__(self, algorithm: QCAlgorithm, annual_rate=0.070):
        self.algorithm = algorithm
        self.annual_rate = annual_rate
        self.total_margin_costs: float = 0.
        self.year_margin_costs: dict[int, float] = {}
        algorithm.schedule.on(
            algorithm.date_rules.every_day(),
            algorithm.time_rules.after_market_close("SPY"),
            self.calculate_eod_margin_interest)

    def calculate_eod_margin_interest(self):
        """
        Calculate end-of-day margin interest following Alpaca's specification.
        Only charges interest on overnight debit balance.
        """
        if self.algorithm.is_warming_up:
            return
        date = self.algorithm.time.date()

        # Calculate settlement date debit balance (borrowed amount)
        if self.algorithm.portfolio.cash < 0:
            settlement_date_debit_balance = abs(self.algorithm.portfolio.cash)

            # Formula documented on Alpaca website
            daily_margin_interest_charge = (settlement_date_debit_balance * self.annual_rate) / 360

            # Handle weekend charges: Friday EOD balance incurs 3 days of interest
            days_to_charge = 1
            if self.algorithm.time.weekday() == 4:  # Friday (0=Monday, 4=Friday)
                days_to_charge = 3  # Charge for Fri, Sat, Sun
            total_charge = daily_margin_interest_charge * days_to_charge

            # Track damage
            self.total_margin_costs += total_charge
            if date.year not in self.year_margin_costs:
                self.year_margin_costs[date.year] = 0.0
            self.year_margin_costs[date.year] += total_charge

            # Deduct margin interest from cash
            self.algorithm.portfolio.cash_book["USD"].add_amount(-total_charge)
            self.algorithm.set_runtime_statistic("Margin Interest", f"${-self.total_margin_costs:,.2f}")

            # Log the charge
            weekend_note = " (3-day weekend charge)" if days_to_charge == 3 else ""
            self.algorithm.debug(f"EOD Margin Interest: ${total_charge:,.2f} | "
                                 f"Debit Balance: ${settlement_date_debit_balance:,.2f} | "
                                 f"YTD Damage: ${self.year_margin_costs[date.year]:,.2f}")
