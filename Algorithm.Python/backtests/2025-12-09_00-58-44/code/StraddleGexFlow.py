from AlgorithmImports import *
from datetime import timedelta

class StraddleGexFlow(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2024, 1, 1)
        self.SetEndDate(2024, 12, 31)
        self.SetCash(1_000_000)

        self.symbols = ["NVDA", "TSLA", "AAPL"]
        self.equities = {}
        self.options = {}
        self.positions = {}  # {symbol: {"call": contract, "put": contract}}
        self.vol_history = {s: [] for s in self.symbols}
        self.iv_history = {s: [] for s in self.symbols}  # Track IV for IV Rank calc

        for s in self.symbols:
            equity = self.AddEquity(s, Resolution.Minute)
            self.equities[s] = equity.Symbol

            option = self.AddOption(s, Resolution.Minute)
            option.SetFilter(lambda u: u.Strikes(-10, 10).Expiration(30, 50))
            self.options[s] = option.Symbol

        self.Schedule.On(
            self.DateRules.EveryDay(),
            self.TimeRules.AfterMarketOpen("NVDA", 1),
            self.UpdateDailyMetrics
        )
        self.Schedule.On(
            self.DateRules.EveryDay(),
            self.TimeRules.AfterMarketOpen("NVDA", 30),
            self.CheckSignals
        )

    def UpdateDailyMetrics(self):
        """Update volume and IV history daily for all symbols."""
        for s in self.symbols:
            # Get option chain
            chain = self.CurrentSlice.OptionChains.get(self.options[s])
            if not chain:
                continue

            # Track total option volume
            total_vol = sum(c.Volume for c in chain)
            self.vol_history[s].append(total_vol)
            if len(self.vol_history[s]) > 20:
                self.vol_history[s].pop(0)

            # Track ATM IV for IV Rank calculation
            spot = self.Securities[self.equities[s]].Price
            atm_ivs = [c.ImpliedVolatility for c in chain
                       if abs(c.Strike - spot) / spot < 0.02 and c.ImpliedVolatility > 0]
            if atm_ivs:
                avg_iv = sum(atm_ivs) / len(atm_ivs)
                self.iv_history[s].append(avg_iv)
                if len(self.iv_history[s]) > 252:  # ~1 year of trading days
                    self.iv_history[s].pop(0)

    def CheckSignals(self):
        """Check entry/exit signals for each symbol."""
        for s in self.symbols:
            spot = self.Securities[self.equities[s]].Price
            if spot <= 0:
                continue

            chain = self.CurrentSlice.OptionChains.get(self.options[s])
            if not chain or len(list(chain)) == 0:
                continue

            # Calculate IV Rank (current IV percentile over past year)
            ivr = self.CalculateIVRank(s, chain, spot)

            # Calculate GEX from option chain
            gex = self.CalculateGEX(chain, spot)

            # Signal filters
            if ivr < 40 or abs(gex) < 5_000_000:
                continue

            # Volume spike filter (4x average)
            if len(self.vol_history[s]) < 10:
                continue
            avg_vol = sum(self.vol_history[s]) / len(self.vol_history[s])
            today_vol = self.vol_history[s][-1] if self.vol_history[s] else 0
            if avg_vol <= 0 or today_vol < avg_vol * 4:
                continue

            # ENTRY
            if s not in self.positions:
                call, put = self.GetATMStraddle(chain, spot)
                if call and put:
                    self.MarketOrder(call.Symbol, 1)
                    self.MarketOrder(put.Symbol, 1)
                    self.positions[s] = {"call": call.Symbol, "put": put.Symbol, "entry_time": self.Time}
                    self.Log(f"ENTER {s} | IVR={ivr:.0f} | GEX=${gex/1e6:.1f}M | Vol={today_vol/avg_vol:.1f}x")

            # EXIT
            else:
                pnl_pct = self.GetPositionPnL(s)
                if pnl_pct <= -0.03 or (gex > 0 and pnl_pct < 0.01):
                    self.LiquidatePosition(s)
                    self.Log(f"EXIT {s} | PnL={pnl_pct:.2%} | GEX=${gex/1e6:.1f}M")

    def CalculateIVRank(self, symbol, chain, spot):
        """Calculate IV Rank: where current IV sits in past year's range (0-100)."""
        # Get current ATM IV
        atm_ivs = [c.ImpliedVolatility for c in chain
                   if abs(c.Strike - spot) / spot < 0.05 and c.ImpliedVolatility > 0]
        if not atm_ivs:
            return 0
        current_iv = sum(atm_ivs) / len(atm_ivs)

        # Need at least 20 days of history
        if len(self.iv_history[symbol]) < 20:
            return 50  # Default to middle if not enough history

        iv_min = min(self.iv_history[symbol])
        iv_max = max(self.iv_history[symbol])
        if iv_max == iv_min:
            return 50

        ivr = (current_iv - iv_min) / (iv_max - iv_min) * 100
        return max(0, min(100, ivr))

    def CalculateGEX(self, chain, spot):
        """
        Calculate Gamma Exposure (GEX).
        GEX = Σ(OI × Gamma × 100 × Spot²) for calls (positive) and puts (negative)
        """
        gex = 0
        for contract in chain:
            if contract.Greeks is None or contract.Greeks.Gamma == 0:
                continue
            if abs(contract.Strike - spot) / spot > 0.10:
                continue

            gamma = contract.Greeks.Gamma
            oi = contract.OpenInterest

            # Calls add positive gamma, puts add negative gamma to dealers
            # (assuming dealers are short options to retail)
            contract_gex = oi * gamma * 100 * spot * spot / 100
            if contract.Right == OptionRight.Call:
                gex += contract_gex
            else:
                gex -= contract_gex

        return gex

    def GetATMStraddle(self, chain, spot):
        """Find ATM call and put with target expiry (~40 DTE)."""
        contracts = list(chain)
        if not contracts:
            return None, None

        # Find best expiry (closest to 40 DTE within 30-50 range)
        expiries = set(c.Expiry for c in contracts)
        target_dte = 40
        best_expiry = min(expiries, key=lambda e: abs((e.date() - self.Time.date()).days - target_dte))

        # Filter to this expiry
        exp_contracts = [c for c in contracts if c.Expiry == best_expiry]
        calls = [c for c in exp_contracts if c.Right == OptionRight.Call]
        puts = [c for c in exp_contracts if c.Right == OptionRight.Put]

        if not calls or not puts:
            return None, None

        # Find ATM
        atm_call = min(calls, key=lambda c: abs(c.Strike - spot))
        atm_put = min(puts, key=lambda c: abs(c.Strike - spot))

        return atm_call, atm_put

    def GetPositionPnL(self, symbol):
        """Calculate combined P&L percentage for the straddle position."""
        if symbol not in self.positions:
            return 0

        pos = self.positions[symbol]
        total_cost = 0
        total_value = 0

        for key in ["call", "put"]:
            contract_symbol = pos.get(key)
            if contract_symbol and contract_symbol in self.Portfolio:
                holding = self.Portfolio[contract_symbol]
                if holding.Invested:
                    total_cost += abs(holding.AveragePrice * holding.Quantity * 100)
                    total_value += abs(holding.Price * holding.Quantity * 100)

        if total_cost == 0:
            return 0
        return (total_value - total_cost) / total_cost

    def LiquidatePosition(self, symbol):
        """Liquidate both legs of the straddle."""
        if symbol not in self.positions:
            return

        pos = self.positions[symbol]
        for key in ["call", "put"]:
            contract_symbol = pos.get(key)
            if contract_symbol:
                self.Liquidate(contract_symbol)

        del self.positions[symbol]

    def OnData(self, data):
        """Handle any additional data events if needed."""
        pass
