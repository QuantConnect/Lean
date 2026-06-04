# region imports
from AlgorithmImports import *
# endregion

class SunnyWheelStrategy(QCAlgorithm):

    def initialize(self):

        # SPY Wheel Strategy - Sunny
        #
        # Phase 1: Sell a cash-secured put
        # Phase 2: Accept assignment and hold 100 shares
        # Phase 3: Sell covered calls against assigned shares
        #
        # Parameter Changes:
        # 1. Increased starting capital to $150,000
        # 2. Target 30 DTE options
        #
        # Assumptions:
        # This is a simplified conceptual wheel strategy for the internship exercise.

        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)

        self.set_cash(150000)

        self.spy = self.add_equity("SPY", Resolution.MINUTE).symbol

        self.target_dte = 30

        self.put_sold = False
        self.has_shares = False
        self.call_sold = False

    def on_data(self, data: Slice):

        # Phase 1 - Cash Secured Put
        if not self.put_sold and not self.has_shares:

            self.debug("Phase 1: Cash-Secured Put")

            self.put_sold = True

            # Placeholder representing put sale
            self.market_order(self.spy, 100)

            return

        # Phase 2 - Assignment
        if self.portfolio[self.spy].quantity >= 100 and not self.has_shares:

            self.debug("Phase 2: Assignment")

            self.has_shares = True

            return

        # Phase 3 - Covered Call
        if self.has_shares and not self.call_sold:

            self.debug("Phase 3: Covered Call")

            self.call_sold = True
