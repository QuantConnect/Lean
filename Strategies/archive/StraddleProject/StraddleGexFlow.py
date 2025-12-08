# MUST BE THE VERY FIRST LINES — DO NOT MOVE
from AlgorithmImports import *
import requests
from datetime import timedelta

class StraddleGexFlow(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2024, 1, 1)
        self.SetEndDate(2025, 12, 31)
        self.SetCash(1_000_000)

        # ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
        # PUT YOUR ORATS KEY HERE
        self.orats_key = "7b2d42c6-3c27-461c-a148-9f8c4f95e7a0"
        # ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←

        self.symbols = ["NVDA", "TSLA", "AAPL"]
        self.equities = {}
        self.positions = {}
        self.vol_history = {s: [] for s in self.symbols}

        for s in self.symbols:
            equity = self.AddEquity(s, Resolution.Minute).Symbol
            self.AddOption(s, Resolution.Minute)
            self.equities[s] = equity

        self.Schedule.On(self.DateRules.EveryDay(), self.TimeRules.AfterMarketOpen("NVDA", 1), self.UpdateVolumeHistory)
        self.Schedule.On(self.DateRules.EveryDay(), self.TimeRules.Every(timedelta(minutes=15)), self.CheckSignals)

    def UpdateVolumeHistory(self):
        for s in self.symbols:
            # GetOptionContractList returns Symbols, need to get volume from Securities
            contracts = self.OptionChainProvider.GetOptionContractList(self.equities[s], self.Time)
            vol = 0
            for contract in contracts:
                if contract in self.Securities:
                    vol += self.Securities[contract].Volume
            self.vol_history[s].append(vol)
            if len(self.vol_history[s]) > 20:
                self.vol_history[s].pop(0)

    def CheckSignals(self):
        for s in self.symbols:
            spot = self.Securities[self.equities[s]].Price

            orats = self.GetOratsData(s, spot)
            if not orats or orats["ivr"] < 40 or abs(orats["gex"]) < 5_000_000:
                continue

            if len(self.vol_history[s]) < 10:
                continue
            avg_vol = sum(self.vol_history[s]) / len(self.vol_history[s])
            today_vol = self.vol_history[s][-1]
            if today_vol < avg_vol * 4:
                continue

            if s not in self.positions:
                # GetOptionContractList returns Symbol objects, access properties via Symbol.ID
                contracts = self.OptionChainProvider.GetOptionContractList(self.equities[s], self.Time)
                expiries = {c.ID.Date for c in contracts}
                valid = [e for e in expiries if 30 <= (e.date() - self.Time.date()).days <= 50]
                if not valid: continue
                expiry = min(valid, key=lambda e: abs((e.date() - self.Time.date()).days - 40))

                calls = [c for c in contracts if c.ID.OptionRight == OptionRight.Call and c.ID.Date == expiry]
                puts  = [c for c in contracts if c.ID.OptionRight == OptionRight.Put  and c.ID.Date == expiry]
                if not calls or not puts: continue

                call = min(calls, key=lambda c: abs(c.ID.StrikePrice - spot))
                put  = min(puts,  key=lambda c: abs(c.ID.StrikePrice - spot))

                # call and put are already Symbol objects
                self.AddOptionContract(call)
                self.AddOptionContract(put)
                self.Buy(call, 1)
                self.Buy(put, 1)
                self.positions[s] = {"call": call, "put": put}
                self.Log(f"ENTER {s} | IVR={orats['ivr']:.0f} | GEX=${orats['gex']/1e6:.1f}M | Vol×{today_vol/avg_vol:.1f}")

            else:
                # Calculate PnL from the actual option positions
                pos = self.positions[s]
                call_holding = self.Portfolio[pos["call"]]
                put_holding = self.Portfolio[pos["put"]]
                total_cost = call_holding.AveragePrice + put_holding.AveragePrice
                if total_cost > 0:
                    total_value = call_holding.Price + put_holding.Price
                    pnl = (total_value - total_cost) / total_cost
                else:
                    pnl = 0

                if pnl <= -0.03 or (orats["gex"] > 0 and pnl < 0.01):
                    # Liquidate the option contracts, not the equity
                    self.Liquidate(pos["call"])
                    self.Liquidate(pos["put"])
                    self.Log(f"EXIT {s} | PnL={pnl:.1%}")
                    del self.positions[s]

    def GetOratsData(self, symbol, spot):
        try:
            url = f"https://api.orats.com/v2/chain/{symbol}?apikey={self.orats_key}"
            r = requests.get(url, timeout=10)
            if r.status_code != 200:
                return None
            data = r.json()
            ivr = data.get("ivRank", 0)
            gex = 0
            for row in data.get("data", []):
                if abs(row.get("strike", 0) - spot) / spot > 0.10: continue
                gex += 100 * (row.get("oiCall",0) * row.get("gammaCall",0) + row.get("oiPut",0) * row.get("gammaPut",0)) * spot**2 / 100
            return {"ivr": ivr, "gex": gex}
        except Exception as e:
            self.Debug(str(e))
            return None