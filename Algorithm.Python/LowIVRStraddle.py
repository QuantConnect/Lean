"""
Low IVR Straddle Strategy
=========================
Hypothesis: Buy straddles when IV is cheap, exit on profit or time.

Entry Rules:
- DTE: 45-70 days
- IVR: < 40 (buy cheap IV)
- Earnings: > 21 days away
- Bid-Ask Spread: < 4%

Exit Rules:
- +25% profit OR Day 20 (whichever first)

Position Sizing:
- $10,000 per trade
- 5-day cooldown per ticker
"""

from AlgorithmImports import *
from datetime import timedelta


class LowIVRStraddle(QCAlgorithm):

    def Initialize(self):
        # Backtest period
        self.SetStartDate(2024, 1, 1)
        self.SetEndDate(2024, 12, 31)
        self.SetCash(1_000_000)
        
        # Warmup to build IV history (need ~252 days for full year of IV data)
        self.SetWarmUp(timedelta(days=252))

        # Universe (31 tickers)
        self.symbols = [
            # Original 15
            "NVDA", "TSLA", "META", "AMD", "AAPL",
            "SMCI", "MSFT", "AMZN", "NFLX", "COIN",
            "GOOGL", "AVGO", "PLTR", "LULU", "ADBE",
            # Added 16 tech
            "MU", "CRWD", "ORCL", "DDOG", "TXN",
            "DELL", "GOOG", "CRM", "NOW", "LRCX",
            "PANW", "QCOM", "AMAT", "INTC", "IBM", "WDC"
        ]

        # Parameters
        self.position_size = 10000          # $10,000 per trade
        self.dte_min = 45                   # Minimum DTE
        self.dte_max = 70                   # Maximum DTE
        self.ivr_max = 40                   # Max IVR (we want LOW IV)
        self.max_spread_pct = 0.04          # Max 4% bid-ask spread
        self.earnings_buffer_days = 21      # No earnings within 21 days
        self.profit_target_pct = 0.25       # +25% profit target
        self.max_hold_days = 20             # Exit by day 20
        self.ticker_cooldown_days = 5       # 5 days between same ticker

        # State tracking
        self.equities = {}
        self.options = {}
        self.positions = {}  # {symbol: {call, put, entry_date, entry_cost}}
        self.last_entry = {}  # {symbol: last_entry_date}
        self.iv_history = {s: [] for s in self.symbols}

        # Add equities and options
        for s in self.symbols:
            equity = self.AddEquity(s, Resolution.Minute)
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw)
            self.equities[s] = equity.Symbol

            option = self.AddOption(s, Resolution.Minute)
            option.SetFilter(lambda u: u.Strikes(-5, 5).Expiration(self.dte_min, self.dte_max))
            self.options[s] = option.Symbol

        # Schedule daily checks
        self.Schedule.On(
            self.DateRules.EveryDay(),
            self.TimeRules.AfterMarketOpen(self.symbols[0], 30),
            self.DailyUpdate
        )

    def DailyUpdate(self):
        """Daily check for entries and exits."""
        self.CheckExits()
        self.CheckEntries()

    def CheckExits(self):
        """Check if any positions should be closed."""
        to_close = []

        for symbol, pos in self.positions.items():
            days_held = (self.Time.date() - pos["entry_date"].date()).days
            current_value = self.GetPositionValue(symbol)
            entry_cost = pos["entry_cost"]

            if entry_cost > 0:
                pnl_pct = (current_value - entry_cost) / entry_cost
            else:
                pnl_pct = 0

            exit_reason = None

            # Exit Rule 1: +25% profit
            if pnl_pct >= self.profit_target_pct:
                exit_reason = "PROFIT_TARGET"

            # Exit Rule 2: Day 20 time stop
            elif days_held >= self.max_hold_days:
                exit_reason = "TIME_STOP"

            if exit_reason:
                to_close.append((symbol, exit_reason, pnl_pct, days_held))

        # Close positions
        for symbol, reason, pnl_pct, days_held in to_close:
            self.ClosePosition(symbol, reason, pnl_pct, days_held)

    def CheckEntries(self):
        """Check for new entry opportunities."""
        for symbol in self.symbols:
            # Skip if already have position
            if symbol in self.positions:
                continue

            # Skip if in cooldown
            if symbol in self.last_entry:
                days_since = (self.Time.date() - self.last_entry[symbol].date()).days
                if days_since < self.ticker_cooldown_days:
                    continue

            # Get option chain
            chain = self.CurrentSlice.OptionChains.get(self.options[symbol])
            if not chain:
                continue

            # Get underlying price
            if self.equities[symbol] not in self.Securities:
                continue
            spot = self.Securities[self.equities[symbol]].Price
            if spot <= 0:
                continue

            # Calculate IVR
            ivr = self.CalculateIVR(symbol, chain, spot)
            if ivr is None or ivr >= self.ivr_max:
                continue

            # Check earnings
            if self.HasEarningsSoon(symbol):
                continue

            # Find best straddle
            straddle = self.FindBestStraddle(chain, spot)
            if straddle is None:
                continue

            call, put = straddle

            # Check bid-ask spread
            if not self.CheckSpread(call, put):
                continue

            # Enter position
            self.EnterPosition(symbol, call, put, ivr)

    def CalculateIVR(self, symbol, chain, spot):
        """Calculate IV Rank (0-100)."""
        # Get current ATM IV
        atm_contracts = [c for c in chain if abs(c.Strike - spot) / spot < 0.05 and c.ImpliedVolatility > 0]
        if not atm_contracts:
            return None

        current_iv = sum(c.ImpliedVolatility for c in atm_contracts) / len(atm_contracts)

        # Update history
        self.iv_history[symbol].append(current_iv)
        if len(self.iv_history[symbol]) > 252:
            self.iv_history[symbol].pop(0)

        # Need history for IVR
        if len(self.iv_history[symbol]) < 20:
            return 50  # Default to middle

        iv_min = min(self.iv_history[symbol])
        iv_max = max(self.iv_history[symbol])

        if iv_max == iv_min:
            return 50

        ivr = (current_iv - iv_min) / (iv_max - iv_min) * 100
        return max(0, min(100, ivr))

    def HasEarningsSoon(self, symbol):
        """Check if earnings are within buffer period."""
        # Note: QuantConnect doesn't have built-in earnings calendar in backtesting
        # For now, we'll skip this check - in live trading, integrate with external data
        # TODO: Integrate with earnings calendar (UW data or similar)
        return False

    def FindBestStraddle(self, chain, spot):
        """Find ATM straddle within DTE range."""
        contracts = list(chain)
        if not contracts:
            return None

        # Filter to target DTE range
        valid_expiries = set()
        for c in contracts:
            dte = (c.Expiry.date() - self.Time.date()).days
            if self.dte_min <= dte <= self.dte_max:
                valid_expiries.add(c.Expiry)

        if not valid_expiries:
            return None

        # Pick expiry closest to middle of range (e.g., ~57 DTE)
        target_dte = (self.dte_min + self.dte_max) / 2
        best_expiry = min(valid_expiries, key=lambda e: abs((e.date() - self.Time.date()).days - target_dte))

        # Filter to this expiry
        exp_contracts = [c for c in contracts if c.Expiry == best_expiry]
        calls = [c for c in exp_contracts if c.Right == OptionRight.Call]
        puts = [c for c in exp_contracts if c.Right == OptionRight.Put]

        if not calls or not puts:
            return None

        # Find ATM strike
        strikes = set(c.Strike for c in calls) & set(c.Strike for c in puts)
        if not strikes:
            return None

        atm_strike = min(strikes, key=lambda s: abs(s - spot))

        # Get the contracts
        call = next((c for c in calls if c.Strike == atm_strike), None)
        put = next((c for c in puts if c.Strike == atm_strike), None)

        if call is None or put is None:
            return None

        return (call, put)

    def CheckSpread(self, call, put):
        """Check if bid-ask spread is acceptable."""
        for contract in [call, put]:
            if contract.BidPrice <= 0 or contract.AskPrice <= 0:
                return False
            spread = (contract.AskPrice - contract.BidPrice) / contract.AskPrice
            if spread > self.max_spread_pct:
                return False
        return True

    def EnterPosition(self, symbol, call, put, ivr):
        """Enter a straddle position."""
        # Calculate position size
        straddle_price = (call.AskPrice + put.AskPrice)  # Pay the ask
        if straddle_price <= 0:
            return

        contracts = int(self.position_size / (straddle_price * 100))
        if contracts < 1:
            contracts = 1

        # Place orders
        self.MarketOrder(call.Symbol, contracts)
        self.MarketOrder(put.Symbol, contracts)

        # Track position
        entry_cost = straddle_price * 100 * contracts
        self.positions[symbol] = {
            "call": call.Symbol,
            "put": put.Symbol,
            "entry_date": self.Time,
            "entry_cost": entry_cost,
            "entry_ivr": ivr,
            "contracts": contracts,
            "strike": call.Strike,
            "expiry": call.Expiry
        }
        self.last_entry[symbol] = self.Time

        dte = (call.Expiry.date() - self.Time.date()).days
        self.Log(f"ENTRY {symbol} | Strike={call.Strike} | DTE={dte} | IVR={ivr:.0f} | Cost=${entry_cost:.0f} | Contracts={contracts}")

    def GetPositionValue(self, symbol):
        """Get current value of a position."""
        if symbol not in self.positions:
            return 0

        pos = self.positions[symbol]
        total = 0

        for key in ["call", "put"]:
            opt_symbol = pos.get(key)
            if opt_symbol and opt_symbol in self.Securities:
                security = self.Securities[opt_symbol]
                # Use mid price for valuation
                if security.BidPrice > 0 and security.AskPrice > 0:
                    mid = (security.BidPrice + security.AskPrice) / 2
                else:
                    mid = security.Price
                total += mid * 100 * pos["contracts"]

        return total

    def ClosePosition(self, symbol, reason, pnl_pct, days_held):
        """Close a straddle position."""
        if symbol not in self.positions:
            return

        pos = self.positions[symbol]

        # Liquidate
        self.Liquidate(pos["call"])
        self.Liquidate(pos["put"])

        self.Log(f"EXIT {symbol} | Reason={reason} | PnL={pnl_pct:.1%} | Days={days_held} | IVR@Entry={pos['entry_ivr']:.0f}")

        del self.positions[symbol]

    def OnData(self, data):
        """Handle data events."""
        pass

    def OnEndOfAlgorithm(self):
        """Summary at end of backtest."""
        self.Log("="*50)
        self.Log("BACKTEST COMPLETE")
        self.Log(f"Final Portfolio Value: ${self.Portfolio.TotalPortfolioValue:,.0f}")
        self.Log("="*50)
