"""
Low IVR Straddle Strategy - LIFECYCLE ANALYSIS
==============================================
Full daily tracking to measure TRUE capture rate and test exit signals.

Entry Rules:
- DTE: 45-70 days
- IVR: < 40 (buy cheap IV)
- Earnings: > 21 days away
- Bid-Ask Spread: < 4%

Exit Rules (Momentum Exit + Day 30):
- +25% profit threshold, then exit on 4% drop from peak
- Day 30 time stop fallback

Daily Tracking:
- P&L and Peak P&L
- Straddle IV and IV ratio (vega guard signal)
- RSI(14) and ADX(14) on underlying
- Greeks (delta, gamma, theta, vega)
- Signal flags (vega guard, RSI overbought, delta drift)

Position Sizing:
- $10,000 per trade
- 5-day cooldown per ticker
"""

from AlgorithmImports import *
from datetime import timedelta
import json


class LowIVRStraddle(QCAlgorithm):

    def Initialize(self):
        # Backtest period
        self.SetStartDate(2024, 1, 1)
        self.SetEndDate(2024, 12, 31)
        self.SetCash(1_000_000)
        
        # Warmup to build IV history (need ~252 days for full year of IV data)
        self.SetWarmUp(timedelta(days=252))

        # Universe (30 tickers for lifecycle analysis)
        self.symbols = [
            # Original 15
            "NVDA", "TSLA", "META", "AMD", "AAPL",
            "SMCI", "MSFT", "AMZN", "NFLX", "COIN",
            "GOOGL", "AVGO", "PLTR", "LULU", "ADBE",
            # Added 15 tech (top performers from earlier tests)
            "MU", "CRWD", "DDOG", "TXN", "DELL",
            "CRM", "NOW", "LRCX", "PANW", "QCOM",
            "AMAT", "IBM", "WDC", "GOOG", "ORCL"
        ]

        # Parameters
        self.position_size = 10000          # $10,000 per trade
        self.dte_min = 45                   # Minimum DTE
        self.dte_max = 70                   # Maximum DTE
        self.ivr_max = 40                   # Max IVR (we want LOW IV)
        self.max_spread_pct = 0.04          # Max 4% bid-ask spread
        self.earnings_buffer_days = 21      # No earnings within 21 days
        self.ticker_cooldown_days = 5       # 5 days between same ticker

        # Exit Parameters (Momentum Exit + Day 30)
        self.protection_threshold = 0.25    # Enter protection mode at +25%
        self.drop_from_peak_exit = 0.04     # Exit if drops 4% from peak
        self.max_hold_days = 30             # Time stop fallback (Day 30)

        # Lifecycle tracking counters
        self.total_trades = 0
        self.total_capture_rate = 0
        self.vega_guard_exits = 0
        self.rsi_exits = 0
        self.momentum_exits = 0
        self.time_stop_exits = 0

        # State tracking
        self.equities = {}
        self.options = {}
        self.positions = {}  # Enhanced with lifecycle tracking
        self.last_entry = {}  # {symbol: last_entry_date}
        self.iv_history = {s: [] for s in self.symbols}

        # Technical indicators
        self.rsi = {}
        self.adx = {}

        # Add equities, options, and indicators
        for s in self.symbols:
            equity = self.AddEquity(s, Resolution.Minute)
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw)
            self.equities[s] = equity.Symbol

            option = self.AddOption(s, Resolution.Minute)
            option.SetFilter(lambda u: u.Strikes(-5, 5).Expiration(self.dte_min, self.dte_max))
            self.options[s] = option.Symbol

            # RSI(14) on underlying price
            self.rsi[s] = self.RSI(self.equities[s], 14, MovingAverageType.Wilders, Resolution.Daily)

            # ADX(14) on underlying price
            self.adx[s] = self.ADX(self.equities[s], 14, Resolution.Daily)

        # Schedule entry checks (30 min after open)
        self.Schedule.On(
            self.DateRules.EveryDay(),
            self.TimeRules.AfterMarketOpen(self.symbols[0], 30),
            self.CheckEntries
        )

        # Schedule daily metrics logging and exit checks at 3:45 PM ET
        self.Schedule.On(
            self.DateRules.EveryDay(),
            self.TimeRules.BeforeMarketClose(self.symbols[0], 15),
            self.DailyUpdate
        )

    def DailyUpdate(self):
        """Daily logging and exit checks at 3:45 PM ET."""
        self.LogDailyMetrics()
        self.CheckExits()

    def LogDailyMetrics(self):
        """Log comprehensive daily metrics for each open position."""
        for symbol, pos in self.positions.items():
            # P&L calculation
            current_value = self.GetPositionValue(symbol)
            entry_cost = pos["entry_cost"]
            pnl_pct = (current_value - entry_cost) / entry_cost if entry_cost > 0 else 0

            # Update peak if new high
            if pnl_pct > pos.get("peak_pnl_pct", 0):
                pos["peak_pnl_pct"] = pnl_pct
                pos["peak_pnl_date"] = self.Time

            # Get current straddle IV
            call_sec = self.Securities.get(pos["call"])
            put_sec = self.Securities.get(pos["put"])
            current_iv = 0
            if call_sec and put_sec:
                call_iv = getattr(call_sec, 'ImpliedVolatility', 0) or 0
                put_iv = getattr(put_sec, 'ImpliedVolatility', 0) or 0
                current_iv = (call_iv + put_iv) / 2 if (call_iv + put_iv) > 0 else 0

            # IV ratio (vega guard signal)
            entry_iv = pos.get("entry_iv", 0)
            iv_ratio = current_iv / entry_iv if entry_iv > 0 else 1

            # Update peak IV
            if current_iv > pos.get("peak_iv", 0):
                pos["peak_iv"] = current_iv

            # RSI and ADX from indicators
            rsi_val = self.rsi[symbol].Current.Value if self.rsi[symbol].IsReady else 50
            adx_val = self.adx[symbol].Current.Value if self.adx[symbol].IsReady else 25

            # Greeks from option contracts
            delta = gamma = theta = vega = 0
            if call_sec and put_sec:
                if hasattr(call_sec, 'Greeks') and call_sec.Greeks:
                    delta += call_sec.Greeks.Delta
                    gamma += call_sec.Greeks.Gamma
                    theta += call_sec.Greeks.Theta
                    vega += call_sec.Greeks.Vega
                if hasattr(put_sec, 'Greeks') and put_sec.Greeks:
                    delta += put_sec.Greeks.Delta
                    gamma += put_sec.Greeks.Gamma
                    theta += put_sec.Greeks.Theta
                    vega += put_sec.Greeks.Vega

            # Delta drift tracking
            if abs(delta) > 0.65:
                pos["delta_drift_days"] = pos.get("delta_drift_days", 0) + 1
            else:
                pos["delta_drift_days"] = 0

            # Days held
            days_held = (self.Time.date() - pos["entry_date"].date()).days

            # Build snapshot dict
            snapshot = {
                "date": self.Time.strftime("%Y-%m-%d"),
                "days_held": days_held,
                "pnl_pct": round(pnl_pct * 100, 2),
                "peak_pnl_pct": round(pos.get("peak_pnl_pct", 0) * 100, 2),
                "current_iv": round(current_iv, 4),
                "entry_iv": round(entry_iv, 4),
                "iv_ratio": round(iv_ratio, 3),
                "rsi": round(rsi_val, 1),
                "adx": round(adx_val, 1),
                "delta": round(delta, 3),
                "gamma": round(gamma, 4),
                "theta": round(theta, 2),
                "vega": round(vega, 2),
                "vega_guard": iv_ratio < 0.78,
                "rsi_ob": rsi_val >= 70,
                "delta_drift": pos.get("delta_drift_days", 0) >= 2,
            }

            # Store snapshot
            if "daily_snapshots" not in pos:
                pos["daily_snapshots"] = []
            pos["daily_snapshots"].append(snapshot)

            # Log for parsing later
            self.Log(f"SNAPSHOT {symbol} | {json.dumps(snapshot)}")

    def CheckExits(self):
        """Check exit conditions: momentum exit, time stop, or expiration."""
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

            # Check if we should enter profit protection mode
            if not pos.get("in_protection_mode", False):
                if pnl_pct >= self.protection_threshold:
                    pos["in_protection_mode"] = True
                    pos["peak_pnl_pct"] = pnl_pct
                    pos["peak_pnl_date"] = self.Time
                    self.Log(f"PROTECTION_MODE {symbol} | PnL={pnl_pct:.1%} | Peak set")

            # If in protection mode, check for momentum exit
            if pos.get("in_protection_mode", False):
                peak = pos.get("peak_pnl_pct", 0)
                drop_from_peak = peak - pnl_pct

                if drop_from_peak >= self.drop_from_peak_exit:
                    exit_reason = "MOMENTUM_EXIT"

            # Time stop fallback
            if exit_reason is None and days_held >= self.max_hold_days:
                exit_reason = "TIME_STOP"

            # Expiration safety net
            days_to_expiry = (pos["expiry"].date() - self.Time.date()).days
            if exit_reason is None and days_to_expiry <= 1:
                exit_reason = "EXPIRATION"

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

        # Track position with lifecycle fields
        entry_cost = straddle_price * 100 * contracts
        entry_iv = (call.ImpliedVolatility + put.ImpliedVolatility) / 2 if (call.ImpliedVolatility + put.ImpliedVolatility) > 0 else 0

        self.positions[symbol] = {
            "call": call.Symbol,
            "put": put.Symbol,
            "entry_date": self.Time,
            "entry_cost": entry_cost,
            "entry_ivr": ivr,
            "contracts": contracts,
            "strike": call.Strike,
            "expiry": call.Expiry,
            # Lifecycle tracking fields
            "entry_iv": entry_iv,
            "peak_pnl_pct": 0,
            "peak_pnl_date": self.Time,
            "peak_iv": entry_iv,
            "in_protection_mode": False,
            "delta_drift_days": 0,
            "daily_snapshots": [],
        }
        self.last_entry[symbol] = self.Time

        dte = (call.Expiry.date() - self.Time.date()).days
        self.Log(f"ENTRY {symbol} | Strike={call.Strike} | DTE={dte} | IVR={ivr:.0f} | IV={entry_iv:.3f} | Cost=${entry_cost:.0f} | Contracts={contracts}")

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
        """Close a straddle position with comprehensive exit logging."""
        if symbol not in self.positions:
            return

        pos = self.positions[symbol]

        # Calculate capture rate
        peak_pnl = pos.get("peak_pnl_pct", 0)
        if peak_pnl > 0:
            capture_rate = (pnl_pct / peak_pnl) * 100
        else:
            capture_rate = 100 if pnl_pct <= 0 else 0

        peak_date = pos.get("peak_pnl_date", pos["entry_date"])
        days_from_peak = (self.Time.date() - peak_date.date()).days

        # Update counters
        self.total_trades += 1
        self.total_capture_rate += capture_rate
        if reason == "MOMENTUM_EXIT":
            self.momentum_exits += 1
        elif reason == "TIME_STOP":
            self.time_stop_exits += 1

        # Liquidate
        self.Liquidate(pos["call"])
        self.Liquidate(pos["put"])

        # Comprehensive exit summary log
        self.Log(f"EXIT_SUMMARY {symbol} | "
                 f"Entry={pos['entry_date'].strftime('%Y-%m-%d')} | "
                 f"Exit={self.Time.strftime('%Y-%m-%d')} | "
                 f"Days={days_held} | "
                 f"PeakPnL={peak_pnl:.1%} | "
                 f"PeakDate={peak_date.strftime('%Y-%m-%d')} | "
                 f"ExitPnL={pnl_pct:.1%} | "
                 f"CaptureRate={capture_rate:.0f}% | "
                 f"DaysFromPeak={days_from_peak} | "
                 f"Reason={reason} | "
                 f"IVR@Entry={pos['entry_ivr']:.0f}")

        del self.positions[symbol]

    def OnData(self, data):
        """Handle data events."""
        pass

    def OnEndOfAlgorithm(self):
        """Summary statistics at end of backtest."""
        # Calculate averages
        avg_capture = self.total_capture_rate / self.total_trades if self.total_trades > 0 else 0

        # Runtime statistics (show in QC dashboard summary)
        self.SetRuntimeStatistic("Total Trades", str(self.total_trades))
        self.SetRuntimeStatistic("Avg Capture Rate", f"{avg_capture:.0f}%")
        self.SetRuntimeStatistic("Momentum Exits", str(self.momentum_exits))
        self.SetRuntimeStatistic("Time Stop Exits", str(self.time_stop_exits))

        # Log summary
        self.Log("="*60)
        self.Log("BACKTEST SUMMARY")
        self.Log("="*60)
        self.Log(f"Final Portfolio Value: ${self.Portfolio.TotalPortfolioValue:,.0f}")
        self.Log(f"Total Trades: {self.total_trades}")
        self.Log(f"Avg Capture Rate: {avg_capture:.0f}%")
        self.Log(f"Momentum Exits: {self.momentum_exits}")
        self.Log(f"Time Stop Exits: {self.time_stop_exits}")
        self.Log("="*60)
